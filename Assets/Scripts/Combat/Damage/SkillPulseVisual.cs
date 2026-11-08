using UnityEngine;

// A short-lived visual only: PlayerActiveSkill applies the damage immediately.
[RequireComponent(typeof(SpriteRenderer))]
public class SkillPulseVisual : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float duration = 0.3f;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;
    private Vector3 fullScale;
    private float elapsed;
    private bool playing;

    public void Play(float radius)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer.sprite == null || radius <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        initialColor = spriteRenderer.color;
        Vector3 size = spriteRenderer.sprite.bounds.size;
        // The effect is spawned at the scene root, so it ignores the player's scale.
        fullScale = new Vector3(radius * 2f / Mathf.Max(size.x, 0.001f),
            radius * 2f / Mathf.Max(size.y, 0.001f), 1f);
        transform.localScale = Vector3.zero;
        elapsed = 0f;
        playing = true;
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
        transform.localScale = fullScale * progress;
        Color color = initialColor;
        color.a *= 1f - progress;
        spriteRenderer.color = color;
        if (progress >= 1f) Destroy(gameObject);
    }
}
