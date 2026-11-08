using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Sentinel Animations")]
    public sealed class SentinelAnimationSet : ScriptableObject
    {
        public EnemyAnimationData common;
        public EnemySpriteClip block, windup, slam, recovery;
        public bool IsValid => common != null && common.IsValid && block != null && block.IsValid
            && windup != null && windup.IsValid && slam != null && slam.IsValid && recovery != null && recovery.IsValid;
    }
}
