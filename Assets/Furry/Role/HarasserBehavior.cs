using UnityEngine;
using System.Collections.Generic;

public class HarasserBehavior : BaseBehavior
{
    [Header("Harasser настройки")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float leapDistance = 5f;         // Дальше — скачки, ближе — шаги

    [Header("Заход за спину")]
    [SerializeField] private float behindPlayerDistance = 4f;
    [SerializeField] private float behindAngleThreshold = 60f;

    [Header("Передние харассеры")]
    [SerializeField] private float frontOffset = 3f;           // Смещение вперёд от игрока
    [SerializeField] private float sideSpread = 3f;            // Разброс в стороны

    private AttackExecutor attackExecutor;
    private PackMemberMovement movement;
    private PackManager packManager;

    private Vector2 flankTarget;

    public HarasserBehavior()
    {
        behaviorName = "Harasser";
    }

    public override void Initialize(PackMember member)
    {
        base.Initialize(member);
        attackExecutor = GetComponent<AttackExecutor>();
        if (attackExecutor == null)
            attackExecutor = gameObject.AddComponent<AttackExecutor>();

        movement = GetComponent<PackMemberMovement>();
        packManager = FindObjectOfType<PackManager>();
    }

    public override void UpdateBehavior()
    {
        if (playerTransform == null || packManager == null) return;

        // Определяем мою роль среди харассеров
        var harassers = packManager.GetMembersByRole(PackManager.TacticalRole.Harasser);
        int myIndex = harassers.IndexOf(packMember);
        int totalHarassers = harassers.Count;

        // Только один харассер (последний в списке) заходит сзади
        bool iAmBackstabber = (totalHarassers >= 2 && myIndex == totalHarassers - 1);

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Переключаем режим движения
        if (movement != null)
        {
            if (distToPlayer > leapDistance)
                movement.SetMovementMode(PackMemberMovement.MovementMode.Leap);
            else
                movement.SetMovementMode(PackMemberMovement.MovementMode.Step);
        }

        if (iAmBackstabber)
        {
            UpdateBackstabber();
        }
        else
        {
            UpdateFrontHarasser(myIndex, totalHarassers);
        }
    }

    // ===== ПЕРЕДНИЙ ХАРАССЕР =====
    private void UpdateFrontHarasser(int myIndex, int totalHarassers)
    {
        Vector2 playerLookDir = GetPlayerLookDirection();
        Vector2 perpendicular = new Vector2(-playerLookDir.y, playerLookDir.x);

        int frontCount = totalHarassers - 1;
        if (frontCount <= 0) frontCount = 1;

        float sideSign;
        if (frontCount == 1)
            sideSign = 0f;
        else
            sideSign = (myIndex % 2 == 0) ? -1f : 1f;

        Vector2 targetPos = (Vector2)playerTransform.position
            + playerLookDir * frontOffset
            + perpendicular * sideSign * sideSpread;

        MoveToPosition(targetPos, 6f);

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if (distToPlayer < attackRange)
        {
            attackExecutor.LungeAttack(GetDirectionToPlayer());
        }
    }

    // ===== БЭКСТАБЕР (заходит сзади) =====
    private void UpdateBackstabber()
    {
        Vector2 behindPlayer = CalculateBehindPlayerPosition();

        if (Vector2.Distance(flankTarget, behindPlayer) > 1f)
            flankTarget = behindPlayer;

        MoveToPosition(flankTarget, 6f);

        if (IsBehindPlayer() && IsPlayerInRange(attackRange))
        {
            attackExecutor.BackstabAttack(GetDirectionToPlayer());
            Debug.Log($"{name}: БЭКСТАБ!");
        }
    }

    // ===== ПРОВЕРКА ПОЗИЦИИ ЗА СПИНОЙ =====
    private bool IsBehindPlayer()
    {
        if (playerTransform == null) return false;

        Vector2 toPlayer = playerTransform.position - transform.position;
        Vector2 playerLookDir = GetPlayerLookDirection();

        float angle = Vector2.Angle(playerLookDir, -toPlayer.normalized);
        return angle < behindAngleThreshold;
    }

    private Vector2 CalculateBehindPlayerPosition()
    {
        Vector2 playerLookDir = GetPlayerLookDirection();
        return (Vector2)playerTransform.position - playerLookDir * behindPlayerDistance;
    }

    // ===== ВСПОМОГАТЕЛЬНЫЕ =====
    private Vector2 GetPlayerLookDirection()
    {
        var playerDirection = FindObjectOfType<PlayerDirection>();
        if (playerDirection != null)
            return playerDirection.LookDirection;

        if (packManager.Alpha != null)
            return ((Vector2)playerTransform.position - (Vector2)packManager.Alpha.transform.position).normalized;

        return Vector2.down;
    }
}
