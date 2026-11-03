using UnityEngine;

[RequireComponent(
    typeof(PlayerController), typeof(PlayerHealth), typeof(SpriteRenderer))]
public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] private CharacterData characterData;

    private void Start()
    {
        if (CharacterSelection.Current != null)
        {
            characterData = CharacterSelection.Current;
        }
        if (characterData == null)
        {
            Debug.LogError("PlayerCharacter: Character Data is missing.", this);
            return;
        }

        CharacterProgressionSave.MigrateLegacyTraining(characterData);
        CharacterTrainingBonuses bonuses = CharacterProgressionSave.GetBonuses(characterData);

        GetComponent<PlayerController>()
            .SetStartingMoveSpeed(characterData.BaseMoveSpeed);
        GetComponent<PlayerHealth>()
            .SetStartingHealth(CurrencySave.GetStartingMaxHealth(characterData));
        GetComponent<PlayerAutoAttack>()
            .SetStartingWeapon(characterData.StartingWeapon, bonuses.Damage, bonuses.AttackSpeed);

        GetComponent<PlayerDodge>()?.SetStartingDistanceBonus(bonuses.DodgeDistance);

        PlayerActiveSkill activeSkill = GetComponent<PlayerActiveSkill>();
        if (activeSkill != null)
        {
            activeSkill.SetStartingDamageBonus(bonuses.Damage);
            if (CharacterProgressionSave.IsEEnhanced(characterData) && !characterData.HasConfiguredEnhancedESkill)
                Debug.LogWarning("Enhanced E data is missing or invalid. Using the base E until it is restored.", this);
            if (CharacterProgressionSave.IsQEnhanced(characterData) && !characterData.HasConfiguredEnhancedQSkill)
                Debug.LogWarning("Enhanced Q data is missing or invalid. Using the base Q until it is restored.", this);
            activeSkill.SetStartingSkills(CharacterProgressionSave.GetBattleESkill(characterData), CharacterProgressionSave.GetBattleQSkill(characterData),
                CharacterProgressionSave.IsQUnlocked(characterData));
        }
        else
            Debug.LogWarning("Add PlayerActiveSkill to the combat Player to enable E skills.", this);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite sprite = CharacterSkinSave.GetGameplaySprite(characterData);
        if (sprite != null)
            spriteRenderer.sprite = sprite;
        spriteRenderer.color = CharacterSkinSave.GetTint(characterData);
        GetComponent<DamageFlash>()?.SetBaseColor(spriteRenderer.color);
    }
}
