using System;
using System.Collections.Generic;

namespace AbyssBar.Enemies
{
    public static class CorrosivePoolPreset
    {
        public static EnemyPoolRow[] Build(EnemySpawnPool source, EnemyDefinition corrosive)
        {
            if (source == null || corrosive == null || source.rows == null) throw new ArgumentException("Missing pool or corrosive definition.");
            var milestones = new SortedSet<int> { 1, 6, 7, 8, 9, 10 };
            foreach (var row in source.rows) if (row != null) milestones.Add(row.firstWave);
            var result = new List<EnemyPoolRow>();
            foreach (int wave in milestones)
            {
                if (!source.TrySnapshot(wave, out var entries, out string error)) throw new ArgumentException(error);
                var list = new List<EnemyPoolEntry>(entries);
                if (wave >= 6 && !list.Exists(e => e.enemy.enemyId == corrosive.enemyId))
                    list.Add(new EnemyPoolEntry { enemy = corrosive, weight = 6, maximumAlive = Cap(wave), minimumSpawns = 1 });
                result.Add(new EnemyPoolRow { firstWave = wave, entries = list.ToArray() });
            }
            return result.ToArray();
        }
        public static int Cap(int wave) => wave < 6 ? 0 : wave <= 7 ? 1 : wave <= 9 ? 2 : 3;
    }
}
