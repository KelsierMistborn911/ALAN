using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Упрощённая система отображения цифр урона (без TextMeshPro)
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("Настройки отображения")]
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private float popupDuration = 1f;
    [SerializeField] private float floatHeight = 1.5f;

    [Header("Цвета")]
    [SerializeField] private Color normalDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.red;
    [SerializeField] private float criticalThreshold = 30f;

    private static DamagePopup instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Создаём canvas если его нет
        if (worldCanvas == null)
        {
            CreateWorldCanvas();
        }

        // Создаём префаб если его нет
        if (popupPrefab == null)
        {
            CreatePopupPrefab();
        }
    }

    private void CreateWorldCanvas()
    {
        GameObject canvasGO = new GameObject("DamagePopupCanvas");
        worldCanvas = canvasGO.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;

        // Находим камеру
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            worldCanvas.worldCamera = mainCamera;

        // Масштабируем canvas
        RectTransform rect = canvasGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 100);
        rect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    private void CreatePopupPrefab()
    {
        popupPrefab = new GameObject("DamagePopupPrefab");
        popupPrefab.SetActive(false);

        // Добавляем TextMesh (обычный, без Pro)
        var textMesh = popupPrefab.AddComponent<TextMesh>();
        textMesh.fontSize = 40;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // Добавляем Renderer для правильного отображения
        var renderer = popupPrefab.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 100;
        }

        DontDestroyOnLoad(popupPrefab);
    }

    /// <summary>
    /// Показать цифру урона
    /// </summary>
    public static void Show(float damage, Vector3 position, bool isHeal = false)
    {
        if (instance == null)
        {
            Debug.LogWarning("DamagePopup не инициализирован!");
            return;
        }

        instance.StartCoroutine(instance.ShowPopupCoroutine(damage, position, damage >= instance.criticalThreshold));
    }

    private IEnumerator ShowPopupCoroutine(float damage, Vector3 position, bool isCritical)
    {
        GameObject popup = Instantiate(popupPrefab, position, Quaternion.identity, worldCanvas.transform);
        popup.SetActive(true);

        // Настраиваем текст
        TextMesh textMesh = popup.GetComponent<TextMesh>();
        if (textMesh != null)
        {
            textMesh.text = $"-{damage:F0}";
            textMesh.color = isCritical ? criticalDamageColor : normalDamageColor;
            if (isCritical) textMesh.fontSize = 60;
        }

        // Анимация
        Vector3 startPos = position;
        Vector3 endPos = position + Vector3.up * floatHeight;
        float elapsed = 0;

        while (elapsed < popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popupDuration;

            popup.transform.position = Vector3.Lerp(startPos, endPos, t);

            // Затухание
            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = 1f - t;
                textMesh.color = c;
            }

            yield return null;
        }

        Destroy(popup);
    }
}
