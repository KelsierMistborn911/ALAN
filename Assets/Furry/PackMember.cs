using UnityEngine;
using System.Collections.Generic;

public class PackMember : MonoBehaviour, IDamageable
{
    [Header("Параметры")]
    [SerializeField] private float maxHealth = 100f;

    // ===== Компоненты =====
    private PackMemberMovement movement;
    private PackMemberVisual visual;
    private Transform player;

    // ===== Данные от PackFormation =====
    public PackManager.TacticalRole CurrentRole { get; private set; } = PackManager.TacticalRole.Unassigned;
    public PackFormation.WeightSet Weights { get; private set; }
    public PackFormation.Sector Sector { get; private set; }

    // ===== Контекст (обновляется из PackFormation) =====
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

    // ===== Инициализация =====
    private void Awake()
    {
        movement = GetComponent<PackMemberMovement>();
        visual = GetComponent<PackMemberVisual>();

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

    // ===== Установка данных от PackFormation =====
    public void SetTacticalRole(PackManager.TacticalRole role)
    {
        CurrentRole = role;
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

    /// <summary>
    /// Обновляет контекст: позиции Alpha, игрока и всех союзников.
    /// Вызывается из PackFormation после пересчёта секторов.
    /// </summary>
    public void UpdateContext(Vector2 alphaPos, Vector2 playerPos, List<Vector2> allies)
    {
        alphaPosition = alphaPos;
        playerPosition = playerPos;
        allyPositions = allies;

        if (movement != null)
            movement.UpdateContext(alphaPos, playerPos, allies);
    }

    // ===== Здоровье =====
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

        // === ПОКАЗАТЬ ЦИФРУ УРОНА ===
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

        OnDied?.Invoke(this);
        Destroy(gameObject);
    }
}
