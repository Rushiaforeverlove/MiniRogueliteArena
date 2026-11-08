using System;
using System.Collections.Generic;
using UnityEngine;
namespace AbyssBar.Upgrades
{
    public sealed class OrbitConstruct : MonoBehaviour
    {
        private RunUpgradeRuntime owner;
        private UpgradeCardData data;
        private int rank;
        private bool bottles;
        private float phase, shieldAt;
        private readonly List<GameObject> markers = new List<GameObject>();
        private readonly Dictionary<string, float> targetAt = new Dictionary<string, float>();
        public void Configure(RunUpgradeRuntime context, UpgradeCardData definition, int level, bool isBottle)
        {
            owner = context; data = definition; rank = level; bottles = isBottle;
            int count = bottles ? (int)N("count") : (int)N("counts");
            while (markers.Count < count)
                markers.Add(RunVisual.Marker(owner, owner.Position, .25f, bottles ? new Color(.6f, .8f, 1) : Color.white));
        }
        private float N(string key) => data.Number(key, rank);
        private void Update()
        {
            if (owner == null || !owner.Active) { foreach (var m in markers) if (m != null) Destroy(m); Destroy(gameObject); return; }
            if (Time.timeScale <= 0) return;
            float speed = Time.time < owner.OrbitBoostUntil ? 1 + owner.Param("item.orbiting_bottles", "dodgeOrbitSpeedBonus") : 1;
            float step = Time.deltaTime * speed * Mathf.PI * 2 / N("orbitPeriod");
            float oldPhase = phase; phase += step;
            int samples = Mathf.Max(1, Mathf.CeilToInt(step / .08f));
            var candidates = owner.Targets(owner.Position, N("orbitRadius") + 2);
            for (int i = 0; i < markers.Count; i++)
            {
                for (int s = 1; s <= samples; s++)
                {
                    float angle = oldPhase + step * s / samples + Mathf.PI * 2 * i / markers.Count;
                    Vector2 at = owner.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * N("orbitRadius");
                    foreach (var enemy in candidates)
                    {
                        if (enemy == null || !enemy.IsAlive) continue;
                        float edgeDistance = Vector2.Distance(at, enemy.transform.position);
                        foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
                            if (collider.enabled) edgeDistance = Mathf.Min(edgeDistance, Vector2.Distance(at, collider.ClosestPoint(at)));
                        string key = (bottles ? i + ":" : "shared:") + enemy.RuntimeId;
                        if (edgeDistance > N("hitRadius") || RunProjectile.Blocked(owner.Position, at)
                            || RunProjectile.Blocked(at, enemy.transform.position)
                            || targetAt.TryGetValue(key, out float time) && Time.time < time) continue;
                        targetAt[key] = Time.time + N(bottles ? "perBottleTargetIcd" : "sharedTargetIcd");
                        owner.Hit(enemy, owner.AttackBasis * N(bottles ? "contactCoefficient" : "coefficient"));
                        if (bottles && i == 0) Burn(enemy);
                        if (bottles && i == 1) enemy.Status.Slow(N("slow"), N("slowDuration"));
                        if (bottles && i == 2 && Time.time >= shieldAt) { shieldAt = Time.time + N("shieldIcd"); owner.Health.AddShield(N("shield")); }
                    }
                    if (s == samples && markers[i] != null) markers[i].transform.position = at;
                }
            }
            if (targetAt.Count > 500)
            { var keys = new List<string>(targetAt.Keys); foreach (var k in keys) if (targetAt[k] <= Time.time) targetAt.Remove(k); }
        }
        private sealed class BurnState { public EnemyHealth enemy; public float until, next, damage; }
        private readonly Dictionary<int, BurnState> burns = new Dictionary<int, BurnState>();
        private void Burn(EnemyHealth enemy)
        {
            int id = enemy.RuntimeId;
            if (burns.TryGetValue(id, out var old))
            {
                if (old.until > Time.time) { old.until = Time.time + N("burnDuration"); return; }
                BurnTick(id, old); // Deliver due ticks before replacing an expired application.
            }
            var state = new BurnState { enemy = enemy, until = Time.time + N("burnDuration"), next = Time.time,
                damage = owner.AttackBasis * N("burnCoefficient") };
            burns[id] = state; BurnTick(id, state);
        }
        private void BurnTick(int id, BurnState state)
        {
            if (!burns.TryGetValue(id, out var current) || !ReferenceEquals(current, state)) return;
            if (state.enemy == null || !state.enemy.IsAlive) { burns.Remove(id); return; }
            int caughtUp = 0;
            while (state.next < state.until - .00001f && state.next <= Time.time + .00001f && caughtUp++ < 64)
            { state.next += N("burnInterval"); owner.Hit(state.enemy, state.damage); }
            if (state.next >= state.until) { burns.Remove(id); return; }
            owner.Later(Mathf.Max(0, state.next - Time.time), () => BurnTick(id, state));
        }
    }
}

