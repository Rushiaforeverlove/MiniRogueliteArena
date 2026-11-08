using System;
using System.Collections.Generic;

namespace AbyssBar.Enemies
{
    public static class SentinelPoolPreset
    {
        public static EnemyPoolRow[] Build(EnemySpawnPool source, EnemyDefinition sentinel)
        {
            if (source == null || sentinel == null || source.rows == null) throw new ArgumentException("Missing pool or sentinel definition.");
            var milestones = new SortedSet<int> { 1, 8, 9, 10 };
            foreach (var row in source.rows) if (row != null) milestones.Add(row.firstWave);
            var result = new List<EnemyPoolRow>();
            foreach (int wave in milestones)
            {
                if (!source.TrySnapshot(wave, out var entries, out string error)) throw new ArgumentException(error);
                var list = new List<EnemyPoolEntry>(entries);
                if (wave >= 8 && !list.Exists(e => e.enemy.enemyId == sentinel.enemyId))
                    list.Add(new EnemyPoolEntry { enemy = sentinel, weight = 5, maximumAlive = Cap(wave), minimumSpawns = 1 });
                result.Add(new EnemyPoolRow { firstWave = wave, entries = list.ToArray() });
            }
            return result.ToArray();
        }
        public static int Cap(int wave) => wave < 8 ? 0 : wave == 8 ? 1 : wave == 9 ? 2 : 3;
    }
}
