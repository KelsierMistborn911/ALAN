using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PackMemberMovement : MonoBehaviour
{
    [Header("Скачки (преследование)")]
    [SerializeField] private float leapInterval = 0.4f;        // Интервал между скачками
    [SerializeField] private float leapForce = 12f;            // Сила скачка
    [SerializeField] private float leapDuration = 0.15f;       // Длительность рывка

    [Header("Шаги (маневрирование)")]
    [SerializeField] private float stepInterval = 0.3f;        // Интервал между шагами
    [SerializeField] private float stepForce = 6f;             // Сила шага

    [Header("Торможение")]
    [SerializeField] private float friction = 4f;
    [SerializeField] private float stopThreshold = 0.1f;

    [Header("Скорости")]
    [SerializeField] private float maxLeapSpeed = 8f;
    [SerializeField] private float maxStepSpeed = 4f;

    [Header("Alpha — избегание")]
    [SerializeField] private float alphaComfortMin = 6f;
    [SerializeField] private float alphaComfortMax = 10f;
    [SerializeField] private float alphaDriftSpeed = 1.5f;
    [SerializeField] private float alphaDriftInterval = 3f;

    [Header("Отладка")]
    [SerializeField] private bool showDebug = true;

    // ===== Компоненты =====
    private Rigidbody2D rb;
    private PackMember packMember;
    private PackPathfinder pathfinder;

    // ===== Данные от PackFormation =====
    private PackFormation.WeightSet weights;
    private PackFormation.Sector sector;

    // ===== Контекст =====
    private Vector2 alphaPosition;
    private Vector2 playerPosition;
    private List<Vector2> allyPositions;

    // ===== Состояние движения =====
    public enum MovementMode
    {
        Leap,   // Скачки — преследование (харассеры)
        Step    // Шаги — маневрирование (фланкеры, альфа)
    }

    private MovementMode currentMode = MovementMode.Step;
    private float stepTimer;
    private float leapTimer;
    private bool isLeaping;
    private float leapEndTime;

    // ===== Состояние Alpha =====
    private float driftTimer;
    private float driftDirection = 1f;

    // ===== Свойства =====
    public bool IsMoving => rb.velocity.magnitude > 0.1f;
    public MovementMode CurrentMode => currentMode;

    // ===== Инициализация =====
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.angularDrag = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        packMember = GetComponent<PackMember>();
        pathfinder = GetComponent<PackPathfinder>();

        stepTimer = stepInterval;
        leapTimer = leapInterval;
    }

    // ===== Установка данных =====
    public void SetWeights(PackFormation.WeightSet w) => weights = w;
    public void SetSector(PackFormation.Sector s) => sector = s;

    public void UpdateContext(Vector2 alphaPos, Vector2 playerPos, List<Vector2> allies)
    {
        alphaPosition = alphaPos;
        playerPosition = playerPos;
        allyPositions = allies;
    }

    /// <summary>Переключить режим движения</summary>
    public void SetMovementMode(MovementMode mode)
    {
        currentMode = mode;
    }

    // ===== Физика =====
    private void FixedUpdate()
    {
        if (packMember.IsDead || packMember.CurrentRole == PackManager.TacticalRole.Unassigned)
        {
            ApplyFriction();
            return;
        }

        // Получаем целевую точку от Pathfinder
        Vector2 targetPosition = GetTargetPosition();
        Vector2 myPos = transform.position;
        Vector2 toTarget = targetPosition - myPos;
        float distToTarget = toTarget.magnitude;

        if (distToTarget < 0.2f)
        {
            ApplyFriction();
            return;
        }

        Vector2 moveDirection = toTarget.normalized;

        switch (currentMode)
        {
            case MovementMode.Leap:
                UpdateLeapMovement(moveDirection);
                break;
            case MovementMode.Step:
                UpdateStepMovement(moveDirection);
                break;
        }

        // Разделение с сородичами
        ApplySeparation();

        // Боковое трение
        ApplyDirectionalFriction(moveDirection);
    }

    private void UpdateLeapMovement(Vector2 direction)
    {
        if (isLeaping)
        {
            if (Time.time >= leapEndTime)
                isLeaping = false;
            return;
        }

        leapTimer += Time.fixedDeltaTime;
        if (leapTimer >= leapInterval)
        {
            leapTimer = 0f;
            StartLeap(direction);
        }
    }

    private void StartLeap(Vector2 direction)
    {
        isLeaping = true;
        leapEndTime = Time.time + leapDuration;

        rb.AddForce(direction * leapForce, ForceMode2D.Impulse);

        // Ограничение скорости
        if (rb.velocity.magnitude > maxLeapSpeed)
            rb.velocity = rb.velocity.normalized * maxLeapSpeed;

        if (showDebug)
            Debug.Log($"{name}: СКАЧОК! Сила: {leapForce}, Направление: {direction}");
    }

    private void UpdateStepMovement(Vector2 direction)
    {
        stepTimer += Time.fixedDeltaTime;

        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;
            rb.AddForce(direction * stepForce, ForceMode2D.Impulse);

            // Ограничение скорости
            if (rb.velocity.magnitude > maxStepSpeed)
                rb.velocity = rb.velocity.normalized * maxStepSpeed;
        }
    }

    private Vector2 GetTargetPosition()
    {
        // Используем Pathfinder для расчёта позиции
        if (pathfinder != null)
        {
            return pathfinder.GetTargetPosition(
                playerPosition,
                alphaPosition,
                allyPositions,
                sector,
                packMember.CurrentRole
            );
        }

        // Запасной вариант — центр сектора
        float midAngle = (sector.minAngle + sector.maxAngle) / 2f * Mathf.Deg2Rad;
        float midRadius = (sector.minRadius + sector.maxRadius) / 2f;
        return playerPosition + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * midRadius;
    }

    private void ApplySeparation()
    {
        if (allyPositions == null || weights.awayFromAllies <= 0) return;

        Vector2 myPos = transform.position;
        foreach (var allyPos in allyPositions)
        {
            float dist = Vector2.Distance(myPos, allyPos);
            if (dist < 2f && dist > 0.01f)
            {
                Vector2 awayFromAlly = (myPos - allyPos) / dist;
                rb.AddForce(awayFromAlly * weights.awayFromAllies, ForceMode2D.Force);
            }
        }
    }

    private void ApplyDirectionalFriction(Vector2 forwardDir)
    {
        Vector2 forwardVelocity = Vector2.Dot(rb.velocity, forwardDir) * forwardDir;
        Vector2 sideDir = new Vector2(-forwardDir.y, forwardDir.x);
        Vector2 sideVelocity = Vector2.Dot(rb.velocity, sideDir) * sideDir;

        // Боковое трение сильное
        sideVelocity = Vector2.Lerp(sideVelocity, Vector2.zero, friction * Time.fixedDeltaTime);

        rb.velocity = forwardVelocity + sideVelocity;
    }

    private void ApplyFriction()
    {
        Vector2 velocity = rb.velocity;
        if (velocity.sqrMagnitude < stopThreshold * stopThreshold)
        {
            rb.velocity = Vector2.zero;
        }
        else
        {
            Vector2 frictionForce = -velocity.normalized * friction * Time.fixedDeltaTime;
            if (frictionForce.sqrMagnitude > velocity.sqrMagnitude)
                rb.velocity = Vector2.zero;
            else
                rb.AddForce(frictionForce, ForceMode2D.Impulse);
        }
    }

    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
        isLeaping = false;
    }

    // ===== Gizmos =====
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        if (playerPosition != Vector2.zero)
        {
            Gizmos.color = new Color(0, 1, 0, 0.4f);
            float midAngle = (sector.minAngle + sector.maxAngle) / 2f * Mathf.Deg2Rad;
            float midRadius = (sector.minRadius + sector.maxRadius) / 2f;
            Vector2 midPoint = playerPosition + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * midRadius;
            Gizmos.DrawWireSphere(midPoint, 0.5f);
        }

        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = currentMode == MovementMode.Leap ? Color.red : Color.yellow;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized);
        }
    }
}
