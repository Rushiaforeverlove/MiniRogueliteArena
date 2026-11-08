using System;
using UnityEngine;

namespace AbyssBar.Mio
{
    [Serializable] public sealed class MioTuning
    {
        public float tapDamage = 180, chargedDamage = 420, chargeSeconds = .9f;
        public float slashRadius = 3.2f, slashDegrees = 60, waveRange = 9, waveSpeed = 11;
        public float eRadius = 5, ePush = 2.75f, waveWidth = 1.5f;
        public int waveTargets = 4;
        public float tapSeconds = .48f, releaseSeconds = .65f, impactDelay = .12f;
        public float visualAimHeight = .5f, groundDepth = .55f, primaryBufferSeconds = .3f;
        public float eDamage = 180, eDistance = 2.8f, eDuration = .18f, spinDuration = .5f;
        public float qSeconds = 6, qEnterSeconds = .36f, qExitSeconds = .5f;
        public float qPushRadius = 3, qPushDistance = 1.8f;
        public float rageDashMultiplier = 1.35f, rageSpinMultiplier = 1.35f, rageSpinPush = 1;
        public float rageTapWaveCoefficient = .8f, rageRadialCoefficient = .65f;
        public float enhancedEDelay = .4f, enhancedECoefficient = .6f, enhancedQCoefficient = .35f;
    }
    [Serializable] public sealed class MioSpriteFrame
    {
        public Sprite sprite;
        public float scale = 1;
    }
    [Serializable] public sealed class MioSpriteClip
    {
        public string key;
        public float fps = 8;
        public bool loop;
        public float[] frameEndFractions = Array.Empty<float>();
        public MioSpriteFrame[] frames = Array.Empty<MioSpriteFrame>();
        public float Duration => frames == null || frames.Length == 0 ? 0 : frames.Length / Mathf.Max(.1f, fps);
        public MioSpriteFrame At(float age, float overrideDuration = 0)
        {
            if (frames == null || frames.Length == 0) return null;
            if (frameEndFractions != null && frameEndFractions.Length == frames.Length)
            {
                float length = overrideDuration > 0 ? overrideDuration : Duration;
                float p = Mathf.Max(0, age) / Mathf.Max(.001f, length);
                p = loop ? p % 1 : Mathf.Min(1, p);
                for (int i = 0; i < frames.Length; i++) if (p < frameEndFractions[i]) return frames[i];
                return frames[frames.Length - 1];
            }
            int n = overrideDuration > 0 ? Mathf.FloorToInt(Mathf.Max(0, age) / overrideDuration * frames.Length)
                : Mathf.FloorToInt(Mathf.Max(0, age) * Mathf.Max(.1f, fps));
            return frames[loop ? n % frames.Length : Mathf.Min(frames.Length - 1, n)];
        }
    }
    [CreateAssetMenu(menuName = "Abyss Bar/Mio/Character Definition")]
    public sealed class MioCharacterDefinition : ScriptableObject
    {
        public const string RoleId = "mio_01";
        public CharacterData character;
        public MioTuning tuning = new MioTuning();
        public MioSpriteClip[] clips = Array.Empty<MioSpriteClip>();
        public Sprite cardIcon;
        public float visualScale = 1;
        public int animationRevision;
        public Material seatedMaterial;
        public Material energyMaterial;
        public MioSpriteClip Find(string key) => Array.Find(clips ?? Array.Empty<MioSpriteClip>(), c => c != null && c.key == key);
        public bool Matches(CharacterData data) => data != null && data.CharacterId == RoleId;
        public static MioCharacterDefinition Load() => Resources.Load<MioCharacterDefinition>("Mio/MioCharacterDefinition");
    }
    // Reusable skill extension contract. Existing shot/shock skills keep their path.
    public interface IPlayerSkillAction
    {
        bool Accepts(ActiveSkillData data);
        float Basis(ActiveSkillData data);
        PlayerActiveSkill.UseResult CanExecute(ActiveSkillData data);
        void Execute(ActiveSkillData data, AbyssBar.Upgrades.RunAction action);
        void CancelSkillActions();
    }
}
