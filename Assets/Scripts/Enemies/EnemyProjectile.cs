using System.Collections.Generic;
using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Enemies
{
    public sealed class EnemyProjectile : MonoBehaviour
    {
        private EnemyCombatWorld world;
        private Vector2 position, direction;
        private float speed, radius, until, damage;
        private bool consumed;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        public void Initialize(EnemyCombatWorld owner, Vector2 origin, Vector2 heading, float hitDamage, RangedEnemySettings settings)
        {
            world = owner; position = origin; direction = heading.sqrMagnitude > .0001f ? heading.normalized : Vector2.right;
            damage = hitDamage; speed = settings.projectileSpeed; radius = settings.projectileRadius;
            until = Time.time + settings.projectileLifetime; transform.position = position;
            var visual = gameObject.AddComponent<SpriteRenderer>();
            visual.sprite = RunVisual.CircleSprite; visual.sharedMaterial = settings.effectMaterial;
            visual.color = new Color(.15f, 1f, .9f); visual.sortingOrder = 29000;
            transform.localScale = Vector3.one * (radius * 2);
            var core = new GameObject("Bright core"); core.transform.SetParent(transform, false);
            core.transform.localScale = Vector3.one * .5f;
            var center = core.AddComponent<SpriteRenderer>(); center.sprite = visual.sprite;
            center.sharedMaterial = settings.effectMaterial; center.color = Color.white; center.sortingOrder = 29001;
        }
        private void Update()
        { if (world == null || !world.Active || Time.time >= until) Consume(); }
        private void FixedUpdate()
        {
            if (consumed || world == null || !world.Active || Time.time >= until) { Consume(); return; }
            if (Time.timeScale <= 0) return;
            float distance = speed * Time.fixedDeltaTime;
            hits.Clear();
            Physics2D.CircleCast(position, radius, direction, ContactFilter2D.noFilter, hits, distance);
            // A wall wins ties; hit order is independent of physics query ordering.
            hits.Sort((a, b) => {
                int order = a.distance.CompareTo(b.distance);
                if (order != 0) return order;
                return IsBlocker(a.collider).CompareTo(IsBlocker(b.collider)) * -1;
            });
            foreach (var hit in hits)
            {
                var c = hit.collider; if (c == null || c.isTrigger) continue;
                var target = c.GetComponentInParent<PlayerHealth>();
                if (target != null)
                {
                    if (target != world.Player) continue;
                    if (target.GetComponent<PlayerDodge>()?.IsDodging == true) continue;
                    target.ReceiveDamage(damage); Consume(); return;
                }
                if (IsBlocker(c)) { Consume(); return; }
            }
            position += direction * distance; transform.position = position;
        }
        public static bool IsBlocker(Collider2D c) => c != null && !c.isTrigger
            && c.GetComponentInParent<EnemyHealth>() == null && c.GetComponentInParent<PlayerHealth>() == null;
        private void Consume() { if (consumed) return; consumed = true; Destroy(gameObject); }
        private void OnDestroy() { if (world != null) world.Forget(this); }
    }
}
