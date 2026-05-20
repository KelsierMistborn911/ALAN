using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отвечает за расчёт ЦЕЛЕВОЙ ПОЗИЦИИ для члена стаи.
/// Учитывает роль, сектор, позиции союзников и игрока.
/// Не управляет движением — только выдаёт точку.
/// </summary>
public class PackPathfinder : MonoBehaviour
{
    [Header("Харассер — атака")]
    [SerializeField] private float frontOffset = 2f;         // Смещение спереди игрока
    [SerializeField] private float behindOffset = 2.5f;      // Смещение за спиной игрока
    [SerializeField] private float sideSpread = 2f;          // Разброс в стороны для передних

    [Header("Фланкер — окружение")]
    [SerializeField] private float circleRadius = 5f;
    [SerializeField] private float angleBetweenFlankers = 45f;

    [Header("Альфа — наблюдение")]
    [SerializeField] private float alphaDistance = 7f;

    [Header("Отладка")]
    [SerializeField] private bool showDebug = true;

    private PackMember packMember;
    private PackManager packManager;
    private Transform playerTransform;

    private void Awake()
    {
        packMember = GetComponent<PackMember>();
        packManager = FindObjectOfType<PackManager>();
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary>
    /// Основной метод — возвращает точку, куда нужно двигаться
    /// </summary>
    public Vector2 GetTargetPosition(
        Vector2 playerPos,
        Vector2 alphaPos,
        List<Vector2> allyPositions,
        PackFormation.Sector sector,
        PackManager.TacticalRole role)
    {
        switch (role)
        {
            case PackManager.TacticalRole.Harasser:
                return GetHarasserTarget(playerPos, allyPositions);
            case PackManager.TacticalRole.FlankerLeft:
            case PackManager.TacticalRole.FlankerRight:
                return GetFlankerTarget(playerPos, role, allyPositions);
            case PackManager.TacticalRole.Alpha:
                return GetAlphaTarget(playerPos);
            default:
                return GetDefaultTarget(playerPos, sector);
        }
    }

    // ===== ХАРАССЕР: разделение на передних и заднего =====
    private Vector2 GetHarasserTarget(Vector2 playerPos, List<Vector2> allyPositions)
    {
        if (playerTransform == null)
            return playerPos;

        // Считаем, какой я по счёту харассер
        var allHarassers = packManager.GetMembersByRole(PackManager.TacticalRole.Harasser);
        int myIndex = allHarassers.IndexOf(packMember);
        int totalHarassers = allHarassers.Count;

        Vector2 playerLookDir = GetPlayerLookDirection();
        Vector2 behindPlayer = -playerLookDir;

        if (totalHarassers >= 3 && myIndex == totalHarassers - 1)
        {
            // ПОСЛЕДНИЙ харассер — заходит ЗА СПИНУ
            return playerPos + behindPlayer * behindOffset;
        }
        else
        {
            // ПЕРВЫЕ 1-2 харассера — атакуют СПЕРЕДИ с разбросом
            Vector2 perpendicular = new Vector2(-playerLookDir.y, playerLookDir.x);

            // Чередуем лево/право
            int frontIndex = myIndex; // 0, 1
            float sideSign = (frontIndex % 2 == 0) ? -1f : 1f;

            return playerPos + playerLookDir * frontOffset + perpendicular * sideSign * sideSpread;
        }
    }

    // ===== ФЛАНКЕР: окружение по кругу =====
    private Vector2 GetFlankerTarget(Vector2 playerPos, PackManager.TacticalRole side, List<Vector2> allyPositions)
    {
        // Получаем всех фланкеров этой стороны
        var flankers = packManager.GetMembersByRole(side);
        int myIndex = flankers.IndexOf(packMember);
        int total = flankers.Count;

        // Равномерно распределяем по сектору
        float baseAngle = (side == PackManager.TacticalRole.FlankerLeft) ? 90f : -90f;
        float sectorHalf = 60f; // 60 градусов в каждую сторону

        float angle;
        if (total <= 1)
        {
            angle = baseAngle;
        }
        else
        {
            float step = (sectorHalf * 2f) / (total - 1);
            angle = baseAngle - sectorHalf + step * myIndex;
        }

        angle *= Mathf.Deg2Rad;
        return playerPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;
    }

    // ===== АЛЬФА: держит дистанцию =====
    private Vector2 GetAlphaTarget(Vector2 playerPos)
    {
        if (playerTransform == null)
            return playerPos + Vector2.up * alphaDistance;

        Vector2 awayFromPlayer = ((Vector2)transform.position - playerPos).normalized;
        if (awayFromPlayer.sqrMagnitude < 0.01f)
            awayFromPlayer = Vector2.up;

        return playerPos + awayFromPlayer * alphaDistance;
    }

    // ===== По умолчанию: центр сектора =====
    private Vector2 GetDefaultTarget(Vector2 playerPos, PackFormation.Sector sector)
    {
        float midAngle = (sector.minAngle + sector.maxAngle) / 2f * Mathf.Deg2Rad;
        float midRadius = (sector.minRadius + sector.maxRadius) / 2f;
        return playerPos + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * midRadius;
    }

    // ===== Вспомогательные методы =====
    private Vector2 GetPlayerLookDirection()
    {
        var playerDirection = FindObjectOfType<PlayerDirection>();
        if (playerDirection != null)
            return playerDirection.LookDirection;

        // Запасной вариант: направление к игроку от альфы
        if (packManager.Alpha != null)
            return ((Vector2)playerTransform.position - (Vector2)packManager.Alpha.transform.position).normalized;

        return Vector2.down;
    }

    // ===== Gizmos =====
    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        if (packMember == null) return; // Защита от вызова до Awake

        // Проверяем, что все нужные данные есть
        if (packMember.PlayerPosition == Vector2.zero) return;

        Vector2 target = GetTargetPosition(
            packMember.PlayerPosition,
            packMember.AlphaPosition,
            packMember.AllyPositions,
            packMember.Sector,
            packMember.CurrentRole
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target, 0.3f);
        Gizmos.DrawLine(transform.position, target);
    }
}
