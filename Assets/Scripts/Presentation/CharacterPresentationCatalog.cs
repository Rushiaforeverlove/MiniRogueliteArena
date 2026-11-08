using System;
using UnityEngine;
namespace AbyssBar.Presentation
{
    [CreateAssetMenu(menuName="Abyss Bar/Character Presentation Catalog")]
    public sealed class CharacterPresentationCatalog:ScriptableObject
    {
        public CharacterPresentationDefinition[] characters=Array.Empty<CharacterPresentationDefinition>();
        public CharacterPresentationDefinition Find(string id)=>Array.Find(characters,c=>c!=null&&c.characterId==id);
    }
}
