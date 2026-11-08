using System;
using System.Collections.Generic;

namespace AbyssBar.Enemies
{
    // Multipliers only reduce incoming damage. Status/critical bonuses are
    // resolved by the attacker before EnemyHealth applies these defenses.
    public interface IEnemyDamageMitigation { float IncomingDamageMultiplier { get; } }

    // A specialized reaction may own its hit animation, while damage and kill
    // ownership stay with EnemyHealth. Return true to suppress generic hurt.
    public interface IEnemyHitReaction { bool OnHitReceived(float incoming, float applied); }

    public static class EnemyDamageMitigation
    {
        public static float Resolve(float damage, IEnumerable<IEnemyDamageMitigation> defenses)
        {
            if (damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage)) return 0;
            if (defenses == null) return damage;
            foreach (var defense in defenses)
            {
                if (defense == null) continue;
                float multiplier = defense.IncomingDamageMultiplier;
                if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) continue;
                damage *= Math.Max(0, Math.Min(1, multiplier));
            }
            return damage;
        }
    }
}
