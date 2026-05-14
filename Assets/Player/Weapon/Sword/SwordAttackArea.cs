// SwordAttackArea.cs
using UnityEngine;

public class SwordAttackArea : MonoBehaviour
{
    private float damage;
    private float hitForce;
    private bool hasHit;

    public void Initialize(float baseDamage, float force)
    {
        damage = baseDamage;
        hitForce = force;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            float totalDamage = damage + hitForce * 0.5f;
            target.TakeDamage(totalDamage);
            hasHit = true;

            // Находим CombatController и показываем урон
            CombatController combat = FindObjectOfType<CombatController>();
            if (combat != null)
            {
                combat.ShowDamageNumber(totalDamage, other.transform.position);
            }
        }
    }
}
