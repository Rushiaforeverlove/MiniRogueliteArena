using UnityEngine;

namespace AbyssBar.Characters
{
    // Presentation only: never use a human name, role title or asset filename
    // as the key for unlocks, skins, training or upgrade-card eligibility.
    [CreateAssetMenu(fileName = "Profile", menuName = "Abyss Bar/Character Profile")]
    public sealed class CharacterProfileData : ScriptableObject
    {
        public string characterName;
        public string roleTitle;
        public string combatRole;
        [TextArea(2, 4)] public string summary;
        public string[] specialties;
        [TextArea(2, 4)] public string strength;
        [TextArea(2, 4)] public string caution;
        [TextArea(2, 3)] public string baseEHint;
        [TextArea(2, 3)] public string enhancedEHint;
        [TextArea(2, 3)] public string baseQHint;
        [TextArea(2, 3)] public string enhancedQHint;
        public bool IsValid => !string.IsNullOrWhiteSpace(characterName)
            && !string.IsNullOrWhiteSpace(roleTitle) && !string.IsNullOrWhiteSpace(combatRole)
            && !string.IsNullOrWhiteSpace(summary) && !string.IsNullOrWhiteSpace(strength)
            && !string.IsNullOrWhiteSpace(caution) && !string.IsNullOrWhiteSpace(baseEHint)
            && !string.IsNullOrWhiteSpace(baseQHint);
    }
}
