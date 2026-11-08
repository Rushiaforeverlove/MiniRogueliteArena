using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    // A death behavior may replace the generic visual with a battle-scoped
    // aftermath. It never owns kill/XP/wave accounting.
    public interface IEnemyDeathPresentation
    {
        bool TryPlayDeath(SpriteRenderer source);
    }
}
