using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Управление стаей: активация, роли, ротация при потерях.
/// Не занимается расчётом позиций — это PackFormation.
/// </summary>
public class PackManager : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Transform player;
    [SerializeField] private PackFormation formation;

    [Header("Состав ролей")]
    [SerializeField] private int maxHarassers = 2;
    [SerializeField] private int maxFlankersPerSide = 2;

    [Header("Отладка")]
    [SerializeField] private bool showDebugGUI = true;

    // ===== Состояние =====
    private List<PackMember> allMembers = new();
    private Dictionary<PackMember, TacticalRole> roles = new();
    private PackMember alpha;

    private bool isPackActive = false;

    public enum HealthStatus
    {
        Healthy,
        Wounded,
        Dead
    }

    // ===== Типы =====
    public enum TacticalRole
    {
        Unassigned,
        Alpha,
        FlankerLeft,
        FlankerRight,
        Harasser,
        Reserve
    }

    // ===== Публичные свойства =====
    public bool IsPackActive => isPackActive;
    public PackMember Alpha => alpha;
    public Transform Player => player;

    // ===== Инициализация =====
    private void Start()
    {
        if (formation == null)
            formation = GetComponent<PackFormation>();
    }

    // Тестовое управление
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) ActivatePack();
    }

    // ===== Регистрация =====
    public void RegisterMember(PackMember member)
    {
        if (allMembers.Contains(member)) return;

        allMembers.Add(member);
        roles[member] = TacticalRole.Unassigned;

        member.OnDied += HandleMemberDied;

        if (player != null)
            member.SetPlayer(player);

        // Если стая активна — сразу назначаем Reserve
        if (isPackActive)
        {
            roles[member] = TacticalRole.Reserve;
            formation?.OnMemberAdded(member);
        }
    }

    public void UnregisterMember(PackMember member)
    {
        allMembers.Remove(member);
        roles.Remove(member);
        member.OnDied -= HandleMemberDied;
    }

    // ===== Активация =====
    public void ActivatePack()
    {
        if (isPackActive || allMembers.Count == 0) return;
        isPackActive = true;

        AssignAllRoles();
        formation?.OnPackActivated();
    }

    private void AssignAllRoles()
    {
        if (allMembers.Count == 0) return;

        List<PackMember> pool = allMembers
            .Where(m => m != null && !m.IsDead)
            .OrderByDescending(m => Vector2.Distance(m.transform.position, player.position))
            .ToList();

        // 1. Alpha — самый дальний
        alpha = pool[0];
        roles[alpha] = TacticalRole.Alpha;
        pool.RemoveAt(0);

        // 2. Harasser'ы — самые ближние к игроку
        int harassersToAssign = Mathf.Min(maxHarassers, pool.Count);
        for (int i = 0; i < harassersToAssign; i++)
        {
            // Берём ближайших — они в конце списка
            var harasser = pool[pool.Count - 1];
            roles[harasser] = TacticalRole.Harasser;
            pool.RemoveAt(pool.Count - 1);
        }

        // 3. Фланкеры — оставшиеся
        int flankersTotal = Mathf.Min(maxFlankersPerSide * 2, pool.Count);
        int flankersPerSide = flankersTotal / 2;

        for (int i = 0; i < flankersPerSide && pool.Count > 0; i++)
        {
            roles[pool[0]] = TacticalRole.FlankerLeft;
            pool.RemoveAt(0);
        }
        for (int i = 0; i < flankersPerSide && pool.Count > 0; i++)
        {
            roles[pool[0]] = TacticalRole.FlankerRight;
            pool.RemoveAt(0);
        }

        // 4. Остальные — Reserve
        foreach (var member in pool)
        {
            roles[member] = TacticalRole.Reserve;
        }
    }

    // ===== Обработка смерти и ротация =====
    private void HandleMemberDied(PackMember member)
    {
        if (!roles.ContainsKey(member)) return;

        TacticalRole deadRole = roles[member];
        allMembers.Remove(member);
        roles.Remove(member);

        // Все погибли
        if (allMembers.Count == 0)
        {
            alpha = null;
            formation?.OnPackWiped();
            return;
        }

        // Ротация
        switch (deadRole)
        {
            case TacticalRole.Alpha:
                PromoteNewAlpha();
                break;
            case TacticalRole.Harasser:
                FillRoleFromReserve(TacticalRole.Harasser);
                break;
            case TacticalRole.FlankerLeft:
            case TacticalRole.FlankerRight:
                FillRoleFromReserve(deadRole);
                break;
            case TacticalRole.Reserve:
                // Просто уменьшилось кольцо
                break;
        }

        formation?.OnMemberDied(member);
    }

    private void PromoteNewAlpha()
    {
        // Новый Alpha — ближайший к игроку (он уже в бою)
        PackMember newAlpha = allMembers
            .Where(m => m != null && !m.IsDead)
            .OrderBy(m => Vector2.Distance(m.transform.position, player.position))
            .FirstOrDefault();

        if (newAlpha == null) return;

        TacticalRole oldRole = roles[newAlpha];
        roles[newAlpha] = TacticalRole.Alpha;
        alpha = newAlpha;

        // Заполнить освободившуюся роль
        FillRoleFromReserve(oldRole);
        formation?.OnAlphaChanged(newAlpha);
    }

    private void FillRoleFromReserve(TacticalRole targetRole)
    {
        // Приоритет: Reserve → Flanker → другой Flanker → Alpha (последний)
        PackMember replacement = allMembers
            .Where(m => m != null && !m.IsDead && roles.ContainsKey(m) && roles[m] == TacticalRole.Reserve)
            .FirstOrDefault();

        if (replacement == null)
        {
            replacement = allMembers
                .Where(m => m != null && !m.IsDead && roles.ContainsKey(m)
                    && (roles[m] == TacticalRole.FlankerLeft || roles[m] == TacticalRole.FlankerRight))
                .FirstOrDefault();
        }

        if (replacement == null)
        {
            replacement = allMembers
                .Where(m => m != null && !m.IsDead && roles.ContainsKey(m)
                    && roles[m] != TacticalRole.Alpha)
                .FirstOrDefault();
        }

        if (replacement != null)
        {
            roles[replacement] = targetRole;
        }
    }

    // ===== Публичные методы для PackFormation =====
    public List<PackMember> GetMembersByRole(TacticalRole role)
    {
        return allMembers
            .Where(m => m != null && !m.IsDead && roles.ContainsKey(m) && roles[m] == role)
            .ToList();
    }

    public Dictionary<PackMember, TacticalRole> GetAllRoles()
    {
        return new Dictionary<PackMember, TacticalRole>(roles);
    }

    public List<PackMember> GetAllMembers()
    {
        return new List<PackMember>(allMembers.Where(m => m != null && !m.IsDead));
    }

    public int GetAliveCount()
    {
        return allMembers.Count(m => m != null && !m.IsDead);
    }

    // ===== Отладка =====
    private void OnGUI()
    {
        if (!showDebugGUI || !isPackActive) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.Label($"Стая: {GetAliveCount()} живых");
        GUILayout.Label($"Alpha: {(alpha != null ? alpha.name : "нет")}");
        GUILayout.Space(10);

        foreach (var kvp in roles)
        {
            if (kvp.Key != null)
                GUILayout.Label($"  {kvp.Key.name}: {kvp.Value}");
        }
        GUILayout.EndArea();
    }
}
