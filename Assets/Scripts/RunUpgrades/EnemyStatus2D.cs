using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class EnemyStatus2D : MonoBehaviour
    {
        private float markUntil, staggerUntil, stunUntil, resistUntil, continuousStunStart = -10;
        private float slowUntil, slow;
        private EnemyHealth health;
        public bool Marked => Time.time < markUntil;
        public bool Staggered => Time.time < staggerUntil;
        public float OutgoingMultiplier => Staggered ? .85f : 1;
        public float MoveMultiplier
        {
            get
            {
                ExpireStun(); if (Time.time < stunUntil) return 0;
                float strongest = 0; foreach (var s in slows) if (s.until > Time.time) strongest = Mathf.Max(strongest, s.fraction);
                return 1 - strongest;
            }
        }
        private void Awake() { health = GetComponent<EnemyHealth>(); }
        public void Mark(float seconds) { markUntil = Mathf.Max(markUntil, Time.time + seconds); }
        public void Stagger(float seconds) { staggerUntil = Mathf.Max(staggerUntil, Time.time + seconds); }
        public void Slow(float fraction, float seconds)
        {
            // Keep all sources independently: weaker long slows must not extend a strong slow.
            slows.Add(new SlowEntry { fraction = Mathf.Clamp(fraction, 0, .6f), until = Time.time + seconds });
        }
        private struct SlowEntry { public float fraction, until; }
        private readonly System.Collections.Generic.List<SlowEntry> slows = new System.Collections.Generic.List<SlowEntry>();
        private void Update()
        {
            slows.RemoveAll(s => s.until <= Time.time); slow = 0; slowUntil = 0;
            foreach (var s in slows) { slow = Mathf.Max(slow, s.fraction); slowUntil = Mathf.Max(slowUntil, s.until); }
            ExpireStun();
        }
        private void ExpireStun()
        { if (continuousStunStart > -5 && Time.time >= stunUntil) { resistUntil = Mathf.Max(resistUntil, stunUntil + 1); continuousStunStart = -10; } }
        public void Stun(float seconds)
        {
            ExpireStun();
            if (health != null && health.IsBoss) { Slow(.5f, seconds); return; }
            if (Time.time < resistUntil) return;
            if (Time.time >= stunUntil) continuousStunStart = Time.time;
            stunUntil = Mathf.Min(continuousStunStart + 1.5f, Mathf.Max(stunUntil, Time.time + seconds));
        }
        public void Push(Vector2 awayFrom, float distance)
        {
            Vector2 direction = ((Vector2)transform.position - awayFrom).normalized;
            if (direction == Vector2.zero) direction = Vector2.right;
            if (distance < 0) direction = -direction;
            float allowed = Mathf.Abs(distance) * (health != null && health.IsBoss ? .5f : 1);
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) return;
            var hits = new System.Collections.Generic.List<RaycastHit2D>();
            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            rb.Cast(direction, filter, hits, allowed + .02f);
            foreach (var hit in hits)
                if (RunProjectile.IsWall(hit.collider)) allowed = Mathf.Min(allowed, Mathf.Max(0, hit.distance - .02f));
            rb.position += direction * allowed;
        }
    }
}

