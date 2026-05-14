using UnityEngine;

public class Shield : Weapon
{
    [SerializeField] private GameObject blockEffectPrefab; // префаб эффекта блока
    [SerializeField] private float blockDuration = 0.3f;   // длительность визуального эффекта

    public override void Block(float hitForce)
    {
        if (blockEffectPrefab == null)
        {
            Debug.LogWarning("BlockEffectPrefab не назначен!");
            return;
        }

        // Создаём объект эффекта на месте игрока (или сдвиньте по need)
        GameObject effect = Instantiate(blockEffectPrefab, transform.position, Quaternion.identity);
        // Можно привязать к игроку, чтобы следовал за ним при движении:
        // effect.transform.SetParent(transform);

        Destroy(effect, blockDuration);

        // Здесь можно вызвать событие триггера (например, через UnityEvent)
        BlockEffect blockFX = effect.GetComponent<BlockEffect>();
        if (blockFX != null)
        {
            blockFX.TriggerBlock();
        }

        // Дополнительно: временно дать игроку неуязвимость (опционально)
        // IDamageable playerDamageable = GetComponentInParent<IDamageable>();
        // if (playerDamageable != null) playerDamageable.SetInvulnerable(blockDuration);
    }
}
