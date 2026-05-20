using UnityEngine;

/// <summary>
/// Единый центр управления всеми атаками.
/// Поведения вызывают нужный метод, AttackExecutor выполняет всю логику.
/// </summary>
public class AttackExecutor : MonoBehaviour
{
    [Header("Обычная атака")]
    [SerializeField] private float normalDamage = 12f;
    [SerializeField] private float normalRange = 2f;
    [SerializeField] private float normalCooldown = 2f;
    [SerializeField] private float normalRecoil = 5f;

    [Header("Рывок с ударом (Lunge)")]
    [SerializeField] private float lungeDamage = 18f;
    [SerializeField] private float lungeRange = 2f;
    [SerializeField] private float lungeCooldown = 2.5f;
    [SerializeField] private float lungeForce = 10f;

    [Header("Мощная атака (Heavy)")]
    [SerializeField] private float heavyDamage = 40f;
    [SerializeField] private float heavyRange = 2.5f;
    [SerializeField] private float heavyCooldown = 4f;
    [SerializeField] private float heavyForce = 12f;

    [Header("Атака со спины (Backstab)")]
    [SerializeField] private float backstabDamage = 30f;
    [SerializeField] private float backstabRange = 2f;
    [SerializeField] private float backstabCooldown = 3f;
    [SerializeField] private float backstabRecoil = 8f;

    // Кешированные компоненты
    private AttackSprite attackSprite;
    private Rigidbody2D rb;
    private CombatController playerCombat;
    private Transform playerTransform;

    // Таймеры для кулдаунов
    private float lastNormalAttackTime;
    private float lastLungeAttackTime;
    private float lastHeavyAttackTime;
    private float lastBackstabAttackTime;

    private void Awake()
    {
        attackSprite = GetComponent<AttackSprite>();
        rb = GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCombat = player.GetComponent<CombatController>();
        }
    }

    // ==================== ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ПОВЕДЕНИЙ ====================

    /// <summary>Обычная атака (Flanker)</summary>
    public void NormalAttack(Vector2 direction)
    {
        if (Time.time < lastNormalAttackTime + normalCooldown) return;
        lastNormalAttackTime = Time.time;

        // Спрайт
        attackSprite?.ShowAttack(direction, AttackSprite.AttackType.Normal);

        // Урон
        if (IsPlayerInRange(normalRange))
            playerCombat?.ShowDamageNumber(normalDamage, playerTransform.position);

        // Отскок
        rb?.AddForce(-direction * normalRecoil, ForceMode2D.Impulse);
    }

    /// <summary>Рывок с ударом (Harasser)</summary>
    public void LungeAttack(Vector2 direction)
    {
        if (Time.time < lastLungeAttackTime + lungeCooldown) return;
        lastLungeAttackTime = Time.time;

        // Спрайт
        attackSprite?.ShowAttack(direction, AttackSprite.AttackType.Normal);

        // Рывок ВПЕРЁД
        rb?.AddForce(direction * lungeForce, ForceMode2D.Impulse);

        // Урон (проверим в конце рывка через корутину)
        StartCoroutine(DelayedDamage(lungeDamage, lungeRange, 0.3f));
    }

    /// <summary>Мощная атака (Alpha)</summary>
    public void HeavyAttack(Vector2 direction)
    {
        if (Time.time < lastHeavyAttackTime + heavyCooldown) return;
        lastHeavyAttackTime = Time.time;

        // Спрайт (особый тип)
        attackSprite?.ShowAttack(direction, AttackSprite.AttackType.Power);

        // Мощный рывок вперёд
        rb?.AddForce(direction * heavyForce, ForceMode2D.Impulse);

        // Урон с задержкой
        StartCoroutine(DelayedDamage(heavyDamage, heavyRange, 0.3f));
    }

    /// <summary>Критическая атака со спины (Backstabber)</summary>
    public void BackstabAttack(Vector2 direction)
    {
        if (Time.time < lastBackstabAttackTime + backstabCooldown) return;
        lastBackstabAttackTime = Time.time;

        // Спрайт (красный, крупный)
        attackSprite?.ShowAttack(direction, AttackSprite.AttackType.Backstab);

        // Урон
        if (IsPlayerInRange(backstabRange))
            playerCombat?.ShowDamageNumber(backstabDamage, playerTransform.position);

        // Сильный отскок НАЗАД
        rb?.AddForce(-direction * backstabRecoil, ForceMode2D.Impulse);
    }

    // ==================== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ====================

    private bool IsPlayerInRange(float range)
    {
        return playerTransform != null &&
               Vector2.Distance(transform.position, playerTransform.position) < range;
    }

    /// <summary>Нанести урон с задержкой (после рывка)</summary>
    private System.Collections.IEnumerator DelayedDamage(float damage, float range, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (IsPlayerInRange(range))
            playerCombat?.ShowDamageNumber(damage, playerTransform.position);
    }
}
