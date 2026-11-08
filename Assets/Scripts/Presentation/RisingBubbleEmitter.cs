using UnityEngine;

namespace AbyssBar.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RisingBubbleEmitter : MonoBehaviour
    {
        private const int Segments = 24;
        private sealed class BubbleView
        {
            public GameObject root;
            public LineRenderer ring, highlight;
            public readonly Vector3[] ringPoints = new Vector3[Segments];
            public readonly Vector3[] highlightPoints = new Vector3[3];
        }

        private RisingBubbleSettings settings;
        private SpriteRenderer source;
        private Sprite sprite;
        private Material material;
        private BubbleView[] pool;
        private double elapsed;
        private bool visible = true;

        public double Elapsed => elapsed;
        public int ActiveCount { get; private set; }
        public int Capacity => pool != null ? pool.Length : 0;
        public Vector3 AnchorWorld { get; private set; }

        public void Initialize(SpriteRenderer renderer, Sprite anchorSprite, Material template, RisingBubbleSettings configuration)
        {
            source = renderer;
            sprite = anchorSprite;
            settings = configuration ?? new RisingBubbleSettings();
            if (pool == null)
            {
                material = template != null ? new Material(template) : new Material(Shader.Find("Sprites/Default"));
                material.name = "Pooled rising bubble material";
                material.mainTexture = Texture2D.whiteTexture;
                material.color = Color.white;
            }
            EnsurePool();
            ResetClock();
        }

        public void ResetClock() { elapsed = 0; HideAll(); }
        public void SetVisible(bool value) { visible = value; if (!value) HideAll(); }

        // Explicitly driven by the seat: no second Update clock or separate pause logic.
        public void Advance(float seconds)
        {
            if (!isActiveAndEnabled || !visible || source == null || sprite == null || settings == null)
            { HideAll(); return; }
            if (Time.timeScale > 0 && seconds > 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) elapsed += seconds;
            EnsurePool();
            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;
            Vector3 localAnchor = new Vector3(settings.anchor.x * width - sprite.pivot.x / sprite.pixelsPerUnit,
                (1 - settings.anchor.y) * height - sprite.pivot.y / sprite.pixelsPerUnit, 0);
            if (source.flipX) localAnchor.x = -localAnchor.x;
            if (source.flipY) localAnchor.y = -localAnchor.y;
            AnchorWorld = source.transform.TransformPoint(localAnchor);
            float unit = height / Mathf.Max(.01f, settings.referenceSpriteHeight) * Mathf.Abs(source.transform.lossyScale.y);
            long newest = (long)System.Math.Floor(elapsed / settings.Interval);
            ActiveCount = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                BubbleView view = pool[i];
                if (!RisingBubbleMotion.Sample(settings, newest - i, elapsed, out var sample))
                { view.root.SetActive(false); continue; }
                ActiveCount++;
                view.root.SetActive(true);
                Vector3 center = AnchorWorld + (Vector3)sample.Offset * unit;
                float radius = sample.Radius * unit;
                for (int point = 0; point < Segments; point++)
                {
                    float angle = point * Mathf.PI * 2 / Segments;
                    view.ringPoints[point] = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * .92f, 0);
                }
                view.ring.SetPositions(view.ringPoints);
                view.highlightPoints[0] = center + new Vector3(-.42f, .25f, 0) * radius;
                view.highlightPoints[1] = center + new Vector3(-.35f, .39f, 0) * radius;
                view.highlightPoints[2] = center + new Vector3(-.2f, .46f, 0) * radius;
                view.highlight.SetPositions(view.highlightPoints);
                Color color = settings.color;
                color.a *= sample.Alpha * source.color.a;
                Style(view.ring, color, settings.lineWidth * unit);
                Style(view.highlight, color, settings.lineWidth * unit * .8f);
            }
        }

        private void Style(LineRenderer line, Color color, float width)
        {
            line.startColor = line.endColor = color;
            line.widthMultiplier = width;
            line.sortingLayerID = source.sortingLayerID;
            line.sortingOrder = source.sortingOrder + 1;
        }

        private void EnsurePool()
        {
            int count = settings.PoolCapacity;
            if (pool != null && pool.Length == count) return;
            if (pool != null) foreach (var view in pool) { view.root.SetActive(false); Destroy(view.root); }
            pool = new BubbleView[count];
            for (int i = 0; i < count; i++)
            {
                var root = new GameObject("Rising sleep bubble " + i);
                root.layer = gameObject.layer;
                root.transform.SetParent(transform, false);
                var view = new BubbleView { root = root };
                view.ring = MakeLine(root, true, Segments);
                var glint = new GameObject("Bubble highlight");
                glint.layer = gameObject.layer;
                glint.transform.SetParent(root.transform, false);
                view.highlight = MakeLine(glint, false, 3);
                root.SetActive(false);
                pool[i] = view;
            }
        }

        private LineRenderer MakeLine(GameObject owner, bool loop, int points)
        {
            var line = owner.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = loop;
            line.positionCount = points;
            line.numCapVertices = 2;
            line.sharedMaterial = material;
            return line;
        }

        private void HideAll()
        {
            ActiveCount = 0;
            if (pool != null) foreach (var view in pool) if (view.root != null) view.root.SetActive(false);
        }

        private void OnDisable() => HideAll();
        private void OnDestroy() { if (material != null) Destroy(material); }
    }
}
