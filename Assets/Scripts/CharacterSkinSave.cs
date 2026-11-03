using UnityEngine;

// Per-character cosmetic ownership and selection. Paid unlocks use the shared
// skin-ticket balance. Cosmetic changes never alter character stats.
public static class CharacterSkinSave
{
    public enum SkinPurchaseResult
    {
        Purchased,
        AlreadyOwned,
        NotEnoughTickets,
        InvalidSkin,
        InvalidCost
    }

    private const string EquippedKeyPrefix = "AbyssBar.Save.v1.EquippedSkin.";
    private const string OwnedKeyPrefix = "AbyssBar.Save.v1.OwnedSkin.";

    public static CharacterSkinData GetEquippedSkin(CharacterData character)
    {
        if (!HasCharacterId(character)) return null;

        string savedId = PlayerPrefs.GetString(EquippedKeyPrefix + character.CharacterId, "");
        CharacterSkinData fallback = null;
        foreach (CharacterSkinData skin in character.Skins)
        {
            if (!IsOwned(character, skin)) continue;

            if (fallback == null) fallback = skin;
            if (skin.SkinId == savedId) return skin;
        }

        // Missing/removed/locked selections fall back to the first owned entry.
        return fallback;
    }

    public static bool IsOwned(CharacterData character, CharacterSkinData skin)
    {
        if (!BelongsToCharacter(character, skin)) return false;

        return skin.UnlockedByDefault || PlayerPrefs.GetInt(
            OwnedKeyPrefix + character.CharacterId + "." + skin.SkinId, 0) == 1;
    }

    public static bool TryEquip(CharacterData character, CharacterSkinData skin)
    {
        if (!IsOwned(character, skin)) return false;

        string key = EquippedKeyPrefix + character.CharacterId;
        if (PlayerPrefs.GetString(key, "") == skin.SkinId) return true;

        PlayerPrefs.SetString(key, skin.SkinId);
        PlayerPrefs.Save();
        return true;
    }

    public static SkinPurchaseResult TryBuySkin(CharacterData character, CharacterSkinData skin, int cost)
    {
        if (cost <= 0) return SkinPurchaseResult.InvalidCost;
        if (!BelongsToCharacter(character, skin)) return SkinPurchaseResult.InvalidSkin;
        if (IsOwned(character, skin)) return SkinPurchaseResult.AlreadyOwned;
        if (!CurrencySave.TrySpendSkinTicketsWithoutSaving(cost))
            return SkinPurchaseResult.NotEnoughTickets;

        // Persist both balance and ownership with one save request. Equipping is separate.
        PlayerPrefs.SetInt(OwnedKeyPrefix + character.CharacterId + "." + skin.SkinId, 1);
        PlayerPrefs.Save();
        return SkinPurchaseResult.Purchased;
    }

    public static Sprite GetGameplaySprite(CharacterData character)
    {
        if (character == null) return null;

        CharacterSkinData skin = GetEquippedSkin(character);
        return skin != null && skin.GameplaySprite != null
            ? skin.GameplaySprite : character.GameplaySprite;
    }

    public static Sprite GetSeatedSprite(CharacterData character)
    {
        CharacterSkinData skin = GetEquippedSkin(character);
        return skin != null && skin.SeatedSprite != null
            ? skin.SeatedSprite : GetGameplaySprite(character);
    }

    public static Color GetTint(CharacterData character)
    {
        CharacterSkinData skin = GetEquippedSkin(character);
        return skin != null ? skin.Tint : Color.white;
    }

    private static bool HasCharacterId(CharacterData character)
    {
        return character != null && !string.IsNullOrWhiteSpace(character.CharacterId);
    }

    public static bool BelongsToCharacter(CharacterData character, CharacterSkinData skin)
    {
        if (!HasCharacterId(character) || skin == null || string.IsNullOrWhiteSpace(skin.SkinId))
            return false;

        foreach (CharacterSkinData entry in character.Skins)
        {
            if (entry == skin) return true;
        }

        return false;
    }
}
