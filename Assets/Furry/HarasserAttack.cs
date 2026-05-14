using UnityEngine;

/// <summary>
/// Управляет атакующим поведением харассера.
/// Каждые decisionInterval секунд оценивает ситуацию и выбирает действие
/// (уворот, рывок, прыжок, серия ударов, манёвр, бегство).
/// На время атаки отключает PackMemberMovement.
/// </summary>
public class HarasserAttack : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PackMember packMember;
    [SerializeField] private PackMemberMovement movement;
    [SerializeField] private Rigidbody2D rb;

    [Header("Интервал принятия решений")]
    [SerializeField] private float decisionInterval = 0.3f;

    [Header("Атаки — импульсы")]
    [SerializeField] private float lungeImpulse = 12f;
    [SerializeField] private float jumpBackImpulse = 8f;
    [SerializeField] private float jumpForwardImpulse = 15f;
    [SerializeField] private float dodgeImpulse = 10f;
    [SerializeField] private float strikeDamage = 15f;
    [SerializeField] private int closeComboMaxHits = 3;

    [Header("Дистанции")]
    [SerializeField] private float jumpMinDistance = 5f;
    [SerializeField] private float lungeMinDistance = 2.5f;
    [SerializeField] private float closeDistance = 1.8f;
    [SerializeField] private float strikeReach = 1.5f;
    [SerializeField] private float retreatDistance = 8f;

    [Header("Кулдауны")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float comboHitInterval = 0.4f;

    // ===== Состояние =====
    private float decisionTimer;
    private float cooldownTimer;
    private bool isBusy;
    private ActionState currentState;
    private int comboHitsLeft;
    private Vector2 dodgeDirection;
    private Vector2 lungeDirection;
    private Vector2 retreatTarget;
    private float stateTimer;

    private enum ActionState
    {
        Idle,
        Dodge,
        Retreat,
        CircleToFlank,
        CloseDistance,
        JumpPrepare,
        JumpAttack,
        LungeAttack,
        CloseCombo
    }

    // ===== Внешние ссылки =====
    private CombatController playerCombat;
    private PlayerDirection playerDirection;
    private Transform playerTransform;

    private void Awake()
    {
        if (packMember == null) packMember = GetComponent<PackMember>();
        if (movement == null) movement = GetComponent<PackMemberMovement>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        playerCombat = FindObjectOfType<CombatController>();
        playerDirection = FindObjectOfType<PlayerDirection>();
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;
    }

    private void Update()
    {
        if (packMember.IsDead || packMember.CurrentRole != PackManager.TacticalRole.Harasser)
            return;

        cooldownTimer -= Time.deltaTime;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f && !isBusy && cooldownTimer <= 0f)
        {
            decisionTimer = decisionInterval;
            EvaluateAndAct();
        }

        if (isBusy)
        {
            UpdateCurrentAction();
        }
    }

    // ===== Принятие решений =====

    private void EvaluateAndAct()
    {
        if (playerTransform == null) return;

        Vector2 myPos = transform.position;
        Vector2 playerPos = playerTransform.position;
        Vector2 toPlayer = playerPos - myPos;
        float distToPlayer = toPlayer.magnitude;
        Vector2 playerLookDir = playerDirection != null ? playerDirection.LookDirection : Vector2.down;

        float angleToLook = Vector2.Angle(playerLookDir, toPlayer.normalized);
        bool playerSeesMe = angleToLook < 90f;
        bool playerBackToMe = angleToLook > 150f;
        bool playerWindingUp = playerCombat != null && playerCombat.IsWindingUp;
        bool playerAttacking = playerCombat != null && playerCombat.IsAttacking;
        bool iAmWounded = packMember.GetHealthStatus() == PackManager.HealthStatus.Wounded;

        float baseRandom = Random.Range(0f, 10f);

        float scoreDodge = baseRandom;
        float scoreRetreat = baseRandom;
        float scoreCircle = baseRandom * 0.5f;
        float scoreClose = baseRandom * 0.3f;
        float scoreJump = baseRandom * 0.2f;
        float scoreLunge = baseRandom * 0.3f;
        float scoreCombo = baseRandom * 0.2f;

        // Уворот при замахе
        if (playerWindingUp && distToPlayer < 3f)
            scoreDodge = 80f + baseRandom;
        else if (playerAttacking && distToPlayer < 3f)
            scoreDodge = 40f + baseRandom;

        // Бегство если ранен
        if (iAmWounded)
            scoreRetreat = 50f + baseRandom;

        // Манёвр захода
        if (playerSeesMe && !playerWindingUp && distToPlayer > closeDistance)
            scoreCircle = 30f + baseRandom;

        // Сближение
        if (distToPlayer > jumpMinDistance)
            scoreClose = 40f + baseRandom;
        else if (distToPlayer > lungeMinDistance)
            scoreClose = 20f + baseRandom;

        // Прыжок издалека
        if (distToPlayer >= jumpMinDistance)
            scoreJump = 20f + baseRandom;

        // Рывок со средней дистанции
        if (distToPlayer >= lungeMinDistance && distToPlayer < jumpMinDistance)
        {
            scoreLunge = 20f + baseRandom;
            if (playerBackToMe) scoreLunge += 50f;
            if (playerWindingUp) scoreLunge -= 30f;
        }
        else if (distToPlayer < lungeMinDistance && distToPlayer > closeDistance)
        {
            scoreLunge = 15f + baseRandom;
            if (playerBackToMe) scoreLunge += 40f;
        }

        // Серия с места вблизи
        if (distToPlayer <= closeDistance && !playerWindingUp)
            scoreCombo = 25f + baseRandom;

        // Бросок кубика
        float totalScore = scoreDodge + scoreRetreat + scoreCircle + scoreClose + scoreJump + scoreLunge + scoreCombo;
        float roll = Random.Range(0f, totalScore);

        if (roll < scoreDodge)
            StartAction(ActionState.Dodge);
        else if (roll < scoreDodge + scoreRetreat)
            StartAction(ActionState.Retreat);
        else if (roll < scoreDodge + scoreRetreat + scoreCircle)
            StartAction(ActionState.CircleToFlank);
        else if (roll < scoreDodge + scoreRetreat + scoreCircle + scoreClose)
            StartAction(ActionState.CloseDistance);
        else if (roll < scoreDodge + scoreRetreat + scoreCircle + scoreClose + scoreJump)
            StartAction(ActionState.JumpPrepare);
        else if (roll < scoreDodge + scoreRetreat + scoreCircle + scoreClose + scoreJump + scoreLunge)
            StartAction(ActionState.LungeAttack);
        else
            StartAction(ActionState.CloseCombo);
    }

    // ===== Запуск действия =====

    private void StartAction(ActionState state)
    {
        currentState = state;
        isBusy = true;
        stateTimer = 0f;
        movement.enabled = false;

        Vector2 myPos = transform.position;
        Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : myPos;
        Vector2 toPlayer = playerPos - myPos;
        Vector2 playerLookDir = playerDirection != null ? playerDirection.LookDirection : Vector2.down;

        switch (state)
        {
            case ActionState.Dodge:
                Vector2 attackDir = playerLookDir;
                dodgeDirection = Vector2.Perpendicular(attackDir);
                if (Random.value > 0.5f) dodgeDirection = -dodgeDirection;
                dodgeDirection = (dodgeDirection - toPlayer.normalized * 0.5f).normalized;
                rb.AddForce(dodgeDirection * dodgeImpulse, ForceMode2D.Impulse);
                stateTimer = 0.5f;
                break;

            case ActionState.Retreat:
                retreatTarget = myPos + (myPos - playerPos).normalized * retreatDistance;
                rb.AddForce((myPos - playerPos).normalized * dodgeImpulse, ForceMode2D.Impulse);
                stateTimer = 2f;
                break;

            case ActionState.CircleToFlank:
                Vector2 flankDir = Vector2.Perpendicular(toPlayer.normalized);
                if (Random.value > 0.5f) flankDir = -flankDir;
                retreatTarget = playerPos + flankDir * lungeMinDistance;
                stateTimer = 1.5f;
                break;

            case ActionState.CloseDistance:
                stateTimer = 1f;
                break;

            case ActionState.JumpPrepare:
                rb.AddForce(-toPlayer.normalized * jumpBackImpulse, ForceMode2D.Impulse);
                stateTimer = 0.6f;
                break;

            case ActionState.LungeAttack:
                lungeDirection = toPlayer.normalized;
                rb.AddForce(lungeDirection * lungeImpulse, ForceMode2D.Impulse);
                stateTimer = 0.4f;
                break;

            case ActionState.CloseCombo:
                comboHitsLeft = Random.Range(2, closeComboMaxHits + 1);
                stateTimer = comboHitInterval;
                break;
        }
    }

    // ===== Обновление текущего действия =====

    private void UpdateCurrentAction()
    {
        stateTimer -= Time.deltaTime;

        switch (currentState)
        {
            case ActionState.Dodge:
            case ActionState.Retreat:
                if (stateTimer <= 0f)
                    FinishAction();
                break;

            case ActionState.CircleToFlank:
                MoveToward(retreatTarget);
                if (stateTimer <= 0f || Vector2.Distance(transform.position, retreatTarget) < 0.5f)
                    FinishAction();
                break;

            case ActionState.CloseDistance:
                if (playerTransform != null)
                    MoveToward(playerTransform.position);
                if (stateTimer <= 0f)
                    FinishAction();
                break;

            case ActionState.JumpPrepare:
                if (stateTimer <= 0f)
                {
                    currentState = ActionState.JumpAttack;
                    stateTimer = 0.5f;
                    if (playerTransform != null)
                    {
                        Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                        rb.AddForce(toPlayer * jumpForwardImpulse, ForceMode2D.Impulse);
                    }
                }
                break;

            case ActionState.JumpAttack:
                if (stateTimer <= 0f)
                {
                    TryDealDamage();
                    FinishAction();
                }
                break;

            case ActionState.LungeAttack:
                if (playerTransform != null)
                {
                    float dist = Vector2.Distance(transform.position, playerTransform.position);
                    if (dist < strikeReach)
                    {
                        TryDealDamage();
                        FinishAction();
                    }
                }
                if (stateTimer <= 0f)
                    FinishAction();
                break;

            case ActionState.CloseCombo:
                if (stateTimer <= 0f)
                {
                    if (playerTransform != null)
                    {
                        float dist = Vector2.Distance(transform.position, playerTransform.position);
                        if (dist < strikeReach * 1.2f)
                        {
                            TryDealDamage();
                        }
                    }

                    comboHitsLeft--;
                    if (comboHitsLeft <= 0)
                    {
                        FinishAction();
                    }
                    else
                    {
                        stateTimer = comboHitInterval;
                        if (playerCombat != null && playerCombat.IsWindingUp)
                        {
                            Vector2 attackDir = playerDirection != null ? playerDirection.LookDirection : Vector2.down;
                            dodgeDirection = Vector2.Perpendicular(attackDir);
                            rb.AddForce(dodgeDirection * dodgeImpulse, ForceMode2D.Impulse);
                            FinishAction();
                        }
                        else
                        {
                            Vector2 toPlayer = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                            Vector2 sideStep = Vector2.Perpendicular(toPlayer);
                            if (Random.value > 0.5f) sideStep = -sideStep;
                            rb.AddForce(sideStep * 3f, ForceMode2D.Impulse);
                        }
                    }
                }
                break;
        }
    }

    // ===== Завершение действия =====

    private void FinishAction()
    {
        isBusy = false;
        currentState = ActionState.Idle;
        movement.enabled = true;
        cooldownTimer = attackCooldown;
    }

    // ===== Вспомогательные методы =====

    private void MoveToward(Vector2 target)
    {
        Vector2 toTarget = target - (Vector2)transform.position;
        if (toTarget.magnitude < 0.3f)
        {
            rb.velocity *= 0.9f;
            return;
        }
        rb.velocity = Vector2.Lerp(rb.velocity, toTarget.normalized * 5f, 0.3f);
    }

    private void TryDealDamage()
    {
        if (playerTransform == null) return;
        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist < strikeReach * 1.5f)
        {
            Debug.Log($"{name}: удар по игроку, урон {strikeDamage}");
        }
    }

    // ===== Отладка =====
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || packMember == null || packMember.IsDead)
            return;

        Color c = currentState switch
        {
            ActionState.Dodge => Color.cyan,
            ActionState.Retreat => Color.magenta,
            ActionState.CircleToFlank => Color.yellow,
            ActionState.CloseDistance => Color.grey,
            ActionState.JumpPrepare => new Color(1f, 0.5f, 0f),
            ActionState.JumpAttack => Color.red,
            ActionState.LungeAttack => Color.red,
            ActionState.CloseCombo => new Color(1f, 0f, 0.5f),
            _ => Color.green
        };

        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, 0.6f);

        if (currentState == ActionState.LungeAttack)
            Gizmos.DrawRay(transform.position, lungeDirection * 2f);
        if (currentState == ActionState.Dodge)
            Gizmos.DrawRay(transform.position, dodgeDirection * 2f);
    }
}
