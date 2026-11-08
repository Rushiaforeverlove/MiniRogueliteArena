namespace AbyssBar.Showcase
{
    public enum ShowcasePage { Overview, Attack, E, Q }
    public enum ShowcasePattern { Straight, Fan, Radial, Shockwave, Melee, DashSpin, Empower }
    // Detached immutable-in-use specification: no player, scene, save or enemy
    // ownership. Future attack types extend the resolver/simulator, not the UI.
    public sealed class ShowcaseAttackPlan
    {
        public ShowcasePattern pattern;
        public float damage, radius, explosionRadius, spreadDegrees;
        public float speed = 8, lifetime = 3, attackInterval = .6f, pulseInterval = .5f;
        public int projectileCount = 1, maxTargets = 1, pulseCount = 1;
        public bool repeatBasic;
        public AbyssBar.Mio.MioCharacterDefinition mio;
        public bool mioEnhancedE,mioEnhancedQ;
        public float mioChargeSeconds=.9f,mioChargeDamage=420;
        public float mioEDistance=2.8f;
        public UnityEngine.Sprite projectileSprite;
    }
}
