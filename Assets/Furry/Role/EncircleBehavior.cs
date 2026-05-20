using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Поведение "Окружение" - группа окружает цель и атакует с разных сторон
/// </summary>
public class EncircleBehavior : BaseBehavior
{
    [Header("Настройки окружения")]
    [SerializeField] private float encircleRadius = 4f;
    [SerializeField] private float attackRadius = 2f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private float attackInterval = 2f;

    [Header("Атака")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float chargeSpeed = 8f;

    private float currentAngle;
    private float nextAttackTime;
    private bool isAttacking;
    private List<EncircleBehavior> groupMembers;

    public EncircleBehavior()
    {
        behaviorName = "Encircle";
    }

    public override void Initialize(PackMember member)
    {
        base.Initialize(member);
        FindGroupMembers();

        // Вычисляем уникальный угол для этого члена группы
        int memberIndex = groupMembers.IndexOf(this);
        float angleStep = 360f / Mathf.Max(1, groupMembers.Count);
        currentAngle = memberIndex * angleStep;
    }

    public override void UpdateBehavior()
    {
        if (playerTransform == null) return;

        if (!isAttacking)
        {
            UpdateEncirclePosition();

            // Периодическая атака
            if (Time.time >= nextAttackTime && IsPlayerInRange(attackRadius * 1.5f))
            {
                StartAttack();
            }
        }
        else
        {
            UpdateAttack();
        }
    }

    private void UpdateEncirclePosition()
    {
        // Плавно вращаемся вокруг игрока
        currentAngle += rotationSpeed * Time.deltaTime;
        if (currentAngle >= 360f) currentAngle -= 360f;

        Vector2 encirclePos = (Vector2)playerTransform.position +
                              new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                                         Mathf.Sin(currentAngle * Mathf.Deg2Rad)) * encircleRadius;

        MoveToPosition(encirclePos, 5f);

        // Держим дистанцию до других членов группы
        AvoidGroupMembers();
    }

    private void StartAttack()
    {
        isAttacking = true;
        nextAttackTime = Time.time + attackInterval;

        // Рывок к игроку
        Vector2 attackDir = GetDirectionToPlayer();
        rb.AddForce(attackDir * chargeSpeed, ForceMode2D.Impulse);
    }

    private void UpdateAttack()
    {
        if (IsPlayerInRange(attackRadius))
        {
            // Наносим урон
            var attackSprite = GetComponent<AttackSprite>();
            if (attackSprite != null)
                attackSprite.ShowAttack(GetDirectionToPlayer());

            playerCombat?.TakeDamage(attackDamage);

            isAttacking = false;

            // Отскок назад
            Vector2 retreatDir = -GetDirectionToPlayer();
            rb.AddForce(retreatDir * 5f, ForceMode2D.Impulse);
        }
        else if (Vector2.Distance(transform.position, playerTransform.position) > encircleRadius * 1.5f)
        {
            // Промахнулись - возвращаемся в строй
            isAttacking = false;
        }
    }

    private void AvoidGroupMembers()
    {
        foreach (var member in groupMembers)
        {
            if (member == this) continue;

            float dist = Vector2.Distance(transform.position, member.transform.position);
            if (dist < 1.5f)
            {
                Vector2 awayDir = (transform.position - member.transform.position).normalized;
                rb.AddForce(awayDir * 3f, ForceMode2D.Force);
            }
        }
    }

    private void FindGroupMembers()
    {
        groupMembers = new List<EncircleBehavior>();
        var allMembers = FindObjectsOfType<EncircleBehavior>();

        // Фильтруем членов одной стаи
        var packManager = FindObjectOfType<PackManager>();
        if (packManager != null)
        {
            foreach (var member in allMembers)
            {
                if (packManager.GetAllMembers().Contains(member.packMember))
                {
                    groupMembers.Add(member);
                }
            }
        }
    }
}
