using UnityEngine;

public abstract class BaseBehavior : MonoBehaviour, IBehavior
{
    [Header("Базовые настройки поведения")]
    [SerializeField] protected string behaviorName = "Base Behavior";
    [SerializeField] protected float priority = 1f;

    protected PackMember packMember;
    protected PackMemberMovement movement;
    protected Rigidbody2D rb;
    protected Transform playerTransform;
    protected CombatController playerCombat;

    public string BehaviorName => behaviorName;
    public bool IsActive { get; protected set; }
    public float Priority => priority;

    public event System.Action<BaseBehavior> OnBehaviorComplete;
    public event System.Action<BaseBehavior> OnBehaviorInterrupted;

    public virtual void Initialize(PackMember member)
    {
        packMember = member;
        movement = member.GetComponent<PackMemberMovement>();
        rb = member.GetComponent<Rigidbody2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerCombat = player.GetComponent<CombatController>();
        }
    }

    public virtual void OnBehaviorActivated()
    {
        IsActive = true;
        enabled = true;
    }

    public virtual void OnBehaviorDeactivated()
    {
        IsActive = false;
        enabled = false;
    }

    public abstract void UpdateBehavior();

    protected virtual bool IsPlayerInRange(float range)
    {
        return playerTransform != null &&
               Vector2.Distance(transform.position, playerTransform.position) < range;
    }

    protected virtual Vector2 GetDirectionToPlayer()
    {
        if (playerTransform == null) return Vector2.zero;
        return ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
    }

    protected virtual void MoveToPosition(Vector2 target, float speed = 5f)
    {
        if (rb == null) return;
        Vector2 toTarget = target - (Vector2)transform.position;
        if (toTarget.magnitude > 0.1f)
            rb.velocity = toTarget.normalized * speed;
    }

    protected virtual void ExecuteAttack(Vector2 direction, float damage, bool isCritical = false)
    {
        AttackSprite attackSprite = GetComponent<AttackSprite>();
        if (attackSprite != null)
        {
            if (isCritical)
                attackSprite.ShowAttack(direction, AttackSprite.AttackType.Backstab);
            else
                attackSprite.ShowAttack(direction, AttackSprite.AttackType.Normal);
        }

        // Проверяем попадание
        if (IsPlayerInRange(1.5f) && playerCombat != null)
        {
            // Используем существующий метод ShowDamageNumber
            playerCombat.ShowDamageNumber(damage, playerTransform.position);
            Debug.Log($"{packMember.name}: нанёс урон {damage}");
        }
    }
}
