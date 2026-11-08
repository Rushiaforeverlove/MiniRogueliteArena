using UnityEngine;

namespace AbyssBar.Combat
{
    public sealed class MouseAttackReticle : MonoBehaviour
    {
        private SpriteRenderer visual;
        private Sprite sprite;
        private Texture2D texture;
        public void Show(Vector2 point, float z, float size, Material material, int sortingLayer, bool held)
        {
            if (visual == null)
            {
                var go = new GameObject("Mouse aim reticle"); go.layer = gameObject.layer; go.transform.SetParent(transform, false);
                visual = go.AddComponent<SpriteRenderer>();
                texture = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                var pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f, r = dx * dx + dy * dy;
                    bool ring = r >= 19 * 19 && r <= 22 * 22;
                    bool cross = Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) >= 25 || Mathf.Abs(dy) <= 1 && Mathf.Abs(dx) >= 25;
                    pixels[y * 64 + x] = ring || cross ? Color.white : Color.clear;
                }
                texture.SetPixels(pixels); texture.Apply(); sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
                visual.sprite = sprite; visual.sortingOrder = 32000;
            }
            visual.enabled = true; visual.sortingLayerID = sortingLayer;
            if (material != null) visual.sharedMaterial = material;
            visual.color = held ? new Color(1, .75f, .3f, .95f) : new Color(.4f, .95f, 1, .9f);
            visual.transform.position = new Vector3(point.x, point.y, z);
            var scale = transform.lossyScale;
            visual.transform.localScale = new Vector3(size / Mathf.Max(.001f, Mathf.Abs(scale.x)), size / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1);
        }
        public void Hide() { if (visual != null) visual.enabled = false; }
        private void OnDisable() { Hide(); }
        private void OnDestroy() { if (sprite != null) Destroy(sprite); if (texture != null) Destroy(texture); }
    }
}
