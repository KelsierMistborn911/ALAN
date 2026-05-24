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
    private float roleReassignInterval = 6f;
    private float lastRoleReassignTime;
    private int loopIteration;

    void Start()
    {
        tactics = GetComponent<WolfTactics>();
        Debug.Log($"[WolfStrategy] Стратегия инициализирована, частота обновления={loopTick}с");
    }

    public void ProcessDetection(Transform target, List<WolfEnemy> packMembers, List<WolfMovement> packMovements,
                                 float encirclementRadius, float minDistanceBetweenWolves)
    {
        Debug.Log($"[WolfStrategy] 🔥 ЗАПУСК СТРАТЕГИИ СТАИ! Цель: {target.name}");

        if (activeLoop != null)
        {
            Debug.Log($"[WolfStrategy] Останавливаем предыдущий цикл стратегии");
            StopCoroutine(activeLoop);
            activeLoop = null;
        }

        this.target = target;
        this.packMembers = packMembers;
        this.packMovements = packMovements;
        this.encirclementRadius = encirclementRadius;
        this.minDistanceBetweenWolves = minDistanceBetweenWolves;
        lastRoleReassignTime = Time.time;
        loopIteration = 0;

        activeLoop = StartCoroutine(StrategyLoop());
    }

    IEnumerator StrategyLoop()
    {
        isLoopRunning = true;
        Debug.Log($"[WolfStrategy] Цикл стратегии запущен");

        while (isLoopRunning && target != null && packMembers != null && packMembers.Count > 0)
        {
            loopIteration++;

            bool allDead = true;
            int aliveCount = 0;
            foreach (var wolf in packMembers)
            {
                if (wolf != null)
                {
                    allDead = false;
                    aliveCount++;
                }
            }

            if (allDead)
            {
                Debug.Log($"[WolfStrategy] ❌ ВСЕ ВОЛКИ МЕРТВЫ! Останавливаем стратегию");
                break;
            }

            bool shouldReassignRoles = Time.time - lastRoleReassignTime >= roleReassignInterval;

            if (shouldReassignRoles)
            {
                Debug.Log($"[WolfStrategy] 🔄 Перераспределение ролей (интервал {roleReassignInterval}с)");
            }

            tactics.Execute(target, packMembers, packMovements, encirclementRadius, minDistanceBetweenWolves, shouldReassignRoles);

            if (shouldReassignRoles)
            {
                lastRoleReassignTime = Time.time;
            }

            if (loopIteration % 20 == 0) // Лог каждые 10 секунд (loopTick=0.5 => 20 итераций = 10с)
            {
                Debug.Log($"[WolfStrategy] 📊 Цикл #{loopIteration}: цель={target.name}, живых волков={aliveCount}");
            }

            yield return new WaitForSeconds(loopTick);
        }

        Debug.Log($"[WolfStrategy] Цикл стратегии завершен (всего итераций={loopIteration})");
        isLoopRunning = false;
        activeLoop = null;
    }

    public void StopStrategy()
    {
        Debug.Log($"[WolfStrategy] Остановка стратегии по запросу");
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