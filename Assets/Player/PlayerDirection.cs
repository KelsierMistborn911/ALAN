using UnityEngine;

public class PlayerDirection : MonoBehaviour
{
    [Tooltip("ѕри зажатом Shift персонаж смотрит по движению (WASD), иначе Ч на мышь.")]
    [SerializeField] private bool shiftFollowsMovement = true;

    [Header("ќтладка")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private float gizmoLength = 1.5f;
    [SerializeField] private Color gizmoColor = Color.yellow;

    public Vector2 LookDirection { get; private set; } = Vector2.down;
    public int DirectionIndex { get; private set; } = 4; // 4 = S (вниз)

    private Vector2 movementInput;

    // 8 направлений по часовой: N, NE, E, SE, S, SW, W, NW
    private static readonly Vector2[] directions = new Vector2[]
    {
        new Vector2(0, 1),                     // 0: N
        new Vector2(1, 1).normalized,          // 1: NE
        new Vector2(1, 0),                     // 2: E
        new Vector2(1, -1).normalized,         // 3: SE
        new Vector2(0, -1),                    // 4: S
        new Vector2(-1, -1).normalized,        // 5: SW
        new Vector2(-1, 0),                    // 6: W
        new Vector2(-1, 1).normalized          // 7: NW
    };

    void Update()
    {
        Vector2 targetDirection;

        // ќпредел€ем источник направлени€
        if (shiftFollowsMovement && Input.GetKey(KeyCode.LeftShift) && movementInput.sqrMagnitude > 0.01f)
        {
            // –ежим спринта Ч смотрим по движению
            targetDirection = movementInput;
        }
        else
        {
            // ќбычный режим Ч смотрим на мышь (вариант B)
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            targetDirection = (mouseWorldPos - transform.position).normalized;
        }

        // ќкругл€ем до ближайшего из 8 направлений
        if (targetDirection.sqrMagnitude > 0.01f)
        {
            float bestDot = -1f;
            int bestIndex = DirectionIndex;

            for (int i = 0; i < directions.Length; i++)
            {
                float dot = Vector2.Dot(targetDirection, directions[i]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestIndex = i;
                }
            }

            DirectionIndex = bestIndex;
            LookDirection = directions[DirectionIndex];
        }
    }

    // ¬ызываетс€ из PlayerMovement.Update()
    public void SetMovementInput(Vector2 input)
    {
        movementInput = input;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;

        Gizmos.color = gizmoColor;
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)(LookDirection * gizmoLength);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }
}
