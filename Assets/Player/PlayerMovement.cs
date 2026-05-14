using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Шаги (импульсы)")]
    [SerializeField] private float stepInterval = 0.3f;        // интервал между шагами
    [SerializeField] private float stepForce = 8f;             // сила шага (УВЕЛИЧИЛ)
    [SerializeField] private float sprintStepMultiplier = 1.5f; // усиление шага на шифте

    [Header("Торможение")]
    [SerializeField] private float friction = 4f;              // общее трение (УМЕНЬШИЛ)
    [SerializeField] private float stopThreshold = 0.1f;       // порог остановки

    [Header("Рывки")]
    [SerializeField] private float longDashForce = 15f;        // длинный рывок (пробел)
    [SerializeField] private float longDashCooldown = 1.5f;
    [SerializeField] private float shortDashForce = 8f;        // короткий рывок (альт)
    [SerializeField] private float shortDashCooldown = 0.8f;
    [SerializeField] private float dashDuration = 0.15f;

    [Header("Занос (опционально)")]
    [SerializeField] private float sideFriction = 5f;

    [Header("Отладка")]
    [SerializeField] private bool showDebugInfo = true;

    private Rigidbody2D rb;
    private PlayerParams playerParams;
    private PlayerDirection playerDirection;

    private Vector2 movementInput;
    private float stepTimer;
    private float longDashTimer;
    private float shortDashTimer;
    private bool isDashing;
    private float dashEndTime;

    public bool IsMoving => movementInput.sqrMagnitude > 0.01f;
    public Vector2 MovementInput => movementInput;
    public bool IsDashing => isDashing;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerParams = GetComponent<PlayerParams>();
        playerDirection = GetComponent<PlayerDirection>();

        if (playerParams == null)
            Debug.LogError("PlayerParams не найден!");
        if (rb == null)
            Debug.LogError("Rigidbody2D не найден!");

        // Настройка Rigidbody2D для лучшего контроля
        rb.gravityScale = 0f;  // Отключаем гравитацию для top-down движения
        rb.drag = 0f;          // Мы управляем трением сами
        rb.angularDrag = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Плавное движение

        stepTimer = stepInterval; // Первый шаг сразу
    }

    void Update()
    {
        if (!isDashing)
        {
            movementInput.x = Input.GetAxisRaw("Horizontal");
            movementInput.y = Input.GetAxisRaw("Vertical");
            movementInput = movementInput.normalized;
        }

        if (playerDirection != null)
            playerDirection.SetMovementInput(movementInput);

        // Рывки
        if (!isDashing && movementInput.sqrMagnitude > 0.01f)
        {
            if (Input.GetKeyDown(KeyCode.Space) && longDashTimer <= 0f)
            {
                StartDash(longDashForce);
                longDashTimer = longDashCooldown;
            }
            else if (Input.GetKeyDown(KeyCode.LeftAlt) && shortDashTimer <= 0f)
            {
                StartDash(shortDashForce);
                shortDashTimer = shortDashCooldown;
            }
        }

        // Таймеры
        if (longDashTimer > 0f) longDashTimer -= Time.deltaTime;
        if (shortDashTimer > 0f) shortDashTimer -= Time.deltaTime;

        if (isDashing && Time.time >= dashEndTime)
        {
            isDashing = false;
        }

        // Отладка
        if (showDebugInfo && movementInput.sqrMagnitude > 0.01f)
        {
            Debug.Log($"Velocity: {rb.velocity.magnitude:F2}, StepTimer: {stepTimer:F2}, IsSprinting: {Input.GetKey(KeyCode.LeftShift)}");
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
            return;

        if (movementInput.sqrMagnitude > 0.01f)
        {
            // Система шагов
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);

            stepTimer += Time.fixedDeltaTime;

            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;

                // Учитываем массу
                float mass = playerParams != null ? playerParams.Mass : 1f;
                float currentStepForce = stepForce / mass; // Делим на массу

                if (isSprinting)
                    currentStepForce *= sprintStepMultiplier;

                // Применяем импульс
                rb.AddForce(movementInput * currentStepForce, ForceMode2D.Impulse);

                // Отладка шага
                if (showDebugInfo)
                {
                    Debug.Log($"STEP! Force: {currentStepForce:F2}, Direction: {movementInput}");
                }
            }

            // Боковое трение
            Vector2 forwardDir = movementInput;
            Vector2 sideDir = new Vector2(-forwardDir.y, forwardDir.x);
            float sideSpeed = Vector2.Dot(rb.velocity, sideDir);

            float massForFriction = playerParams != null ? playerParams.Mass : 1f;
            float currentSideFriction = sideFriction / massForFriction;

            sideSpeed = Mathf.MoveTowards(sideSpeed, 0f, currentSideFriction * Time.fixedDeltaTime);
            float forwardSpeed = Vector2.Dot(rb.velocity, forwardDir);

            // Не даём скорости упасть ниже нуля вперёд
            if (forwardSpeed < 0) forwardSpeed = 0;

            rb.velocity = forwardSpeed * forwardDir + sideSpeed * sideDir;
        }
        else
        {
            // Торможение
            stepTimer = stepInterval;

            Vector2 velocity = rb.velocity;
            if (velocity.sqrMagnitude < stopThreshold * stopThreshold)
            {
                rb.velocity = Vector2.zero;
            }
            else
            {
                // Плавное замедление через AddForce
                Vector2 frictionForce = -velocity.normalized * friction * Time.fixedDeltaTime;

                // Не даём трению развернуть скорость
                if (frictionForce.sqrMagnitude > velocity.sqrMagnitude)
                {
                    rb.velocity = Vector2.zero;
                }
                else
                {
                    rb.AddForce(frictionForce, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void StartDash(float force)
    {
        isDashing = true;
        dashEndTime = Time.time + dashDuration;

        float mass = playerParams != null ? playerParams.Mass : 1f;
        rb.velocity = movementInput * (force / mass); // Делим на массу

        if (showDebugInfo)
        {
            Debug.Log($"DASH! Force: {force}, Result speed: {rb.velocity.magnitude:F2}");
        }
    }

    public float LongDashCooldownRemaining => Mathf.Max(0f, longDashTimer);
    public float ShortDashCooldownRemaining => Mathf.Max(0f, shortDashTimer);
}
