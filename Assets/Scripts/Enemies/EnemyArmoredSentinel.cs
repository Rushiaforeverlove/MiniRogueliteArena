using AbyssBar.EnemyAnimations;
using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyArmoredSentinel : MonoBehaviour, IEnemyCombatBinding, IEnemyPhysicsAction,
        IEnemyAnimationAction, IEnemyDamageMitigation, IEnemyHitReaction
    {
        [SerializeField] private SentinelAttackSettings settings;
        [SerializeField] private SentinelAnimationSet animations;
        private EnemyHealth health;
        private EnemyCombatWorld world;
        private SentinelAttackTimeline timeline;
        private Vector2 lockedCenter, lockedFacing;
        private float blockStarted = float.NegativeInfinity;
        private SpriteRenderer indicator;
        private static Sprite ringSprite;
        public bool IsConfigured => settings != null && settings.IsValid && animations != null && animations.IsValid;
        public SentinelPhase Phase => timeline != null ? timeline.Phase : SentinelPhase.Approach;
        public SentinelAnimationSet Animations => animations;
        public bool ArmorActive => isActiveAndEnabled && IsConfigured && health != null && health.IsAlive
            && health.Status.MoveMultiplier > 0 && Phase != SentinelPhase.Slam && Phase != SentinelPhase.Recovery;
        public float IncomingDamageMultiplier => ArmorActive ? 1 - settings.armorReduction : 1;
        public Vector2 ImpactCenter => lockedCenter;
        private void Awake() { health = GetComponent<EnemyHealth>(); }
        public void Bind(Transform target, EnemyCombatWorld owner)
        {
            world = owner; blockStarted = float.NegativeInfinity; SetIndicator(false);
            if (IsConfigured) timeline = new SentinelAttackTimeline(settings.windupSeconds, settings.slamSeconds,
                settings.recoverySeconds, settings.cooldownAfterImpact);
        }
        public bool TryGetStep(Transform target, Vector2 position, float seconds, out Vector2 displacement)
        {
            displacement = Vector2.zero;
            if (!IsConfigured || timeline == null || target == null || world == null || !world.Active || !health.IsAlive)
            { timeline?.Interrupt(); SetIndicator(false); return true; }
            if (Time.timeScale <= 0 || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return true;
            if (health.Status.MoveMultiplier <= 0)
            { timeline.Interrupt(); timeline.Tick(seconds); SetIndicator(false); return true; }
            if (Phase == SentinelPhase.Windup && !world.Visible(position)) timeline.Interrupt();
            bool impact = timeline.Tick(seconds);
            if (impact) ApplyImpact();
            if (Phase == SentinelPhase.Approach)
            {
                Vector2 offset = (Vector2)target.position - position;
                if (world.Visible(position) && offset.sqrMagnitude <= settings.triggerRange * settings.triggerRange
                    && ClearLine(position, target.position) && timeline.Begin())
                {
                    lockedFacing = offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.right;
                    lockedCenter = position + lockedFacing * settings.centerOffset;
                }
            }
            SetIndicator(Phase == SentinelPhase.Windup || Phase == SentinelPhase.Slam);
            return Phase != SentinelPhase.Approach;
        }
        private bool ClearLine(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to)) if (EnemyProjectile.IsBlocker(hit.collider)) return false;
            return true;
        }
        private void ApplyImpact()
        {
            // This is an instantaneous single hit. Ending dodge in the circle
            // after this edge cannot deliver a delayed or per-frame hit.
            var player = world.Player;
            if (player == null || !player.IsAlive || player.GetComponent<PlayerDodge>()?.IsDodging == true) return;
            // A pushed sentinel cannot strike through a wall into its old area.
            if (!ClearLine(transform.position, lockedCenter)) return;
            foreach (var collider in player.GetComponentsInChildren<Collider2D>())
            {
                if (collider == null || !collider.enabled || collider.isTrigger) continue;
                Vector2 closest = collider.ClosestPoint(lockedCenter);
                if ((closest - lockedCenter).sqrMagnitude > settings.slamRadius * settings.slamRadius || !ClearLine(lockedCenter, closest)) continue;
                player.ReceiveDamage(CombatBalance.Damage(settings.legacyDamage) * health.Status.OutgoingMultiplier); return;
            }
        }
        public bool OnHitReceived(float incoming, float applied)
        {
            if (Phase == SentinelPhase.Slam && isActiveAndEnabled && applied > 0) return true;
            if (!ArmorActive || applied <= 0) return false;
            blockStarted = Time.time; return true;
        }
        public bool TryGetPose(out EnemySpriteClip clip, out int frame, out Vector2 facing)
        {
            clip = null; frame = 0; facing = lockedFacing;
            if (!IsConfigured || timeline == null || health == null || health.Status.MoveMultiplier <= 0) return false;
            float duration;
            switch (Phase)
            {
                case SentinelPhase.Windup: clip = animations.windup; duration = settings.windupSeconds; break;
                case SentinelPhase.Slam: clip = animations.slam; duration = settings.slamSeconds; break;
                case SentinelPhase.Recovery: clip = animations.recovery; duration = settings.recoverySeconds; break;
                default:
                    if (!ArmorActive || Time.time - blockStarted >= animations.block.Duration) return false;
                    clip = animations.block; frame = clip.FrameAt(Time.time - blockStarted, false); return true;
            }
            frame = EnemyAnimationMath.Frame(timeline.Elapsed, clip.frames.Length / duration, clip.frames.Length, false); return true;
        }
        private void Update()
        {
            if (world == null || !world.Active || health == null || !health.IsAlive || health.Status.MoveMultiplier <= 0)
            { timeline?.Interrupt(); SetIndicator(false); return; }
            if (Time.timeScale <= 0) return;
            SetIndicator(Phase == SentinelPhase.Windup || Phase == SentinelPhase.Slam);
        }
        private void SetIndicator(bool show)
        {
            if (!show) { if (indicator != null) indicator.enabled = false; return; }
            if (indicator == null)
            {
                var go = new GameObject("Sentinel locked slam area"); go.transform.SetParent(transform, false);
                indicator = go.AddComponent<SpriteRenderer>(); indicator.sharedMaterial = settings.warningMaterial;
                indicator.sprite = RadiusSprite(); indicator.sortingOrder = 28000;
            }
            indicator.enabled = true; indicator.transform.position = lockedCenter;
            // Indicator is child-owned, but world radius must ignore body scale.
            Vector3 scale = transform.lossyScale;
            indicator.transform.localScale = new Vector3(settings.slamRadius * 2 / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                settings.slamRadius * 2 / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1);
            indicator.color = Phase == SentinelPhase.Windup ? new Color(1, .55f, .15f,
                Mathf.Lerp(.35f, .95f, timeline.Elapsed / settings.windupSeconds)) : new Color(1, .2f, .1f, .8f);
        }
        private static Sprite RadiusSprite()
        {
            if (ringSprite != null) return ringSprite;
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float dx = x - 63.5f, dy = y - 63.5f, square = dx * dx + dy * dy;
                pixels[y * 128 + x] = square <= 63.5f * 63.5f && square >= 60.5f * 60.5f ? Color.white : Color.clear;
            }
            texture.SetPixels(pixels); texture.Apply();
            ringSprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(.5f, .5f), 128); return ringSprite;
        }
        private void OnDisable() { timeline?.Interrupt(); SetIndicator(false); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVisual()
        { if (ringSprite != null) { Destroy(ringSprite.texture); Destroy(ringSprite); } ringSprite = null; }
    }
}
