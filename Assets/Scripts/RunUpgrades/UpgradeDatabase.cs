using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    [Serializable] public class UpgradeRosterEntry
    {
        public CharacterData character;
        public bool unlockedByDefault = true;
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Upgrades/Database")]
    public sealed class UpgradeDatabase : ScriptableObject
    {
        private static readonly HashSet<string> positive = new HashSet<string> {
            "orbitPeriod", "burnInterval", "interval", "shotInterval", "tick", "shieldInterval", "extraRoundIcd", "icd", "targetIcd",
            "perBottleTargetIcd", "sharedTargetIcd", "shieldIcd", "duration", "durations", "slowDuration", "markDuration", "staggerDuration",
            "moveTime", "window", "delay", "cooldown", "resetAfter", "pickupIcd", "countIcd", "invulnerability", "stability",
            "stabilityDurations", "speedDuration", "burnDuration", "pathRadius", "radius", "orbitRadius", "hitRadius", "width",
            "minimumDistance", "distance", "healthPerRank", "damageThreshold", "absorbedThreshold" };
        private static readonly HashSet<string> counts = new HashSet<string> {
            "count", "counts", "charges", "qCharges", "dodgeCharges", "restoreE", "extraCounts", "extraTargets", "extraAttackTargets",
            "targets", "maxTargets", "attackThresholds", "killThresholds", "attackHits", "eSpends", "shots", "satellites", "initialMarks",
            "capacity", "initialTokens", "supportShots", "hurtEvents", "packs", "killThreshold", "dodgeThreshold", "maxPickupsPerCast",
            "targetsPerAction", "targetsPerCast", "healthPerRank" };
        public UpgradeCardData[] cards = Array.Empty<UpgradeCardData>();
        public UpgradeRosterEntry[] roster = Array.Empty<UpgradeRosterEntry>();
        public float[] rarityWeights = { 70, 25, 5 };
        public float[] categoryWeights = { 30, 20, 35, 15 };
        public bool IsRoleUnlocked(string id)
        {
            foreach (var entry in roster ?? Array.Empty<UpgradeRosterEntry>())
                if (entry?.character != null && entry.character.CharacterId == id)
                    return entry.unlockedByDefault || PlayerPrefs.GetInt("AbyssBar.Save.v3.UnlockedRole." + id, 0) == 1;
            return false;
        }
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (cards == null) { errors.Add("Missing card array."); return errors; }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var c in cards)
            {
                if (c == null || string.IsNullOrWhiteSpace(c.id) || !ids.Add(c.id))
                { errors.Add("Missing/duplicate card ID."); continue; }
                if (!UpgradeEffectRegistry.Supports(c.effectKey)) errors.Add(c.id + ": unknown effect " + c.effectKey);
                if (c.maxRank < 0 || !FiniteNonnegative(c.drawWeight)) errors.Add(c.id + ": invalid rank/weight");
                if ((int)c.quality < 0 || (int)c.quality > 2 || (int)c.category < 0 || (int)c.category > 3) errors.Add(c.id + ": invalid quality/category");
                if ((c.category == UpgradeCategory.Personal || c.category == UpgradeCategory.Support) && string.IsNullOrWhiteSpace(c.ownerRoleId))
                    errors.Add(c.id + ": missing owner role ID");
                var keys = new HashSet<string>();
                foreach (var n in c.numbers ?? Array.Empty<UpgradeNumber>())
                {
                    if (n == null || string.IsNullOrEmpty(n.key) || !keys.Add(n.key) || n.values == null || n.values.Length == 0)
                    { errors.Add(c.id + ": invalid numeric parameter"); continue; }
                    foreach (float v in n.values)
                    {
                        if (float.IsNaN(v) || float.IsInfinity(v)) errors.Add(c.id + ": nonfinite parameter");
                        if (positive.Contains(n.key) && v <= 0) errors.Add(c.id + ": " + n.key + " must be positive");
                        if (counts.Contains(n.key) && (v < 1 || v != Mathf.RoundToInt(v))) errors.Add(c.id + ": " + n.key + " must be a positive integer");
                    }
                }
                foreach (var required in UpgradeEffectRegistry.RequiredParameters(c.effectKey))
                    if (!keys.Contains(required)) errors.Add(c.id + ": missing parameter " + required);
            }
            if (rarityWeights == null || rarityWeights.Length != 3 || categoryWeights == null || categoryWeights.Length != 4)
                errors.Add("Expected 3 quality weights and 4 category weights.");
            else
            {
                foreach (var weights in new[] { rarityWeights, categoryWeights })
                {
                    bool any = false;
                    foreach (float w in weights) { if (!FiniteNonnegative(w)) errors.Add("Invalid draw weight."); if (w > 0) any = true; }
                    if (!any) errors.Add("At least one weight per tier must be positive.");
                }
            }
            return errors;
        }
        private static bool FiniteNonnegative(float v) => v >= 0 && !float.IsNaN(v) && !float.IsInfinity(v);
    }
}

