using UnityEngine;

namespace AbyssBar.Enemies
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Corrosive Settings")]
    public sealed class CorrosiveEnemySettings : ScriptableObject
    {
        [Min(.1f)] public float warningSeconds = .6f;
        [Min(.1f)] public float radius = 1.5f;
        [Min(.1f)] public float activeSeconds = 4;
        [Min(.1f)] public float tickInterval = .5f;
        [Min(.1f)] public float fadeSeconds = .5f;
        [Min(1)] public int legacyDamage = 1;
        [Range(1, 32)] public int maximumFields = 6;
        public Material material;
        public bool IsValid => Positive(warningSeconds) && Positive(radius) && Positive(activeSeconds)
            && Positive(tickInterval) && Positive(fadeSeconds) && legacyDamage > 0
            && maximumFields >= 1 && maximumFields <= 32 && material != null;
        private static bool Positive(float f) => f > 0 && !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
