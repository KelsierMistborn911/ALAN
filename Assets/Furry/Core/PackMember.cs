using UnityEngine;
using System.Collections.Generic;

public class PackMember : MonoBehaviour, IDamageable
{
    [Header("Параметры")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Роль")]
    [SerializeField] private bool isPermanentAlpha = false;

    // ===== Компоненты =====
    private PackMemberMovement movement;
    private PackMemberVisual visual;
    private BehaviorManager behaviorManager;
    private Transform player;

    // ===== Данные от PackFormation =====
    public PackManager.TacticalRole CurrentRole { get; private set; } = PackManager.TacticalRole.Unassigned;
    public PackFormation.WeightSet Weights { get; private set; }
    public PackFormation.Sector Sector { get; private set; }

    // ===== Контекст =====
    private Vector2 alphaPosition;
    private Vector2 playerPosition;
    private List<Vector2> allyPositions;

    // ===== Состояние =====
    private float currentHealth;
    private bool isDead = false;

    // ===== События =====
    public System.Action<PackMember> OnDied;
    public System.Action<PackMember> OnHealthChanged;

    // ===== Свойства =====
    public bool IsDead => isDead;
    public Vector2 AlphaPosition => alphaPosition;
    public Vector2 PlayerPosition => playerPosition;
    public List<Vector2> AllyPositions => allyPositions;
    public bool IsPermanentAlpha => isPermanentAlpha;


    private void Awake()
    {
        movement = GetComponent<PackMemberMovement>();
        visual = GetComponent<PackMemberVisual>();
        behaviorManager = GetComponent<BehaviorManager>();

        if (movement == null)
            Debug.LogWarning($"PackMember {name}: PackMemberMovement не найден!");
        if (visual == null)
            Debug.LogWarning($"PackMember {name}: PackMemberVisual не найден!");
    }

    private void Start()
    {
        currentHealth = maxHealth;

        PackManager packManager = FindObjectOfType<PackManager>();
        if (packManager != null)
        {
            packManager.RegisterMember(this);
        }
        else
        {
            Debug.LogWarning($"PackMember {name}: PackManager не найден на сцене!");
        }
    }

    public void SetTacticalRole(PackManager.TacticalRole role)
    {
        CurrentRole = role;

        // Активируем соответствующее поведение через BehaviorManager
        if (behaviorManager != null)
        {
            switch (role)
            {
                case PackManager.TacticalRole.Alpha:
                    behaviorManager.SwitchBehavior("Alpha");
                    break;
                case PackManager.TacticalRole.FlankerLeft:
                case PackManager.TacticalRole.FlankerRight:
                    behaviorManager.SwitchBehavior("Flanker");
                    break;
                case PackManager.TacticalRole.Harasser:
                    behaviorManager.SwitchBehavior("Harasser");
                    break;
                default:
                    behaviorManager.SwitchBehavior("Encircle");
                    break;
            }
        }
    }

    public void SetPotentialWeights(PackFormation.WeightSet weights)
    {
        Weights = weights;
        if (movement != null)
            movement.SetWeights(weights);
    }

    public void SetSector(PackFormation.Sector sector)
    {
        Sector = sector;
        if (movement != null)
            movement.SetSector(sector);
    }

    public void SetPlayer(Transform playerTransform)
    {
        player = playerTransform;
        if (visual != null)
            visual.SetPlayer(playerTransform);
    }

    public void UpdateContext(Vector2 alphaPos, Vector2 playerPos, List<Vector2> allies)
    {
        alphaPosition = alphaPos;
        playerPosition = playerPos;
        allyPositions = allies;

        if (movement != null)
            movement.UpdateContext(alphaPos, playerPos, allies);
    }

    public PackManager.HealthStatus GetHealthStatus()
    {
        if (isDead) return PackManager.HealthStatus.Dead;
        if (currentHealth < maxHealth * 0.5f) return PackManager.HealthStatus.Wounded;
        return PackManager.HealthStatus.Healthy;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(this);

        // Используем существующую систему отображения урона
        CombatController combat = FindObjectOfType<CombatController>();
        if (combat != null)
        {
            combat.ShowDamageNumber(amount, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        if (movement != null)
            movement.StopMoving();

        if (behaviorManager != null)
            behaviorManager.enabled = false;

        OnDied?.Invoke(this);

        // Добавляем эффект смерти (опционально)
        var deathEffect = GetComponent<ParticleSystem>();
        if (deathEffect != null)
            deathEffect.Play();

        Destroy(gameObject, 0.5f);
    }
}
