using UnityEngine;

namespace AbyssBar.Mio
{
    // Shares the role key already used by the run-upgrade roster.
    public static class CharacterUnlockSave
    {
        private const string Prefix = "AbyssBar.Save.v3.UnlockedRole.";
        public static bool IsUnlocked(CharacterData data) => data != null
            && (data.UnlockedByDefault || PlayerPrefs.GetInt(Prefix + data.CharacterId, 0) == 1);
        public static bool TryPurchase(CharacterData data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.CharacterId) || IsUnlocked(data) || data.UnlockCost <= 0) return false;
            if (!CurrencySave.TrySpendSupplyCoinsWithoutSaving(data.UnlockCost)) return false;
            PlayerPrefs.SetInt(Prefix + data.CharacterId, 1); PlayerPrefs.Save(); return true;
        }
    }
}
