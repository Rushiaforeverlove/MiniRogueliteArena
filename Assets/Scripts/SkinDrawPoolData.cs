using UnityEngine;

[System.Serializable]
public class SkinDrawEntry
{
    [SerializeField] private CharacterData character;
    [SerializeField] private CharacterSkinData skin;

    public CharacterData Character => character;
    public CharacterSkinData Skin => skin;
}

[CreateAssetMenu(fileName = "SkinDrawPool", menuName = "Abyss Bar/Skin Draw Pool")]
public class SkinDrawPoolData : ScriptableObject
{
    [SerializeField, Min(1)] private int drawCost = 5;
    [SerializeField] private SkinDrawEntry[] entries;

    public int DrawCost => drawCost;
    public SkinDrawEntry[] Entries => entries ?? System.Array.Empty<SkinDrawEntry>();
}
