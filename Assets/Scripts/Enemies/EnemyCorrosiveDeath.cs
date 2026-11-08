using AbyssBar.EnemyAnimations;
using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Enemies
{
    public sealed class EnemyCorrosiveDeath : MonoBehaviour, IEnemyCombatBinding, IEnemyDeathPresentation
    {
        [SerializeField] private CorrosiveEnemySettings settings;
        [SerializeField] private CorrosiveAnimationSet animations;
        private EnemyCombatWorld world;
        public bool IsConfigured => settings != null && settings.IsValid && animations != null && animations.IsValid;
        public CorrosiveAnimationSet Animations => animations;
        public void Bind(Transform target, EnemyCombatWorld owner) { world = owner; }
        public bool TryPlayDeath(SpriteRenderer source)
        {
            if (!IsConfigured || world == null || !world.Active) return false;
            var health = GetComponent<EnemyHealth>();
            return world.GetCorrosionFields(settings).Spawn(source, animations, settings,
                CombatBalance.Damage(settings.legacyDamage) * (health != null ? health.Status.OutgoingMultiplier : 1));
        }
    }
}
