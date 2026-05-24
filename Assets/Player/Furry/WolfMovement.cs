using UnityEngine;

public enum WolfGait
{
    BipedalWalk,
    BipedalRun,
    QuadrupedalLeap
}

[RequireComponent(typeof(Rigidbody2D))]
public class WolfMovement : MonoBehaviour
{
    [Header("Физика волка")]
    [SerializeField] private float mass = 80f;
    [SerializeField] private float groundDrag = 3f;
    [SerializeField] private float angularDrag = 5f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Двуногий шаг")]
    [SerializeField] private float walkImpulse = 800f;      // УВЕЛИЧЕНО с 200
    [SerializeField] private float walkStepInterval = 0.3f;
    [SerializeField] private float walkTurnTorque = 300f;

    [Header("Двуногий бег")]
    [SerializeField] private float runImpulse = 1500f;      // УВЕЛИЧЕНО с 500
    [SerializeField] private float runStepInterval = 0.5f;
    [SerializeField] private float runTurnTorque = 150f;

    [Header("Прыжки на четвереньках")]
    [SerializeField] private float leapImpulse = 2500f;     // УВЕЛИЧЕНО с 900
    [SerializeField] private float leapInterval = 0.8f;
    [SerializeField] private float leapTurnTorque = 50f;
    [SerializeField] private float leapUpwardRatio = 0.3f;

    [Header("Зона патрулирования")]
    [SerializeField] private float zoneRadius = 10f;
    [SerializeField] private float brakingDistance = 5f;

    [Header("Диагностика")]
    [SerializeField] private bool enableDebugLogs = true;

    private Rigidbody2D rb;
    private Vector3 targetPoint;
    private bool hasTarget;
    private WolfGait currentGait = WolfGait.BipedalWalk;
    private float stepCooldown;
    private float lastLogTime;
    private float logInterval = 1f;

    public WolfGait CurrentGait
    {
        get => currentGait;
        set
        {
            if (currentGait != value)
            {
                if (enableDebugLogs)
                    Debug.Log($"[WolfMovement] {name}: смена аллюра {currentGait} → {value}");
                currentGait = value;
            }
        }
    }

    public Vector2 Velocity => rb.velocity;
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
        rb = GetComponent<Rigidbody2D>();

        // НАСТРОЙКА ФИЗИКИ
        rb.mass = mass;
        rb.drag = groundDrag;
        rb.angularDrag = angularDrag;
        rb.gravityScale = 0;

        // ВАЖНО: Не замораживаем вращение, чтобы AddTorque работал
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Оставляем, но Torque будет работать по-другому

        // Альтернатива: использовать AddForce для поворота
        // rb.constraints = RigidbodyConstraints2D.None;

