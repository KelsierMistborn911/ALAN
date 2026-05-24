using UnityEngine;
using System.Collections.Generic;

public class WolfTactics : MonoBehaviour
{
    [Header("Дистанции")]
    [SerializeField] private float zoneRadius = 3f;
    [SerializeField] private float tooCloseDistance = 4f;
    [SerializeField] private float tooFarDistance = 45f;
    [SerializeField] private float encirclementDistance = 25f;
    [SerializeField] private float alphaReleaseDistance = 50f;

    [Header("Альфа-волк")]
    [SerializeField] private float alphaStandoffDistance = 30f;  // Дистанция, на которой альфа держится от цели
    [SerializeField] private float alphaRearOffset = 15f;        // Насколько альфа сзади относительно направления атаки

    [Header("Распределение ролей")]
    [SerializeField] private int rearPursuers = 3;
    [SerializeField] private int rightFlankers = 3;
    [SerializeField] private int leftFlankers = 3;

    [Header("Аллюры")]
    [SerializeField] private float farDistance = 15f;
    [SerializeField] private float midDistance = 5f;

    [Header("Разделение")]
    [SerializeField] private float separationWeight = 0.6f;
    [SerializeField] private float minDistanceBetweenWolves = 5f;

    private Transform target;
    private List<WolfEnemy> packMembers;
    private List<WolfMovement> packMovements;
    private WolfEnemy alpha;
    private float radius;
    private Vector3[] separationOffsets;

    private List<WolfEnemy> rearGroup = new List<WolfEnemy>();
    private List<WolfEnemy> rightGroup = new List<WolfEnemy>();
    private List<WolfEnemy> leftGroup = new List<WolfEnemy>();
    private List<WolfEnemy> escortGroup = new List<WolfEnemy>();
    private List<WolfEnemy> attackGroup = new List<WolfEnemy>();
    private List<WolfEnemy> rearFlankGroup = new List<WolfEnemy>();

    private Dictionary<WolfEnemy, Vector3> assignedPoints = new Dictionary<WolfEnemy, Vector3>();
    private Dictionary<WolfEnemy, Vector3> cachedRandomOffsets = new Dictionary<WolfEnemy, Vector3>();

    private bool rolesNeedReassign = true;

    public void Execute(Transform target, List<WolfEnemy> packMembers, List<WolfMovement> packMovements,
                        float radius, float minDistBetweenWolves, bool shouldReassignRoles)
    {
        this.target = target;
        this.packMembers = packMembers;
        this.packMovements = packMovements;
        this.radius = radius;
        this.minDistanceBetweenWolves = minDistBetweenWolves;

        if (separationOffsets == null || separationOffsets.Length != packMembers.Count)
        {
            separationOffsets = new Vector3[packMembers.Count];
        }

        assignedPoints.Clear();

        if (!ValidateConditions()) return;

        FindAlpha();
        Vector3 alphaPos = GetAlphaPosition();
        Vector3 dirToTarget = GetDirectionToTarget(alphaPos);
        float distToTarget = Vector3.Distance(alphaPos, target.position);

        if (shouldReassignRoles || rolesNeedReassign)
        {
            DistributeRoles(dirToTarget);
            UpdateRandomOffsets();
            rolesNeedReassign = false;
        }

        if (distToTarget <= tooCloseDistance)
        {
            StepBack(alphaPos, dirToTarget);
        }

        if (distToTarget >= tooFarDistance)
        {
            CloseIn(alphaPos, dirToTarget);
        }

        if (distToTarget > encirclementDistance)
        {
            MovePursuersToPositions(dirToTarget);
            MoveEscortAroundAlpha(alphaPos, dirToTarget);
        }

        if (distToTarget <= alphaReleaseDistance && escortGroup.Count > 0)
        {
            ReleaseEscortToRear(dirToTarget);
        }

        if (distToTarget <= encirclementDistance)
        {
            EncirclementFromRear(alphaPos, dirToTarget);
        }

        ApplyMovement();
        UpdateGait();
    }

    private bool ValidateConditions()
    {
        if (target == null) return false;
        if (packMembers == null || packMembers.Count == 0) return false;

        foreach (var w in packMembers)
        {
            if (w != null) return true;
        }
        return false;
    }

    private void FindAlpha()
    {
        alpha = null;
        foreach (var wolf in packMembers)
        {
            if (wolf != null && wolf.isSpecial)
            {
                alpha = wolf;
                return;
            }
        }

        foreach (var wolf in packMembers)
        {
            if (wolf != null) { alpha = wolf; return; }
        }
    }

    private Vector3 GetAlphaPosition()
    {
        if (alpha != null) return alpha.GetPosition();

        foreach (var wolf in packMembers)
        {
            if (wolf != null) return wolf.GetPosition();
        }
        return Vector3.zero;
    }

