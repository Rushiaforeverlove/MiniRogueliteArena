using System.Collections.Generic;
using UnityEngine;

// Draws uniformly from valid, distinct, unowned skins in the configured pool.
public static class SkinDrawService
{
    public enum DrawResult
    {
        Drawn,
        NotEnoughTickets,
        CollectedAll,
        InvalidPool
    }

    // Also used by the UI, so the displayed catalogue matches the actual pool.
    public static List<SkinDrawEntry> GetPoolEntries(SkinDrawPoolData pool)
    {
        var entries = new List<SkinDrawEntry>();
        if (pool == null) return entries;

        var identities = new HashSet<string>();
        foreach (SkinDrawEntry entry in pool.Entries)
        {
            if (entry == null || !CharacterSkinSave.BelongsToCharacter(entry.Character, entry.Skin))
                continue;

            string identity = entry.Character.CharacterId + "." + entry.Skin.SkinId;
            if (identities.Add(identity)) entries.Add(entry);
        }

        return entries;
    }

    public static DrawResult TryDraw(SkinDrawPoolData pool, out SkinDrawEntry reward)
    {
        reward = null;
        if (pool == null || pool.DrawCost <= 0) return DrawResult.InvalidPool;

        List<SkinDrawEntry> entries = GetPoolEntries(pool);
        if (entries.Count == 0) return DrawResult.InvalidPool;

        var candidates = new List<SkinDrawEntry>();
        foreach (SkinDrawEntry entry in entries)
        {
            if (!CharacterSkinSave.IsOwned(entry.Character, entry.Skin)) candidates.Add(entry);
        }

        if (candidates.Count == 0) return DrawResult.CollectedAll;
        if (CurrencySave.SkinTickets < pool.DrawCost) return DrawResult.NotEnoughTickets;

        SkinDrawEntry selected = candidates[Random.Range(0, candidates.Count)];
        CharacterSkinSave.SkinPurchaseResult purchase = CharacterSkinSave.TryBuySkin(
            selected.Character, selected.Skin, pool.DrawCost);

        if (purchase == CharacterSkinSave.SkinPurchaseResult.NotEnoughTickets)
            return DrawResult.NotEnoughTickets;
        if (purchase != CharacterSkinSave.SkinPurchaseResult.Purchased)
            return DrawResult.InvalidPool;

        reward = selected;
        return DrawResult.Drawn;
    }
}
