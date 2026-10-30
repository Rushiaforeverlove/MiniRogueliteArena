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

        GetComponent<PlayerController>()
            .SetStartingMoveSpeed(characterData.BaseMoveSpeed);
        GetComponent<PlayerHealth>()
            .SetStartingHealth(characterData.BaseMaxHealth);
        GetComponent<PlayerAutoAttack>()
            .SetStartingWeapon(characterData.StartingWeapon);

        if (characterData.GameplaySprite != null)
        {
            GetComponent<SpriteRenderer>().sprite = characterData.GameplaySprite;
        }
    }
}