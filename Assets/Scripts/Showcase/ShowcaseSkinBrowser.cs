using System;
using System.Linq;

namespace AbyssBar.Showcase
{
    // Browsing is read-only. Only the explicit Apply command can persist.
    public sealed class ShowcaseSkinBrowser
    {
        public CharacterData Character { get; private set; }
        private CharacterSkinData[] skins = Array.Empty<CharacterSkinData>();
        public int Count => skins.Length;
        public int Index { get; private set; }
        public CharacterSkinData Current => Count > 0 ? skins[Index] : null;
        public bool CanApply => Current != null && CharacterSkinSave.IsOwned(Character, Current);
        public bool IsEquipped => Current != null && CharacterSkinSave.GetEquippedSkin(Character) == Current;
        public void Open(CharacterData character)
        {
            Character = character;
            skins = character == null ? Array.Empty<CharacterSkinData>() : character.Skins.Where(s => s != null).ToArray();
            int equipped = Array.IndexOf(skins, CharacterSkinSave.GetEquippedSkin(character));
            Index = equipped >= 0 ? equipped : 0;
        }
        public void Move(int direction) { if (Count > 0) Index = ((Index + direction) % Count + Count) % Count; }
        public bool Apply() => CanApply && CharacterSkinSave.TryEquip(Character, Current);
    }
}
