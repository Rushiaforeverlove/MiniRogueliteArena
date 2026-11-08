using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssBar.Enemies
{
    // One battle owns every corrosive aftermath, its active-field budget and
    // one shared damage clock. Overlapping fields never multiply hit attempts.
    public sealed class CorrosionFieldWorld : MonoBehaviour
    {
        private EnemyCombatWorld owner;
        private int maximumFields;
        private float interval, nextDamageTime;
        private bool ended;
        private readonly List<CorrosionAftermath> entries = new List<CorrosionAftermath>();
        private readonly List<CorrosionAftermath> active = new List<CorrosionAftermath>();
        private Collider2D[] playerColliders;
        public int ActiveFieldCount => active.Count;
        public int VisualCount => entries.Count;
        public void Initialize(EnemyCombatWorld battle, int cap, float damageInterval)
        {
            owner = battle; maximumFields = Mathf.Clamp(cap, 1, 32); interval = Mathf.Max(.0001f, damageInterval);
            playerColliders = owner.Player != null ? owner.Player.GetComponentsInChildren<Collider2D>() : new Collider2D[0];
        }
        public bool Spawn(SpriteRenderer source, EnemyAnimations.CorrosiveAnimationSet clips, CorrosiveEnemySettings settings, float damage)
        {
            if (ended || owner == null || !owner.Active || source == null || settings == null || clips == null || !clips.IsValid || !settings.IsValid
                || damage < 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return false;
            if (entries.Count >= 48) Remove(entries[0]);
            var go = new GameObject("Corrosive death and field");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene); go.transform.SetParent(transform, false);
            go.transform.position = source.transform.position;
            var actor = go.AddComponent<CorrosionAftermath>(); actor.Initialize(source, clips, settings, damage);
            entries.Add(actor); return true;
        }
        private void Update()
        {
            if (!ended && (owner == null || !owner.Active)) EndBattle();
            if (!ended && Time.timeScale <= 0) return;
            float seconds = ended ? Time.unscaledDeltaTime : Time.deltaTime;
            if (seconds <= 0) return;
            foreach (var actor in entries.ToArray())
            {
                if (actor == null) { entries.Remove(actor); active.Remove(actor); continue; }
                actor.Advance(seconds);
                if (actor.IsFinished) { Remove(actor); continue; }
                if (!ended && actor.Lifetime.Phase == CorrosionPhase.Active && !active.Contains(actor))
                {
                    if (active.Count >= maximumFields)
                    {
                        var oldest = active[0]; active.RemoveAt(0); oldest.BeginFade();
                    }
                    active.Add(actor);
                }
                if (actor.Lifetime.Phase != CorrosionPhase.Active) active.Remove(actor);
            }
            if (ended) return;
            if (active.Count == 0) { nextDamageTime = Time.time; return; }
            if (Time.time + .000001f < nextDamageTime) return;
            nextDamageTime = Time.time + interval; // No backlog of damage after a long frame.
            if (owner.Player == null || !owner.Player.IsAlive || owner.Player.GetComponent<PlayerDodge>()?.IsDodging == true) return;
            float strongest = 0;
            foreach (var actor in active)
                if (TouchesPlayer(actor)) strongest = Mathf.Max(strongest, actor.Damage);
            if (strongest > 0) owner.Player.ReceiveDamage(strongest);
        }
        private bool TouchesPlayer(CorrosionAftermath actor)
        {
            foreach (var collider in playerColliders)
            {
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                Vector2 closest = collider.ClosestPoint(actor.Center);
                if ((closest - actor.Center).sqrMagnitude > actor.Radius * actor.Radius) continue;
                bool wall = false;
                foreach (var hit in Physics2D.LinecastAll(actor.Center, closest))
                    if (EnemyProjectile.IsBlocker(hit.collider)) { wall = true; break; }
                if (!wall) return true;
            }
            return false;
        }
        public void EndBattle()
        {
            if (ended) return; ended = true; active.Clear();
            foreach (var actor in entries) if (actor != null) actor.CancelHazard();
            // Keep only the death-body animation, advancing unscaled under the
            // result overlay; pending and existing damaging fields are gone now.
        }
        private void Remove(CorrosionAftermath actor)
        { entries.Remove(actor); active.Remove(actor); if (actor != null) Destroy(actor.gameObject); }
        private void OnDisable()
        {
            ended = true;
            foreach (var actor in entries.ToArray()) if (actor != null) Destroy(actor.gameObject);
            entries.Clear(); active.Clear();
        }
    }
}
