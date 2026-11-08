using UnityEngine;

namespace AbyssBar.Enemies
{
    // Movement has one owner (EnemyController). Future charge/melee behaviors
    // provide an intent here instead of adding competing Rigidbody movers.
    public interface IEnemyMovementPolicy
    {
        Vector2 MovementDirection(Transform target, Vector2 position);
    }

    // A battle context can be supplied to any future combat behavior without
    // adding a new enemy-type branch to EnemySpawner.
    public interface IEnemyCombatBinding
    {
        bool IsConfigured { get; }
        void Bind(Transform target, EnemyCombatWorld world);
    }
}
