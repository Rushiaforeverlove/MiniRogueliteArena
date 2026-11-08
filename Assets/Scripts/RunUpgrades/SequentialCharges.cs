using System;

namespace AbyssBar.Upgrades
{
    // Only one missing slot recharges at a time. Extra capacity arrives filled.
    public sealed class SequentialCharges
    {
        public int Capacity { get; private set; } = 1;
        public int Available { get; private set; } = 1;
        public float Remaining { get; private set; }
        public float Duration { get; private set; } = 1;
        public void Reset(float duration) { Capacity = Available = 1; Remaining = 0; Duration = Math.Max(.05f, duration); }
        public void Configure(int capacity, float duration)
        {
            capacity = Math.Max(1, capacity); duration = Math.Max(.05f, duration);
            if (Available < Capacity) Remaining *= duration / Duration;
            Available = Math.Min(capacity, Available + Math.Max(0, capacity - Capacity));
            Capacity = capacity; Duration = duration;
        }
        public bool Spend()
        {
            if (Available <= 0) return false;
            if (Available == Capacity) Remaining = Duration;
            Available--; return true;
        }
        public void Tick(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt) || Available >= Capacity) return;
            Remaining -= dt;
            while (Remaining <= 0 && Available < Capacity) { Available++; Remaining += Duration; }
            if (Available == Capacity) Remaining = 0;
        }
        public void AdvanceCurrent(float seconds)
        {
            if (seconds <= 0 || Available == Capacity) return;
            Remaining -= seconds;
            if (Remaining <= 0) { Available++; Remaining = Available < Capacity ? Duration : 0; }
        }
        public void Restore(int count = 1)
        {
            Available = Math.Min(Capacity, Available + Math.Max(0, count));
            if (Available == Capacity) Remaining = 0;
        }
    }
}

