using AbyssBar.EnemyAnimations;
using UnityEngine;

namespace AbyssBar.Enemies
{
    // Visual-only aftermath, intentionally without health, colliders or loot.
    // CorrosionFieldWorld is its sole clock and damage owner.
    public sealed class CorrosionAftermath : MonoBehaviour
    {
        private CorrosiveAnimationSet clips;
        private SpriteRenderer body, puddle, ring;
        private float burstDuration;
        private static Sprite ringSprite;
        public CorrosionLifetime Lifetime { get; private set; }
        public float Radius { get; private set; }
        public float Damage { get; private set; }
        public Vector2 Center => transform.position;
        public bool IsFinished => Lifetime.Phase == CorrosionPhase.Finished && Lifetime.Age >= Lifetime.WarningSeconds + burstDuration;
        public void Initialize(SpriteRenderer source, CorrosiveAnimationSet animationSet, CorrosiveEnemySettings settings, float damage)
        {
            clips = animationSet; Radius = settings.radius; Damage = damage; burstDuration = clips.burst.Duration;
            Lifetime = new CorrosionLifetime(settings.warningSeconds, settings.activeSeconds, settings.fadeSeconds);
            body = Child("Death body", settings.material, source.sortingLayerID, source.sortingOrder);
            body.transform.localScale = source.transform.lossyScale; body.transform.rotation = source.transform.rotation;
            body.flipX = source.flipX; body.spriteSortPoint = SpriteSortPoint.Pivot;
            puddle = Child("Corrosive pool", settings.material, source.sortingLayerID, -9000);
            ring = Child("Hazard radius indicator", settings.material, source.sortingLayerID, -8900);
            ring.sprite = RadiusSprite(); ring.transform.localScale = Vector3.one * (Radius * 2);
            Render();
        }
        private SpriteRenderer Child(string name, Material material, int layer, int order)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = material;
            renderer.sortingLayerID = layer; renderer.sortingOrder = order; renderer.color = Color.white; return renderer;
        }
        public void Advance(float seconds) { Lifetime.Tick(seconds); Render(); }
        public void BeginFade() { Lifetime.BeginFade(); Render(); }
        public void CancelHazard() { Lifetime.CancelHazard(); puddle.enabled = false; ring.enabled = false; }
        private void Render()
        {
            float age = Lifetime.Age;
            if (age < Lifetime.WarningSeconds)
            {
                body.enabled = true;
                int frame = EnemyAnimationMath.Frame(age, clips.inflate.frames.Length / Lifetime.WarningSeconds, clips.inflate.frames.Length, false);
                body.sprite = clips.inflate.frames[frame];
            }
            else if (age < Lifetime.WarningSeconds + burstDuration)
            { body.enabled = true; body.sprite = clips.burst.frames[clips.burst.FrameAt(age - Lifetime.WarningSeconds, false)]; }
            else body.enabled = false;

            CorrosionPhase phase = Lifetime.Phase;
            puddle.enabled = phase == CorrosionPhase.Active || phase == CorrosionPhase.Fading;
            ring.enabled = phase != CorrosionPhase.Finished;
            if (phase == CorrosionPhase.Warning)
                ring.color = new Color(1, .7f, .15f, Mathf.Lerp(.25f, .85f, age / Lifetime.WarningSeconds));
            if (phase == CorrosionPhase.Active)
            {
                float activeAge = Lifetime.ActiveAge;
                var clip = activeAge < clips.formation.Duration ? clips.formation : clips.bubbling;
                float clock = activeAge < clips.formation.Duration ? activeAge : activeAge - clips.formation.Duration;
                puddle.sprite = clip.frames[clip.FrameAt(clock, clip == clips.bubbling)]; puddle.color = Color.white;
                ring.color = new Color(.7f, 1, .1f, .8f);
            }
            else if (phase == CorrosionPhase.Fading)
            {
                int frame = EnemyAnimationMath.Frame(Lifetime.FadeProgress, clips.dissolve.frames.Length, clips.dissolve.frames.Length, false);
                puddle.sprite = clips.dissolve.frames[frame]; puddle.color = new Color(1, 1, 1, 1 - Lifetime.FadeProgress);
                ring.color = new Color(.55f, .6f, .5f, .35f * (1 - Lifetime.FadeProgress));
            }
            if (puddle.enabled && puddle.sprite != null)
            {
                Vector3 size = puddle.sprite.bounds.size;
                puddle.transform.localScale = new Vector3(Radius * 2 / Mathf.Max(.001f, size.x), Radius * 2 / Mathf.Max(.001f, size.y), 1);
            }
        }
        private static Sprite RadiusSprite()
        {
            if (ringSprite != null) return ringSprite;
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float dx = x - 63.5f, dy = y - 63.5f, square = dx * dx + dy * dy;
                pixels[y * 128 + x] = square <= 63.5f * 63.5f && square >= 60.5f * 60.5f ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply();
            ringSprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128); return ringSprite;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVisual()
        { if (ringSprite != null) { Destroy(ringSprite.texture); Destroy(ringSprite); } ringSprite = null; }
    }
}
