using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class PackMemberMovement : MonoBehaviour
{
    [Header("Скорости")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float maxForce = 15f;

    [Header("Alpha — избегание")]
    [SerializeField] private float alphaComfortMin = 6f;
    [SerializeField] private float alphaComfortMax = 10f;
    [SerializeField] private float alphaPanicMultiplier = 2f;
    [SerializeField] private float alphaDriftSpeed = 1.5f;
    [SerializeField] private float alphaDriftInterval = 3f;

    [Header("Harasser — заход за спину")]
    [SerializeField] private float harasserBehindForce = 1.2f;
    [SerializeField] private float harasserSpread = 60f;

    [Header("Движение")]
    [SerializeField] private float reachDistance = 0.3f;
    [SerializeField] private float slowdownDistance = 1f;

    [Header("Отладка")]
    [SerializeField] private bool showDebug = true;

    // ===== Компоненты =====
    private Rigidbody2D rb;
    private PackMember packMember;

    // ===== Данные от PackFormation =====
    private PackFormation.WeightSet weights;
    private PackFormation.Sector sector;

    // ===== Контекст =====
    private Vector2 alphaPosition;
    private Vector2 playerPosition;
    private List<Vector2> allyPositions;

    // ===== Состояние Alpha =====
    private float driftTimer;
    private float driftDirection = 1f;

    // ===== Свойства =====
    public bool IsMoving => rb.velocity.magnitude > 0.1f;

    // ===== Инициализация =====
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        packMember = GetComponent<PackMember>();
    }

    // ===== Установка данных =====
    public void SetWeights(PackFormation.WeightSet w)
    {
        weights = w;
    }

    public void SetSector(PackFormation.Sector s)
    {
        sector = s;
    }

    public void UpdateContext(Vector2 alphaPos, Vector2 playerPos, List<Vector2> allies)
    {
        alphaPosition = alphaPos;
        playerPosition = playerPos;
        allyPositions = allies;
    }

    // ===== Физика =====
    private void FixedUpdate()
    {
        if (packMember.IsDead || packMember.CurrentRole == PackManager.TacticalRole.Unassigned)
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.5f);
            return;
        }

        Vector2 totalForce = CalculatePotentialFields();
        totalForce = Vector2.ClampMagnitude(totalForce, maxForce);

        float maxSpeed = packMember.CurrentRole == PackManager.TacticalRole.Harasser ? runSpeed : walkSpeed;
        Vector2 desiredVelocity = totalForce.normalized * maxSpeed;

        // Плавное применение
        rb.velocity = Vector2.Lerp(rb.velocity, desiredVelocity, 0.3f);
    }

    private Vector2 CalculatePotentialFields()
    {
        Vector2 force = Vector2.zero;
        Vector2 myPos = transform.position;

        float distToPlayer = Vector2.Distance(myPos, playerPosition);
        Vector2 awayFromPlayer = distToPlayer > 0.01f ? (myPos - playerPosition) / distToPlayer : Vector2.up;

        // ===== ПРИОРИТЕТ 1: Держать дистанцию от игрока =====
        float minDist = sector.minRadius;

        if (distToPlayer < minDist)
        {
            float urgency = (minDist - distToPlayer) / minDist;
            force += awayFromPlayer * urgency * maxForce;
        }
        else if (distToPlayer > sector.maxRadius)
        {
            Vector2 towardPlayer = -awayFromPlayer;
            force += towardPlayer * weights.towardSector * 2f;
        }

        // ===== ПРИОРИТЕТ 2: Держаться в зоне =====
        if (packMember.CurrentRole == PackManager.TacticalRole.Harasser)
        {
            // Приоритет 1: дистанция (жёстко)
            if (distToPlayer < minDist)
            {
                float urgency = (minDist - distToPlayer) / minDist;
                force += awayFromPlayer * urgency * maxForce;
            }
            else
            {
                // Приоритет 2: зона (только если дистанция в норме)
                Vector2 targetInSector = GetTargetInSector();
                Vector2 towardTarget = targetInSector - myPos;
                float distToTarget = towardTarget.magnitude;

                if (distToTarget > reachDistance)
                {
                    towardTarget /= distToTarget;
                    force += towardTarget * weights.towardSector * 5f;
                }
            }
        }
        else
        {
            if (distToPlayer >= minDist)
            {
                Vector2 targetInSector = GetTargetInSector();
                Vector2 towardTarget = targetInSector - myPos;
                float distToTarget = towardTarget.magnitude;

                if (distToTarget > reachDistance)
                {
                    towardTarget /= distToTarget;
                    force += towardTarget * weights.towardSector * 2f;
                }
            }
        }

        // ===== ПРИОРИТЕТ 3: Разделение с сородичами =====
        if (weights.awayFromAllies > 0 && allyPositions != null)
        {
            foreach (var allyPos in allyPositions)
            {
                if (Vector2.Distance(myPos, allyPos) < 0.01f) continue;

                Vector2 awayFromAlly = myPos - allyPos;
                float distToAlly = awayFromAlly.magnitude;

                if (distToAlly < 2f && distToAlly > 0.01f)
                {
                    awayFromAlly /= distToAlly;
                    force += awayFromAlly * weights.awayFromAllies * 2f;
                }
            }
        }

        // ===== Alpha: дрейф =====
        if (packMember.CurrentRole == PackManager.TacticalRole.Alpha && distToPlayer >= minDist)
        {
            force += AlphaDriftForce(awayFromPlayer);
        }

        // ===== Демпфирование =====
        force -= rb.velocity * weights.damping;

        return force;
    }

    private Vector2 GetTargetInSector()
    {
        if (playerPosition == Vector2.zero)
            return (Vector2)transform.position;

        float angle = Random.Range(sector.minAngle, sector.maxAngle) * Mathf.Deg2Rad;
        float radius = Random.Range(sector.minRadius, sector.maxRadius);

        return playerPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private Vector2 AlphaDriftForce(Vector2 awayDir)
    {
        driftTimer += Time.fixedDeltaTime;
        if (driftTimer >= alphaDriftInterval)
        {
            driftTimer = 0f;
            driftDirection = Random.value > 0.5f ? 1f : -1f;
        }

        Vector2 perpDir = new Vector2(-awayDir.y, awayDir.x) * driftDirection;
        return perpDir * alphaDriftSpeed;
    }

    // ===== Остановка =====
    public void StopMoving()
    {
        rb.velocity = Vector2.zero;
    }

    // ===== Gizmos =====
    private void OnDrawGizmos()
    {
        if (!showDebug) return;

        // Сектор
        if (playerPosition != Vector2.zero)
        {
            Gizmos.color = new Color(0, 1, 0, 0.4f);
            float midAngle = (sector.minAngle + sector.maxAngle) / 2f * Mathf.Deg2Rad;
            float midRadius = (sector.minRadius + sector.maxRadius) / 2f;
            Vector2 midPoint = playerPosition + new Vector2(Mathf.Cos(midAngle), Mathf.Sin(midAngle)) * midRadius;
            Gizmos.DrawWireSphere(midPoint, 0.5f);
        }

        // Направление силы
        if (rb != null && rb.velocity.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rb.velocity.normalized);
        }
    }
}
