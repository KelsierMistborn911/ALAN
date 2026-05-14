using UnityEngine;

public class PlayerDirectionPointer : MonoBehaviour
{
    [SerializeField] private Sprite[] arrowSprites = new Sprite[8]; // 0=N,1=NE,2=E,3=SE,4=S,5=SW,6=W,7=NW
    [SerializeField] private SpriteRenderer arrowRenderer;

    private PlayerDirection playerDirection;

    void Start()
    {
        playerDirection = GetComponentInParent<PlayerDirection>();
        if (arrowRenderer == null)
            arrowRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (playerDirection != null && arrowSprites.Length == 8 && arrowRenderer != null)
        {
            arrowRenderer.sprite = arrowSprites[playerDirection.DirectionIndex];
        }
    }
}
