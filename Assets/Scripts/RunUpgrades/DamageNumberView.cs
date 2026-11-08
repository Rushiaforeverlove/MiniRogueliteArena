using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class DamageNumberView : MonoBehaviour
    {
        private float expires;
        private Vector3 at;
        private TMP_Text label;
        private RunUpgradeRuntime owner;
        // Pool belongs to the battle player; no scene references survive a scene change.
        public static void Show(Vector3 at, float amount, RunUpgradeRuntime owner)
        {
            var views = owner.damageNumberPool;
            DamageNumberView view = null;
            foreach (var v in views) if (!v.gameObject.activeSelf) { view = v; break; }
            if (view == null && views.Count < 96)
            {
                var go = new GameObject("Damage number"); go.transform.SetParent(owner.transform); go.AddComponent<RunOwnedObject>();
                view = go.AddComponent<DamageNumberView>(); view.label = go.AddComponent<TextMeshPro>();
                views.Add(view);
                view.label.fontSize = 3; view.label.alignment = TextAlignmentOptions.Center;
                view.label.rectTransform.sizeDelta = new Vector2(3, 1);
                var renderer = view.label.GetComponent<MeshRenderer>(); renderer.sortingOrder = 30000;
                var playerRenderer = owner.GetComponent<SpriteRenderer>();
                if (playerRenderer != null) renderer.sortingLayerID = playerRenderer.sortingLayerID;
            }
            if (view == null) return;
            view.gameObject.SetActive(true); view.owner = owner; view.at = at + Vector3.up * .5f;
            view.expires = Time.time + .65f; view.label.text = Mathf.Max(1, Mathf.RoundToInt(amount)).ToString("N0");
            view.label.color = amount >= owner.AttackBasis * 2 ? new Color(1, .65f, .2f) : Color.white;
            view.transform.position = view.at;
        }
        private void LateUpdate()
        {
            if (owner == null || !owner.Active || Time.time >= expires) { gameObject.SetActive(false); return; }
            transform.position = at + Vector3.up * (.65f - (expires - Time.time));
        }
    }
}

