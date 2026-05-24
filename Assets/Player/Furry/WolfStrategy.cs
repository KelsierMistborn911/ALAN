using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class WolfStrategy : MonoBehaviour
{
    private WolfTactics tactics;
    private Transform target;
    private List<WolfEnemy> packMembers;
    private List<WolfMovement> packMovements;
    private float encirclementRadius;
    private float minDistanceBetweenWolves;
    private Coroutine activeLoop;
    private bool isLoopRunning;
    private float loopTick = 0.5f;
    private float roleReassignInterval = 6f; // Интервал перераспределения ролей
    private float lastRoleReassignTime;

    void Start()
    {
        tactics = GetComponent<WolfTactics>();
    }

    public void ProcessDetection(Transform target, List<WolfEnemy> packMembers, List<WolfMovement> packMovements,
                                 float encirclementRadius, float minDistanceBetweenWolves)
    {
        if (activeLoop != null)
        {
            StopCoroutine(activeLoop);
            activeLoop = null;
        }

        this.target = target;
        this.packMembers = packMembers;
        this.packMovements = packMovements;
        this.encirclementRadius = encirclementRadius;
        this.minDistanceBetweenWolves = minDistanceBetweenWolves;
        lastRoleReassignTime = Time.time; // Первое распределение сразу

        activeLoop = StartCoroutine(StrategyLoop());
    }

    IEnumerator StrategyLoop()
    {
        isLoopRunning = true;

        while (isLoopRunning && target != null && packMembers != null && packMembers.Count > 0)
        {
            // Проверка на смерть всей стаи
            bool allDead = true;
            foreach (var wolf in packMembers)
            {
                if (wolf != null) { allDead = false; break; }
            }
            if (allDead) break;

            // Проверяем, нужно ли перераспределить роли
            bool shouldReassignRoles = Time.time - lastRoleReassignTime >= roleReassignInterval;

            // Единственный вызов тактики
            tactics.Execute(target, packMembers, packMovements, encirclementRadius, minDistanceBetweenWolves, shouldReassignRoles);

            if (shouldReassignRoles)
            {
                lastRoleReassignTime = Time.time;
            }

            yield return new WaitForSeconds(loopTick);
        }

        isLoopRunning = false;
        activeLoop = null;
    }

    public void StopStrategy()
    {
        isLoopRunning = false;
        if (activeLoop != null)
        {
            StopCoroutine(activeLoop);
            activeLoop = null;
        }
        target = null;
        packMembers = null;
        packMovements = null;
    }

    void OnDisable()
    {
        StopStrategy();
    }
}
