using UnityEngine;

public enum WolfGait
{
    BipedalWalk,
    BipedalRun,
    QuadrupedalLeap
}

[RequireComponent(typeof(Rigidbody2D))]  // ← ИСПРАВЛЕНО
public class WolfMovement : MonoBehaviour
{
    [Header("Физика волка")]
    [SerializeField] private float mass = 80f;
    [SerializeField] private float groundDrag = 3f;
    [SerializeField] private float angularDrag = 5f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Двуногий шаг")]
    [SerializeField] private float walkImpulse = 200f;
    [SerializeField] private float walkStepInterval = 0.3f;
    [SerializeField] private float walkTurnTorque = 300f;

    [Header("Двуногий бег")]
    [SerializeField] private float runImpulse = 500f;
    [SerializeField] private float runStepInterval = 0.5f;
    [SerializeField] private float runTurnTorque = 150f;

    [Header("Прыжки на четвереньках")]
    [SerializeField] private float leapImpulse = 900f;
    [SerializeField] private float leapInterval = 0.8f;
    [SerializeField] private float leapTurnTorque = 50f;
    [SerializeField] private float leapUpwardRatio = 0.3f;

    [Header("Зона патрулирования")]
    [SerializeField] private float zoneRadius = 10f;
    [SerializeField] private float brakingDistance = 5f;

    private Rigidbody2D rb;  // ← ИСПРАВЛЕНО
    private Vector3 targetPoint;
    private bool hasTarget;
    private WolfGait currentGait = WolfGait.BipedalWalk;
    private float stepCooldown;

    public WolfGait CurrentGait
    {
        get => currentGait;
        set => currentGait = value;
    }

    public Vector2 Velocity => rb.velocity;  // ← ИСПРАВЛЕНО
    public float CurrentSpeed => rb.velocity.magnitude;

    private Vector3 IsoForward
    {
        get
        {
            if (!hasTarget) return transform.right;

            Vector3 toTarget = targetPoint - transform.position;
            float dot = Vector3.Dot(toTarget, transform.right);
            return dot >= 0f ? transform.right : -transform.right;
        }
    }

    private float AngleToTarget
    {
        get
        {
            if (!hasTarget) return 0f;
            Vector3 toTarget = (targetPoint - transform.position).normalized;
            return Vector3.SignedAngle(IsoForward, toTarget, Vector3.up);
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();  // ← ИСПРАВЛЕНО
        rb.mass = mass;
        rb.drag = groundDrag;
        rb.angularDrag = angularDrag;
        rb.gravityScale = 0;  // ← ИСПРАВЛЕНО (вместо useGravity = false)
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;  // ← ИСПРАВЛЕНО
    }

    public void SetTargetZone(Vector3 point, float radius)
    {
        targetPoint = point;
        zoneRadius = radius;
        hasTarget = true;
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        stepCooldown -= Time.fixedDeltaTime;

        Vector3 toTarget = targetPoint - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 directionToTarget = toTarget.normalized;

        bool needToMove = distanceToTarget > zoneRadius;

        if (needToMove)
        {
            ApplySteering(directionToTarget);

            if (stepCooldown <= 0f)
            {
                ApplyStepImpulse(directionToTarget, distanceToTarget);
                stepCooldown = GetStepInterval();
            }
        }
        else
        {
            Brake(distanceToTarget);
        }

        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }

    void ApplySteering(Vector3 directionToTarget)
    {
        float angle = AngleToTarget;
        float turnTorque = GetTurnTorque();

        float speedFactor = 1f / (1f + rb.velocity.magnitude * 0.3f);
        float effectiveTorque = turnTorque * speedFactor;

        // Rigidbody2D использует AddTorque с одним параметром float
        rb.AddTorque(angle * effectiveTorque * Time.fixedDeltaTime);
    }

    void ApplyStepImpulse(Vector3 directionToTarget, float distanceToTarget)
    {
        float impulse = GetStepImpulse();

        Vector3 impulseDirection = IsoForward;

        if (currentGait == WolfGait.QuadrupedalLeap)
        {
            impulseDirection = (IsoForward + Vector3.up * leapUpwardRatio).normalized;
        }

        float brakingFactor = Mathf.Clamp01((distanceToTarget - zoneRadius) / brakingDistance);
        impulse *= brakingFactor;

        float speedTowardsTarget = Vector2.Dot(rb.velocity, directionToTarget);  // ← ИСПРАВЛЕНО
        if (speedTowardsTarget > 0)
        {
            float speedFactor = 1f - Mathf.Clamp01(speedTowardsTarget / maxSpeed);
            impulse *= speedFactor;
        }

        rb.AddForce((Vector2)impulseDirection * impulse, ForceMode2D.Impulse);  // ← ИСПРАВЛЕНО
    }

    void Brake(float distanceToTarget)
    {
        if (rb.velocity.magnitude > 2f && distanceToTarget < zoneRadius * 0.5f)
        {
            rb.AddForce(-rb.velocity.normalized * 2f, ForceMode2D.Force);  // ← ИСПРАВЛЕНО
        }
    }

    float GetStepImpulse()
    {
        return currentGait switch
        {
            WolfGait.BipedalWalk => walkImpulse,
            WolfGait.BipedalRun => runImpulse,
            WolfGait.QuadrupedalLeap => leapImpulse,
            _ => walkImpulse
        };
    }

    float GetStepInterval()
    {
        return currentGait switch
        {
            WolfGait.BipedalWalk => walkStepInterval,
            WolfGait.BipedalRun => runStepInterval,
            WolfGait.QuadrupedalLeap => leapInterval,
            _ => walkStepInterval
        };
    }

    float GetTurnTorque()
    {
        return currentGait switch
        {
            WolfGait.BipedalWalk => walkTurnTorque,
            WolfGait.BipedalRun => runTurnTorque,
            WolfGait.QuadrupedalLeap => leapTurnTorque,
            _ => walkTurnTorque
        };
    }

    void OnDrawGizmos()
    {
        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPoint, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPoint, zoneRadius);
            Gizmos.DrawLine(transform.position, targetPoint);
        }
    }
}
