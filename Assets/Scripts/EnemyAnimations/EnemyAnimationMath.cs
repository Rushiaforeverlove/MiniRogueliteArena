using System;

namespace AbyssBar.EnemyAnimations
{
    public static class EnemyAnimationMath
    {
        public static int Frame(float seconds, float fps, int count, bool loop)
        {
            if (count <= 0 || fps <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || float.IsNaN(fps) || float.IsInfinity(fps)) return 0;
            double step = Math.Floor(Math.Max(0, seconds) * (double)fps + .000001);
            return loop ? (int)(step % count) : (int)Math.Min(count - 1, step);
        }
        public static int AimFrame(float progress, int impactFrame)
        {
            if (float.IsNaN(progress) || impactFrame <= 0) return 0;
            return (int)Math.Min(impactFrame - 1, Math.Floor(Math.Max(0, Math.Min(1, progress)) * impactFrame));
        }
    }
    public sealed class EnemyMeleeTimeline
    {
        private float elapsed, duration, impact;
        private bool hitAttempted;
        public bool Active { get; private set; }
        public float Elapsed => elapsed;
        public void Start(float seconds, float impactTime)
        {
            if (seconds <= 0 || impactTime < 0 || impactTime >= seconds
                || float.IsNaN(seconds) || float.IsNaN(impactTime)
                || float.IsInfinity(seconds) || float.IsInfinity(impactTime)) throw new ArgumentException("Invalid attack timeline.");
            duration = seconds; impact = impactTime; elapsed = 0; hitAttempted = false; Active = true;
        }
        public bool Tick(float seconds)
        {
            if (!Active || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            elapsed = Math.Min(duration, elapsed + seconds);
            bool hit = !hitAttempted && elapsed + .000001f >= impact;
            if (hit) hitAttempted = true;
            if (elapsed + .000001f >= duration) Active = false;
            return hit;
        }
        public void Cancel() { Active = false; }
    }
}
