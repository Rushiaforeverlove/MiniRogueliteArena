using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class ScoutUpgradeEffects : BuiltinUpgradeEffect
    {
        public ScoutUpgradeEffects(string code) : base(code) { }
        private float reloadUntil, laserUntil, nextInfoPickup, movingTime, lastFocusAt;
        private bool reload, info;
        private int passes, farKills, focusTarget, focusHits;
        private Vector2 lastMove;
        private long focusAction;
        public override void Acquired(int previousRank) { if (Code == "SCT-P05" && previousRank == 0) passes = (int)N("initialTokens"); }
        public override void Stats(ref RunStats s)
        {
            if (Code == "SCT-W02") { s.extraTargets += (int)N("extraTargets"); s.eExtraTargets += (int)N("extraTargets"); s.explosionRadiusBonus += N("explosionRadiusBonus"); }
            if (Code == "SCT-W06") s.range += N("rangePerRank") * Rank;
            if (Code == "SCT-P03") s.extraTargets += (int)N("extraAttackTargets");
        }
        public override bool FreeE()
        { if (Code != "SCT-P05" || passes <= 0) return false; passes--; return true; }
        public override void BeforeAction(RunAction a)
        {
            if (Code == "SCT-W04" && a.kind == RunActionKind.Attack && reload && Time.time < reloadUntil)
            { a.multiplier *= 1 + N("bonuses"); reload = false; }
            if (Code == "SCT-N5-W01" && a.kind == RunActionKind.Attack && info)
            { a.multiplier *= 1 + N("nextDamageBonus"); info = false; C.NextAttackRange = 0; }
            if (Code == "SCT-P02" && Spending(a, RunActionKind.Q)) C.MarkArea(C.Position, N("radius"), N("markDuration"));
            if (Code == "SCT-G01" && Spending(a, RunActionKind.Q))
            {
                var enemies = C.Targets(C.Position);
                for (int i = 0; i < Mathf.Min(enemies.Count, (int)N("initialMarks")); i++) enemies[i].Status.Mark(4);
                var zone = C.Zone(Data.id, C.Position, 1, N("duration"), N("shotInterval"), SatelliteRound, null, true);
                zone.created = Time.time;
            }
            if (Code == "SCT-N5-G01" && Spending(a, RunActionKind.Q))
            { C.MarkArea(C.Position, N("markRadius"), N("markDuration")); laserUntil = Time.time + N("window"); FireLaser(); }
        }
        private void SatelliteRound(Vector2 pos)
        {
            for (int i = 0; i < (int)N("satellites"); i++)
            {
                var targets = C.Targets(C.Position, 8, false, true);
                var e = targets.Count > 0 ? targets[i % targets.Count] : C.Nearest();
                if (e == null) continue;
                if (!e.Status.Marked) e.Status.Mark(4);
                Vector2 from = C.Position + Rotate(Vector2.right, i * 120 + Time.time * 60) * .7f;
                RunVisual.Circle(C, from, .15f, .25f, Color.cyan);
                C.Shot(from, (Vector2)e.transform.position - from, C.AttackBasis * N("coefficient"));
            }
        }
        private void FireLaser()
        {
            if (!Gate("laser", N("icd"))) return;
            var e = C.Targets(C.Position, 8, true);
            if (e.Count > 0) C.Shot(C.Position, (Vector2)e[0].transform.position - C.Position, C.AttackBasis * N("coefficient"), (int)N("maxTargets"));
        }
        public override void BeforeHit(RunHit h)
        {
            var a = h.action;
            if (Code == "SCT-W01" && a.kind == RunActionKind.Attack) h.target.Status.Mark(N("durations"));
            if (Code == "SCT-W03" && Distance(h.target) >= N("minimumDistance")) h.damage *= 1 + N("bonuses");
            if (Code == "SCT-P01" && a.kind == RunActionKind.E && a.Once(Data.id + ".first")) h.target.Status.Mark(4);
            if (Code == "SCT-P02" && a.kind == RunActionKind.Q && !a.free && h.target.Status.Marked) h.damage *= 1 + N("damageBonus");
            if (Code == "SCT-N5-W01" && h.target.IsFullHealth) h.target.Status.Mark(N("markDuration"));
        }
        public override void AfterHit(RunHit h)
        {
            var a = h.action;
            if (Code == "SCT-W08" && a.kind == RunActionKind.Attack && focusAction != a.id)
            {
                if (focusTarget != h.target.RuntimeId || Time.time - lastFocusAt > N("resetAfter")) focusHits = 0;
                focusTarget = h.target.RuntimeId; focusAction = a.id; lastFocusAt = Time.time;
                if (++focusHits >= (int)N("attackHits")) { focusHits = 0; C.Hit(h.target, C.AttackBasis * N("coefficients")); }
            }
            if (Code == "SCT-P01" && a.kind == RunActionKind.E && h.target.Status.Marked && a.TargetOnce(Data.id, h.target))
            {
                Vector2 at = h.target.transform.position;
                C.Shot(at, Rotate(a.direction, 90), a.basis * N("coefficient"));
                C.Shot(at, Rotate(a.direction, -90), a.basis * N("coefficient"));
            }
            if (Code == "SCT-P03" && (a.kind == RunActionKind.Attack || a.kind == RunActionKind.E)
                && h.targetOrdinal == 2 && a.Once(Data.id) && Gate("recharge", N("icd"))) C.Skills.AdvanceE(N("advanceSeconds"));
            if (Code == "SCT-N5-P01" && a.kind == RunActionKind.E && a.TargetOnce(Data.id, h.target))
            {
                if (!a.targets.TryGetValue(Data.id, out var ids) || ids.Count > N("targetsPerCast")) return;
                Vector2 at = h.target.transform.position; EnemyHealth target = h.target; float basis = a.basis;
                C.Later(N("delay"), () => C.Area(at, .35f, basis * (target != null && target.Status.Marked ? N("markedCoefficient") : N("coefficient"))));
            }
        }
        public override void AfterAction(RunAction a)
        {
            if (Code != "SCT-G02" || !C.StandingOnFireline || (a.kind != RunActionKind.Attack && a.kind != RunActionKind.E) || !Gate("guided", N("icd"))) return;
            var targets = C.Targets(C.Position, 8, false, true);
            for (int i = 0; i < Mathf.Min(targets.Count, (int)N("targets")); i++)
                C.Shot(C.Position, (Vector2)targets[i].transform.position - C.Position, C.AttackBasis * N("coefficient"));
        }
        public override void DodgeEnded(Vector2 start, Vector2 end)
        {
            if (Code == "SCT-W04") { reload = true; reloadUntil = Time.time + N("window"); }
            if (Code == "SCT-W05")
            {
                foreach (var e in C.Targets((start + end) * .5f, Vector2.Distance(start, end) * .5f + N("pathRadius")))
                    if (Vector2.Distance(RunProjectile.Closest(start, end, e.transform.position), e.transform.position) <= N("pathRadius")) e.Status.Mark(N("markDuration"));
                C.scoutSpeedUntil = Time.time + N("duration"); C.scoutSpeed = N("speedBonuses");
            }
            if (Code == "SCT-P04" || Code == "SCT-G02")
                C.Fireline(start, end, N("duration"), C.Param("scout.firing_lane", "damageBonus"),
                    C.Has("scout.route_zero") ? C.Param("scout.route_zero", "pathRadius") : N("width") * .5f);
        }
        public override void Kill(EnemyHealth enemy, Vector2 position, bool marked, float distance)
        {
            if (Code == "SCT-P05" && distance >= N("minimumDistance") && ++farKills >= N("killThreshold"))
            { farKills = 0; passes = Mathf.Min((int)N("capacity"), passes + 1); }
            if (Code == "SCT-G01" && marked)
            { var z = C.FindZone(Data.id); if (z != null) z.end = Mathf.Min(z.created + N("maxDuration"), z.end + N("killExtension")); }
            if (Code == "SCT-N5-W01" && marked) C.Pickup(position, 0, 0, () => {
                if (Time.time < nextInfoPickup) return; nextInfoPickup = Time.time + N("pickupIcd"); info = true; C.NextAttackRange = N("nextRange");
            });
            if (Code == "SCT-N5-G01" && marked && Time.time < laserUntil) FireLaser();
        }
        public override void Tick(float dt)
        {
            if (Code != "SCT-W07") return;
            Vector2 direction = C.MoveDirection;
            if (direction == Vector2.zero) { movingTime = 0; return; }
            movingTime += dt;
            if (lastMove != Vector2.zero && movingTime >= N("moveTime")
                && Vector2.Angle(lastMove, direction) >= N("turnDegrees") - .1f && Gate("turn", N("cooldown"))) C.NearestShot(N("coefficients"));
            lastMove = direction;
        }
    }
}

