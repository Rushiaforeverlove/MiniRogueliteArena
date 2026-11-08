using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class SupportUpgradeEffects : BuiltinUpgradeEffect
    {
        public SupportUpgradeEffects(string code) : base(code) { }
        private float nextMark, bootUntil;
        private bool boot, lightKitReady, pathfinder;
        public override void Acquired(int previousRank) { if (Code == "SUP-SCT-W01") nextMark = Time.time + N("interval"); }
        public override void Tick(float dt)
        {
            if (Code == "SUP-SCT-W01" && Time.time >= nextMark)
            {
                nextMark = Time.time + N("interval"); var targets = C.Targets(C.Position, 8, true);
                if (targets.Count > 0) targets[0].Status.Mark(N("markDuration"));
            }
            if (Code == "SUP-SCT-W03" && C.MovingSeconds >= N("moveTime")) lightKitReady = true;
        }
        public override bool FreeE() { if (Code != "SUP-SCT-G01" || !pathfinder) return false; pathfinder = false; usedPathfinder = true; return true; }
        private bool usedPathfinder;
        public override float ETargetRangeMultiplier() => Code == "SUP-SCT-W03" && (lightKitReady || C.MovingSeconds >= N("moveTime")) ? 1 + N("rangeBonus") : 1;
        public override void BeforeAction(RunAction a)
        {
            if (Code == "SUP-SCT-W02" && a.kind == RunActionKind.Attack && Count("attack", (int)N("attackThresholds")))
                C.Shot(C.Position, a.direction, C.AttackBasis * N("coefficient"), (int)N("maxTargets"));
            if (Code == "SUP-SCT-W03" && a.kind == RunActionKind.E && lightKitReady)
            { a.multiplier *= 1 + N("damageBonus"); a.rangeMultiplier *= 1 + N("rangeBonus"); lightKitReady = false; C.ResetMovingTime(); }
            if (Code == "SUP-SCT-P01" && Spending(a, RunActionKind.Q))
            {
                var targets = C.Targets(C.Position, 8, true);
                for (int i = 0; i < Mathf.Min(targets.Count, (int)N("targets")); i++)
                { targets[i].Status.Mark(N("markDuration")); C.Shot(C.Position, (Vector2)targets[i].transform.position - C.Position, C.AttackBasis * N("coefficient")); }
            }
            if (Code == "SUP-SCT-G01" && a.kind == RunActionKind.E && usedPathfinder)
            { usedPathfinder = false; Radial((int)N("supportShots"), C.AttackBasis * N("coefficient")); }
            if (Code == "SUP-GRD-W04" && a.kind == RunActionKind.Attack && Count("attack", (int)N("attackThresholds"))) a.once.Add(Data.id + ".ready");
            if (Code == "SUP-GRD-P01" && Spending(a, RunActionKind.E) && Gate("e", N("cooldown")))
            { C.Health.AddShield(N("shield")); C.StabilityUntil = Mathf.Max(C.StabilityUntil, Time.time + N("stability")); }
            if (Code == "SUP-GRD-G01" && Spending(a, RunActionKind.Q))
                C.Zone(Data.id, a.origin, N("radius"), N("duration"), 1, p => {
                    C.Area(p, N("radius"), C.AttackBasis * N("coefficient"), null, true, true);
                    if (Vector2.Distance(C.Position, p) <= N("radius")) C.Health.AddShield(N("shieldPerPulse"));
                });
        }
        public override void BeforeHit(RunHit h)
        {
            if (Code == "SUP-GRD-W02" && (h.action.kind == RunActionKind.E || h.action.kind == RunActionKind.Q)
                && Distance(h.target) <= N("distance") && h.action.Once(Data.id)) h.target.Status.Stagger(N("staggerDuration"));
        }
        public override void AfterHit(RunHit h)
        {
            var a = h.action;
            if (Code == "SUP-SCT-P02" && a.kind == RunActionKind.E && a.Once(Data.id) && Gate("return", N("cooldown")))
            {
                Vector2 at = h.target.transform.position; Vector2 toward = (C.Position - at).normalized;
                C.Shot(at - toward * .35f, toward, a.basis * N("coefficient"), (int)N("maxTargets"), null, true, N("markDuration"));
            }
            if (Code == "SUP-GRD-W04" && a.once.Contains(Data.id + ".ready") && a.Once(Data.id))
                C.Area(h.target.transform.position, N("radius"), C.AttackBasis * N("coefficient"), null, true, true);
        }
        public override void DodgeEnded(Vector2 start, Vector2 end)
        {
            if (Code == "SUP-SCT-W04")
            {
                var targets = C.Targets(C.Position);
                for (int i = 0; i < Mathf.Min(targets.Count, (int)N("targets")); i++) targets[i].Status.Mark(N("markDuration"));
                C.supportPickupUntil = Time.time + N("duration"); C.supportPickupBonus = N("pickupBonus");
            }
            if (Code == "SUP-GRD-W03") { boot = true; bootUntil = Time.time + N("duration"); }
        }
        public override float Reduction() => Code == "SUP-GRD-W03" && boot && Time.time < bootUntil ? N("reductions") : 0;
        public override void IncomingHit() { if (Code == "SUP-GRD-W03") boot = false; }
        public override void Hurt(float actual)
        {
            if (Code == "SUP-GRD-W01" && Gate("hurt", N("cooldown"))) C.Health.AddShield(N("shields"));
            if (Code == "SUP-GRD-P02" && Gate("count", N("countIcd")) && Count("hurt", (int)N("hurtEvents")))
            { C.SlowUntil = 0; C.SlowFraction = 0; C.Dodge?.Restore((int)N("dodgeCharges")); C.Health.AddShield(N("shield")); }
        }
        public override void Kill(EnemyHealth enemy, Vector2 at, bool marked, float distance)
        {
            if (Code != "SUP-SCT-G01" || !Count("kills", (int)N("killThreshold"))) return;
            C.MarkArea(C.Position, N("radius"), N("markDuration")); pathfinder = true;
        }
    }
}

