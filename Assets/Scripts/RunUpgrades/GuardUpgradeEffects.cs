using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class GuardUpgradeEffects : BuiltinUpgradeEffect
    {
        public GuardUpgradeEffects(string code) : base(code) { }
        private float nextShield, absorbed;
        private readonly Dictionary<int, float> pushAt = new Dictionary<int, float>();
        public override void Acquired(int previousRank)
        { if (previousRank == 0) nextShield = Time.time + (Code == "GRD-P01" ? N("shieldInterval") : N("interval")); }
        public override void Stats(ref RunStats s) { if (Code == "GRD-W05") s.dodgeDistance += N("distanceBonuses"); }
        public override void BeforeAction(RunAction a)
        {
            if (Code == "GRD-W01" && Spending(a, RunActionKind.E)) C.Health.AddShield(N("amounts"), N("duration"));
            if (Code == "GRD-W03" && a.kind == RunActionKind.Attack && Count("attack", (int)N("attackThresholds"))) a.once.Add(Data.id + ".ready");
            if (Code == "GRD-W07" && a.kind == RunActionKind.Attack && C.Health.Shield > 0) a.multiplier *= 1 + N("damageBonuses");
            if (Code == "GRD-P05" && Spending(a, RunActionKind.Q)) C.Health.AddShield(N("shield"));
            if (Code == "GRD-G01" && Spending(a, RunActionKind.Q)) C.Health.AddShield(C.Health.MaxHealth * N("shieldHealthFraction"), N("duration"));
            if (Code == "GRD-N5-P01" && Spending(a, RunActionKind.Q)) StaggerArea(C.Position, N("radius"), N("staggerDuration"));
            if (Code == "GRD-N5-G01" && Spending(a, RunActionKind.Q))
                C.Zone(Data.id, a.origin, N("radius"), N("duration"), 1, p => {
                    if (Vector2.Distance(C.Position, p) <= N("radius")) C.Health.AddShield(N("shieldPerSecond"));
                });
        }
        public override void BeforeHit(RunHit h)
        {
            if (!h.shockwave) return;
            if (Code == "GRD-W04" && h.action.pulseIndex == 0) h.target.Status.Stagger(N("durations"));
        }
        public override void ModifyDamage(RunHit h)
        {
            if (!h.shockwave) return;
            if (Code == "GRD-W06" && Distance(h.target) <= N("distance")) h.damage *= 1 + N("bonuses");
            if (Code == "GRD-P03" && h.areaRadius > 0 && h.distanceFromOrigin >= h.areaRadius * (1 - N("outerFraction")))
                h.damage *= 1 + N("outerDamageBonus");
        }
        public override void AfterHit(RunHit h)
        {
            var a = h.action;
            if (Code == "GRD-W03" && a.once.Contains(Data.id + ".ready") && a.Once(Data.id))
            { h.target.Status.Push(C.Position, N("knockback")); h.target.Status.Stun(N("stun")); }
            if (Code == "GRD-G01" && (a.kind == RunActionKind.E || a.kind == RunActionKind.Q) && C.Health.Shield > 0 && a.Once(Data.id)) C.Health.AddShield(N("shieldPerAction"));
            if (Code == "GRD-N5-P01" && a.kind == RunActionKind.Q && !a.free && h.target.Status.Staggered && a.TargetOnce(Data.id, h.target)
                && a.targets[Data.id].Count <= N("maxPickupsPerCast")) C.Pickup(h.target.transform.position, 0, N("shieldPerPickup"));
        }
        public override void AfterAnyHit(RunHit h)
        {
            if (Code == "GRD-W08" && h.shockwave && Distance(h.target) <= N("distance"))
            {
                int id = h.target.RuntimeId;
                if (!pushAt.TryGetValue(id, out float time) || Time.time >= time)
                { pushAt[id] = Time.time + N("targetIcd"); h.target.Status.Push(C.Position, N("knockback")); h.target.Status.Slow(N("slow"), N("duration")); }
            }
            if (Code == "GRD-P03" && h.shockwave && h.areaRadius > 0 && h.distanceFromOrigin >= h.areaRadius * (1 - N("outerFraction")))
                h.target.Status.Push(h.origin, N("knockback"));
        }
        public override void Pulse(RunAction a, Vector2 center, float radius, float basis, bool last)
        {
            if (Code == "GRD-P02")
            {
                var old = C.FindZone(Data.id);
                if (old != null && (a.id != echoAction || a.pulseIndex > 0) && Vector2.Distance(center, old.center) <= radius + old.radius)
                { var at = old.center; float previousBasis = echoBasis; C.RemoveZone(old); C.Area(at, N("radius"), previousBasis * N("comboCoefficient"), null, true, true); }
                if (a.pulseIndex == 0)
                {
                    echoAction = a.id; echoBasis = basis;
                    C.Zone(Data.id, center, N("radius"), N("duration"), 10, null,
                        p => C.Area(p, N("radius"), basis * N("normalCoefficient"), null, true, true));
                }
            }
            if (Code == "GRD-G02" && a.kind == RunActionKind.Q && last)
            {
                Vector2 at = center; float r = radius / (1 + C.Param("guard.outer_ring", "radiusBonus")) * (1 + N("radiusBonus"));
                float damage = basis * a.pulseCount * N("totalBaseCoefficient");
                C.Later(N("delay"), () => C.Area(at, r, damage, null, true, true, e => e.Status.Push(at, -N("pull"))));
            }
            if (Code == "GRD-N5-G01" && a.kind == RunActionKind.E && a.Once(Data.id))
            {
                var z = C.FindZone(Data.id);
                if (z != null && Vector2.Distance(C.Position, z.center) <= z.radius)
                    C.Area(C.Position, radius / (1 + C.Param("guard.outer_ring", "radiusBonus")), basis * N("extraECoefficient"), null, true, true);
            }
        }
        private long echoAction;
        private float echoBasis;
        public override float Reduction()
        {
            if (Code == "GRD-W02") return Mathf.Min(N("maxEnemies"), C.Targets(C.Position, N("radius")).Count) * N("perEnemy");
            if (Code == "GRD-P04") { var z = C.FindZone(Data.id); return z != null && Vector2.Distance(C.Position, z.center) <= z.radius ? N("damageReduction") : 0; }
            return 0;
        }
        public override void DodgeEnded(Vector2 start, Vector2 end)
        {
            if (Code == "GRD-W05") C.StabilityUntil = Mathf.Max(C.StabilityUntil, Time.time + N("stabilityDurations"));
            if (Code == "GRD-P04")
            {
                var entered = new HashSet<int>();
                C.Zone(Data.id, end, N("radius"), N("duration"), .1f, p => {
                    foreach (var e in C.Targets(p, N("radius"))) if (entered.Add(e.RuntimeId)) e.Status.Stagger(N("staggerDuration"));
                });
            }
        }
        public override void ShieldBroken()
        { if (Code == "GRD-P01" && Gate("break", N("cooldown"))) C.Area(C.Position, N("radius"), C.AttackBasis * N("coefficient"), null, true, true, e => e.Status.Push(C.Position, N("knockback"))); }
        public override void ShieldAbsorbed(float amount)
        {
            if (Code != "GRD-P05") return;
            absorbed += amount;
            if (absorbed >= N("absorbedThreshold") && Gate("advance", N("icd"))) { absorbed = 0; C.Skills.AdvanceE(N("advanceSeconds")); }
        }
        public override void Kill(EnemyHealth e, Vector2 at, bool marked, float distance)
        { if (Code == "GRD-N5-W01" && Count("kills", (int)N("killThresholds"))) { C.Health.Heal(N("heal")); C.Health.AddShield(N("shield")); } }
        public override void Tick(float dt)
        {
            if ((Code == "GRD-W07" || Code == "GRD-P01") && Time.time >= nextShield)
            { nextShield = Time.time + (Code == "GRD-P01" ? N("shieldInterval") : N("interval")); C.Health.AddShield(N("shield")); }
            if (pushAt.Count > 500) { var ids = new List<int>(pushAt.Keys); foreach (int id in ids) if (pushAt[id] < Time.time) pushAt.Remove(id); }
        }
    }
}

