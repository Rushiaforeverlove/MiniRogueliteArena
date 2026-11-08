using System;
using System.Collections.Generic;

namespace AbyssBar.Enemies
{
    public static class ChargePoolPreset
    {
        // Initial data only. Runtime reads EnemySpawnPool; subsequent prepares
        // preserve its tuned entries instead of recalculating this preset.
        public static EnemyPoolRow[] Build(EnemySpawnPool source, EnemyDefinition charger)
        {
            if (source == null || charger == null) throw new ArgumentException("Missing source pool or charger.");
            var milestones = new SortedSet<int> { 1, 4, 5, 6, 7, 8, 9, 10 };
            if (source.rows == null) throw new ArgumentException("Missing source rows.");
            foreach (var row in source.rows) if (row != null) milestones.Add(row.firstWave);
            var result = new List<EnemyPoolRow>();
            foreach (int wave in milestones)
            {
                if (!source.TrySnapshot(wave, out var entries, out string error)) throw new ArgumentException(error);
                var list = new List<EnemyPoolEntry>(entries);
                if (wave >= 4 && !list.Exists(e => e.enemy.enemyId == charger.enemyId))
                    list.Add(new EnemyPoolEntry { enemy = charger, weight = 8, maximumAlive = Cap(wave), minimumSpawns = 1 });
                result.Add(new EnemyPoolRow { firstWave = wave, entries = list.ToArray() });
            }
            return result.ToArray();
        }
        public static int Cap(int wave) => wave < 4 ? 0 : wave == 4 ? 1 : wave <= 6 ? 2 : wave <= 8 ? 3 : 4;
    }
}
