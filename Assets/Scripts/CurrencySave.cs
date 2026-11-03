using UnityEngine;

// Shared wallet. CharacterProgressionSave owns node progress and legacy migration.
// CharacterData assets remain the unchanged base definitions.
public static class CurrencySave
{
    private const string SupplyCoinsKey = "AbyssBar.Save.v1.SupplyCoins";
    private const string SkinTicketsKey = "AbyssBar.Save.v1.SkinTickets";
    private const string HealthTrainingKeyPrefix = "AbyssBar.Save.v1.HealthTraining.";

    // Read from storage instead of keeping a second, potentially stale balance.
    public static int SupplyCoins => Mathf.Max(0, PlayerPrefs.GetInt(SupplyCoinsKey, 0));
    public static int SkinTickets => Mathf.Max(0, PlayerPrefs.GetInt(SkinTicketsKey, 0));

    // Returns the amount actually granted, including when the balance hits int.MaxValue.
    public static int AddSupplyCoins(int amount)
    {
        int grantedAmount = AddCurrencyWithoutSaving(SupplyCoinsKey, amount);
        if (grantedAmount > 0) PlayerPrefs.Save();
        return grantedAmount;
    }

    public static int AddSkinTickets(int amount)
    {
        int grantedAmount = AddCurrencyWithoutSaving(SkinTicketsKey, amount);
        if (grantedAmount > 0) PlayerPrefs.Save();
        return grantedAmount;
    }

    // Battle settlement grants both currencies before requesting one save.
    public static void GrantBattleRewards(int supplyCoins, int skinTickets,
        out int grantedCoins, out int grantedTickets)
    {
        grantedCoins = AddCurrencyWithoutSaving(SupplyCoinsKey, supplyCoins);
        grantedTickets = AddCurrencyWithoutSaving(SkinTicketsKey, skinTickets);
        if (grantedCoins > 0 || grantedTickets > 0) PlayerPrefs.Save();
    }

    private static int AddCurrencyWithoutSaving(string key, int amount)
    {
        if (amount <= 0) return 0;

        int balance = Mathf.Max(0, PlayerPrefs.GetInt(key, 0));
        int grantedAmount = Mathf.Min(amount, int.MaxValue - balance);
        if (grantedAmount > 0)
            PlayerPrefs.SetInt(key, balance + grantedAmount);
        return grantedAmount;
    }

    // Used by the draw save service, which also records ownership and calls Save().
    internal static bool TrySpendSkinTicketsWithoutSaving(int amount)
    {
        if (amount <= 0 || SkinTickets < amount) return false;

        PlayerPrefs.SetInt(SkinTicketsKey, SkinTickets - amount);
        return true;
    }

    // The progression service saves the purchase and wallet changes together.
    internal static bool TrySpendSupplyCoinsWithoutSaving(int amount)
    {
        if (amount <= 0 || SupplyCoins < amount) return false;
        PlayerPrefs.SetInt(SupplyCoinsKey, SupplyCoins - amount);
        return true;
    }

    internal static int AddSupplyCoinsWithoutSaving(int amount)
    {
        return AddCurrencyWithoutSaving(SupplyCoinsKey, amount);
    }

    internal static int GetLegacyHealthTrainingLevel(CharacterData character)
    {
        if (character == null || string.IsNullOrWhiteSpace(character.CharacterId)) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(HealthTrainingKeyPrefix + character.CharacterId, 0), 0, 5);
    }

    // Only purchased node-2 HP is added; retired legacy HP stays excluded.
    public static int GetStartingMaxHealth(CharacterData character)
    {
        if (character == null) return 1;
        int basis = Mathf.Max(1, character.BaseMaxHealth);
        return basis + Mathf.Min(CharacterProgressionSave.GetBonuses(character).MaxHealth, int.MaxValue - basis);
    }
}
