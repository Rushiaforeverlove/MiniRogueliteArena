using AbyssBar.Enemies;
using UnityEngine;

namespace AbyssBar.EnemyAnimations
{
    [RequireComponent(typeof(SpriteRenderer), typeof(EnemyHealth))]
    public sealed class EnemyAnimationPresenter : MonoBehaviour
    {
        [SerializeField] private EnemyAnimationData animations;
        private SpriteRenderer body;
        private EnemyHealth health;
        private EnemyDamage melee;
        private RangedEnemyAttack ranged;
        private EnemyController movement;
        private IEnemyAnimationAction action;
        private IEnemyDeathPresentation deathPresentation;
        private float hurtStarted = float.NegativeInfinity;
        private float stateClock;
        private string state;
        private bool dead;
        private bool ready;
        public EnemyAnimationData Data => animations;
        public bool IsConfigured => ready;
        private void Awake()
        {
            body = GetComponent<SpriteRenderer>(); health = GetComponent<EnemyHealth>();
            melee = GetComponent<EnemyDamage>(); ranged = GetComponent<RangedEnemyAttack>();
            movement = GetComponent<EnemyController>();
            action = GetComponent<IEnemyAnimationAction>();
            deathPresentation = GetComponent<IEnemyDeathPresentation>();
            ready = animations != null && animations.IsValid;
            if (IsConfigured) body.sprite = animations.idle.frames[0];
        }
        public void PlayHurt() { if (!dead) hurtStarted = Time.time; }
        public void PlayDeath()
        {
            if (dead) return; dead = true;
            bool handled = deathPresentation != null && deathPresentation.TryPlayDeath(body);
            if (!handled && IsConfigured) EnemyDeathVisual.Spawn(body, animations.death);
            body.enabled = false;
        }
        private void LateUpdate()
        {
            if (dead || !IsConfigured || !health.IsAlive) return;
            if (Time.timeScale <= 0) return;
            EnemySpriteClip clip; string next; int frame;
            // Combat timelines remain authoritative; sprite changes never apply damage.
            if (Time.time - hurtStarted < animations.hurt.Duration)
            { clip = animations.hurt; next = "hurt"; frame = clip.FrameAt(Time.time - hurtStarted, false); }
            else if (action != null && action.TryGetPose(out clip, out frame, out Vector2 facing))
            { next = "special"; Face(facing.x); }
            else if (melee != null && melee.IsAttacking)
            {
                clip = animations.attack; next = "attack"; frame = clip.FrameAt(melee.AttackElapsed, false);
                Face(melee.AttackDirection.x);
            }
            else if (ranged != null && ranged.Phase == RangedAttackPhase.Aim)
            {
                clip = animations.attack; next = "aim";
                frame = EnemyAnimationMath.AimFrame(ranged.AimProgress, clip.impactFrame); Face(ranged.AimDirection.x);
            }
            else if (ranged != null && Time.time - ranged.LastShotTime <
                (animations.attack.frames.Length - animations.attack.impactFrame) / animations.attack.fps)
            {
                clip = animations.attack; next = "release";
                frame = clip.impactFrame + EnemyAnimationMath.Frame(Time.time - ranged.LastShotTime,
                    clip.fps, clip.frames.Length - clip.impactFrame, false); Face(ranged.AimDirection.x);
            }
            else
            {
                bool moving = movement != null && movement.isActiveAndEnabled && movement.IsMoving;
                next = health.Status.MoveMultiplier <= 0 ? "stun" : moving ? "walk" : "idle";
                clip = next == "stun" ? animations.stun : moving ? animations.walk : animations.idle;
                if (state != next) stateClock = (health.RuntimeId % 7) * .03f;
                stateClock += Time.deltaTime * (next == "walk" ? Mathf.Max(.25f, health.Status.MoveMultiplier) : 1);
                frame = clip.FrameAt(stateClock, true);
                if (moving) Face(movement.MovementDirection.x);
            }
            state = next; body.sprite = clip.frames[frame];
        }
        private void Face(float x) { if (Mathf.Abs(x) > .1f) body.flipX = x < 0; }
    }
}
