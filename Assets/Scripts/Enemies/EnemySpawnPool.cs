using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Enemies
{
    [Serializable]
    public sealed class EnemyPoolEntry
    {
        public EnemyDefinition enemy;
        [Min(.001f)] public float weight = 1;
        [Min(1)] public int maximumAlive = 90;
        [Min(0)] public int minimumSpawns = 0;
        public EnemyPoolEntry Copy() => new EnemyPoolEntry {
            enemy = enemy, weight = weight, maximumAlive = maximumAlive, minimumSpawns = minimumSpawns
        };
    }

    [Serializable]
    public sealed class EnemyPoolRow
    {
        [Min(1)] public int firstWave = 1;
        public EnemyPoolEntry[] entries;
    }

    [CreateAssetMenu(menuName = "Abyss Bar/Enemies/Enemy Spawn Pool")]
    public sealed class EnemySpawnPool : ScriptableObject
    {
        public EnemyPoolRow[] rows;
        public bool TrySnapshot(int wave, out EnemyPoolEntry[] snapshot, out string error)
        {
            snapshot = null; error = null;
            if (rows == null || rows.Length == 0) { error = "Enemy pool needs rows."; return false; }
            var waveIds = new HashSet<int>(); EnemyPoolRow chosen = null;
            foreach (var row in rows)
            {
                if (row == null || row.firstWave < 1 || !waveIds.Add(row.firstWave)
                    || row.entries == null || row.entries.Length == 0)
                { error = "Enemy pool rows need unique positive firstWave and entries."; return false; }
                var ids = new HashSet<string>();
                foreach (var entry in row.entries)
                {
                    if (entry == null || entry.enemy == null) { error = "Missing enemy entry."; return false; }
                    if (!entry.enemy.Validate(out error)) return false;
                    if (!ids.Add(entry.enemy.enemyId) || entry.weight <= 0 || float.IsNaN(entry.weight)
                        || float.IsInfinity(entry.weight) || entry.maximumAlive < 1 || entry.minimumSpawns < 0)
                    { error = "Invalid or duplicate enemy pool entry."; return false; }
                }
                if (row.firstWave <= wave && (chosen == null || row.firstWave > chosen.firstWave)) chosen = row;
            }
            if (chosen == null) { error = "Enemy pool has no row for wave " + wave; return false; }
            snapshot = new EnemyPoolEntry[chosen.entries.Length];
            for (int i = 0; i < snapshot.Length; i++) snapshot[i] = chosen.entries[i].Copy();
            return true;
        }
    }
}
