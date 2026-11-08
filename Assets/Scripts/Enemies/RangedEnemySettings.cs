using UnityEngine;

namespace AbyssBar.Enemies
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Ranged Settings")]
    public sealed class RangedEnemySettings : ScriptableObject
    {
        [Min(.5f)] public float attackRange = 5.5f;
        [Min(.1f)] public float aimSeconds = .8f;
        [Min(.1f)] public float recoverySeconds = 2f;
        [Min(.1f)] public float projectileSpeed = 6f;
        [Min(.1f)] public float projectileLifetime = 3f;
        [Min(.01f)] public float projectileRadius = .12f;
        [Min(1)] public int legacyDamage = 1;
        public Material effectMaterial;
        public bool IsValid => Positive(attackRange) && Positive(aimSeconds) && Positive(recoverySeconds)
            && Positive(projectileSpeed) && Positive(projectileLifetime) && Positive(projectileRadius)
            && legacyDamage > 0 && effectMaterial != null;
        private static bool Positive(float n) => n > 0 && !float.IsNaN(n) && !float.IsInfinity(n);
    }
}
