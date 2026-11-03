using UnityEngine;

// Cosmetic data only. Combat stats continue to come from CharacterData and training.
[CreateAssetMenu(fileName = "CharacterSkin", menuName = "Abyss Bar/Character Skin")]
public class CharacterSkinData : ScriptableObject
{
    [SerializeField] private string skinId = "skin_01";
    [SerializeField] private string displayName = "預設造型";
    [SerializeField] private Sprite gameplaySprite;
    [SerializeField] private Sprite seatedSprite;
    [SerializeField] private Sprite portrait;
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private bool unlockedByDefault = true;

    public string SkinId => skinId;
    public string DisplayName => displayName;
    public Sprite GameplaySprite => gameplaySprite;
    public Sprite SeatedSprite => seatedSprite;
    public Sprite Portrait => portrait;
    public Color Tint => tint;
    public bool UnlockedByDefault => unlockedByDefault;
}
