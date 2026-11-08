using System;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public enum UpgradeQuality { White, Purple, Gold }
    public enum UpgradeCategory { General, Item, Personal, Support }
    [Serializable] public class UpgradeNumber
    {
        public string key;
        public float[] values;
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Upgrades/Card")]
    public sealed class UpgradeCardData : ScriptableObject
    {
        public string id, title, ownerRoleId, effectKey;
        [TextArea(3, 8)] public string description;
        public UpgradeQuality quality;
        public UpgradeCategory category;
        public Sprite icon;
        [TextArea] public string artBrief;
        [Min(0)] public int maxRank = 1; // 0 means unlimited; runtime stores ranks separately.
        [Min(0)] public float drawWeight = 1;
        public bool requiresQ, requiresNodeFive;
        public string[] requiredCardIds = Array.Empty<string>();
        public UpgradeNumber[] numbers = Array.Empty<UpgradeNumber>();
        public float Number(string key, int rank = 1, float fallback = 0)
        {
            foreach (var n in numbers)
                if (n != null && n.key == key && n.values != null && n.values.Length > 0)
                    return n.values[Math.Max(0, Math.Min(rank - 1, n.values.Length - 1))];
            return fallback;
        }
    }
}

