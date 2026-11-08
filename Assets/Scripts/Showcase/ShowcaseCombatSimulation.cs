using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Showcase
{
    // Local targets and swept projectile segments. Deliberately no EnemyHealth,
    // Living registry, physics, PlayerPrefs, rewards, upgrades or battle objects.
    public sealed partial class ShowcaseCombatSimulation
    {
        public sealed class Target
        {
            public Vector2 position;
            public float health = 1000, lastHit = -100, deathAt = -100;
        }
        public sealed class Shot
        {
            public Vector2 position, direction;
            public float age;
            public float damage;
            public int mioCastId;
            public int mioMaxTargets;
            public float mioWidth;
            public bool mioReleased;
            public readonly HashSet<int> hit = new HashSet<int>();
        }
        public sealed class Pulse { public Vector2 position; public float radius, age; }
        public sealed class Hit { public Vector2 position; public float amount, age; }
        public readonly List<Target> targets = new List<Target>();
        public readonly List<Shot> shots = new List<Shot>();
        public readonly List<Pulse> pulses = new List<Pulse>();
        public readonly List<Hit> hits = new List<Hit>();
        public readonly Vector2 hero = new Vector2(-1.7f, 0);
        public ShowcaseAttackPlan Plan { get; }
        private double elapsed;
        public float Time => (float)elapsed;
        public float LastCast { get; private set; }
        public int CastCount { get; private set; }
        public int EmittedPulses { get; private set; }
        private float nextBasic = .2f, nextPulse;
        private int pendingPulses;
        private bool started;
        public ShowcaseCombatSimulation(ShowcaseAttackPlan plan)
        {
            Plan = plan ?? throw new ArgumentNullException(nameof(plan));
            targets.Add(new Target { position = new Vector2(0, 0) });
            targets.Add(new Target { position = new Vector2(1.1f, 0) });
            targets.Add(new Target { position = new Vector2(0, .9f) });
            if(plan.mio!=null)ConfigureMioTargets();
        }
        public void Tick(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (Plan.mio != null) { TickMio(dt); return; }
            elapsed += dt;
            foreach (var p in pulses) p.age += dt; pulses.RemoveAll(p => p.age >= .45f);
            foreach (var h in hits) h.age += dt; hits.RemoveAll(h => h.age >= .75f);
            if (!started && Time + .000001f >= .2f) { started = true; Cast(); }
            else if (Plan.repeatBasic && Time + .000001f >= nextBasic && Time <= 2.8f) Cast();
            while (pendingPulses > 0 && Time + .000001f >= nextPulse)
            { pendingPulses--; EmitPulse(hero, Plan.radius); nextPulse += Math.Max(.05f, Plan.pulseInterval); }
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i]; shot.age += dt;
                Vector2 next = shot.position + shot.direction * (Plan.speed * dt);
                var order = new List<int>();
                for (int n = 0; n < targets.Count; n++) order.Add(n);
                order.Sort((a, b) => DistanceSquared(shot.position, targets[a].position).CompareTo(DistanceSquared(shot.position, targets[b].position)));
                bool spent = false;
                foreach (int n in order)
                {
                    var target = targets[n];
                    if (target.health <= 0 || shot.hit.Contains(n) || DistanceSquared(Closest(shot.position, next, target.position), target.position) > .32f * .32f) continue;
                    shot.hit.Add(n);
                    if (Plan.explosionRadius > 0) { EmitPulse(target.position, Plan.explosionRadius); spent = true; break; }
                    Damage(target);
                    if (shot.hit.Count >= Math.Max(1, Plan.maxTargets)) { spent = true; break; }
                }
                shot.position = next;
                if (spent || shot.age >= Plan.lifetime) shots.RemoveAt(i);
            }
        }
        private void Cast()
        {
            LastCast = Time - .2f; CastCount++; nextBasic = Time + Math.Max(.02f, Plan.attackInterval);
            if (Plan.pattern == ShowcasePattern.Shockwave)
            {
                EmitPulse(hero, Plan.radius); pendingPulses = Math.Max(0, Plan.pulseCount - 1);
                nextPulse = Time + Math.Max(.05f, Plan.pulseInterval); return;
            }
            int count = Plan.pattern == ShowcasePattern.Straight ? 1 : Math.Max(1, Math.Min(36, Plan.projectileCount));
            for (int i = 0; i < count; i++)
            {
                float degrees = Plan.pattern == ShowcasePattern.Radial ? 360f * i / count
                    : count == 1 ? 0 : -Plan.spreadDegrees * .5f + Plan.spreadDegrees * i / (count - 1);
                double angle = degrees * Math.PI / 180;
                if (shots.Count >= 96) shots.RemoveAt(0);
                shots.Add(new Shot { position = hero, direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) });
            }
        }
        private void EmitPulse(Vector2 at, float radius)
        {
            if (Plan.pattern == ShowcasePattern.Shockwave) LastCast = Time - .2f;
            if (pulses.Count >= 64) pulses.RemoveAt(0);
            EmittedPulses++; pulses.Add(new Pulse { position = at, radius = radius });
            foreach (var target in targets)
                if (target.health > 0 && DistanceSquared(at, target.position) <= (radius + .3f) * (radius + .3f)) Damage(target);
        }
        private void Damage(Target target)
        {
            if (target.health <= 0) return;
            float damage = Math.Max(0, Plan.damage); target.health = Math.Max(0, target.health - damage);
            target.lastHit = Time; if (target.health == 0) target.deathAt = Time;
            if (hits.Count >= 64) hits.RemoveAt(0);
            hits.Add(new Hit { position = target.position, amount = damage });
        }
        private static float DistanceSquared(Vector2 a, Vector2 b) => (a - b).sqrMagnitude;
        private static Vector2 Closest(Vector2 a, Vector2 b, Vector2 point)
        {
            Vector2 d = b - a; float length = d.sqrMagnitude;
            float along = length <= .000001f ? 0 : ((point.x - a.x) * d.x + (point.y - a.y) * d.y) / length;
            return a + d * Math.Max(0, Math.Min(1, along));
        }
    }
}
