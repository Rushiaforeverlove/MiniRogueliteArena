using System;
using UnityEngine;

namespace AbyssBar.Waves
{
    [Serializable]
    public sealed class BattleWaveDefinition
    {
        [Min(1)] public int enemyCount = 20;
        [Min(.05f)] public float spawnInterval = .8f;
        [Min(1)] public int maximumAlive = 20;
        public BattleWaveDefinition Copy() => new BattleWaveDefinition {
            enemyCount = enemyCount, spawnInterval = spawnInterval, maximumAlive = maximumAlive };
        public bool IsValid => enemyCount > 0 && maximumAlive > 0 && spawnInterval >= .05f
            && !float.IsNaN(spawnInterval) && !float.IsInfinity(spawnInterval);
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Waves/Battle Wave Plan", fileName = "BattleWavePlan")]
    public sealed class BattleWavePlan : ScriptableObject
    {
        public string planId = "station_clear_ten_v1";
        [Min(0)] public float intermissionSeconds = 5;
        public BattleWaveDefinition[] waves = CreatePreviewWaves();
        public static BattleWaveDefinition[] CreatePreviewWaves()
        {
            var result = new BattleWaveDefinition[10];
            for (int i = 0; i < result.Length; i++) result[i] = new BattleWaveDefinition();
            return result;
        }
        public bool TrySnapshot(out BattleWaveDefinition[] definitions, out string error)
        {
            definitions = null; error = null;
            if (waves == null || waves.Length == 0) error = "A wave plan must contain at least one wave.";
            else if (intermissionSeconds < 0 || float.IsNaN(intermissionSeconds) || float.IsInfinity(intermissionSeconds)) error = "Invalid intermission duration.";
            else for (int i = 0; i < waves.Length; i++)
                if (waves[i] == null || !waves[i].IsValid) { error = "Invalid wave " + (i + 1); break; }
            if (error != null) return false;
            definitions = new BattleWaveDefinition[waves.Length];
            for (int i = 0; i < waves.Length; i++) definitions[i] = waves[i].Copy();
            return true;
        }
    }
}
