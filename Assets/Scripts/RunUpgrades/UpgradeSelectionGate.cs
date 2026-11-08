using System;

namespace AbyssBar.Upgrades
{
    // An uninterrupted neutral interval is required for EVERY offer. Uses
    // unscaled time because the battle is paused while choosing upgrades.
    public sealed class UpgradeSelectionGate
    {
        private double neutralSeconds;
        private readonly double requiredSeconds;
        private int openedFrame;
        public int Epoch { get; private set; }
        public bool IsOpen { get; private set; }
        public bool Ready { get; private set; }
        public UpgradeSelectionGate(float quietSeconds = .2f)
        {
            requiredSeconds = quietSeconds > 0 && !float.IsNaN(quietSeconds) && !float.IsInfinity(quietSeconds) ? quietSeconds : .2;
        }
        public void Begin(int frame)
        { Epoch++; IsOpen = true; Ready = false; neutralSeconds = 0; openedFrame = frame; }
        public void Suspend(int frame)
        { if (IsOpen) Begin(frame); }
        public void Close()
        { Epoch++; IsOpen = false; Ready = false; neutralSeconds = 0; }
        public void Tick(float unscaledSeconds, int frame, bool neutral)
        {
            if (!IsOpen || Ready || frame <= openedFrame) return;
            if (!neutral) { neutralSeconds = 0; return; }
            if (unscaledSeconds <= 0 || float.IsNaN(unscaledSeconds) || float.IsInfinity(unscaledSeconds)) return;
            neutralSeconds += unscaledSeconds;
            if (neutralSeconds + .000001 >= requiredSeconds) Ready = true;
        }
    }
}
