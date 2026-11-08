using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    // Swept segment collision: fast shots cannot skip walls/targets between physics frames.
    public sealed class RunProjectile : MonoBehaviour
    {
        private RunUpgradeRuntime owner;
        private Vector2 position, direction;
        public Vector2 Direction=>direction;
        private float damage, until, mark;
        private float speed = 8;
        private float remainingRange = float.PositiveInfinity;
        private int maxTargets;
        private bool generated, crossedFireline;
        private RunAction action;
        private Action<EnemyHealth> onHit;
        private ProjectileEffects mechanics;
        private readonly HashSet<int> hit = new HashSet<int>();
        public static void Spawn(RunUpgradeRuntime owner, Vector2 origin, Vector2 direction, float damage, int targets,
            RunAction action, bool generated, float mark, Action<EnemyHealth> onHit, ProjectileEffects? mechanics)
        {
            if (direction.sqrMagnitude < .0001f) direction = Vector2.right;
            var go = RunVisual.Marker(owner, origin, .15f, generated ? new Color(.7f, .8f, 1) : Color.yellow);
            var shot = go.AddComponent<RunProjectile>();
            shot.owner = owner; shot.position = origin; shot.direction = direction.normalized; shot.damage = damage;
            shot.maxTargets = Mathf.Max(1, targets); shot.until = Time.time + 3; shot.mark = mark; shot.onHit = onHit;
            shot.action = action; shot.generated = generated; shot.mechanics = mechanics ?? ProjectileEffects.Default;
            // Reuse the equipped weapon/skill projectile's presentation and flight tuning.
            var source = action != null && action.kind == RunActionKind.E ? owner.Skills.Skill?.ProjectilePrefab
                : action != null && action.kind == RunActionKind.Q ? owner.Skills.UltimateSkill?.ProjectilePrefab : owner.Attack?.ProjectilePrefab;
            var settings = source != null ? source.GetComponent<Projectile>() : null;
            if (settings != null) { shot.speed = settings.Speed; shot.until = Time.time + settings.Lifetime; }
            if (!generated && action != null && action.kind == RunActionKind.Attack && action.projectileRange > 0
                && !float.IsNaN(action.projectileRange) && !float.IsInfinity(action.projectileRange))
            {
                shot.remainingRange = action.projectileRange;
                shot.until = Mathf.Max(shot.until, Time.time + shot.remainingRange / shot.speed + .1f);
            }
            if (!generated && source != null)
            {
                var original = source.GetComponent<SpriteRenderer>(); var renderer = go.GetComponent<SpriteRenderer>();
                if (original != null && original.sprite != null)
                {
                    renderer.sprite = original.sprite; renderer.color = original.color;
                    var parentScale = owner.transform.lossyScale;
                    go.transform.localScale = new Vector3(source.transform.localScale.x / Mathf.Max(.001f, parentScale.x),
                        source.transform.localScale.y / Mathf.Max(.001f, parentScale.y), 1);
                }
            }
            owner.NotifyProjectileCreated(shot,action,generated);
        }
        public static bool IsWall(Collider2D c) => c != null && (c.CompareTag("Wall") || c.transform.root.CompareTag("Wall"));
        public static bool Blocked(Vector2 a, Vector2 b)
        { foreach (var hit in Physics2D.LinecastAll(a, b)) if (IsWall(hit.collider)) return true; return false; }
        private void FixedUpdate()
        {
            if (owner == null || !owner.Active || Time.time >= until) { Destroy(gameObject); return; }
            if (Time.timeScale <= 0) return;
            float step = Mathf.Min(speed * Time.fixedDeltaTime, remainingRange);
            if (step <= .000001f) { Destroy(gameObject); return; }
            Vector2 next = position + direction * step;
            float wallDistance = Vector2.Distance(position, next) + 1;
            foreach (var w in Physics2D.LinecastAll(position, next)) if (IsWall(w.collider)) wallDistance = Mathf.Min(wallDistance, w.distance);
            var candidates = owner.Targets(position, Vector2.Distance(position, next) + 3);
            candidates.Sort((a, b) => Vector2.Dot((Vector2)a.transform.position - position, direction).CompareTo(Vector2.Dot((Vector2)b.transform.position - position, direction)));
            if (!generated && !crossedFireline && owner.FirelineIntersects(position, next, out float boost))
            { crossedFireline = true; damage *= 1 + boost; }
            foreach (var enemy in candidates)
            {
                if (enemy == null || !enemy.IsAlive || hit.Contains(enemy.RuntimeId)) continue;
                Vector2 closest = Closest(position, next, enemy.transform.position);
                float edgeDistance = Vector2.Distance(closest, enemy.transform.position);
                foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
                    if (collider.enabled) edgeDistance = Mathf.Min(edgeDistance, Vector2.Distance(closest, collider.ClosestPoint(closest)));
                if (edgeDistance > .1f || Vector2.Distance(position, closest) >= wallDistance) continue;
                hit.Add(enemy.RuntimeId);
                if (mark > 0) enemy.Status.Mark(mark);
                if (mechanics.ExplosionRadius > 0)
                {
                    owner.Area(enemy.transform.position, mechanics.ExplosionRadius, damage, action, generated, false);
                    Destroy(gameObject); return;
                }
                owner.Hit(enemy, damage, action, generated, false, position, hit.Count);
                onHit?.Invoke(enemy);
                if (hit.Count >= maxTargets) { Destroy(gameObject); return; }
            }
            if (wallDistance <= Vector2.Distance(position, next)) { Destroy(gameObject); return; }
            position = next; transform.position = position;
            remainingRange = Mathf.Max(0, remainingRange - step);
            if (remainingRange <= .000001f) Destroy(gameObject);
        }
        public static Vector2 Closest(Vector2 a, Vector2 b, Vector2 p)
        { var d = b - a; return a + d * (d.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, d) / d.sqrMagnitude)); }
    }
}

