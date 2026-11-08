using UnityEngine;

namespace AbyssBar.Maps
{
    [DisallowMultipleComponent]
    public sealed class ArenaMapBounds : MonoBehaviour
    {
        [SerializeField] private ArenaMapData map;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        public ArenaMapData Map => map;
        public SpriteRenderer BackgroundRenderer => backgroundRenderer;
        public Rect PlayableRect => new Rect((Vector2)transform.position - map.playableSize * .5f, map.playableSize);
        public Rect SpawnRect
        {
            get
            {
                Rect r = PlayableRect;
                return Rect.MinMaxRect(r.xMin + map.spawnClearance, r.yMin + map.spawnClearance,
                    r.xMax - map.spawnClearance, r.yMax - map.spawnClearance);
            }
        }
        public void Configure(ArenaMapData data, SpriteRenderer renderer)
        { map = data; backgroundRenderer = renderer; }
        public static Rect CameraRect(Camera camera)
        {
            float height = camera.orthographicSize * 2;
            return new Rect((Vector2)camera.transform.position - new Vector2(height * camera.aspect, height) * .5f,
                new Vector2(height * camera.aspect, height));
        }
        private void OnDrawGizmosSelected()
        {
            if (map == null) return;
            Gizmos.color = Color.green; Gizmos.DrawWireCube(PlayableRect.center, PlayableRect.size);
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(SpawnRect.center, SpawnRect.size);
        }
    }
}
