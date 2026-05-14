// Health.cs
using UnityEngine;
using System;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool showDamageNumbers = true;

    public float CurrentHealth { get; private set; }
    public event Action<float> OnDamaged;
    public event Action OnDeath;

    private void Start()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHealth <= 0) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        // Показываем цифры урона
        if (showDamageNumbers)
        {
            CombatController combat = FindObjectOfType<CombatController>();
            if (combat != null)
            {
                combat.ShowDamageNumber(amount, transform.position);
            }
        }

        OnDamaged?.Invoke(amount);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }
}
