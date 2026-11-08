using UnityEngine;

namespace AbyssBar.Enemies
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Sentinel Settings")]
    public sealed class SentinelAttackSettings : ScriptableObject
    {
        [Range(0, .95f)] public float armorReduction = .4f;
        [Min(.1f)] public float triggerRange = 2.4f;
        [Min(0)] public float centerOffset = .9f;
        [Min(.1f)] public float slamRadius = 1.8f;
        [Min(.1f)] public float windupSeconds = 1;
        [Min(.1f)] public float slamSeconds = .35f;
        [Min(.1f)] public float recoverySeconds = 1.4f;
        [Min(.1f)] public float cooldownAfterImpact = 3.5f;
        [Min(1)] public int legacyDamage = 3;
        public Material warningMaterial;
        public bool IsValid => armorReduction >= 0 && armorReduction <= .95f && !float.IsNaN(armorReduction)
            && Positive(triggerRange) && centerOffset >= 0 && !float.IsNaN(centerOffset) && !float.IsInfinity(centerOffset)
            && Positive(slamRadius) && Positive(windupSeconds) && Positive(slamSeconds) && Positive(recoverySeconds)
            && Positive(cooldownAfterImpact) && legacyDamage > 0 && warningMaterial != null;
        private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
