using UnityEngine;

public class WolfEnemy : MonoBehaviour
{
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] public bool isSpecial;

    private WolfMovement movement;
    private WolfPackManager packManager;
    private bool detected;
    private Vector3 currentTargetPoint;

    void Awake()
    {
        movement = GetComponent<WolfMovement>();
        packManager = FindObjectOfType<WolfPackManager>();
    }

    void Update()
    {
        if (detected) return;

        Collider2D player = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        if (player != null)
        {
            Debug.Log($"🐺 ВОЛК {name} ОБНАРУЖИЛ ИГРОКА! Расстояние: {Vector3.Distance(transform.position, player.transform.position)}");
            detected = true;
            packManager.OnPlayerDetected(player.transform);
        }
    }

    public void MoveToZone(Vector3 point, float zoneRadius)
    {
        currentTargetPoint = point;
        movement.SetTargetZone(point, zoneRadius);
    }

    public Vector3 GetPosition() => transform.position;
    public Vector3 GetTargetPoint() => currentTargetPoint;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
