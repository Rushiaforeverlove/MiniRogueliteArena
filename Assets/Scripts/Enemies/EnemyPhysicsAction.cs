using UnityEngine;

namespace AbyssBar.Enemies
{
    // Actions request a step; EnemyController remains the only Rigidbody mover.
    // Return false to use normal chase movement, true to override it (including
    // a zero step during anticipation and recovery).
    public interface IEnemyPhysicsAction
    {
        bool TryGetStep(Transform target, Vector2 position, float seconds, out Vector2 displacement);
    }
}
