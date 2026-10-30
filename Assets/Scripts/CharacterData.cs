using UnityEngine;

[CreateAssetMenu(
    fileName = "Character",
    menuName = "Abyss Bar/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string characterId = "character_01";
    [SerializeField] private string displayName = "Scout";

    [Header("Visuals")]
    [SerializeField] private Sprite portrait;
    [SerializeField] private Sprite gameplaySprite;

    [Header("Starting Stats")]
    [SerializeField, Min(1)] private int baseMaxHealth = 5;
    [SerializeField, Min(0.1f)] private float baseMoveSpeed = 5f;
    [SerializeField] private WeaponData startingWeapon;
    public string CharacterId => characterId;
    public string DisplayName => displayName;
    public Sprite Portrait => portrait;
    public Sprite GameplaySprite => gameplaySprite;
    public int BaseMaxHealth => baseMaxHealth;
    public float BaseMoveSpeed => baseMoveSpeed;
    public WeaponData StartingWeapon => startingWeapon;
}