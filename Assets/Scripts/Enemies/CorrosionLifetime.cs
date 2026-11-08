using System;

namespace AbyssBar.Enemies
{
    public enum CorrosionPhase { Warning, Active, Fading, Finished }
    public sealed class CorrosionLifetime
    {
        private readonly float warning, active, fade;
        private double evictedAt = -1;
        private double age;
        private bool cancelled;
        public float Age => (float)age;
        public float WarningSeconds => warning;
        public float ActiveAge => Math.Max(0, Age - warning);
        public CorrosionPhase Phase
        {
            get
            {
                if (cancelled) return CorrosionPhase.Finished;
                if (evictedAt >= 0) return age < evictedAt + fade - .000001 ? CorrosionPhase.Fading : CorrosionPhase.Finished;
                if (age < warning - .000001) return CorrosionPhase.Warning;
                if (age < (double)warning + active - .000001) return CorrosionPhase.Active;
                return age < (double)warning + active + fade - .000001 ? CorrosionPhase.Fading : CorrosionPhase.Finished;
            }
        }
        public float FadeProgress => (float)Math.Max(0, Math.Min(1, (age - (evictedAt >= 0 ? evictedAt : (double)warning + active)) / fade));
        public CorrosionLifetime(float warning, float active, float fade)
        {
            foreach (float v in new[] { warning, active, fade })
                if (v <= 0 || float.IsNaN(v) || float.IsInfinity(v)) throw new ArgumentException("Invalid corrosion timings.");
            this.warning = warning; this.active = active; this.fade = fade;
        }
        public void Tick(float seconds)
        { if (seconds > 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) age += seconds; }
        public void BeginFade()
        {
            if (Phase == CorrosionPhase.Active) evictedAt = age;
            else if (Phase == CorrosionPhase.Warning) CancelHazard();
        }
        public void CancelHazard() { cancelled = true; }
    }
}
