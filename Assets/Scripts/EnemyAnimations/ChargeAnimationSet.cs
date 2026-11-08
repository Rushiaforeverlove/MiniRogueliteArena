using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Charge Animations")]
    public sealed class ChargeAnimationSet : ScriptableObject
    {
        public EnemyAnimationData common;
        public EnemySpriteClip windup, rush, wallImpact, recovery;
        public bool IsValid => common != null && common.IsValid && windup != null && windup.IsValid
            && rush != null && rush.IsValid && wallImpact != null && wallImpact.IsValid
            && recovery != null && recovery.IsValid;
    }
}
