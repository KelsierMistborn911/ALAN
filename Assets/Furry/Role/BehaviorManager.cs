using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BehaviorManager : MonoBehaviour
{
    [Header("Настройки менеджера")]
    [SerializeField] private bool enableDebugLogs = true;

    private PackMember packMember;
    private List<BaseBehavior> availableBehaviors = new List<BaseBehavior>();
    private BaseBehavior currentBehavior;
    private BaseBehavior defaultBehavior;

    public BaseBehavior CurrentBehavior => currentBehavior;
    public event System.Action<BaseBehavior, BaseBehavior> OnBehaviorChanged;

    private void Awake()
    {
        packMember = GetComponent<PackMember>();
        if (packMember == null)
        {
            Debug.LogError($"{name}: PackMember не найден!");
            return;
        }

        // Собираем все поведения на объекте
        availableBehaviors = GetComponents<BaseBehavior>().ToList();

        // Отключаем все поведения по умолчанию
        foreach (var behavior in availableBehaviors)
        {
            behavior.enabled = false;
            behavior.Initialize(packMember);
        }

        if (enableDebugLogs)
            Debug.Log($"{name}: Найдено {availableBehaviors.Count} поведений");
    }

    private void Update()
    {
        if (currentBehavior != null && currentBehavior.IsActive)
        {
            currentBehavior.UpdateBehavior();
        }
    }

    public bool SwitchBehavior(string behaviorName)
    {
        var newBehavior = availableBehaviors.Find(b => b.BehaviorName == behaviorName);
        if (newBehavior == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"{name}: Поведение '{behaviorName}' не найдено!");
            return false;
        }

        return SwitchBehavior(newBehavior);
    }

    public bool SwitchBehavior(BaseBehavior newBehavior)
    {
        if (newBehavior == currentBehavior) return true;

        if (!availableBehaviors.Contains(newBehavior))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"{name}: Поведение {newBehavior.BehaviorName} не зарегистрировано!");
            return false;
        }

        if (currentBehavior != null)
        {
            currentBehavior.OnBehaviorDeactivated();
            if (enableDebugLogs)
                Debug.Log($"{packMember.name}: Деактивировано {currentBehavior.BehaviorName}");
        }

        currentBehavior = newBehavior;
        currentBehavior.OnBehaviorActivated();

        OnBehaviorChanged?.Invoke(currentBehavior, newBehavior);

        if (enableDebugLogs)
            Debug.Log($"{packMember.name}: Активировано {currentBehavior.BehaviorName}");

        return true;
    }

    public void SetDefaultBehavior(BaseBehavior behavior)
    {
        defaultBehavior = behavior;
    }

    public void ReturnToDefault()
    {
        if (defaultBehavior != null)
            SwitchBehavior(defaultBehavior);
    }

    public T GetBehavior<T>() where T : BaseBehavior
    {
        return availableBehaviors.OfType<T>().FirstOrDefault();
    }
}
