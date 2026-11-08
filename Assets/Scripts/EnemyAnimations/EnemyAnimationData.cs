using System;
using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    [Serializable]
    public sealed class EnemySpriteClip
    {
        public Sprite[] frames;
        [Min(.1f)] public float fps = 10;
        [Min(0)] public int impactFrame = 4;
        public float Duration => frames != null && fps > 0 ? frames.Length / fps : 0;
        public float ImpactTime => impactFrame / fps;
        public int FrameAt(float seconds, bool loop)
            => EnemyAnimationMath.Frame(seconds, fps, frames != null ? frames.Length : 0, loop);
        public bool IsValid
        {
            get
            {
                if (frames == null || frames.Length == 0 || fps <= 0 || float.IsNaN(fps) || float.IsInfinity(fps)
                    || impactFrame < 0 || impactFrame >= frames.Length) return false;
                foreach (var frame in frames) if (frame == null) return false;
                return true;
            }
        }
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Animation Data")]
    public sealed class EnemyAnimationData : ScriptableObject
    {
        public EnemySpriteClip idle, walk, attack, hurt, death, stun;
        public Vector2 muzzleOffset = new Vector2(0, .65f);
        public bool IsValid => idle != null && idle.IsValid && walk != null && walk.IsValid
            && attack != null && attack.IsValid && attack.impactFrame > 0
            && hurt != null && hurt.IsValid && death != null && death.IsValid && stun != null && stun.IsValid;
    }
}
