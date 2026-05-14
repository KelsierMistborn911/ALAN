using UnityEngine;

[System.Serializable]
public class DirectionalAnimation
{
    [SerializeField] public Sprite[] framesN;
    [SerializeField] public Sprite[] framesNE;
    [SerializeField] public Sprite[] framesE;
    [SerializeField] public Sprite[] framesSE;
    [SerializeField] public Sprite[] framesS;
    [SerializeField] public Sprite[] framesSW;
    [SerializeField] public Sprite[] framesW;
    [SerializeField] public Sprite[] framesNW;

    public Sprite[] GetFrames(int directionIndex)
    {
        return directionIndex switch
        {
            0 => framesN,
            1 => framesNE,
            2 => framesE,
            3 => framesSE,
            4 => framesS,
            5 => framesSW,
            6 => framesW,
            7 => framesNW,
            _ => framesS
        };
    }
}

public class PlayerAnimator : MonoBehaviour
{
    public enum PlayerState { Idle, Walk, Run, Windup, Attack }

    [Header("Анимации")]
    [SerializeField] private DirectionalAnimation idleAnim;
    [SerializeField] private DirectionalAnimation walkAnim;
    [SerializeField] private DirectionalAnimation runAnim;
    [SerializeField] private DirectionalAnimation attackAnim; // [0] = windup, [1] = удар

    [Header("Частота кадров")]
    [SerializeField] private float idleFrameRate = 0.5f;
    [SerializeField] private float walkFrameRate = 0.2f;
    [SerializeField] private float runFrameRate = 0.15f;
    [SerializeField] private float attackDuration = 0.3f; // сколько держится кадр удара

    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private PlayerDirection playerDirection;
    private PlayerMovement playerMovement;
    private CombatController combat;
    private PlayerState currentState = PlayerState.Idle;

    private int currentFrame;
    private float frameTimer;
    private float attackTimer;
    private bool attackLock; // чтобы не прерывать атаку

    void Start()
    {
        playerDirection = GetComponent<PlayerDirection>();
        playerMovement = GetComponent<PlayerMovement>();
        combat = GetComponent<CombatController>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        FillEmptySlots();
    }

    void Update()
    {
        UpdateState();
        UpdateAnimation();
    }

    void UpdateState()
    {
        if (combat != null && combat.IsAttacking)
        {
            currentState = PlayerState.Attack;
            combat.AttackJustReleased = false;
            return;
        }

        if (combat != null && combat.IsWindingUp)
        {
            currentState = PlayerState.Windup;
            currentFrame = 0;
            return;
        }

        if (playerMovement != null && playerMovement.IsMoving)
        {
            currentState = Input.GetKey(KeyCode.LeftShift) ? PlayerState.Run : PlayerState.Walk;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }

    void UpdateAnimation()
    {
        if (playerDirection == null) return;

        DirectionalAnimation anim = currentState switch
        {
            PlayerState.Idle => idleAnim,
            PlayerState.Walk => walkAnim,
            PlayerState.Run => runAnim,
            PlayerState.Windup => attackAnim,
            PlayerState.Attack => attackAnim,
            _ => idleAnim
        };

        Sprite[] frames = anim.GetFrames(playerDirection.DirectionIndex);

        if (frames == null || frames.Length == 0) return;

        float rate = currentState switch
        {
            PlayerState.Idle => idleFrameRate,
            PlayerState.Walk => walkFrameRate,
            PlayerState.Run => runFrameRate,
            _ => 0f
        };

        // Windup и Attack не чередуются
        if (currentState == PlayerState.Windup)
        {
            currentFrame = 0;
        }
        else if (currentState == PlayerState.Attack)
        {
            currentFrame = Mathf.Min(1, frames.Length - 1);
        }
        else
        {
            frameTimer -= Time.deltaTime;
            if (frameTimer <= 0)
            {
                frameTimer = rate;
                currentFrame = (currentFrame + 1) % frames.Length;
            }
        }

        if (currentFrame < frames.Length && frames[currentFrame] != null)
            spriteRenderer.sprite = frames[currentFrame];
    }

    void FillEmptySlots()
    {
        // Ищем любой спрайт для заглушек
        Sprite fallback = FindFirstSprite(idleAnim) ?? FindFirstSprite(walkAnim) ??
                          FindFirstSprite(runAnim) ?? FindFirstSprite(attackAnim);

        if (fallback == null) return;

        FillAnimation(idleAnim, fallback);
        FillAnimation(walkAnim, fallback);
        FillAnimation(runAnim, fallback);
        FillAnimation(attackAnim, fallback);
    }

    Sprite FindFirstSprite(DirectionalAnimation anim)
    {
        for (int i = 0; i < 8; i++)
        {
            var frames = anim.GetFrames(i);
            if (frames != null && frames.Length > 0 && frames[0] != null)
                return frames[0];
        }
        return null;
    }

    void FillAnimation(DirectionalAnimation anim, Sprite fallback)
    {
        for (int i = 0; i < 8; i++)
        {
            var frames = anim.GetFrames(i);
            if (frames == null || frames.Length == 0)
            {
                // Устанавливаем через Switch
                SetFrames(anim, i, new Sprite[] { fallback });
            }
            else
            {
                for (int j = 0; j < frames.Length; j++)
                {
                    if (frames[j] == null)
                        frames[j] = fallback;
                }
            }
        }
    }

    void SetFrames(DirectionalAnimation anim, int index, Sprite[] frames)
    {
        switch (index)
        {
            case 0: anim.framesN = frames; break;
            case 1: anim.framesNE = frames; break;
            case 2: anim.framesE = frames; break;
            case 3: anim.framesSE = frames; break;
            case 4: anim.framesS = frames; break;
            case 5: anim.framesSW = frames; break;
            case 6: anim.framesW = frames; break;
            case 7: anim.framesNW = frames; break;
        }
    }
}