    private Vector3 GetDirectionToTarget(Vector3 alphaPos)
    {
        Vector3 dir = target.position - alphaPos;
        return dir.normalized;
    }

    private void StepBack(Vector3 alphaPos, Vector3 dirToTarget)
    {
        // Альфа отступает первым, но держит дистанцию
        Vector3 retreatPoint = target.position - dirToTarget * (alphaStandoffDistance + 5f);
        if (alpha != null)
            alpha.MoveToZone(retreatPoint, zoneRadius);

        foreach (var wolf in packMembers)
        {
            if (wolf == null || wolf == alpha) continue;

            Vector3 offset = GetCachedOffset(wolf);
            Vector3 point = retreatPoint + offset * 5f;
            point.z = 0;
            wolf.MoveToZone(point, zoneRadius);
        }
    }

    private void CloseIn(Vector3 alphaPos, Vector3 dirToTarget)
    {
        if (alpha != null)
        {
            Vector3 alphaPoint = target.position - dirToTarget * alphaStandoffDistance;
            alpha.MoveToZone(alphaPoint, zoneRadius);
        }
    }

    private void DistributeRoles(Vector3 dirToTarget)
    {
        rearGroup.Clear();
        rightGroup.Clear();
        leftGroup.Clear();
        escortGroup.Clear();
        attackGroup.Clear();
        rearFlankGroup.Clear();

        List<WolfEnemy> available = new List<WolfEnemy>();
        foreach (var wolf in packMembers)
        {
            if (wolf != null && wolf != alpha) available.Add(wolf);
        }

        for (int i = 0; i < rearPursuers && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            rearGroup.Add(available[index]);
            available.RemoveAt(index);
        }

        for (int i = 0; i < rightFlankers && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            rightGroup.Add(available[index]);
            available.RemoveAt(index);
        }

        for (int i = 0; i < leftFlankers && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            leftGroup.Add(available[index]);
            available.RemoveAt(index);
        }

        escortGroup.AddRange(available);
    }

    private void UpdateRandomOffsets()
    {
        cachedRandomOffsets.Clear();
        foreach (var wolf in packMembers)
        {
            if (wolf != null && wolf != alpha)
            {
                Vector2 randomCircle = Random.insideUnitCircle;
                cachedRandomOffsets[wolf] = new Vector3(randomCircle.x, randomCircle.y, 0);
            }
        }
    }

    private Vector3 GetCachedOffset(WolfEnemy wolf)
    {
        if (cachedRandomOffsets.TryGetValue(wolf, out Vector3 offset))
            return offset;

        Vector2 randomCircle = Random.insideUnitCircle;
        offset = new Vector3(randomCircle.x, randomCircle.y, 0);
        cachedRandomOffsets[wolf] = offset;
        return offset;
    }

    private void MovePursuersToPositions(Vector3 dirToTarget)
    {
        Vector3 right = new Vector3(dirToTarget.y, -dirToTarget.x, 0);
        Vector3 left = new Vector3(-dirToTarget.y, dirToTarget.x, 0);

        foreach (var wolf in rearGroup)
        {
            if (wolf == null) continue;
            Vector3 point = target.position - dirToTarget * (radius + 5f);
            assignedPoints[wolf] = point;
        }

        foreach (var wolf in rightGroup)
        {
            if (wolf == null) continue;
            Vector3 point = target.position + right * (radius + 5f);
            assignedPoints[wolf] = point;
        }

        foreach (var wolf in leftGroup)
        {
            if (wolf == null) continue;
            Vector3 point = target.position + left * (radius + 5f);
            assignedPoints[wolf] = point;
        }
    }

    private void MoveEscortAroundAlpha(Vector3 alphaPos, Vector3 dirToTarget)
    {
        for (int i = 0; i < escortGroup.Count; i++)
        {
            if (escortGroup[i] == null) continue;
            float angle = (360f / escortGroup.Count) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * 8f;
            assignedPoints[escortGroup[i]] = alphaPos + offset;
        }
    }

    private void ReleaseEscortToRear(Vector3 dirToTarget)
    {
        int releaseCount = Mathf.Min(3, escortGroup.Count);
        for (int i = 0; i < releaseCount; i++)
        {
            WolfEnemy wolf = escortGroup[0];
            escortGroup.RemoveAt(0);
            rearFlankGroup.Add(wolf);
        }

        Vector3 right = new Vector3(dirToTarget.y, -dirToTarget.x, 0);
        for (int i = 0; i < rearFlankGroup.Count; i++)
        {
            if (rearFlankGroup[i] == null) continue;
            float side = (i % 2 == 0) ? 1 : -1;
            Vector3 point = target.position - dirToTarget * (radius + 10f) + right * side * 10f;
            assignedPoints[rearFlankGroup[i]] = point;
        }
    }

