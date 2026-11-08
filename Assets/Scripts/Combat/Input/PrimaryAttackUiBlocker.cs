using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssBar.Combat
{
    // Put this marker on future modal roots. Ordinary passive HUD graphics are
    // ignored; interactive controls block even without a modal marker.
    public sealed class PrimaryAttackUiBlocker : MonoBehaviour { }
    public sealed class PrimaryAttackUiQuery
    {
        private readonly List<RaycastResult> results = new List<RaycastResult>();
        private EventSystem lastSystem;
        private PointerEventData pointer;
        public bool IsBlocked(Vector2 position)
        {
            var system = EventSystem.current;
            if (system == null) return false;
            if (system != lastSystem) { lastSystem = system; pointer = new PointerEventData(system); }
            pointer.Reset(); pointer.position = position; results.Clear(); system.RaycastAll(pointer, results);
            foreach (var hit in results)
            {
                if (hit.gameObject == null || !hit.gameObject.activeInHierarchy) continue;
                if (hit.gameObject.GetComponentInParent<PrimaryAttackUiBlocker>() != null
                    || hit.gameObject.GetComponentInParent<Selectable>() != null) return true;
            }
            return false;
        }
    }
}
