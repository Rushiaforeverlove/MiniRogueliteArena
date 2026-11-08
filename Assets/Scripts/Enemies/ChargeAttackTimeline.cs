using System;

namespace AbyssBar.Enemies
{
    public enum ChargePhase { Approach, Windup, Rush, Recovery }
    public sealed class ChargeAttackTimeline
    {
        private readonly float windup, range, recovery, cooldown;
        public ChargePhase Phase { get; private set; }
        public float PhaseElapsed { get; private set; }
        public float RemainingDistance { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool HitWall { get; private set; }
        public float WindupProgress => Phase == ChargePhase.Windup ? Math.Min(1, PhaseElapsed / windup) : 0;
        public ChargeAttackTimeline(float windup, float range, float recovery, float cooldown)
        {
            foreach (float v in new[] { windup, range, recovery, cooldown })
                if (v <= 0 || float.IsNaN(v) || float.IsInfinity(v)) throw new ArgumentException("Invalid charge timings.");
            this.windup = windup; this.range = range; this.recovery = recovery; this.cooldown = cooldown;
        }
        public bool Begin()
        {
            if (Phase != ChargePhase.Approach || CooldownRemaining > .000001f) return false;
            Phase = ChargePhase.Windup; PhaseElapsed = 0; HitWall = false; RemainingDistance = range; return true;
        }
        public void Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            CooldownRemaining = Math.Max(0, CooldownRemaining - seconds);
            PhaseElapsed += seconds;
            if (Phase == ChargePhase.Windup && PhaseElapsed + .000001f >= windup)
            { Phase = ChargePhase.Rush; PhaseElapsed = 0; }
            else if (Phase == ChargePhase.Recovery && PhaseElapsed + .000001f >= recovery)
            { Phase = ChargePhase.Approach; PhaseElapsed = 0; }
        }
        public void Travel(float distance, bool hitWall)
        {
            if (Phase != ChargePhase.Rush || distance < 0 || float.IsNaN(distance) || float.IsInfinity(distance)) return;
            RemainingDistance = Math.Max(0, RemainingDistance - distance);
            if (hitWall || RemainingDistance <= .000001f) EndRush(hitWall);
        }
        public void Interrupt()
        { if (Phase == ChargePhase.Windup || Phase == ChargePhase.Rush) EndRush(false); }
        private void EndRush(bool wall)
        {
            Phase = ChargePhase.Recovery; PhaseElapsed = 0; HitWall = wall; CooldownRemaining = cooldown;
        }
    }
}