    private void EncirclementFromRear(Vector3 alphaPos, Vector3 dirToTarget)
    {
        // Альфа держится в стороне
        if (alpha != null)
        {
            Vector3 alphaPoint = target.position - dirToTarget * alphaStandoffDistance;
            assignedPoints[alpha] = alphaPoint;
        }

        // Остальные волки окружают
        if (escortGroup.Count > 0)
        {
            Vector3 right = new Vector3(dirToTarget.y, -dirToTarget.x, 0);
            for (int i = 0; i < escortGroup.Count; i++)
            {
                if (escortGroup[i] == null) continue;
                float spread = (i - escortGroup.Count / 2f) * 8f;
                Vector3 point = target.position - dirToTarget * (radius + 5f) + right * spread;
                assignedPoints[escortGroup[i]] = point;
            }
        }
        else
        {
            DistributeWithRearFlank(dirToTarget);
        }
    }

    private void DistributeWithRearFlank(Vector3 dirToTarget)
    {
        List<WolfEnemy> allWolves = new List<WolfEnemy>();
        allWolves.AddRange(rearGroup);
        allWolves.AddRange(rightGroup);
        allWolves.AddRange(leftGroup);
        allWolves.AddRange(rearFlankGroup);

        for (int i = 0; i < allWolves.Count; i++)
        {
            if (allWolves[i] == null) continue;
            float angle = (360f / allWolves.Count) * i * Mathf.Deg2Rad;
            float dist = radius + Random.Range(-2f, 2f);
            Vector3 point = target.position + new Vector3(
                Mathf.Cos(angle) * dist,
                Mathf.Sin(angle) * dist,
                0
            );
            assignedPoints[allWolves[i]] = point;
        }
    }

    private void ApplyMovement()
    {
        CalculateSeparationOffsets();

        foreach (var kvp in assignedPoints)
        {
            WolfEnemy wolf = kvp.Key;
            if (wolf == null) continue;

            Vector3 point = kvp.Value;

            int index = packMembers.IndexOf(wolf);
            if (index >= 0 && index < separationOffsets.Length)
            {
                point += separationOffsets[index] * separationWeight;
            }

            wolf.MoveToZone(point, zoneRadius);
        }

        // Альфа НЕ получает дефолтную точку, если уже назначена специальная позиция
        if (alpha != null && !assignedPoints.ContainsKey(alpha))
        {
            // Только если альфе не назначена позиция в окружении
            Vector3 dirToTarget = GetDirectionToTarget(alpha.GetPosition());
            // Альфа держится сзади-сбоку
            Vector3 right = new Vector3(dirToTarget.y, -dirToTarget.x, 0);
            Vector3 alphaPoint = target.position - dirToTarget * alphaStandoffDistance + right * alphaRearOffset;
            alpha.MoveToZone(alphaPoint, zoneRadius);
        }
    }

    private void CalculateSeparationOffsets()
    {
        for (int i = 0; i < packMembers.Count; i++)
        {
            if (packMembers[i] == null)
            {
                separationOffsets[i] = Vector3.zero;
                continue;
            }

            Vector3 offset = Vector3.zero;

            for (int j = 0; j < packMembers.Count; j++)
            {
                if (i == j || packMembers[j] == null) continue;

                Vector3 dir = packMembers[i].GetPosition() - packMembers[j].GetPosition();
                float dist = dir.magnitude;

                if (dist < minDistanceBetweenWolves && dist > 0.001f)
                {
                    float strength = (minDistanceBetweenWolves - dist) / minDistanceBetweenWolves;
                    offset += dir.normalized * strength * minDistanceBetweenWolves;
                }
            }

            separationOffsets[i] = offset;
        }
    }

    void OnDrawGizmos()
    {
        if (target != null && alpha != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(target.position - GetDirectionToTarget(alpha.GetPosition()) * alphaStandoffDistance, 2f);
        }
    }

    private void UpdateGait()
    {
        for (int i = 0; i < packMembers.Count; i++)
        {
            if (packMembers[i] == null || packMovements[i] == null) continue;

            Vector3 targetPoint = packMembers[i].GetTargetPoint();
            float distanceToTarget = Vector3.Distance(packMembers[i].GetPosition(), targetPoint);

            if (distanceToTarget > farDistance)
                packMovements[i].CurrentGait = WolfGait.QuadrupedalLeap;
            else if (distanceToTarget > midDistance)
                packMovements[i].CurrentGait = WolfGait.BipedalRun;
            else
                packMovements[i].CurrentGait = WolfGait.BipedalWalk;
        }
    }
}
