using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    public interface IEnemyAnimationAction
    {
        bool TryGetPose(out EnemySpriteClip clip, out int frame, out Vector2 facing);
    }
}
