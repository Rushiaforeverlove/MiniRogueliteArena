using System;
using UnityEngine;

namespace AbyssBar.Presentation
{
    [Serializable]
    public sealed class RisingBubbleSettings
    {
        [Tooltip("Sprite-relative position measured from the top-left corner.")]
        public Vector2 anchor = new Vector2(.715f, .205f);
        [Min(.05f)] public float interval = .85f;
        [Min(.1f)] public float lifetime = 3;
        [Min(0)] public float rise = .95f;
        [Min(0)] public float drift = .035f;
        [Min(.001f)] public float startRadius = .035f;
        [Min(.001f)] public float endRadius = .095f;
        [Min(.001f)] public float lineWidth = .011f;
        [Min(.01f)] public float referenceSpriteHeight = 2.2f;
        public Color color = new Color(.915f, .965f, 1, .85f);

        public float Interval => Mathf.Max(.05f, interval);
        public float Lifetime => Mathf.Max(.1f, lifetime);
        public int PoolCapacity => Mathf.Clamp(Mathf.CeilToInt(Lifetime / Interval) + 1, 1, 64);
    }

    public readonly struct RisingBubbleSample
    {
        public readonly Vector2 Offset;
        public readonly float Radius, Alpha;

        public RisingBubbleSample(Vector2 offset, float radius, float alpha)
        { Offset = offset; Radius = radius; Alpha = alpha; }
    }

    // Stateless motion model, shared by every pooled bubble. Time comes from the owner.
    public static class RisingBubbleMotion
    {
        private static float Smooth(float value)
        { float t = Mathf.Clamp01(value); return t * t * (3 - 2 * t); }

        public static bool Sample(RisingBubbleSettings settings, long index, double time, out RisingBubbleSample sample)
        {
            sample = default;
            if (settings == null || index < 0 || double.IsNaN(time) || double.IsInfinity(time)) return false;
            double age = time - index * (double)settings.Interval;
            if (age < 0 || age >= settings.Lifetime) return false;
            float u = (float)(age / settings.Lifetime);
            float x = (float)Math.Sin(index * 2.4) * .01f
                + settings.drift * (float)Math.Sin(index * 1.7 + u * Math.PI) * u;
            float y = settings.rise * (u * .8f + u * u * .2f);
            float radius = Mathf.Lerp(settings.startRadius, settings.endRadius, Smooth(u))
                * (1 - .15f * Smooth((u - .8f) / .2f));
            float alpha = Smooth((float)age / .15f) * (1 - Smooth((u - .65f) / .35f));
            sample = new RisingBubbleSample(new Vector2(x, y), radius, alpha);
            return alpha > .001f;
        }
    }
}
