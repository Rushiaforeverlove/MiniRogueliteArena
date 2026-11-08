using System;
using System.Collections.Generic;
using UnityEngine;
namespace AbyssBar.Upgrades
{
    public sealed class RunPickup : MonoBehaviour
    {
        private RunUpgradeRuntime owner;
        private float heal, shield, expires;
        private Action collected;
        private Vector2 worldPosition;
        public static void Spawn(RunUpgradeRuntime owner, Vector2 at, float heal, float shield, Action collected)
        {
            var go = RunVisual.Marker(owner, at, .22f, heal > 0 ? Color.green : Color.cyan);
            var p = go.AddComponent<RunPickup>(); p.owner = owner; p.heal = heal; p.shield = shield;
            p.expires = Time.time + 5; p.collected = collected; p.worldPosition = at;
        }
        private void Update()
        {
            if (owner == null || !owner.Active || Time.time >= expires) { Destroy(gameObject); return; }
            transform.position = worldPosition;
            if (Time.timeScale <= 0) return;
            if (Vector2.Distance(worldPosition, owner.Position) > .55f) return;
            enabled = false; owner.Health.Heal(heal); owner.Health.AddShield(shield);
            collected?.Invoke(); Destroy(gameObject);
        }
    }
}

