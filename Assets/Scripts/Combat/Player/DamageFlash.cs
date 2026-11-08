using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DamageFlash : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float duration = 0.12f;
    [SerializeField] private Color flashColor = new Color(1f, 0.2f, 0.2f, 1f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float remainingTime;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Play()
    {
        spriteRenderer.color = flashColor;
        remainingTime = duration;
    }

    // PlayerCharacter calls this after applying a skin in Start, after Awake cached its color.
    public void SetBaseColor(Color color)
    {
        originalColor = color;
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (remainingTime <= 0f)
            spriteRenderer.color = color;
    }

    private void Update()
    {
        if (remainingTime <= 0f) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            spriteRenderer.color = originalColor;
        }
    }
}
