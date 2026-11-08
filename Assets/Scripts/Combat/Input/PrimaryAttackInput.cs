using System;
using UnityEngine;

namespace AbyssBar.Combat
{
    public struct PrimaryAttackInput
    {
        public bool pressed, held, released, cancelled;
        public float heldSeconds, deltaTime;
        public Vector2 direction, worldPoint;
    }
    // Every gesture must start with a fresh press in the game viewport.
    // Crossing a modal/UI/focus boundary cancels it until the button is released.
    public sealed class PrimaryAttackGesture
    {
        private bool active, waitForRelease;
        private double heldTime;
        public PrimaryAttackInput Sample(bool down, bool held, bool up, bool blocked, float seconds)
        {
            float dt = seconds >= 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds) ? seconds : 0;
            var frame = new PrimaryAttackInput { deltaTime = dt };
            if (blocked)
            {
                frame.cancelled = active; frame.heldSeconds = (float)heldTime;
                active = false; heldTime = 0; waitForRelease |= held || down; return frame;
            }
            if (waitForRelease)
            { if (!held && !down) waitForRelease = false; return frame; }
            if (down) { active = true; heldTime = 0; frame.pressed = true; }
            if (!active) return frame;
            if (!down) heldTime += dt;
            frame.heldSeconds = (float)heldTime;
            frame.held = held && !up;
            if (up || !held)
            { frame.released = true; active = false; heldTime = 0; }
            return frame;
        }
        public void Interrupt() { active = false; heldTime = 0; waitForRelease = true; }
    }
    public sealed class PrimaryAttackCadence
    {
        private double sinceLast = double.PositiveInfinity;
        public void Tick(float seconds)
        { if (seconds > 0 && !float.IsNaN(seconds) && !float.IsInfinity(seconds)) sinceLast += seconds; }
        public bool TrySpend(float interval)
        {
            if (interval <= 0 || float.IsNaN(interval) || float.IsInfinity(interval) || sinceLast + .000001 < interval) return false;
            sinceLast = 0; return true; // One action per frame; no long-frame backlog.
        }
        public void Reset() { sinceLast = double.PositiveInfinity; }
    }
}
