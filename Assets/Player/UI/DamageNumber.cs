using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 1f;

    private TextMesh textMesh;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>();
    }

    private void Update()
    {
        if (textMesh == null) return;

        timer += Time.deltaTime;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        Color color = textMesh.color;
        color.a = 1 - (timer / lifetime);
        textMesh.color = color;

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    public void Show(float damage, Vector3 position)
    {
        if (textMesh != null)
        {
            textMesh.text = Mathf.RoundToInt(damage).ToString();
            textMesh.fontSize = 48;
            textMesh.color = Color.white;
        }
        else
        {
            Debug.LogError("TextMesh не найден! Добавь компонент TextMesh на префаб DamageNumber.");
        }
    }
}
