using System;

namespace AbyssBar.Enemies
{
    public enum RangedAttackPhase { Approach, Aim, Recovery }
    public sealed class RangedAttackCycle
    {
        public RangedAttackPhase Phase { get; private set; }
        public float Remaining { get; private set; }
        private readonly float windup, recovery;
        public RangedAttackCycle(float aim, float cooldown)
        {
            if (aim <= 0 || cooldown <= 0 || float.IsNaN(aim) || float.IsNaN(cooldown)
                || float.IsInfinity(aim) || float.IsInfinity(cooldown)) throw new ArgumentException("Invalid attack timings.");
            windup = aim; recovery = cooldown;
        }
        public bool BeginAim()
        {
            if (Phase != RangedAttackPhase.Approach) return false;
            Phase = RangedAttackPhase.Aim; Remaining = windup; return true;
        }
        public void Interrupt()
        {
            if (Phase != RangedAttackPhase.Aim) return;
            Phase = RangedAttackPhase.Recovery; Remaining = recovery;
        }
        public bool Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)
                || Phase == RangedAttackPhase.Approach) return false;
            Remaining = Math.Max(0, Remaining - seconds);
            if (Remaining > .000001f) return false;
            if (Phase == RangedAttackPhase.Aim)
            { Phase = RangedAttackPhase.Recovery; Remaining = recovery; return true; }
            Phase = RangedAttackPhase.Approach; return false;
        }
    }
}
