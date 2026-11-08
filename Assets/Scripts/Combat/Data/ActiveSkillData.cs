using UnityEngine;

// Append new values so existing skill assets keep their serialized kind.
public enum ActiveSkillKind { FanShot = 0, Shockwave = 1, RadialShot = 2, MioDash = 3, MioRelease = 4 }

// Shared definitions only. Cooldown and other battle state belong to the player.
[CreateAssetMenu(fileName = "Skill", menuName = "Abyss Bar/Active Skill")]
public class ActiveSkillData : ScriptableObject
{
    [SerializeField] private string displayName = "扇形射擊";
    [SerializeField] private ActiveSkillKind kind;
    [SerializeField, Min(0.1f)] private float cooldown = 6f;
    [SerializeField, Range(1,16)] private int baseCharges = 1;

    [Header("Projectiles (Fan Shot / Radial Shot)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField, Range(1, 36)] private int projectileCount = 5;
    [SerializeField, Range(0f, 180f)] private float spreadDegrees = 60f;
    [SerializeField, Min(0.1f)] private float targetRange = 8f;

    [Header("Shockwave")]
    [SerializeField, Min(0.1f)] private float radius = 3f;
    [SerializeField, Min(1)] private int damage = 4;
    [SerializeField] private SkillPulseVisual pulsePrefab;

    [Header("Projectile Mechanics")]
    [SerializeField, Range(1, 16)] private int projectileMaxTargets = 1;
    [SerializeField] private bool explodeOnEnemyHit;
    [SerializeField, Min(0.1f)] private float explosionRadius = 1.2f;
    [SerializeField] private SkillPulseVisual explosionVisual;

    [Header("Repeated Shockwaves")]
    [SerializeField, Range(1, 10)] private int shockwaveCount = 1;
    [SerializeField, Min(0.05f)] private float shockwaveInterval = 0.5f;

    public string DisplayName => displayName;
    public ActiveSkillKind Kind => kind;
    public float Cooldown => cooldown;
    public int BaseCharges => Mathf.Clamp(baseCharges,1,16);
    public GameObject ProjectilePrefab => projectilePrefab;
    public int ProjectileCount => projectileCount;
    public float SpreadDegrees => spreadDegrees;
    public float TargetRange => targetRange;
    public float Radius => radius;
    public int Damage => AbyssBar.Upgrades.CombatBalance.Damage(damage);
    public SkillPulseVisual PulsePrefab => pulsePrefab;
    public int ProjectileMaxTargets => projectileMaxTargets;
    public bool ExplodeOnEnemyHit => explodeOnEnemyHit;
    public float ExplosionRadius => explosionRadius;
    public int ShockwaveCount => shockwaveCount;
    public float ShockwaveInterval => shockwaveInterval;

    public ProjectileEffects CreateProjectileEffects() => new ProjectileEffects
    {
        MaxTargets = projectileMaxTargets,
        ExplosionRadius = explodeOnEnemyHit ? explosionRadius : 0f,
        ExplosionVisual = explodeOnEnemyHit ? explosionVisual : null
    };

    public bool IsConfigured
    {
        get
        {
            if (float.IsNaN(cooldown) || float.IsInfinity(cooldown) || cooldown < 0.1f)
                return false;
            if (kind == ActiveSkillKind.MioDash || kind == ActiveSkillKind.MioRelease) return baseCharges >= 1 && baseCharges <= 16;
            if (kind == ActiveSkillKind.FanShot || kind == ActiveSkillKind.RadialShot)
                return projectilePrefab != null && projectilePrefab.GetComponent<Projectile>() != null
                    && projectileCount >= 1 && projectileCount <= 36
                    && projectileMaxTargets >= 1 && projectileMaxTargets <= 16
                    && (!explodeOnEnemyHit || (projectileMaxTargets == 1 && IsPositiveFinite(explosionRadius) && explosionVisual != null))
                    && (kind == ActiveSkillKind.RadialShot
                        || (spreadDegrees >= 0f && spreadDegrees <= 180f
                            && targetRange > 0f && !float.IsInfinity(targetRange)));
            if (kind == ActiveSkillKind.Shockwave)
                return IsPositiveFinite(radius) && damage > 0 && pulsePrefab != null
                    && shockwaveCount >= 1 && shockwaveCount <= 10
                    && (shockwaveCount == 1 || (IsPositiveFinite(shockwaveInterval) && shockwaveInterval >= 0.05f));
            return false;
        }
    }
    private static bool IsPositiveFinite(float v) => v > 0f && !float.IsNaN(v) && !float.IsInfinity(v);
}

