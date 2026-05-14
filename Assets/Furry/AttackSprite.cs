using UnityEngine;

public class AttackSprite : MonoBehaviour
{
    [Header("Спрайт атаки")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float spriteOffset = 1.2f;
    [SerializeField] private Color attackColor = new Color(1f, 0.3f, 0f, 0.8f);
    [SerializeField] private Vector2 spriteSize = new Vector2(1.5f, 1.5f);

    private float hideTimer;
    private Vector2 attackDirection;
    private bool isVisible;

    private GameObject spriteObject;
    private SpriteRenderer spriteRenderer;

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
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
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
            Color c = attackColor;
            c.a *= alpha;
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
    /// Показать спрайт атаки в указанном направлении.
    /// Вызывается из HarasserAttack.
    /// </summary>
    public void ShowAttack(Vector2 direction)
    {
        attackDirection = direction.normalized;

        if (spriteRenderer == null) return;

        spriteRenderer.enabled = true;
        isVisible = true;
        hideTimer = showDuration;

        spriteObject.transform.localPosition = attackDirection * spriteOffset;

        float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
        spriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        spriteRenderer.color = attackColor;
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
