using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [SerializeField] protected float baseDamage = 10f;
    [SerializeField] protected float weaponMass = 1f;
    public float WeaponMass => weaponMass;
    public virtual void Attack(float hitForce, Vector2 origin, Vector2 direction) { }
    public virtual void Block(float hitForce) { }
}
