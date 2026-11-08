using System;
using UnityEngine;

namespace AbyssBar.Maps
{
    // Geometry-only sampler. Randomness/obstacle checks are injected for deterministic tests.
    public static class ArenaSpawnSampler
    {
        public static bool TrySample(Rect allowed, Rect visible, Vector2 player, float margin, float minDistance,
            Func<float> random01, Func<Vector2, bool> blocked, out Vector2 position, int attempts = 32)
        {
            position = default;
            if (allowed.width <= 0 || allowed.height <= 0 || visible.width <= 0 || visible.height <= 0 || attempts <= 0) return false;
            Rect ring = Rect.MinMaxRect(visible.xMin - margin, visible.yMin - margin, visible.xMax + margin, visible.yMax + margin);
            Rect exclusion = Rect.MinMaxRect(visible.xMin - margin * .5f, visible.yMin - margin * .5f,
                visible.xMax + margin * .5f, visible.yMax + margin * .5f);
            for (int i = 0; i < attempts * 2; i++)
            {
                // First try the nearby offscreen ring. Near corners, fall back to the arena rim.
                Rect source = i < attempts ? ring : allowed;
                Vector2 candidate = EdgePoint(source, random01());
                if (!ContainsInclusive(allowed, candidate) || ContainsInclusive(exclusion, candidate)
                    || (candidate - player).sqrMagnitude < minDistance * minDistance || blocked != null && blocked(candidate)) continue;
                position = candidate; return true;
            }
            return false; // Never force a spawn into the viewport or through a wall.
        }
        public static Vector2 EdgePoint(Rect rect, float random01)
        {
            float distance = Mathf.Clamp01(random01) * 2 * (rect.width + rect.height);
            if (distance < rect.width) return new Vector2(rect.xMin + distance, rect.yMin);
            distance -= rect.width;
            if (distance < rect.height) return new Vector2(rect.xMax, rect.yMin + distance);
            distance -= rect.height;
            if (distance < rect.width) return new Vector2(rect.xMax - distance, rect.yMax);
            distance -= rect.width;
            return new Vector2(rect.xMin, rect.yMax - distance);
        }
        public static bool ContainsInclusive(Rect rect, Vector2 p) => p.x >= rect.xMin && p.x <= rect.xMax && p.y >= rect.yMin && p.y <= rect.yMax;
    }
}
