using System.Collections.Generic;
using AbyssBar.EnemyAnimations;
using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Enemies
{
    [RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class EnemyChargeAttack : MonoBehaviour, IEnemyCombatBinding, IEnemyPhysicsAction, IEnemyAnimationAction
    {
        [SerializeField] private ChargeAttackSettings settings;
        [SerializeField] private ChargeAnimationSet animations;
        private EnemyHealth health;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private EnemyCombatWorld world;
        private ChargeAttackTimeline timeline;
        private Vector2 lockedDirection;
        private bool contactsIgnored, restoreNextTick, hitAttempted;
        private float rushAnimationSeconds;
        private LayerMask originalExcludedLayers;
        private sealed class IgnoredPair { public Collider2D other; public bool originallyIgnored; }
        private readonly List<IgnoredPair> playerPairs = new List<IgnoredPair>();
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(16);
        private readonly List<SpriteRenderer> warning = new List<SpriteRenderer>();
        private static Sprite lineSprite;
        public bool IsConfigured => settings != null && settings.IsValid && animations != null && animations.IsValid;
        public ChargePhase Phase => timeline != null ? timeline.Phase : ChargePhase.Approach;
        public ChargeAnimationSet Animations => animations;
        private Vector2 CastOrigin(Vector2 position) => position + Vector2.Scale(shape.offset, transform.lossyScale);
        private Vector2 CastSize => Vector2.Scale(shape.size, new Vector2(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)));
        private void Awake() { health = GetComponent<EnemyHealth>(); body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>(); }
        public void Bind(Transform target, EnemyCombatWorld owner)
        {
            RestoreContacts(); world = owner;
            if (IsConfigured) timeline = new ChargeAttackTimeline(settings.windupSeconds, settings.distance,
                settings.recoverySeconds, settings.cooldownAfterRush);
        }
        public bool TryGetStep(Transform target, Vector2 position, float seconds, out Vector2 displacement)
        {
            displacement = Vector2.zero;
            if (restoreNextTick) RestoreContacts(); // Previous MovePosition has now been simulated.
            if (!IsConfigured || timeline == null || target == null || world == null || !world.Active || !health.IsAlive)
            { timeline?.Interrupt(); RestoreContacts(); SetWarning(false, 0); return true; }
            if (Time.timeScale <= 0 || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return true;
            if (health.Status.MoveMultiplier <= 0)
            { timeline.Interrupt(); RestoreContacts(); timeline.Tick(seconds); SetWarning(false, 0); return true; }
            if (timeline.Phase == ChargePhase.Windup && !world.Visible(position))
            { timeline.Interrupt(); SetWarning(false, 0); }

            ChargePhase before = timeline.Phase; timeline.Tick(seconds);
            if (before == ChargePhase.Windup && timeline.Phase == ChargePhase.Rush)
            { hitAttempted = false; rushAnimationSeconds = 0; IgnoreContacts(); return true; }
            if (timeline.Phase == ChargePhase.Approach)
            {
                Vector2 offset = (Vector2)target.position - position;
                if (world.Visible(position) && offset.magnitude <= settings.triggerRange && ClearPath(position, offset))
                {
                    if (timeline.Begin()) lockedDirection = offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.right;
                }
                return timeline.Phase != ChargePhase.Approach;
            }
            if (timeline.Phase != ChargePhase.Rush) return true;

            IgnoreContacts();
            rushAnimationSeconds += seconds * health.Status.MoveMultiplier;
            float step = Mathf.Min(timeline.RemainingDistance, settings.speed * health.Status.MoveMultiplier * seconds);
            float wallDistance; bool wall;
            float allowed = AllowedTravel(position, lockedDirection, step, out wall, out wallDistance);
            if (!hitAttempted)
            {
                foreach (var hit in hits)
                {
                    var collider = hit.collider;
                    if (collider == null || collider.isTrigger || hit.distance > allowed + .0001f
                        || wall && hit.distance >= wallDistance) continue;
                    var player = collider.GetComponentInParent<PlayerHealth>();
                    if (player != world.Player) continue;
                    // Crossing while dodging spends this charge's one hit opportunity,
                    // so ending the dodge inside its body cannot cause a delayed hit.
                    hitAttempted = true;
                    if (player.GetComponent<PlayerDodge>()?.IsDodging != true)
                        player.ReceiveDamage(CombatBalance.Damage(settings.legacyDamage) * health.Status.OutgoingMultiplier);
                    break;
                }
            }
            displacement = lockedDirection * allowed;
            timeline.Travel(allowed, wall);
            if (timeline.Phase != ChargePhase.Rush) restoreNextTick = true;
            return true;
        }
        private bool ClearPath(Vector2 position, Vector2 offset)
        {
            if (offset.sqrMagnitude < .0001f) return true;
            float distance; bool wall;
            AllowedTravel(position, offset.normalized, offset.magnitude, out wall, out distance);
            return !wall;
        }
        private float AllowedTravel(Vector2 position, Vector2 direction, float requested, out bool wall, out float nearestWall)
        {
            hits.Clear();
            var filter = ContactFilter2D.noFilter; filter.useTriggers = false;
            Physics2D.BoxCast(CastOrigin(position), CastSize, 0, direction, filter, hits, requested + .02f);
            nearestWall = float.PositiveInfinity;
            foreach (var hit in hits) if (EnemyProjectile.IsBlocker(hit.collider)) nearestWall = Mathf.Min(nearestWall, hit.distance);
            wall = nearestWall <= requested + .02f;
            return wall ? Mathf.Min(requested, Mathf.Max(0, nearestWall - .02f)) : requested;
        }
        private void IgnoreContacts()
        {
            if (contactsIgnored || world == null || world.Player == null) return;
            contactsIgnored = true; originalExcludedLayers = body.excludeLayers;
            body.excludeLayers = originalExcludedLayers.value | (1 << gameObject.layer);
            // Player and walls both use Default in this project. Ignore ONLY
            // player collider pairs; never exclude their entire shared layer.
            foreach (var other in world.Player.GetComponentsInChildren<Collider2D>())
            {
                if (other == null || other.isTrigger) continue;
                bool previous = Physics2D.GetIgnoreCollision(shape, other);
                playerPairs.Add(new IgnoredPair { other = other, originallyIgnored = previous });
                Physics2D.IgnoreCollision(shape, other, true);
            }
        }
        private void RestoreContacts()
        {
            restoreNextTick = false;
            if (!contactsIgnored) return;
            if (body != null) body.excludeLayers = originalExcludedLayers;
            foreach (var pair in playerPairs) if (shape != null && pair.other != null)
                Physics2D.IgnoreCollision(shape, pair.other, pair.originallyIgnored);
            playerPairs.Clear(); contactsIgnored = false;
        }
        private void Update()
        {
            if (world == null || !world.Active || health == null || !health.IsAlive)
            { timeline?.Interrupt(); RestoreContacts(); SetWarning(false, 0); return; }
            if (Time.timeScale <= 0) return;
            if (Phase == ChargePhase.Windup)
            {
                float wallDistance; bool wall;
                float length = AllowedTravel(body.position, lockedDirection, settings.distance, out wall, out wallDistance);
                SetWarning(true, length);
            }
            else SetWarning(false, 0);
        }
        private void SetWarning(bool show, float length)
        {
            if (!show) { foreach (var part in warning) if (part != null) part.enabled = false; return; }
            if (warning.Count == 0)
            {
                if (lineSprite == null) lineSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
                for (int i = 0; i < 8; i++)
                {
                    var go = new GameObject("Charge route warning"); go.transform.SetParent(transform, false);
                    var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = lineSprite;
                    renderer.sharedMaterial = settings.warningMaterial; renderer.sortingOrder = 28000; warning.Add(renderer);
                }
            }
            Vector2 origin = CastOrigin(body.position);
            float angle = Mathf.Atan2(lockedDirection.y, lockedDirection.x) * Mathf.Rad2Deg;
            for (int i = 0; i < warning.Count; i++)
            {
                var part = warning[i]; part.enabled = length > .1f;
                part.color = new Color(1, .45f, .1f, Mathf.Lerp(.35f, .9f, timeline.WindupProgress));
                if (i < 6)
                {
                    part.transform.position = origin + lockedDirection * ((i + .5f) * length / 6);
                    part.transform.rotation = Quaternion.Euler(0, 0, angle);
                    part.transform.localScale = new Vector3(length / 6 * .7f, .08f, 1);
                }
                else
                {
                    Vector2 side = new Vector2(-lockedDirection.y, lockedDirection.x);
                    part.transform.position = origin + lockedDirection * (length - .15f) + side * (i == 6 ? .13f : -.13f);
                    part.transform.rotation = Quaternion.Euler(0, 0, angle + (i == 6 ? -40 : 40));
                    part.transform.localScale = new Vector3(.4f, .09f, 1);
                }
            }
        }
        public bool TryGetPose(out EnemySpriteClip clip, out int frame, out Vector2 facing)
        {
            clip = null; frame = 0; facing = lockedDirection;
            if (!IsConfigured || timeline == null) return false;
            if (health != null && health.Status.MoveMultiplier <= 0) return false;
            switch (timeline.Phase)
            {
                case ChargePhase.Windup:
                    clip = animations.windup;
                    frame = EnemyAnimationMath.Frame(timeline.PhaseElapsed, clip.frames.Length / settings.windupSeconds, clip.frames.Length, false); return true;
                case ChargePhase.Rush:
                    clip = animations.rush; frame = clip.FrameAt(rushAnimationSeconds, true); return true;
                case ChargePhase.Recovery:
                    float impactDuration = timeline.HitWall ? Mathf.Min(animations.wallImpact.Duration, settings.recoverySeconds * .5f) : 0;
                    if (timeline.HitWall && timeline.PhaseElapsed < impactDuration)
                    {
                        clip = animations.wallImpact;
                        frame = EnemyAnimationMath.Frame(timeline.PhaseElapsed, clip.frames.Length / impactDuration, clip.frames.Length, false);
                    }
                    else
                    {
                        clip = animations.recovery;
                        frame = EnemyAnimationMath.Frame(timeline.PhaseElapsed - impactDuration,
                            clip.frames.Length / (settings.recoverySeconds - impactDuration), clip.frames.Length, false);
                    }
                    return true;
                default: return false;
            }
        }
        private void OnDisable() { timeline?.Interrupt(); RestoreContacts(); SetWarning(false, 0); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVisual() { if (lineSprite != null) Destroy(lineSprite); lineSprite = null; }
    }
}
