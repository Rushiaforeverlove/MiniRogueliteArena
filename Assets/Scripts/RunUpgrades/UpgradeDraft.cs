using System;
using System.Collections.Generic;
using System.Linq;

namespace AbyssBar.Upgrades
{
    // Pure draw service: no UI, Unity random state, saves, or battle effects.
    public static class UpgradeDraft
    {
        public static List<UpgradeCardData> Draw(UpgradeDatabase db, string role, bool q, bool nodeFive,
            IReadOnlyDictionary<string, int> ranks, Func<string, bool> unlocked, Random random, int slots = 4)
        {
            var pool = db.cards.Where(c => Eligible(c, role, q, nodeFive, ranks, unlocked)
                && db.rarityWeights[(int)c.quality] > 0 && db.categoryWeights[(int)c.category] > 0).ToList();
            var result = new List<UpgradeCardData>();
            while (pool.Count > 0 && result.Count < slots)
            {
                // Empty groups are removed BEFORE rolling their tier; weights renormalize.
                var qualities = pool.Select(c => c.quality).Distinct().OrderBy(v => v).ToList();
                var quality = Pick(qualities, v => db.rarityWeights[(int)v], random);
                var sameQuality = pool.Where(c => c.quality == quality).ToList();
                var categories = sameQuality.Select(c => c.category).Distinct().OrderBy(v => v).ToList();
                var category = Pick(categories, v => db.categoryWeights[(int)v], random);
                var group = sameQuality.Where(c => c.category == category).ToList();
                if (category == UpgradeCategory.Support)
                {
                    var owners = group.Select(c => c.ownerRoleId).Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList();
                    string owner = owners[random.Next(owners.Count)];
                    group.RemoveAll(c => c.ownerRoleId != owner);
                }
                var chosen = Pick(group, c => c.drawWeight, random);
                result.Add(chosen);
                pool.RemoveAll(c => c.id == chosen.id);
            }
            return result;
        }
        public static bool Eligible(UpgradeCardData c, string role, bool q, bool nodeFive,
            IReadOnlyDictionary<string, int> ranks, Func<string, bool> unlocked)
        {
            if (c == null || !UpgradeEffectRegistry.Supports(c.effectKey) || c.drawWeight <= 0) return false;
            if (c.requiresQ && !q || c.requiresNodeFive && !nodeFive) return false;
            foreach (string prerequisite in c.requiredCardIds ?? Array.Empty<string>())
                if (!ranks.TryGetValue(prerequisite, out int acquired) || acquired <= 0) return false;
            if (c.category == UpgradeCategory.Personal && c.ownerRoleId != role) return false;
            if (c.category == UpgradeCategory.Support && (c.ownerRoleId == role || !unlocked(c.ownerRoleId))) return false;
            return c.maxRank == 0 || !ranks.TryGetValue(c.id, out int rank) || rank < c.maxRank;
        }
        private static T Pick<T>(List<T> values, Func<T, float> weight, Random random)
        {
            double total = values.Sum(v => Math.Max(0, weight(v)));
            if (total <= 0) return values[random.Next(values.Count)];
            double roll = random.NextDouble() * total;
            foreach (T value in values) { roll -= Math.Max(0, weight(value)); if (roll < 0) return value; }
            return values[values.Count - 1];
        }
    }
}

