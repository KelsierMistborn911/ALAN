using System;
using UnityEngine;

[Serializable]
public class DirectionSet
{
    public Sprite[] N;
    public Sprite[] NE;
    public Sprite[] E;
    public Sprite[] SE;
    public Sprite[] S;
    public Sprite[] SW;
    public Sprite[] W;
    public Sprite[] NW;

    public Sprite[] GetFrames(int index)
    {
        switch (index)
        {
            case 0: return N;
            case 1: return NE;
            case 2: return E;
            case 3: return SE;
            case 4: return S;
            case 5: return SW;
            case 6: return W;
            case 7: return NW;
            default: return S;
        }
    }
}

public class PackMemberVisual : MonoBehaviour
{
    [Header("Анимации")]
    [SerializeField] private DirectionSet idle;
    [SerializeField] private DirectionSet move;

    [Header("Частота кадров")]
    [SerializeField] private float idleFrameRate = 2f;  // 2 кадра в секунду для idle
    [SerializeField] private float moveFrameRate = 8f;  // 8 кадров в секунду для движения

    [Header("Порог движения")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("Компоненты")]
    [SerializeField] private SpriteRenderer bodyRenderer;

    private PackMember packMember;
    private Rigidbody2D rb;

    private int currentFrame;
    private float frameTimer;
    private int lastDirectionIndex = 4; // По умолчанию смотрим вниз (S)
    private int currentDirectionIndex = 4;
    private bool isMoving;

    // Для сглаживания определения движения
    private Vector2 smoothedVelocity;
    private float smoothTime = 0.1f;

    private void Awake()
    {
        packMember = GetComponent<PackMember>();
        rb = GetComponent<Rigidbody2D>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<SpriteRenderer>();

        // Показать первый idle спрайт сразу
        var frames = idle.GetFrames(4);
        if (frames != null && frames.Length > 0 && bodyRenderer != null)
        {
            bodyRenderer.sprite = frames[0];
            Debug.Log($"{name}: Initial sprite set to idle[4][0]");
        }
    }

    private void Update()
    {
        if (packMember.IsDead) return;

        // Используем скорость из Rigidbody2D напрямую, со сглаживанием
        Vector2 currentVelocity = rb.velocity;
        smoothedVelocity = Vector2.Lerp(smoothedVelocity, currentVelocity, Time.deltaTime / smoothTime);

        float currentSpeed = smoothedVelocity.magnitude;
        bool newIsMoving = currentSpeed > moveThreshold;

        // Определяем направление
        int newDirectionIndex = lastDirectionIndex;
        if (newIsMoving && smoothedVelocity.magnitude > 0.001f)
        {
            newDirectionIndex = VelocityToDirectionIndex(smoothedVelocity);
            lastDirectionIndex = newDirectionIndex;
        }

        // Если состояние или направление изменились - сбрасываем анимацию
        if (newIsMoving != isMoving || newDirectionIndex != currentDirectionIndex)
        {
            isMoving = newIsMoving;
            currentDirectionIndex = newDirectionIndex;
            currentFrame = 0;
            frameTimer = 0f;

            Debug.Log($"{name}: State changed - isMoving={isMoving}, direction={currentDirectionIndex}, speed={currentSpeed:F3}");
        }

        // Получаем текущий набор кадров
        Sprite[] frames = isMoving ? move.GetFrames(currentDirectionIndex) : idle.GetFrames(currentDirectionIndex);

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"{name}: No frames for direction {currentDirectionIndex}, isMoving={isMoving}");
            return;
        }

        // Частота кадров в секунду (не интервал!)
        float frameInterval = 1f / (isMoving ? moveFrameRate : idleFrameRate);

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval; // Вычитаем интервал, а не обнуляем
            currentFrame = (currentFrame + 1) % frames.Length;

            if (frames[currentFrame] != null)
            {
                bodyRenderer.sprite = frames[currentFrame];
                Debug.Log($"{name}: Frame {currentFrame}/{frames.Length}, isMoving={isMoving}");
            }
        }
    }

    private int VelocityToDirectionIndex(Vector2 velocity)
    {
        if (velocity.sqrMagnitude < 0.0001f)
            return lastDirectionIndex;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Atan2: 0° = восток (1,0), 90° = север (0,1)
        // Наши индексы: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        int rawIndex = Mathf.RoundToInt(angle / 45f) % 8;

        // rawIndex: 0=E(0°), 1=NE(45°), 2=N(90°), 3=NW(135°), 4=W(180°), 5=SW(225°), 6=S(270°), 7=SE(315°)
        // Нам нужно: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW
        int[] remap = { 2, 1, 0, 7, 6, 5, 4, 3 };
        int result = remap[rawIndex];

        return result;
    }

    public void SetPlayer(Transform t) { }
}
