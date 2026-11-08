using System;
using UnityEngine;

namespace AbyssBar.Maps
{
    [Serializable]
    public sealed class ArenaDecoration
    {
        public string id;
        public Sprite sprite;
        public Vector2 position;
        [Min(.1f)] public float width = 3;
        public bool blocksMovement = true;
        public Vector2 collisionSize = new Vector2(1.5f, .5f);
        public Vector2 collisionOffset = new Vector2(0, .2f);
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Maps/Arena Map", fileName = "ArenaMap")]
    public sealed class ArenaMapData : ScriptableObject
    {
        public string mapId = "station_plaza_01";
        public string displayName = "地表廢棄車站廣場";
        public Sprite background;
        public Material spriteMaterial;
        [Tooltip("Physical space INSIDE the walls. Old arena was 23.5 x 13.5; 47 x 27 is exactly four times its area.")]
        public Vector2 playableSize = new Vector2(47, 27);
        [Tooltip("Normalized artwork rectangle matching the inner edge of the pavement curb. Origin is bottom-left.")]
        public Rect artworkPlayableUv = new Rect(132f / 1564, 160f / 1006, 1296f / 1564, 691f / 1006);
        [Min(.1f)] public float wallThickness = 1;
        [Min(.1f)] public float spawnClearance = .6f;
        [Min(.1f)] public float spawnViewMargin = 1.2f;
        [Min(0)] public float minimumSpawnDistance = 6;
        public ArenaDecoration[] decorations = Array.Empty<ArenaDecoration>();
        public Vector2 ArtworkWorldSize => new Vector2(playableSize.x / artworkPlayableUv.width, playableSize.y / artworkPlayableUv.height);
        public Vector2 ArtworkLocalCenter => -Vector2.Scale(artworkPlayableUv.center - new Vector2(.5f, .5f), ArtworkWorldSize);
        public bool Validate(out string error)
        {
            error = null;
            if (background == null) error = "Missing background sprite.";
            else if (!Positive(playableSize.x) || !Positive(playableSize.y)) error = "Playable size must be finite and positive.";
            else if (!Finite(artworkPlayableUv.x) || !Finite(artworkPlayableUv.y) || !Positive(artworkPlayableUv.width) || !Positive(artworkPlayableUv.height)
                || artworkPlayableUv.xMin < 0 || artworkPlayableUv.yMin < 0 || artworkPlayableUv.xMax > 1 || artworkPlayableUv.yMax > 1)
                error = "Artwork playable UV must stay inside 0..1.";
            else if (!Positive(wallThickness) || !Positive(spawnClearance) || !Positive(spawnViewMargin)
                || float.IsNaN(minimumSpawnDistance) || float.IsInfinity(minimumSpawnDistance) || minimumSpawnDistance < 0)
                error = "Invalid wall/spawn tuning.";
            else if (spawnClearance * 2 >= Mathf.Min(playableSize.x, playableSize.y)) error = "Spawn clearance consumes the arena.";
            foreach (var prop in decorations ?? Array.Empty<ArenaDecoration>())
                if (prop == null || prop.sprite == null || !Positive(prop.width) || !Finite(prop.position.x) || !Finite(prop.position.y)
                    || prop.blocksMovement && (!Positive(prop.collisionSize.x) || !Positive(prop.collisionSize.y)))
                { error = "Decoration missing sprite or valid size/position."; break; }
            return error == null;
        }
        private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
