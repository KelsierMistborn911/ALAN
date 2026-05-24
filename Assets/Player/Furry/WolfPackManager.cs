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

        // Автоматически находим всех волков на сцене
        FindAllWolves();
    }

    void FindAllWolves()
    {
        // Находим всех волков на сцене
        WolfEnemy[] foundWolves = FindObjectsOfType<WolfEnemy>();

        packMembers.Clear();
        packMembers.AddRange(foundWolves);

        Debug.Log($"WolfPackManager: Найдено {packMembers.Count} волков");

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
                    packMovements.Add(movement);
                else
                    Debug.LogWarning($"У волка {wolf.name} нет компонента WolfMovement");
            }
        }
    }

    public void OnPlayerDetected(Transform player)
    {
        // Обновляем список перед атакой (на случай если волки появились позже)
        FindAllWolves();

        strategy.ProcessDetection(player, packMembers, packMovements, encirclementRadius, minDistanceBetweenWolves);
    }
}
