using UnityEngine;

public class PackMemberVisual : MonoBehaviour
{
    [Header("Спрайты")]
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] moveSprites;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    private Transform player;
    private PackMember packMember;
    private PackMemberMovement movement;

    private int currentFrame;
    private float frameTimer;

    private void Awake()
    {
        packMember = GetComponent<PackMember>();
        movement = GetComponent<PackMemberMovement>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    private void Update()
    {
        if (packMember.IsDead || player == null) return;

        UpdateAnimation();
        FlipSprite();
    }

    private void UpdateAnimation()
    {
        Sprite[] frames = movement.IsMoving ? moveSprites : idleSprites;
        if (frames == null || frames.Length == 0) frames = idleSprites;
        if (frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / animationSpeed)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            bodyRenderer.sprite = frames[currentFrame];
        }
    }

    private void FlipSprite()
    {
        bodyRenderer.flipX = player.position.x < transform.position.x;
    }

    public void SetPlayer(Transform t) => player = t;
}
