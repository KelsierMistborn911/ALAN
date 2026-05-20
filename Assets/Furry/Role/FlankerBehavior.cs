using UnityEngine;

public class FlankerBehavior : BaseBehavior
{
    [Header("Flanker настройки")]
    [SerializeField] private float circleRadius = 20f;
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackInterval = 3f;
    [SerializeField] private float safeDistance = 15f;
    [SerializeField] private float retreatSpeed = 6f;        // Новая: скорость отступления


    private AttackExecutor attackExecutor;
    private float currentAngle;
    private float lastAttackTime;

    public FlankerBehavior()
    {
        behaviorName = "Flanker";
    }

    public override void Initialize(PackMember member)
    {
        base.Initialize(member);
        attackExecutor = GetComponent<AttackExecutor>();
        if (attackExecutor == null)
            attackExecutor = gameObject.AddComponent<AttackExecutor>();
        currentAngle = Random.Range(0f, 360f);
    }

    public override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        // Если игрок слишком близко - отступаем
        if (distToPlayer < safeDistance)
        {
            // Отбегаем от игрока
            Vector2 awayFromPlayer = (transform.position - playerTransform.position).normalized;
            MoveToPosition((Vector2)transform.position + awayFromPlayer * 3f, retreatSpeed);
        }
        else
        {
            // Нормальное движение по кругу
            currentAngle += 90f * Time.deltaTime;
            if (currentAngle >= 360f) currentAngle -= 360f;

            Vector2 circlePos = (Vector2)playerTransform.position +
                new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                           Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * circleRadius;

            MoveToPosition(circlePos, 5f);
        }

        // Атакуем ТОЛЬКО если игрок сам подошел близко
        // (фланкер НЕ приближается к игроку для атаки)
        if (Time.time >= lastAttackTime + attackInterval && distToPlayer < attackRange)
        {
            attackExecutor.NormalAttack(GetDirectionToPlayer());
            lastAttackTime = Time.time;
        }
    }
}
