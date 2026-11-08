using UnityEngine;

namespace AbyssBar.Enemies
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Charge Settings")]
    public sealed class ChargeAttackSettings : ScriptableObject
    {
        [Min(.1f)] public float triggerRange = 6;
        [Min(.1f)] public float windupSeconds = .8f;
        [Min(.1f)] public float distance = 6;
        [Min(.1f)] public float speed = 10;
        [Min(.1f)] public float recoverySeconds = 1;
        [Min(.1f)] public float cooldownAfterRush = 3;
        [Min(1)] public int legacyDamage = 2;
        public Material warningMaterial;
        public bool IsValid => Positive(triggerRange) && Positive(windupSeconds) && Positive(distance)
            && Positive(speed) && Positive(recoverySeconds) && Positive(cooldownAfterRush)
            && legacyDamage > 0 && warningMaterial != null;
        private static bool Positive(float f) => f > 0 && !float.IsNaN(f) && !float.IsInfinity(f);
    }
}
