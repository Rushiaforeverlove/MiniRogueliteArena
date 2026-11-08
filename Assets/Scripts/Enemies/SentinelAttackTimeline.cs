using System;

namespace AbyssBar.Enemies
{
    public enum SentinelPhase { Approach, Windup, Slam, Recovery }
    public sealed class SentinelAttackTimeline
    {
        private readonly double windup, slam, recovery, cooldown;
        private double elapsed, remaining;
        public SentinelPhase Phase { get; private set; }
        public float Elapsed => (float)elapsed;
        public float CooldownRemaining => (float)remaining;
        public SentinelAttackTimeline(float warning, float impact, float opening, float cooldownAfterImpact)
        {
            foreach (float value in new[] { warning, impact, opening, cooldownAfterImpact })
                if (value <= 0 || float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentException("Invalid sentinel timings.");
            windup = warning; slam = impact; recovery = opening; cooldown = cooldownAfterImpact;
        }
        public bool Begin()
        {
            if (Phase != SentinelPhase.Approach || remaining > .000001) return false;
            Phase = SentinelPhase.Windup; elapsed = 0; return true;
        }
        // Return true only on the single impact edge. Do not skip the impact
        // frame or deliver several attacks when a physics frame is delayed.
        public bool Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return false;
            remaining = Math.Max(0, remaining - seconds); elapsed += seconds;
            if (Phase == SentinelPhase.Windup && elapsed + .000001 >= windup)
            { Phase = SentinelPhase.Slam; elapsed = 0; remaining = cooldown; return true; }
            if (Phase == SentinelPhase.Slam && elapsed + .000001 >= slam)
            { Phase = SentinelPhase.Recovery; elapsed = 0; }
            else if (Phase == SentinelPhase.Recovery && elapsed + .000001 >= recovery)
            { Phase = SentinelPhase.Approach; elapsed = 0; }
            return false;
        }
        public void Interrupt()
        {
            if (Phase != SentinelPhase.Windup && Phase != SentinelPhase.Slam) return;
            Phase = SentinelPhase.Recovery; elapsed = 0; remaining = cooldown;
        }
    }
}
