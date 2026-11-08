using System.Collections.Generic;
using AbyssBar.Upgrades;
using AbyssBar.EnemyAnimations;
using UnityEngine;

namespace AbyssBar.Enemies
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class RangedEnemyAttack : MonoBehaviour, IEnemyMovementPolicy, IEnemyCombatBinding
    {
        [SerializeField] private RangedEnemySettings settings;
        private Transform target;
        private EnemyHealth health;
        private EnemyCombatWorld world;
        private RangedAttackCycle cycle;
        private SpriteRenderer warning;
        private Vector2 lockedDirection;
        private float lockedDistance;
        private EnemyDamage contactAttack;
        private EnemyAnimationPresenter presentation;
        private readonly List<RaycastHit2D> sightHits = new List<RaycastHit2D>(16);
        private static Sprite lineSprite;
        public bool IsConfigured => settings != null && settings.IsValid;
        public RangedAttackPhase Phase => cycle != null ? cycle.Phase : RangedAttackPhase.Approach;
        public Vector2 AimDirection => lockedDirection;
        public float AimProgress => cycle != null && settings != null ? Mathf.Clamp01(1 - cycle.Remaining / settings.aimSeconds) : 0;
        public float LastShotTime { get; private set; } = float.NegativeInfinity;
        private Vector2 Muzzle => (Vector2)transform.position + (presentation != null && presentation.IsConfigured
            ? presentation.Data.muzzleOffset : Vector2.up * .4f);
        private void Awake()
        {
            health = GetComponent<EnemyHealth>(); contactAttack = GetComponent<EnemyDamage>();
            presentation = GetComponent<EnemyAnimationPresenter>();
        }
        public void Bind(Transform player, EnemyCombatWorld owner)
        {
            target = player; world = owner;
            if (IsConfigured) cycle = new RangedAttackCycle(settings.aimSeconds, settings.recoverySeconds);
        }
        public Vector2 MovementDirection(Transform player, Vector2 position)
        {
            if (!IsConfigured || world == null || !world.Active || player == null) return Vector2.zero;
            Vector2 offset = (Vector2)player.position - position;
            if (cycle != null && cycle.Phase == RangedAttackPhase.Aim) return Vector2.zero;
            // Off-screen shooters keep approaching instead of waiting invisibly.
            return !world.Visible(Muzzle) || offset.magnitude > settings.attackRange || !ClearPath(Muzzle, player.position)
                ? offset.normalized : Vector2.zero;
        }
        private void Update()
        {
            if (cycle == null || target == null || health == null || !health.IsAlive || world == null || !world.Active)
            { ShowWarning(false); return; }
            if (Time.timeScale <= 0) return;
            if (contactAttack != null && contactAttack.IsAttacking)
            { cycle.Interrupt(); ShowWarning(false); cycle.Tick(Time.deltaTime); return; }
            Vector2 offset = (Vector2)target.position - Muzzle;
            bool interrupted = health.Status.MoveMultiplier <= 0 || !world.Visible(Muzzle)
                || offset.magnitude > settings.attackRange + 2;
            if (interrupted) { cycle.Interrupt(); ShowWarning(false); cycle.Tick(Time.deltaTime); return; }
            if (cycle.Tick(Time.deltaTime))
            {
                ShowWarning(false);
                if (ClearPath(Muzzle, Muzzle + lockedDirection * lockedDistance)
                    && world.TryFire(Muzzle, lockedDirection, CombatBalance.Damage(settings.legacyDamage)
                        * health.Status.OutgoingMultiplier, settings)) LastShotTime = Time.time;
                return;
            }
            if (cycle.Phase == RangedAttackPhase.Approach && offset.magnitude <= settings.attackRange
                && ClearPath(Muzzle, target.position) && cycle.BeginAim())
            {
                lockedDirection = offset.sqrMagnitude > .0001f ? offset.normalized : Vector2.right;
                lockedDistance = offset.magnitude;
            }
            ShowWarning(cycle.Phase == RangedAttackPhase.Aim);
        }
        private bool ClearPath(Vector2 from, Vector2 to)
        {
            sightHits.Clear(); Vector2 d = to - from;
            Physics2D.CircleCast(from, settings.projectileRadius, d.normalized, ContactFilter2D.noFilter, sightHits, d.magnitude);
            foreach (var hit in sightHits) if (EnemyProjectile.IsBlocker(hit.collider)) return false;
            return true;
        }
        private void ShowWarning(bool visible)
        {
            if (!visible) { if (warning != null) warning.enabled = false; return; }
            if (warning == null)
            {
                if (lineSprite == null) lineSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
                var go = new GameObject("Riftmaw aim warning"); go.transform.SetParent(transform, false);
                warning = go.AddComponent<SpriteRenderer>(); warning.sprite = lineSprite;
                warning.sharedMaterial = settings.effectMaterial; warning.sortingOrder = 28000;
            }
            float length = settings.attackRange;
            warning.enabled = true; warning.color = new Color(1, .45f, .15f,
                Mathf.Lerp(.25f, .8f, 1 - cycle.Remaining / settings.aimSeconds));
            warning.transform.position = Muzzle + lockedDirection * (length * .5f);
            warning.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(lockedDirection.y, lockedDirection.x) * Mathf.Rad2Deg);
            warning.transform.localScale = new Vector3(length, .045f, 1);
        }
        private void OnDisable() { cycle?.Interrupt(); ShowWarning(false); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetVisual() { if (lineSprite != null) Destroy(lineSprite); lineSprite = null; }
    }
}
