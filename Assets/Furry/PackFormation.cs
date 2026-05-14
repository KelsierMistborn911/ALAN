using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Расчёт секторов вокруг игрока относительно Alpha.
/// Обновляется при смещении Alpha или изменениях в составе.
/// </summary>
public class PackFormation : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PackManager packManager;

    [Header("Радиусы")]
    [SerializeField] private float alphaRadius = 8f;
    [SerializeField] private float waitingRadius = 5f;
    [SerializeField] private float attackingRadius = 2.5f;
    [SerializeField] private float radiusVariation = 0.3f; // ±30%

    [Header("Перестроение")]
    [SerializeField] private float sectorRecalcThreshold = 2f;
    [SerializeField] private float sectorRecalcCooldown = 2f;

    [Header("Потенциальные поля — веса по ролям")]
    [SerializeField] private WeightSet alphaWeights = new(0.9f, 0.3f, 0.1f, 0.2f);
    [SerializeField] private WeightSet flankerWeights = new(0.4f, 0.6f, 0.7f, 0.3f);
    [SerializeField] private WeightSet harasserWeights = new(0.1f, 0.4f, 0.9f, 0.2f);
    [SerializeField] private WeightSet reserveWeights = new(0.5f, 0.6f, 0.5f, 0.3f);

    [Header("Отладка")]
    [SerializeField] private bool showDebugGizmos = true;

    // ===== Состояние =====
    private Dictionary<PackMember, Sector> sectors = new();
    private Vector2 lastAlphaPosition;
    private float lastRecalcTime = -999f;

    // ===== Типы =====
    [System.Serializable]
    public struct WeightSet
    {
        public float awayFromPlayer;
        public float awayFromAllies;
        public float towardSector;
        public float damping;

        public WeightSet(float fromPlayer, float fromAllies, float toSector, float damp)
        {
            awayFromPlayer = fromPlayer;
            awayFromAllies = fromAllies;
            towardSector = toSector;
            damping = damp;
        }
    }

    public struct Sector
    {
        public float minAngle;  // градусы, мировые
        public float maxAngle;
        public float minRadius;
        public float maxRadius;

        public Sector(float minAng, float maxAng, float minRad, float maxRad)
        {
            minAngle = minAng;
            maxAngle = maxAng;
            minRadius = minRad;
            maxRadius = maxRad;
        }
    }

    // ===== Инициализация =====
    private void Start()
    {
        if (packManager == null)
            packManager = GetComponent<PackManager>();
    }

    private void Update()
    {
        if (!packManager.IsPackActive || packManager.Alpha == null || packManager.Player == null)
            return;

        CheckAlphaShift();
    }

    // ===== Публичные методы (вызываются из PackManager) =====
    public void OnPackActivated()
    {
        CalculateAllSectors();
        ApplyToAllMembers();
    }

    public void OnMemberAdded(PackMember member)
    {
        CalculateAllSectors();
        ApplyToAllMembers();
    }

    public void OnMemberDied(PackMember member)
    {
        sectors.Remove(member);
        CalculateAllSectors();
        ApplyToAllMembers();
    }

    public void OnAlphaChanged(PackMember newAlpha)
    {
        CalculateAllSectors();
        ApplyToAllMembers();
    }

    public void OnPackWiped()
    {
        sectors.Clear();
    }

    // ===== Проверка смещения Alpha =====
    private void CheckAlphaShift()
    {
        Vector2 currentAlphaPos = packManager.Alpha.transform.position;
        float shift = Vector2.Distance(currentAlphaPos, lastAlphaPosition);

        if (shift >= sectorRecalcThreshold && Time.time - lastRecalcTime >= sectorRecalcCooldown)
        {
            CalculateAllSectors();
            ApplyToAllMembers();
        }
    }

    // ===== Расчёт секторов =====
    private void CalculateAllSectors()
    {
        sectors.Clear();

        PackMember alpha = packManager.Alpha;
        Transform player = packManager.Player;
        if (alpha == null || player == null) return;

        Vector2 playerPos = player.position;
        Vector2 alphaPos = alpha.transform.position;

        // Ось: Alpha → Игрок
        float axisAngle = Mathf.Atan2(alphaPos.y - playerPos.y, alphaPos.x - playerPos.x) * Mathf.Rad2Deg;
        // Это угол ОТ игрока К Alpha. Инвертируем для направления Alpha→Игрок:
        float attackAxis = axisAngle + 180f; // Направление от Alpha на игрока

        List<PackMember> flankersLeft = packManager.GetMembersByRole(PackManager.TacticalRole.FlankerLeft);
        List<PackMember> flankersRight = packManager.GetMembersByRole(PackManager.TacticalRole.FlankerRight);
        List<PackMember> harassers = packManager.GetMembersByRole(PackManager.TacticalRole.Harasser);
        List<PackMember> reservists = packManager.GetMembersByRole(PackManager.TacticalRole.Reserve);

        // 1. Alpha получает "сектор" = избегание (не используется, но для единообразия)
        sectors[alpha] = new Sector(0, 360, alphaRadius * (1 - radiusVariation), alphaRadius * (1 + radiusVariation));

        // 2. Фланкеры: секторы по бокам от оси Игрок→Alpha
        AssignFlankerSectors(flankersLeft, axisAngle, 90f, 170f);
        AssignFlankerSectors(flankersRight, axisAngle, -170f, -90f);

        // 3. Harasser'ы: за спиной игрока относительно линии Alpha→Игрок
        AssignHarasserSectors(harassers, playerPos, attackAxis);

        // 4. Резерв: заполняет оставшуюся окружность
        AssignReserveSectors(reservists, axisAngle);

        lastAlphaPosition = alphaPos;
        lastRecalcTime = Time.time;
    }

    private void AssignFlankerSectors(List<PackMember> flankers, float axisAngle, float startAngle, float endAngle)
    {
        if (flankers.Count == 0) return;

        float angleRange = Mathf.Abs(endAngle - startAngle);
        float anglePerFlanker = angleRange / flankers.Count;
        float rMin = waitingRadius * (1f - radiusVariation);
        float rMax = waitingRadius * (1f + radiusVariation);

        for (int i = 0; i < flankers.Count; i++)
        {
            float a1 = axisAngle + startAngle + i * anglePerFlanker;
            float a2 = axisAngle + startAngle + (i + 1) * anglePerFlanker;
            sectors[flankers[i]] = new Sector(a1, a2, rMin, rMax);
        }
    }

    private void AssignHarasserSectors(List<PackMember> harassers, Vector2 playerPos, float attackAxis)
    {
        if (harassers.Count == 0) return;

        float behindAngle = attackAxis + 180f;
        float rMin = attackingRadius * (1f - radiusVariation);
        float rMax = attackingRadius * (1f + radiusVariation);

        // Распределяем равномерно по всей окружности,
        // но сдвигаем так чтобы центр был за спиной
        float angleStep = 360f / harassers.Count;
        float spread = angleStep * harassers.Count;
        float startAngle = behindAngle - spread / 2f + angleStep / 2f;

        for (int i = 0; i < harassers.Count; i++)
        {
            float a1 = startAngle + i * angleStep - angleStep / 2f;
            float a2 = startAngle + i * angleStep + angleStep / 2f;
            sectors[harassers[i]] = new Sector(a1, a2, rMin, rMax);
        }
    }

    private void AssignReserveSectors(List<PackMember> reservists, float axisAngle)
    {
        if (reservists.Count == 0) return;

        // Собираем занятые диапазоны (только фланкеры и харассеры)
        List<(float min, float max)> occupied = new();
        foreach (var kvp in sectors)
        {
            var role = packManager.GetAllRoles()[kvp.Key];
            if (role == PackManager.TacticalRole.FlankerLeft ||
                role == PackManager.TacticalRole.FlankerRight ||
                role == PackManager.TacticalRole.Harasser)
            {
                occupied.Add((NormalizeAngle(kvp.Value.minAngle), NormalizeAngle(kvp.Value.maxAngle)));
            }
        }

        // Свободные диапазоны
        List<(float min, float max)> free = GetFreeRanges(occupied);

        if (free.Count == 0) return;

        // Распределяем резервистов
        float rMin = waitingRadius * (1f - radiusVariation);
        float rMax = waitingRadius * (1f + radiusVariation);
        int rangeIndex = 0;
        float portion = 1f / reservists.Count;
        float accum = 0f;

        for (int i = 0; i < reservists.Count; i++)
        {
            if (rangeIndex >= free.Count) rangeIndex = 0;

            var range = free[rangeIndex];
            float rangeLength = range.max - range.min;
            float a1 = range.min + rangeLength * accum;
            float a2 = range.min + rangeLength * (accum + portion);

            sectors[reservists[i]] = new Sector(a1, a2, rMin, rMax);

            accum += portion;
            if (accum >= 0.99f)
            {
                accum -= 1f;
                rangeIndex++;
            }
        }
    }

    private List<(float min, float max)> GetFreeRanges(List<(float min, float max)> occupied)
    {
        List<(float min, float max)> free = new();

        if (occupied.Count == 0)
        {
            free.Add((0f, 360f));
            return free;
        }

        occupied.Sort((a, b) => a.min.CompareTo(b.min));

        if (occupied[0].min > 0f)
            free.Add((0f, occupied[0].min));

        for (int i = 0; i < occupied.Count - 1; i++)
        {
            if (occupied[i].max < occupied[i + 1].min)
                free.Add((occupied[i].max, occupied[i + 1].min));
        }

        if (occupied[occupied.Count - 1].max < 360f)
            free.Add((occupied[occupied.Count - 1].max, 360f));

        return free;
    }

    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

    // ===== Применение к членам =====
    private void ApplyToAllMembers()
    {
        foreach (var member in packManager.GetAllMembers())
        {
            if (member.IsDead) continue;

            var role = packManager.GetAllRoles()[member];

            PackManager.TacticalRole tacticalRole = ConvertRole(role);
            WeightSet weights = GetWeightsForRole(tacticalRole);
            Sector sector = sectors.ContainsKey(member) ? sectors[member]
                : new Sector(0, 360, waitingRadius, waitingRadius);

            member.SetTacticalRole(tacticalRole);
            member.SetPotentialWeights(weights);
            member.SetSector(sector);
        }

        // Передаём позицию Alpha для Harasser'ов и актуальные позиции всех членов
        UpdateMemberContext();
    }

    private void UpdateMemberContext()
    {
        Vector2 alphaPos = packManager.Alpha != null ? (Vector2)packManager.Alpha.transform.position : Vector2.zero;
        Vector2 playerPos = packManager.Player != null ? (Vector2)packManager.Player.position : Vector2.zero;

        List<Vector2> allyPositions = new();
        foreach (var member in packManager.GetAllMembers())
        {
            if (!member.IsDead)
                allyPositions.Add(member.transform.position);
        }

        foreach (var member in packManager.GetAllMembers())
        {
            if (member.IsDead) continue;
            member.UpdateContext(alphaPos, playerPos, allyPositions);
        }
    }

    private WeightSet GetWeightsForRole(PackManager.TacticalRole role)
    {
        return role switch
        {
            PackManager.TacticalRole.Alpha => alphaWeights,
            PackManager.TacticalRole.FlankerLeft => flankerWeights,
            PackManager.TacticalRole.FlankerRight => flankerWeights,
            PackManager.TacticalRole.Harasser => harasserWeights,
            PackManager.TacticalRole.Reserve => reserveWeights,
            _ => reserveWeights
        };
    }

    private PackManager.TacticalRole ConvertRole(PackManager.TacticalRole role)
    {
        return role;
    }

    // ===== Gizmos =====
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || packManager == null || packManager.Player == null) return;

        Vector2 playerPos = packManager.Player.position;

        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(playerPos, alphaRadius);

        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(playerPos, waitingRadius);

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Gizmos.DrawWireSphere(playerPos, attackingRadius);

        if (packManager.Alpha != null)
        {
            Vector2 alphaPos = packManager.Alpha.transform.position;
            Gizmos.color = Color.white;
            Gizmos.DrawLine(playerPos, alphaPos);
        }

        foreach (var kvp in sectors)
        {
            if (kvp.Key == null) continue;
            Color c = Color.grey;
            if (packManager.GetAllRoles().TryGetValue(kvp.Key, out var role))
            {
                c = role switch
                {
                    PackManager.TacticalRole.Alpha => Color.cyan,
                    PackManager.TacticalRole.FlankerLeft => Color.blue,
                    PackManager.TacticalRole.FlankerRight => Color.green,
                    PackManager.TacticalRole.Harasser => Color.red,
                    PackManager.TacticalRole.Reserve => Color.grey,
                    _ => Color.white
                };
            }
            c.a = 0.5f;
            Gizmos.color = c;

            float midAngle = (kvp.Value.minAngle + kvp.Value.maxAngle) / 2f * Mathf.Deg2Rad;
            float midRadius = (kvp.Value.minRadius + kvp.Value.maxRadius) / 2f;
            Vector2 midPoint = playerPos + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * midRadius;
            Gizmos.DrawWireSphere(midPoint, 0.4f);
        }
    }

    // Контекстное меню для ручного пересчёта в редакторе
    [ContextMenu("Recalculate Sectors")]
    private void RecalculateSectors()
    {
        if (Application.isPlaying && packManager != null && packManager.IsPackActive)
        {
            CalculateAllSectors();
            ApplyToAllMembers();
        }
    }
}
