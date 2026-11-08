// Per-shot snapshot. Ordinary attacks use Default; skill assets choose their own effects.
public struct ProjectileEffects
{
    public int MaxTargets;
    public float ExplosionRadius;
    public SkillPulseVisual ExplosionVisual;
    public static ProjectileEffects Default => new ProjectileEffects { MaxTargets = 1 };
}
