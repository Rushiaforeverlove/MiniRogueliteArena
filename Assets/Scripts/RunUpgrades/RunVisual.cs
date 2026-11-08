using System;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class RunVisual : MonoBehaviour
    {
        private static Sprite circle, square;
        private float until;
        private RunUpgradeRuntime owner;
        public static Sprite CircleSprite
        {
            get
            {
                if (circle != null) return circle;
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                    texture.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) <= 15.5f ? Color.white : Color.clear);
                texture.Apply(); circle = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(.5f, .5f), 32);
                return circle;
            }
        }
        public static GameObject Marker(RunUpgradeRuntime owner, Vector2 position, float diameter, Color color)
        {
            var go = new GameObject("Run effect (placeholder art)");
            go.transform.SetParent(owner.transform); go.transform.position = position;
            var parentScale = owner.transform.lossyScale;
            go.transform.localScale = new Vector3(diameter / Mathf.Max(.001f, parentScale.x), diameter / Mathf.Max(.001f, parentScale.y), 1);
            go.AddComponent<RunOwnedObject>();
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = CircleSprite; renderer.color = color;
            var playerRenderer = owner.GetComponent<SpriteRenderer>();
            if (playerRenderer != null) renderer.sortingLayerID = playerRenderer.sortingLayerID;
            renderer.sortingOrder = 30000;
            return go;
        }
        public static GameObject Circle(RunUpgradeRuntime owner, Vector2 position, float radius, float duration, Color color)
        {
            var go = Marker(owner, position, radius * 2, color);
            var visual = go.AddComponent<RunVisual>(); visual.owner = owner; visual.until = Time.time + duration;
            // Detach movement logically in LateUpdate: persistent zones remain in world space.
            visual.worldPosition = position;
            return go;
        }
        private Vector2 worldPosition;
        public static GameObject Line(RunUpgradeRuntime owner, Vector2 a, Vector2 b, float width, float duration, Color color)
        {
            var go = Circle(owner, (a + b) * .5f, 1, duration, color);
            if (square == null) square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            go.GetComponent<SpriteRenderer>().sprite = square;
            var scale = owner.transform.lossyScale;
            go.transform.localScale = new Vector3(Vector2.Distance(a, b) / Mathf.Max(.001f, scale.x), width / Mathf.Max(.001f, scale.y), 1);
            go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            return go;
        }
        public void SetWorldPosition(Vector2 position) { worldPosition = position; transform.position = position; }
        private void LateUpdate()
        {
            if (owner == null || !owner.Active || Time.time >= until) { Destroy(gameObject); return; }
            transform.position = worldPosition;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSprite() { if (circle != null) { Destroy(circle.texture); Destroy(circle); } if (square != null) Destroy(square); circle = square = null; }
    }
}

