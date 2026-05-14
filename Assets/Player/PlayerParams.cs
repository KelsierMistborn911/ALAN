using UnityEngine;

public class PlayerParams : MonoBehaviour
{
    [Header("Масса")]
    [Tooltip("Игровая масса. Влияет на инерцию, занос и силу удара.")]
    [SerializeField] private float _mass = 1f;
    public float Mass
    {
        get => _mass;
        set => _mass = Mathf.Max(value, 0.1f);
    }

    [Header("Слоты оружия")]
    [SerializeField] private Weapon _rightHandWeapon;   // меч
    [SerializeField] private Weapon _leftHandWeapon;    // щит

    // Свойства для доступа из CombatController
    public Weapon RightHandWeapon => _rightHandWeapon;
    public Weapon LeftHandWeapon => _leftHandWeapon;
}
