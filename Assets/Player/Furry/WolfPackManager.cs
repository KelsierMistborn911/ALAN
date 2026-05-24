using UnityEngine;
using System.Collections.Generic;

public class WolfPackManager : MonoBehaviour
{
    public List<WolfEnemy> packMembers = new List<WolfEnemy>();
    public List<WolfMovement> packMovements = new List<WolfMovement>();
    public float encirclementRadius = 20f;
    public float minDistanceBetweenWolves = 5f;

    private WolfStrategy strategy;

    void Awake()
    {
        strategy = GetComponent<WolfStrategy>();
        FindAllWolves();
        Debug.Log($"[WolfPackManager] Менеджер стаи инициализирован");
    }

    void FindAllWolves()
    {
        WolfEnemy[] foundWolves = FindObjectsOfType<WolfEnemy>();

        packMembers.Clear();
        packMembers.AddRange(foundWolves);

        Debug.Log($"[WolfPackManager] Найдено {packMembers.Count} волков в стае");

        foreach (var wolf in packMembers)
        {
            Debug.Log($"[WolfPackManager]   - {wolf.name} (special={wolf.isSpecial})");
        }

        CacheMovements();
    }

    void CacheMovements()
    {
        packMovements.Clear();
        foreach (var wolf in packMembers)
        {
            if (wolf != null)
            {
                WolfMovement movement = wolf.GetComponent<WolfMovement>();
                if (movement != null)
                {
                    packMovements.Add(movement);
                    Debug.Log($"[WolfPackManager] Кэширован Movement для {wolf.name}");
                }
                else
                    Debug.LogWarning($"[WolfPackManager] У волка {wolf.name} нет компонента WolfMovement");
            }
        }
    }

    public void OnPlayerDetected(Transform player)
    {
        Debug.Log($"[WolfPackManager] ⚠️ ИГРОК ОБНАРУЖЕН! Позиция: {player.position}");
        FindAllWolves();

        strategy.ProcessDetection(player, packMembers, packMovements, encirclementRadius, minDistanceBetweenWolves);
    }
}