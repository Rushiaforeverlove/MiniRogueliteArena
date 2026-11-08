using UnityEngine;

[CreateAssetMenu(
    fileName = "Character",
    menuName = "Abyss Bar/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string characterId = "character_01";
    [Tooltip("Fallback name. When Identity Profile is assigned, its Character Name is displayed.")]
    [SerializeField] private string displayName = "Scout";
    [Tooltip("Optional presentation profile. Keep Character Id stable when renaming.")]
    [SerializeField] private AbyssBar.Characters.CharacterProfileData identityProfile;

    [Header("Visuals")]
    [SerializeField] private bool unlockedByDefault = true;
    [SerializeField, Min(1)] private int unlockCost = 500;
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite gameplaySprite;

    [Header("Skins")]
    [SerializeField] private CharacterSkinData[] skins;

    [Header("Starting Stats")]
    [SerializeField, Min(1)] private int baseMaxHealth = 5;
    [SerializeField, Min(0.1f)] private float baseMoveSpeed = 5f;
    [SerializeField] private WeaponData startingWeapon;

    [Header("Starting Active Skill")]
    [SerializeField] private ActiveSkillData startingESkill;
    [Tooltip("Defines Q's effect. Assigning it does not unlock Q.")]
    [SerializeField] private ActiveSkillData ultimateQSkill;

    [Header("Permanent Training")]
    [SerializeField, Min(1)] private int qUnlockCost = 100;
    [Tooltip("Exactly three purchases for node 2. Configure Kind, Amount and Cost for each.")]
    [SerializeField] private CharacterStatTrainingStep[] statTrainingSteps = new CharacterStatTrainingStep[3];

    [Header("Node 3 - Enhanced E")]
    [SerializeField, Min(1)] private int eEnhanceCost = 150;
    [Tooltip("Use a separate, configured skill asset; keep Starting E Skill as the base version.")]
    [SerializeField] private ActiveSkillData enhancedESkill;

    [Header("Node 4 - Enhanced Q")]
    [SerializeField, Min(1)] private int qEnhanceCost = 200;
    [SerializeField] private ActiveSkillData enhancedQSkill;

    public string CharacterId => characterId;
    public bool UnlockedByDefault => unlockedByDefault;
    public int UnlockCost => unlockCost;
    public string DisplayName => identityProfile != null && !string.IsNullOrWhiteSpace(identityProfile.characterName)
        ? identityProfile.characterName : displayName;
    public AbyssBar.Characters.CharacterProfileData IdentityProfile => identityProfile;
    public Sprite Portrait => portrait;
    public Sprite GameplaySprite => gameplaySprite;
    public CharacterSkinData[] Skins => skins ?? System.Array.Empty<CharacterSkinData>();
    public int BaseMaxHealth => AbyssBar.Upgrades.CombatBalance.PlayerHealth(baseMaxHealth);
    public float BaseMoveSpeed => baseMoveSpeed;
    public WeaponData StartingWeapon => startingWeapon;
    public ActiveSkillData StartingESkill => startingESkill;
    public ActiveSkillData UltimateQSkill => ultimateQSkill;
    [Header("Node 5 - Run Upgrade Expansion")][SerializeField, Min(1)] private int nodeFiveCost = 250;
    [SerializeField] private CharacterSkinData masteryRewardSkin;
    public int NodeFiveCost => nodeFiveCost;
    public CharacterSkinData MasteryRewardSkin => masteryRewardSkin;
    public int QUnlockCost => qUnlockCost;
    public int EEnhanceCost => eEnhanceCost;
    public ActiveSkillData EnhancedESkill => enhancedESkill;
    public bool HasConfiguredEnhancedESkill => enhancedESkill != null
        && enhancedESkill != startingESkill && enhancedESkill.IsConfigured;
    public int QEnhanceCost => qEnhanceCost;
    public ActiveSkillData EnhancedQSkill => enhancedQSkill;
    public bool HasConfiguredEnhancedQSkill => enhancedQSkill != null
        && enhancedQSkill != ultimateQSkill && enhancedQSkill.IsConfigured;
    public CharacterStatTrainingStep GetStatTrainingStep(int index)
    {
        return statTrainingSteps == null || index < 0 || index >= statTrainingSteps.Length
            ? null : statTrainingSteps[index];
    }
    public bool HasConfiguredStatTraining
    {
        get
        {
            if (statTrainingSteps == null || statTrainingSteps.Length != 3) return false;
            foreach (CharacterStatTrainingStep step in statTrainingSteps)
                if (step == null || !step.IsConfigured) return false;
            return true;
        }
    }
    public string GetStatTrainingPreview(int index) => GetStatTrainingStep(index)?.Description ?? "效果待設定";
}


