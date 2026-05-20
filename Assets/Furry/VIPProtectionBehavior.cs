using UnityEngine;
using System.Linq;

/// <summary>
/// Поведение "Защита VIP" - охраняет указанную цель, перехватывает атаки
/// </summary>
public class VIPProtectionBehavior : BaseBehavior
{
    [Header("Настройки защиты VIP")]
    [SerializeField] private Transform vipTarget;
    [SerializeField] private float protectionRadius = 3f;
    [SerializeField] private float interceptDistance = 2f;
    [SerializeField] private float interceptSpeed = 8f;

    [Header("Блокирование атак")]
    [SerializeField] private bool canBlockAttacks = true;
    [SerializeField] private float blockCooldown = 1f;

    [Header("Ответная атака")]
    [SerializeField] private float counterDamage = 20f;
    [SerializeField] private float counterReach = 1.5f;

    private enum ProtectionState
    {
        Following,      // Следование за VIP
        Intercepting,   // Перехват угрозы
        Blocking,       // Блокирование атаки
        CounterAttacking // Контратака
    }

    private ProtectionState state = ProtectionState.Following;
    private float blockTimer;
    private Transform currentThreat;
    private float lastBlockTime;

    public void SetVIP(Transform vip)
    {
        vipTarget = vip;
        Debug.Log($"{packMember.name}: Назначен защитником для {vip.name}");
    }

    public override void UpdateBehavior()
    {
        if (vipTarget == null)
        {
            // Ищем VIP автоматически (Alpha)
            var packManager = FindObjectOfType<PackManager>();
            if (packManager?.Alpha != null)
                vipTarget = packManager.Alpha.transform;
            else
                return;
        }

        UpdateProtection();
    }

    private void UpdateProtection()
    {
        switch (state)
        {
            case ProtectionState.Following:
                UpdateFollowing();
                break;
            case ProtectionState.Intercepting:
                UpdateIntercepting();
                break;
            case ProtectionState.Blocking:
                UpdateBlocking();
                break;
            case ProtectionState.CounterAttacking:
                UpdateCounterAttack();
                break;
        }
    }

    private void UpdateFollowing()
    {
        if (vipTarget == null) return;

        // Держим позицию между VIP и игроком
        Vector2 protectionPos = CalculateProtectionPosition();
        MoveToPosition(protectionPos, 6f);

        // Проверяем угрозы
        DetectThreats();

        // Проверяем, нужно ли блокировать атаку
        if (canBlockAttacks && IsPlayerAttacking() && IsPlayerInRange(protectionRadius))
        {
            state = ProtectionState.Blocking;
            blockTimer = 0.3f;
        }
    }

    private void UpdateIntercepting()
    {
        if (currentThreat == null)
        {
            state = ProtectionState.Following;
            return;
        }

        // Бросаемся к угрозе
        MoveToPosition(currentThreat.position, interceptSpeed);

        // Если дошли до угрозы - атакуем
        if (Vector2.Distance(transform.position, currentThreat.position) < interceptDistance)
        {
            state = ProtectionState.CounterAttacking;
        }

        // Если угроза исчезла или далеко - возвращаемся
        if (Vector2.Distance(currentThreat.position, vipTarget.position) > protectionRadius * 2f)
        {
            state = ProtectionState.Following;
        }
    }

    private void UpdateBlocking()
    {
        blockTimer -= Time.deltaTime;

        // Визуальный эффект блока
        Debug.DrawLine(transform.position, vipTarget.position, Color.cyan, 0.1f);

        if (blockTimer <= 0f)
        {
            state = ProtectionState.Following;

            // Если атака была недавно - контратакуем
            if (Time.time < lastBlockTime + 0.5f)
            {
                state = ProtectionState.CounterAttacking;
            }
        }
    }

    private void UpdateCounterAttack()
    {
        if (playerTransform == null)
        {
            state = ProtectionState.Following;
            return;
        }

        // Рывок к игроку
        MoveToPosition(playerTransform.position, interceptSpeed);

        // Наносим урон
        if (Vector2.Distance(transform.position, playerTransform.position) < counterReach)
        {
            var attackSprite = GetComponent<AttackSprite>();
            if (attackSprite != null)
                attackSprite.ShowAttack(GetDirectionToPlayer());

            playerCombat?.TakeDamage(counterDamage);
            Debug.Log($"{packMember.name}: Контратака! Урон {counterDamage}");

            state = ProtectionState.Following;
        }
    }

    private Vector2 CalculateProtectionPosition()
    {
        Vector2 toVIP = (Vector2)vipTarget.position - (Vector2)transform.position;
        Vector2 threatDir = GetDirectionToPlayer();

        // Позиция между VIP и угрозой
        return (Vector2)vipTarget.position + threatDir * protectionRadius;
    }

    private void DetectThreats()
    {
        if (playerCombat == null) return;

        // Проверяем, атакует ли игрок VIP
        if (IsPlayerAttacking() && IsPlayerInRange(protectionRadius))
        {
            currentThreat = playerTransform;
            state = ProtectionState.Intercepting;
        }
    }

    private bool IsPlayerAttacking()
    {
        return playerCombat != null &&
               (playerCombat.IsAttacking || playerCombat.IsWindingUp);
    }

    protected override bool IsPlayerInRange(float range)
    {
        if (vipTarget == null) return false;
        return Vector2.Distance(vipTarget.position, playerTransform.position) < range;
    }
}
