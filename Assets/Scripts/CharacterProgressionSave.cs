using UnityEngine;

// Permanent progression uses CharacterId, never DisplayName or a scene object ID.
public static class CharacterProgressionSave
{
    public enum PurchaseResult
    {
        Purchased, AlreadyCompleted, NotEnoughCoins, InvalidCharacter,
        InvalidNode, NotImplemented, MissingQSkill, InvalidCost,
        Ready, PrerequisiteNotMet, MissingTrainingData, ProgressChanged, MissingEnhancedESkill, MissingEnhancedQSkill
    }

    private const string StepsPrefix = "AbyssBar.Save.v2.TrainingSteps.";
    private const string MigrationPrefix = "AbyssBar.Save.v2.LegacyTrainingMigrated.";
    private const string RefundTotalPrefix = "AbyssBar.Save.v2.LegacyRefundTotal.";
    private const string RefundPendingPrefix = "AbyssBar.Save.v2.LegacyRefundPending.";
    public const int TotalPurchases = 7;
    public const int ImplementedPurchases = 6;
    private static readonly int[] NodeStarts = { 0, 1, 4, 5, 6 };
    private static readonly int[] NodeSizes = { 1, 3, 1, 1, 1 };
    private static readonly string[] NodeTitles = { "解鎖 Q", "數值培養", "強化 E", "強化 Q", "專屬拓展" };

    public static bool HasCharacterId(CharacterData character)
    {
        return character != null && !string.IsNullOrWhiteSpace(character.CharacterId);
    }

