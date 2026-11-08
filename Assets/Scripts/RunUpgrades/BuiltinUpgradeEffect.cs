using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    // Built-in effect families share bookkeeping, not card definitions.
    // The registry binds an effectKey to a behavior code; a new card can reuse that key with different numbers/ID/art.
    public abstract class BuiltinUpgradeEffect : RunEffect
    {
        protected readonly string Code;
        protected readonly Dictionary<string, float> timers = new Dictionary<string, float>();
        protected readonly Dictionary<string, int> counters = new Dictionary<string, int>();
        protected BuiltinUpgradeEffect(string code) { Code = code; }
        protected bool Gate(string key, float duration)
        {
            if (timers.TryGetValue(key, out float time) && Time.time < time) return false;
            timers[key] = Time.time + duration; return true;
        }
        protected bool Count(string key, int required)
        {
            counters.TryGetValue(key, out int count); counters[key] = count + 1;
            if (counters[key] < Mathf.Max(1, required)) return false;
            counters[key] = 0; return true;
        }
        protected bool Spending(RunAction a, RunActionKind kind) => a.kind == kind && !a.free;
        protected static Vector2 Rotate(Vector2 v, float angle) => Quaternion.Euler(0, 0, angle) * v;
        protected void Radial(int count, float damage, Vector2? origin = null, float mark = 0)
        { for (int i = 0; i < count; i++) C.Shot(origin ?? C.Position, Rotate(Vector2.right, 360f * i / count), damage, 1, null, true, mark); }
        protected void StaggerArea(Vector2 center, float radius, float duration)
        { foreach (var e in C.Targets(center, radius)) e.Status.Stagger(duration); }
        protected float Distance(EnemyHealth e) => Vector2.Distance(C.Position, e.transform.position);
    }
}

