using System;

namespace AbyssBar.Mio
{
    // All time is supplied by the owner. No wall clock, coroutines or save state.
    public sealed class MioActionClock
    {
        public enum QPhase { Off, Enter, Active, Exit }
        public QPhase Phase { get; private set; }
        public float Age { get; private set; }
        public float Remaining { get; private set; }
        public int Blades { get; private set; }
        public bool IsReleased => Phase == QPhase.Active;
        public event Action Activated, Finished;
        private float enter, exit, extension;
        public bool Begin(float duration, float enterSeconds, float exitSeconds)
        {
            if (Phase != QPhase.Off || !Positive(duration) || !Positive(enterSeconds) || !Positive(exitSeconds)) return false;
            Remaining = duration; enter = Math.Max(.01f, enterSeconds); exit = Math.Max(.01f, exitSeconds);
            Age = extension = 0; Blades = 0; Phase = QPhase.Enter; return true;
        }
        public void Tick(float dt)
        {
            if (!Positive(dt) || Phase == QPhase.Off) return;
            // Consume phase boundaries exactly, including a long frame.
            int guard = 0;
            while (dt > .000001f && Phase != QPhase.Off && guard++ < 4)
            {
                float length = Phase == QPhase.Enter ? enter - Age : Phase == QPhase.Active ? Remaining : exit - Age;
                float step = Math.Min(dt, Math.Max(0, length)); Age += step; dt -= step;
                if (Phase == QPhase.Active) Remaining = Math.Max(0, Remaining - step);
                if (length - step > .000001f) break;
                Age = 0;
                if (Phase == QPhase.Enter) { Phase = QPhase.Active; Activated?.Invoke(); }
                else if (Phase == QPhase.Active) { Phase = QPhase.Exit; Finished?.Invoke(); }
                else Phase = QPhase.Off;
            }
        }
        public void RecordAttack() { if (IsReleased) Blades = Math.Min(6, Blades + 1); }
        public void Extend(float seconds, float cap)
        { if (!IsReleased || !Positive(seconds)) return; float add = Math.Max(0, Math.Min(seconds, cap - extension)); Remaining += add; extension += add; }
        public void Cancel() { Phase = QPhase.Off; Remaining = Age = extension = 0; Blades = 0; }
        private static bool Positive(float n) => n > 0 && !float.IsNaN(n) && !float.IsInfinity(n);
    }
    public sealed class MioChargeGesture
    {
        public bool Active { get; private set; }
        public float Seconds { get; private set; }
        public void Press() { Active = true; Seconds = 0; }
        public void Tick(float dt) { if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return; if (Active) Seconds += dt; }
        public bool Full(float required) => Active && Seconds + .000001f >= Math.Max(.12f, required);
        public void Consume() { Active = false; Seconds = 0; }
        public void Cancel() { Consume(); }
    }
}
