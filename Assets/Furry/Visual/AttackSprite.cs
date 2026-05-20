using UnityEngine;

public class AttackSprite : MonoBehaviour
{
    [Header("Спрайт атаки")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float spriteOffset = 1.2f;
    [SerializeField] private Color attackColor = new Color(1f, 0.3f, 0f, 0.8f);
    [SerializeField] private Vector2 spriteSize = new Vector2(1.5f, 1.5f);

    [Header("Разные типы атак")]
    [SerializeField] private Sprite backstabSprite;    // Для атак со спины
    [SerializeField] private Sprite powerAttackSprite; // Для мощных атак Alpha
    [SerializeField] private Color backstabColor = new Color(1f, 0f, 0f, 0.9f);
    [SerializeField] private Color powerColor = new Color(1f, 0.5f, 1f, 0.9f);

    private float hideTimer;
    private Vector2 attackDirection;
    private bool isVisible;
    private GameObject spriteObject;
    private SpriteRenderer spriteRenderer;
    private AttackType currentAttackType;

    public enum AttackType
    {
        Normal,
        Backstab,
        Power
    }

    private void Awake()
    {
        CreateSpriteObject();
    }

    private void CreateSpriteObject()
    {
        spriteObject = new GameObject("AttackVisual");
        spriteObject.transform.SetParent(transform);
        spriteObject.transform.localPosition = Vector3.zero;
        spriteObject.transform.localScale = spriteSize;
        spriteObject.hideFlags = HideFlags.HideAndDontSave;

        spriteRenderer = spriteObject.AddComponent<SpriteRenderer>();

        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
        }
        else
        {
            // Создаём простую текстуру-заглушку
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
            Destroy(tex);
        }

        spriteRenderer.color = attackColor;
        spriteRenderer.sortingOrder = 10;
        spriteRenderer.enabled = false;
    }

    private void Update()
    {
        if (!isVisible) return;

        hideTimer -= Time.deltaTime;

        if (hideTimer > 0 && spriteRenderer != null)
        {
            float alpha = Mathf.Clamp01(hideTimer / showDuration);
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;

            float scaleMultiplier = 1f + (1f - alpha) * 0.4f;
            spriteObject.transform.localScale = spriteSize * scaleMultiplier;
        }
        else
        {
            HideSprite();
        }
    }

    /// <summary>
    /// Показать спрайт атаки (обычная атака)
    /// </summary>
    public void ShowAttack(Vector2 direction)
    {
        ShowAttack(direction, AttackType.Normal);
    }

    /// <summary>
    /// Показать спрайт атаки с указанием типа
    /// </summary>
    public void ShowAttack(Vector2 direction, AttackType attackType)
    {
        attackDirection = direction.normalized;
        currentAttackType = attackType;

        if (spriteRenderer == null) return;

        // Выбираем спрайт и цвет в зависимости от типа атаки
        switch (attackType)
        {
            case AttackType.Backstab:
                spriteRenderer.sprite = backstabSprite != null ? backstabSprite : attackSprite;
                spriteRenderer.color = backstabColor;
                spriteSize = new Vector2(2f, 2f); // Больше для бэкстаба
                break;
            case AttackType.Power:
                spriteRenderer.sprite = powerAttackSprite != null ? powerAttackSprite : attackSprite;
                spriteRenderer.color = powerColor;
                spriteSize = new Vector2(2.5f, 2.5f); // Ещё больше для мощной атаки
                break;
            default:
                spriteRenderer.sprite = attackSprite;
                spriteRenderer.color = attackColor;
                spriteSize = new Vector2(1.5f, 1.5f);
                break;
        }

        spriteRenderer.enabled = true;
        isVisible = true;
        hideTimer = showDuration;

        spriteObject.transform.localPosition = attackDirection * spriteOffset;

        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        spriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        spriteObject.transform.localScale = spriteSize;
    }

    private void HideSprite()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        isVisible = false;
    }
}
