using UnityEngine;

public class CombatController : MonoBehaviour
{
    [SerializeField] private PlayerParams playerParams;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float hitForceMultiplier = 1f;
    [SerializeField] private float attackDuration = 0.3f;
    [SerializeField] private float attackSlowdownFactor = 0.5f;
    [SerializeField] private GameObject damageNumberPrefab; // ← ПРЕФАБ ЦИФР УРОНА

    private PlayerDirection playerDirection;

    [HideInInspector] public bool IsWindingUp { get; private set; }
    [HideInInspector] public bool IsAttacking { get; private set; }
    [HideInInspector] public bool AttackJustReleased { get; set; }

    private float attackTimer;

    private void Awake()
    {
        if (playerParams == null) playerParams = GetComponent<PlayerParams>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerDirection == null) playerDirection = GetComponent<PlayerDirection>();
    }

    private void Update()
    {
        if (IsAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                IsAttacking = false;
            }
        }

        if (Input.GetMouseButton(0) && !IsAttacking)
        {
            IsWindingUp = true;
            AttackJustReleased = false;
        }

        if (Input.GetMouseButtonUp(0) && IsWindingUp && !IsAttacking)
        {
            IsWindingUp = false;
            IsAttacking = true;
            AttackJustReleased = true;
            attackTimer = attackDuration;

            float hitForce = CalculateHitForce();
            Vector2 dir = playerDirection?.LookDirection ?? transform.right;
            playerParams.RightHandWeapon?.Attack(hitForce, transform.position, dir);

            Weapon rightWeapon = playerParams.RightHandWeapon;
            float weaponMass = rightWeapon != null ? rightWeapon.WeaponMass : 1f;
            Vector2 slowdown = rb.velocity.normalized * (weaponMass * attackSlowdownFactor);
            rb.velocity -= slowdown;
            if (rb.velocity.sqrMagnitude < 0.01f) rb.velocity = Vector2.zero;
        }

        if (!Input.GetMouseButton(0) && !IsAttacking)
        {
            IsWindingUp = false;
            AttackJustReleased = false;
        }

        if (Input.GetMouseButtonDown(1))
        {
            float hitForce = CalculateHitForce();
            playerParams.LeftHandWeapon?.Block(hitForce);
        }
    }

    private float CalculateHitForce()
    {
        return playerParams.Mass * rb.velocity.magnitude * hitForceMultiplier;
    }

    // ВЫЗЫВАЙ ЭТОТ МЕТОД КОГДА НАНОСИШЬ УРОН
    public void ShowDamageNumber(float damage, Vector3 position)
    {
        if (damageNumberPrefab == null) return;

        GameObject obj = Instantiate(damageNumberPrefab, position + Vector3.up * 2f, Quaternion.identity);
        DamageNumber dmg = obj.GetComponent<DamageNumber>();
        if (dmg != null)
        {
            dmg.Show(damage, position);
        }
    }
}
