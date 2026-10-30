using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDepthSort : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
    }

    private void LateUpdate()
    {
        float groundY = bodyCollider != null
            ? bodyCollider.bounds.center.y
            : transform.position.y;

        spriteRenderer.sortingOrder = -Mathf.RoundToInt(groundY * 100f);
    }
}