using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssBar.EnemyAnimations
{
    // Visual-only corpse: no EnemyHealth, Collider or damage behavior, so it
    // cannot delay wave completion or receive another kill/drop/XP event.
    public sealed class EnemyDeathVisual : MonoBehaviour
    {
        private static readonly List<EnemyDeathVisual> living = new List<EnemyDeathVisual>();
        private SpriteRenderer body;
        private EnemySpriteClip clip;
        private GameResultUI result;
        private float elapsed;
        private Color baseColor;
        public static void Spawn(SpriteRenderer source, EnemySpriteClip death)
        {
            if (living.Count >= 96)
            { var oldest = living[0]; living.RemoveAt(0); if (oldest != null) Destroy(oldest.gameObject); }
            var go = new GameObject("Enemy death animation");
            SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
            go.transform.position = source.transform.position; go.transform.rotation = source.transform.rotation;
            go.transform.localScale = source.transform.lossyScale;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = death.frames[0];
            renderer.sharedMaterial = source.sharedMaterial; renderer.color = Color.white;
            renderer.flipX = source.flipX; renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            var visual = go.AddComponent<EnemyDeathVisual>(); visual.body = renderer; visual.clip = death;
            visual.baseColor = renderer.color; visual.result = FindAnyObjectByType<GameResultUI>(); living.Add(visual);
        }
        private void Update()
        {
            // Card selection pauses corpses. A result overlay must not freeze
            // the final enemy's death animation forever at Time.timeScale = 0.
            float dt = result != null && result.HasSettled ? Time.unscaledDeltaTime : Time.deltaTime;
            if (dt <= 0) return;
            elapsed += dt; body.sprite = clip.frames[clip.FrameAt(elapsed, false)];
            float fade = Mathf.Clamp01((elapsed - clip.Duration - .1f) / .25f);
            body.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1 - fade);
            if (fade >= 1) Destroy(gameObject);
        }
        private void OnDestroy() { living.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() { living.Clear(); }
    }
}
