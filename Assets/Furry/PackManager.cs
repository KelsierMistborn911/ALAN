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
    [SerializeField] private int maxHarassers = 3;
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
            // Если это постоянная альфа — сразу назначаем
            if (member.IsPermanentAlpha)
            {
                alpha = member;
                roles[member] = TacticalRole.Alpha;
            }
            else
            {
                roles[member] = TacticalRole.Reserve;
            }
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

        // 1. Ищем постоянную альфу (isPermanentAlpha = true)
        alpha = allMembers.FirstOrDefault(m => m != null && !m.IsDead && m.IsPermanentAlpha);

        if (alpha != null)
        {
            roles[alpha] = TacticalRole.Alpha;
        }

        // 2. Формируем пул из всех живых, кроме альфы
        List<PackMember> pool = allMembers
            .Where(m => m != null && !m.IsDead && m != alpha)
            .OrderBy(m => Vector2.Distance(m.transform.position, player.position))
            .ToList();

        // 3. Harasser'ы — самые ближние к игроку (всегда 3, если хватает)
        int harassersToAssign = Mathf.Min(maxHarassers, pool.Count);
        for (int i = 0; i < harassersToAssign; i++)
        {
            var harasser = pool[0]; // Ближайший
            roles[harasser] = TacticalRole.Harasser;
            pool.RemoveAt(0);
        }

        // 4. Все остальные — фланкеры (окружают и защищают альфу)
        int halfIndex = pool.Count / 2;
        for (int i = 0; i < pool.Count; i++)
        {
            if (i < halfIndex)
                roles[pool[i]] = TacticalRole.FlankerLeft;
            else
                roles[pool[i]] = TacticalRole.FlankerRight;
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
                // Альфа постоянная — просто сообщаем формации
                formation?.OnMemberDied(member);
                break;
            case TacticalRole.Harasser:
                ReplaceHarasser();
                formation?.OnMemberDied(member);
                break;
            case TacticalRole.FlankerLeft:
            case TacticalRole.FlankerRight:
                // Фланкер умер — просто перераспределяем углы окружения
                formation?.OnMemberDied(member);
                break;
            case TacticalRole.Reserve:
                formation?.OnMemberDied(member);
                break;
        }
    }

    private void ReplaceHarasser()
    {
        // Ближайший живой фланкер становится харассером
        PackMember replacement = allMembers
            .Where(m => m != null && !m.IsDead && roles.ContainsKey(m)
                && (roles[m] == TacticalRole.FlankerLeft || roles[m] == TacticalRole.FlankerRight))
            .OrderBy(m => Vector2.Distance(m.transform.position, player.position))
            .FirstOrDefault();

        if (replacement != null)
        {
            TacticalRole oldSide = roles[replacement];
            roles[replacement] = TacticalRole.Harasser;

            // ВОТ ЧЕГО НЕ ХВАТАЛО — переключить поведение
            replacement.SetTacticalRole(TacticalRole.Harasser);

            Debug.Log($"{replacement.name}: фланкер ({oldSide}) → харассер (замена убитого)");
        }
    }

    // ===== Публичные методы =====
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
