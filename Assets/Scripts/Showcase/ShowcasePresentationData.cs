using System;
using AbyssBar.EnemyAnimations;
using UnityEngine;

namespace AbyssBar.Showcase
{
    [Serializable] public sealed class ShowcaseAvatarRig
    {
        public CharacterData character;
        public CharacterSkinData skin;
        public Sprite sourceSprite;
        public EnemySpriteClip idle, attack, fan, radial, shock;
        public bool IsValid => idle != null && idle.IsValid && attack != null && attack.IsValid
            && fan != null && fan.IsValid && radial != null && radial.IsValid && shock != null && shock.IsValid;
    }
    [CreateAssetMenu(fileName = "ShowcasePresentation", menuName = "Abyss Bar/Showcase Presentation")]
    public sealed class ShowcasePresentationData : ScriptableObject
    {
        public ShowcaseAvatarRig[] avatars;
        public EnemyAnimationData enemy;
        public Material material;
        public ShowcaseAvatarRig Find(CharacterData character, CharacterSkinData skin)
        {
            if (avatars == null) return null;
            Sprite current = skin != null && skin.GameplaySprite != null ? skin.GameplaySprite : character.GameplaySprite;
            ShowcaseAvatarRig fallback = null;
            foreach (var rig in avatars)
            {
                if (rig == null || !rig.IsValid || rig.character != character) continue;
                if (rig.skin != null && rig.skin == skin) return rig;
                if (rig.skin == null && rig.sourceSprite == current) fallback = rig;
            }
            return fallback;
        }
    }
}
