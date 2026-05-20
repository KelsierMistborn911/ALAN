using UnityEngine;

public class AlphaBehavior : BaseBehavior
{
    [Header("Alpha настройки")]
    [SerializeField] private float safeDistance = 10f;
    [SerializeField] private float attackThreshold = 0.3f;
    [SerializeField] private float attackCooldown = 4f;

    private AttackExecutor attackExecutor;
    private float lastAttackTime;

    public AlphaBehavior()
    {
        behaviorName = "Alpha";
    }

    public override void Initialize(PackMember member)
    {
        base.Initialize(member);
        attackExecutor = GetComponent<AttackExecutor>();
        if (attackExecutor == null)
            attackExecutor = gameObject.AddComponent<AttackExecutor>();
    }

    public override void UpdateBehavior()
    {
        if (playerTransform == null || playerCombat == null) return;

        float playerHealthPercent = playerCombat.GetHealthPercent();
        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Держим безопасную дистанцию
        if (distToPlayer < safeDistance)
        {
            Vector2 awayFromPlayer = (transform.position - playerTransform.position).normalized;
            MoveToPosition((Vector2)transform.position + awayFromPlayer * 3f, 4f);
        }
        else if (playerHealthPercent <= attackThreshold && Time.time >= lastAttackTime + attackCooldown)
        {
            // Мощная атака когда игрок ослаблен
            attackExecutor.HeavyAttack(GetDirectionToPlayer());
            lastAttackTime = Time.time;
            Debug.Log($"{name}: Alpha наносит мощный удар!");
        }
        else
        {
            // Медленное движение к игроку
            MoveToPosition(playerTransform.position, 3f);
        }
    }
}
