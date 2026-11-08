using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class ItemUpgradeEffects : BuiltinUpgradeEffect
    {
        public ItemUpgradeEffects(string code) : base(code) { }
        private float nextDrone;
        private OrbitConstruct orbit;
        public override void Acquired(int previousRank)
        {
            if (Code == "ITM-P01" && previousRank == 0) nextDrone = Time.time + N("interval");
            if (Code == "ITM-W06" || Code == "ITM-P04")
            {
                if (orbit == null)
                {
                    var go = new GameObject(Data.title); go.transform.SetParent(C.transform); go.AddComponent<RunOwnedObject>();
                    orbit = go.AddComponent<OrbitConstruct>();
                }
                orbit.Configure(C, Data, Rank, Code == "ITM-P04");
            }
        }
        public override void AfterAction(RunAction a)
        {
            if (Code == "ITM-W01" && a.kind == RunActionKind.Attack && Count("attack", (int)N("attackThresholds")))
            {
                var e = C.Nearest(); if (e == null) return;
                float damage = C.AttackBasis * N("coefficient");
                C.Zone(Data.id, e.transform.position, N("radius"), N("duration"), N("tick"), p => C.Area(p, N("radius"), damage));
            }
            if (Code == "ITM-W02" && a.kind == RunActionKind.Dodge)
                C.Zone(Data.id, a.origin, N("radius"), N("durations"), .1f,
                    p => C.Area(p, N("radius"), 0, null, true, false, e => e.Status.Slow(N("slow"), .2f)));
            if (Code == "ITM-W05" && Spending(a, RunActionKind.E) && Gate("flash", N("icd"))) C.NearestShot(N("coefficient"), 1, N("stuns"));
            if (Code == "ITM-P02" && Spending(a, RunActionKind.Q))
            {
                float damage = C.AttackBasis * N("coefficient");
                C.Zone(Data.id, a.origin, N("radius"), N("duration"), N("tick"),
                    p => { foreach (var e in C.Targets(p, N("radius"))) e.Status.Push(p, -N("pull")); },
                    p => C.Area(p, N("radius"), damage));
            }
            if (Code == "ITM-P03" && a.kind == RunActionKind.Dodge && Count("dodge", (int)N("dodgeThreshold")))
            {
                var visited = new HashSet<int>(); Vector2 pos = C.Position;
                for (int i = 0; i < (int)N("targets"); i++)
                {
                    var e = C.Targets(pos).Find(t => !visited.Contains(t.RuntimeId)); if (e == null) break;
                    visited.Add(e.RuntimeId); e.Status.Slow(N("slow"), N("duration"));
                    C.Hit(e, C.AttackBasis * N("coefficient")); pos = e.transform.position;
                    RunVisual.Circle(C, pos, .25f, .3f, Color.magenta);
                }
            }
            if (Code == "ITM-G01")
            {
                if (Spending(a, RunActionKind.Q))
                    C.Zone(Data.id, a.origin, 1, N("duration"), N("interval"), ShootTurret);
                if (Spending(a, RunActionKind.E) && C.FindZone(Data.id) != null && Gate("extra", N("extraRoundIcd"))) ShootTurret(C.FindZone(Data.id).center);
            }
            if (Code == "ITM-P04" && a.kind == RunActionKind.Dodge) C.OrbitBoostUntil = Time.time + N("speedDuration");
        }
        private void ShootTurret(Vector2 p)
        { var e = C.Nearest(p); if (e != null) C.Shot(p, (Vector2)e.transform.position - p, C.AttackBasis * N("coefficient")); }
        public override void Hurt(float amount)
        {
            if (Code == "ITM-W03" && Gate("hurt", N("icd"))) Radial((int)N("counts"), C.AttackBasis * N("coefficient"));
            if (Code == "ITM-G02" && C.Health.CurrentHealth <= C.Health.MaxHealth * N("healthThreshold") && Gate("bell", N("cooldown")))
            {
                C.Area(C.Position, N("radius"), 0, null, true, false, e => { e.Status.Push(C.Position, N("knockback")); e.Status.Stun(N("stun")); });
                for (int i = 0; i < (int)N("packs"); i++) C.Pickup(C.Position + Rotate(Vector2.right, i * 120) * .8f, N("healPerPack"));
            }
        }
        public override void Kill(EnemyHealth enemy, Vector2 position, bool marked, float distance)
        { if (Code == "ITM-W04" && Count("kill", (int)N("killThresholds"))) C.Pickup(position, N("heal")); }
        public override void Tick(float dt)
        {
            if (Code != "ITM-P01" || Time.time < nextDrone) return;
            nextDrone = Time.time + N("interval"); C.Health.AddShield(N("shield"));
            int shots = (int)N("shots");
            for (int i = 0; i < shots; i++) C.Later(i * N("duration") / shots, () => C.NearestShot(N("coefficient")));
        }
    }
}