    public static int GetCompletedPurchases(CharacterData character)
    {
        if (!HasCharacterId(character)) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(StepsPrefix + character.CharacterId, 0), 0, ImplementedPurchases);
    }

    public static bool IsQUnlocked(CharacterData character) => GetCompletedPurchases(character) >= 1;
    public static bool IsEEnhanced(CharacterData character) => GetCompletedPurchases(character) >= 5;
    public static bool IsQEnhanced(CharacterData character) => GetCompletedPurchases(character) >= 6;
    public static ActiveSkillData GetBattleQSkill(CharacterData character)
    {
        if (character == null) return null;
        return IsQEnhanced(character) && character.HasConfiguredEnhancedQSkill
            ? character.EnhancedQSkill : character.UltimateQSkill;
    }

    // Removing an enhancement asset never erases progress or disables the base E.
    public static ActiveSkillData GetBattleESkill(CharacterData character)
    {
        if (character == null) return null;
        return IsEEnhanced(character) && character.HasConfiguredEnhancedESkill
            ? character.EnhancedESkill : character.StartingESkill;
    }
    public static int GetNodeSize(int node) => node >= 0 && node < 5 ? NodeSizes[node] : 0;
    public static string GetNodeTitle(int node) => node >= 0 && node < 5 ? NodeTitles[node] : "未知節點";
    public static int GetNodeProgress(CharacterData character, int node)
    {
        if (node < 0 || node >= 5) return 0;
        return Mathf.Clamp(GetCompletedPurchases(character) - NodeStarts[node], 0, NodeSizes[node]);
    }

    public static int GetLegacyRefundTotal(CharacterData character)
    {
        if (!HasCharacterId(character)) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(RefundTotalPrefix + character.CharacterId, 0), 0, 300);
    }

    public static int GetPendingRefund(CharacterData character)
    {
        if (!HasCharacterId(character)) return 0;
        return Mathf.Clamp(PlayerPrefs.GetInt(RefundPendingPrefix + character.CharacterId, 0),
            0, GetLegacyRefundTotal(character));
    }

    // Returns only coins credited by this call. Existing currency/cosmetic keys are retained.
    public static int MigrateLegacyTraining(CharacterData character)
    {
        if (!HasCharacterId(character)) return 0;
        string id = character.CharacterId;
        bool initialized = PlayerPrefs.GetInt(MigrationPrefix + id, 0) != 1;
        if (initialized)
        {
            int level = CurrencySave.GetLegacyHealthTrainingLevel(character);
            // Original prices were 20, 40, 60, 80, 100. Sum = 10 * L * (L + 1).
            int refund = 10 * level * (level + 1);
            PlayerPrefs.SetInt(RefundTotalPrefix + id, refund);
            PlayerPrefs.SetInt(RefundPendingPrefix + id, refund);
            PlayerPrefs.SetInt(MigrationPrefix + id, 1);
        }

        int pending = GetPendingRefund(character);
        int credited = CurrencySave.AddSupplyCoinsWithoutSaving(pending);
        if (credited > 0) PlayerPrefs.SetInt(RefundPendingPrefix + id, pending - credited);
        // A full wallet keeps the uncredited remainder for a later visit.
        if (initialized || credited > 0) PlayerPrefs.Save();
        return credited;
    }

    public static CharacterTrainingBonuses GetBonuses(CharacterData character)
    {
        CharacterTrainingBonuses result = new CharacterTrainingBonuses();
        if (character == null) return result;
        int completed = GetNodeProgress(character, 1);
        for (int i = 0; i < completed; i++)
        {
            CharacterStatTrainingStep step = character.GetStatTrainingStep(i);
            if (step == null || !step.HasValidEffect) continue;
            switch (step.Kind)
            {
                case TrainingStatKind.MaxHealth: result.MaxHealth += Mathf.RoundToInt(step.Amount); break;
                case TrainingStatKind.Damage: result.Damage += Mathf.RoundToInt(step.Amount); break;
                case TrainingStatKind.AttackSpeed: result.AttackSpeed += step.Amount; break;
                case TrainingStatKind.DodgeDistance: result.DodgeDistance += step.Amount; break;
            }
        }
        return result;
    }

    public static int GetPurchaseCost(CharacterData character, int node)
    {
        if (character == null) return 0;
        if (node == 0) return IsQUnlocked(character) ? 0 : character.QUnlockCost;
        if (node == 2) return IsEEnhanced(character) ? 0 : character.EEnhanceCost;
        if (node == 3) return IsQEnhanced(character) ? 0 : character.QEnhanceCost;
        int progress = GetNodeProgress(character, 1);
        return node == 1 && progress < 3 ? character.GetStatTrainingStep(progress)?.Cost ?? 0 : 0;
    }

    // Pure query for UI; migration and mutations happen in the purchase operation.
    public static PurchaseResult GetPurchaseStatus(CharacterData character, int node)
    {
        if (!HasCharacterId(character)) return PurchaseResult.InvalidCharacter;
        if (node < 0 || node >= 5) return PurchaseResult.InvalidNode;
        if (node > 3) return PurchaseResult.NotImplemented;
        if (GetNodeProgress(character, node) >= GetNodeSize(node)) return PurchaseResult.AlreadyCompleted;
        if (node == 1 && !IsQUnlocked(character)) return PurchaseResult.PrerequisiteNotMet;
        if (node == 2 && GetNodeProgress(character, 1) < 3) return PurchaseResult.PrerequisiteNotMet;
        if (node == 3 && !IsEEnhanced(character)) return PurchaseResult.PrerequisiteNotMet;
        if (node == 0 && (character.UltimateQSkill == null || !character.UltimateQSkill.IsConfigured))
            return PurchaseResult.MissingQSkill;
        if (node == 1 && !character.HasConfiguredStatTraining) return PurchaseResult.MissingTrainingData;
        if (node == 2 && !character.HasConfiguredEnhancedESkill) return PurchaseResult.MissingEnhancedESkill;
        if (node == 3 && !character.HasConfiguredEnhancedQSkill) return PurchaseResult.MissingEnhancedQSkill;
        int cost = GetPurchaseCost(character, node);
        if (cost <= 0) return PurchaseResult.InvalidCost;
        return CurrencySave.SupplyCoins < cost ? PurchaseResult.NotEnoughCoins : PurchaseResult.Ready;
    }

    public static PurchaseResult TryPurchaseNode(CharacterData character, int node)
    {
        return TryPurchaseNode(character, node, GetCompletedPurchases(character));
    }

    public static PurchaseResult TryPurchaseNode(CharacterData character, int node, int expectedCompletedPurchases)
    {
        if (!HasCharacterId(character)) return PurchaseResult.InvalidCharacter;
        if (node < 0 || node >= 5) return PurchaseResult.InvalidNode;
        if (node > 3) return PurchaseResult.NotImplemented;
        MigrateLegacyTraining(character);
        PurchaseResult status = GetPurchaseStatus(character, node);
        if (status != PurchaseResult.Ready) return status;
        int completed = GetCompletedPurchases(character);
        if (completed != expectedCompletedPurchases) return PurchaseResult.ProgressChanged;
        int cost = GetPurchaseCost(character, node);
        if (!CurrencySave.TrySpendSupplyCoinsWithoutSaving(cost)) return PurchaseResult.NotEnoughCoins;

        PlayerPrefs.SetInt(StepsPrefix + character.CharacterId, completed + 1);
        // Wallet and completed purchase count are included in the same save request.
        PlayerPrefs.Save();
        return PurchaseResult.Purchased;
    }
}
