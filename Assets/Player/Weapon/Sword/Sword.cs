using UnityEngine;

public class Sword : Weapon
{
    [SerializeField] private GameObject swordAttackPrefab; // префаб с областью атаки
    [SerializeField] private float attackDuration = 0.15f;

    public override void Attack(float hitForce, Vector2 origin, Vector2 direction)
    {
        if (swordAttackPrefab == null)
        {
            Debug.LogWarning("SwordAttackPrefab не назначен!");
            return;
        }

        // Создаём объект атаки
        GameObject attackObj = Instantiate(swordAttackPrefab, origin, Quaternion.identity);

        // Поворачиваем в сторону атаки
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        attackObj.transform.rotation = Quaternion.Euler(0, 0, angle);

        // Передаём параметры урона
        SwordAttackArea attackArea = attackObj.GetComponent<SwordAttackArea>();
        if (attackArea != null)
        {
            attackArea.Initialize(baseDamage, hitForce);
        }

        // Уничтожаем через время
        Destroy(attackObj, attackDuration);
    }
}
