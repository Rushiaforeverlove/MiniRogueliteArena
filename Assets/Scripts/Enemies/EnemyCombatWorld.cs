using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Enemies
{
    // A battle-scoped owner. Shots survive their shooter's death, but never
    // survive settlement, disabling the spawner or unloading the battle scene.
    public sealed class EnemyCombatWorld : MonoBehaviour
    {
        private PlayerHealth player;
        private GameResultUI result;
        private Camera gameplayCamera;
        private bool ended;
        private CorrosionFieldWorld corrosion;
        private readonly HashSet<EnemyProjectile> projectiles = new HashSet<EnemyProjectile>();
        public PlayerHealth Player => player;
        public bool Active => !ended && isActiveAndEnabled && player != null && player.IsAlive
            && result != null && !result.HasSettled;
        public void Initialize(PlayerHealth target, GameResultUI resultUI, Camera camera)
        {
            if (result != null) result.BattleFinished -= OnFinished;
            player = target; result = resultUI; gameplayCamera = camera; ended = false;
            if (result != null) result.BattleFinished += OnFinished;
        }
        public bool Visible(Vector2 position)
        {
            if (gameplayCamera == null) return false;
            Vector3 p = gameplayCamera.WorldToViewportPoint(position);
            return p.z > 0 && p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1;
        }
        public bool TryFire(Vector2 origin, Vector2 direction, float damage, RangedEnemySettings settings)
        {
            if (!Active || Time.timeScale <= 0 || projectiles.Count >= 32 || settings == null || !settings.IsValid) return false;
            var go = new GameObject("Riftmaw shot"); go.transform.SetParent(transform, false);
            var shot = go.AddComponent<EnemyProjectile>();
            projectiles.Add(shot); shot.Initialize(this, origin, direction, damage, settings); return true;
        }
        public void Forget(EnemyProjectile shot) { projectiles.Remove(shot); }
        public CorrosionFieldWorld GetCorrosionFields(CorrosiveEnemySettings settings)
        {
            if (corrosion == null)
            {
                corrosion = gameObject.AddComponent<CorrosionFieldWorld>();
                corrosion.Initialize(this, settings.maximumFields, settings.tickInterval);
            }
            return corrosion;
        }
        private void OnFinished(bool victory) { End(); }
        private void End()
        {
            ended = true;
            if (corrosion != null) corrosion.EndBattle();
            foreach (var shot in new List<EnemyProjectile>(projectiles)) if (shot != null) Destroy(shot.gameObject);
            projectiles.Clear();
        }
        private void OnDisable()
        { if (result != null) result.BattleFinished -= OnFinished; End(); }
    }
}
