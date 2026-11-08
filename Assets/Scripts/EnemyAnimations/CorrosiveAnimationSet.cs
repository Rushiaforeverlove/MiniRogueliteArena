using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Corrosive Animations")]
    public sealed class CorrosiveAnimationSet : ScriptableObject
    {
        public EnemyAnimationData common;
        public EnemySpriteClip inflate, burst, formation, bubbling, dissolve;
        public bool IsValid => common != null && common.IsValid && inflate != null && inflate.IsValid
            && burst != null && burst.IsValid && formation != null && formation.IsValid
            && bubbling != null && bubbling.IsValid && dissolve != null && dissolve.IsValid;
    }
}
