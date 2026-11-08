using UnityEngine;

namespace AbyssBar.Upgrades
{
    [CreateAssetMenu(menuName = "Abyss Bar/Combat Balance")]
    public sealed class CombatBalance : ScriptableObject
    {
        [Min(1)] public int playerBaseHealthScale = 200;
        [Min(1)] public int damageScale = 100;
        [Min(1)] public int enemyHealthScale = 100;
        [Min(1)] public int flatTrainingScale = 100;
        private static CombatBalance cached;
        private static CombatBalance Profile => cached != null ? cached : cached = Resources.Load<CombatBalance>("CombatBalance");
        public static int PlayerHealth(int legacy) => Multiply(legacy, Profile != null ? Profile.playerBaseHealthScale : 200);
        public static int Damage(int legacy) => Multiply(legacy, Profile != null ? Profile.damageScale : 100);
        public static int EnemyHealth(int legacy) => Multiply(legacy, Profile != null ? Profile.enemyHealthScale : 100);
        public static int Training(int legacy) => Multiply(legacy, Profile != null ? Profile.flatTrainingScale : 100);
        private static int Multiply(int value, int factor) => (int)System.Math.Min(int.MaxValue, (long)System.Math.Max(0, value) * System.Math.Max(1, factor));
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache() { cached = null; }
    }
}