        if (enableDebugLogs)
            Debug.Log($"[WolfMovement] {name}: физика инициализирована (mass={mass}, drag={groundDrag}, maxSpeed={maxSpeed})");
    }

    void Start()
    {
        // Дополнительная проверка после старта
        if (rb == null)
        {
            Debug.LogError($"[WolfMovement] {name}: Rigidbody2D не найден!");
        }
    }

    public void SetTargetZone(Vector3 point, float radius)
    {
        targetPoint = point;
        zoneRadius = radius;
        hasTarget = true;

        if (enableDebugLogs)
            Debug.Log($"[WolfMovement] {name}: установлена новая цель ({point.x:F1}, {point.y:F1}), радиус={radius:F1}, дистанция={Vector3.Distance(transform.position, point):F1}");
    }

    void FixedUpdate()
    {
        if (!hasTarget) return;

        stepCooldown -= Time.fixedDeltaTime;

        Vector3 toTarget = targetPoint - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 directionToTarget = toTarget.normalized;

        bool needToMove = distanceToTarget > zoneRadius;

        // Периодическое логирование
        if (enableDebugLogs && Time.time - lastLogTime >= logInterval)
        {
            lastLogTime = Time.time;
            Debug.Log($"[WolfMovement] {name}: дист={distanceToTarget:F1}, needToMove={needToMove}, скорость={rb.velocity.magnitude:F1}, аллюр={currentGait}, impulseCD={stepCooldown:F2}");
        }

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
            if (enableDebugLogs && distanceToTarget < zoneRadius * 0.5f && rb.velocity.magnitude > 0.5f)
            {
                Debug.Log($"[WolfMovement] {name}: достиг зоны цели! (дист={distanceToTarget:F1} <= {zoneRadius:F1})");
            }
            Brake(distanceToTarget);
        }

        // Ограничение скорости
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }

        // Диагностика: если скорость 0 но нужно двигаться
        if (enableDebugLogs && needToMove && rb.velocity.magnitude < 0.1f && Time.frameCount % 60 == 0)
        {
            Debug.LogWarning($"[WolfMovement] {name}: скорость = 0, но нужно двигаться! Проверьте импульс или коллизии.");
        }
    }

    void ApplySteering(Vector3 directionToTarget)
    {
        float angle = AngleToTarget;
        float turnTorque = GetTurnTorque();
        float speedFactor = 1f / (1f + rb.velocity.magnitude * 0.3f);
        float effectiveTorque = turnTorque * speedFactor;

        // Используем AddForce для поворота вместо AddTorque (работает даже с FreezeRotation)
        Vector3 torqueDirection = new Vector3(-Mathf.Sin(transform.eulerAngles.z * Mathf.Deg2Rad),
                                               Mathf.Cos(transform.eulerAngles.z * Mathf.Deg2Rad), 0);
        rb.AddForce(torqueDirection * angle * effectiveTorque * Time.fixedDeltaTime * 0.1f, ForceMode2D.Force);

        // Альтернатива: вращать трансформ напрямую
        // float turnAmount = Mathf.Clamp(angle * turnTorque * Time.fixedDeltaTime, -maxTurnSpeed, maxTurnSpeed);
        // transform.Rotate(0, 0, turnAmount);

        if (enableDebugLogs && Mathf.Abs(angle) > 30f && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[WolfMovement] {name}: поворот, угол={angle:F1}°, момент={effectiveTorque:F1}");
        }
    }

    void ApplyStepImpulse(Vector3 directionToTarget, float distanceToTarget)
    {
        float impulse = GetStepImpulse();
        Vector3 impulseDirection = IsoForward;

        if (currentGait == WolfGait.QuadrupedalLeap)
        {
            impulseDirection = (IsoForward + Vector3.up * leapUpwardRatio).normalized;
        }

        // Торможение при приближении к цели
        float brakingFactor = Mathf.Clamp01((distanceToTarget - zoneRadius) / brakingDistance);
        float originalImpulse = impulse;
        impulse *= brakingFactor;

        // Не добавляем импульс если уже быстро движемся к цели
        float speedTowardsTarget = Vector2.Dot(rb.velocity, directionToTarget);
        if (speedTowardsTarget > 0)
        {
            float speedFactor = 1f - Mathf.Clamp01(speedTowardsTarget / maxSpeed);
            impulse *= speedFactor;
        }

        // Минимальный импульс, чтобы сдвинуться с места
        if (impulse < 100f && rb.velocity.magnitude < 1f)
        {
            impulse = 200f;
            if (enableDebugLogs)
                Debug.Log($"[WolfMovement] {name}: минимальный импульс для старта = {impulse}");
        }

        // ПРИМЕНЯЕМ СИЛУ
        rb.AddForce((Vector2)impulseDirection * impulse, ForceMode2D.Impulse);

        if (enableDebugLogs && Time.frameCount % 10 == 0)
        {
            Debug.Log($"[WolfMovement] {name}: шаг {currentGait}, импульс={impulse:F1} (было={originalImpulse:F1}), новая скорость={rb.velocity.magnitude:F1}");
        }
    }

    void Brake(float distanceToTarget)
    {
        if (rb.velocity.magnitude > 2f && distanceToTarget < zoneRadius * 0.5f)
        {
            float brakeForce = 5f; // Увеличено с 2
            rb.AddForce(-rb.velocity.normalized * brakeForce, ForceMode2D.Force);

            if (enableDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[WolfMovement] {name}: торможение (дист={distanceToTarget:F1}, скорость={rb.velocity.magnitude:F1})");
            }
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