using AbyssBar.Enemies;
using AbyssBar.EnemyAnimations;
using AbyssBar.Upgrades;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 1f;
    private float damageTimer, lastContact = float.NegativeInfinity;
    private PlayerHealth victim;
    private EnemyHealth health;
    private EnemyAnimationPresenter presentation;
    private RangedEnemyAttack ranged;
    private readonly EnemyMeleeTimeline attack = new EnemyMeleeTimeline();
    public bool IsAttacking => attack.Active;
    public float AttackElapsed => attack.Elapsed;
    public Vector2 AttackDirection { get; private set; }
    private void Awake()
    {
        health = GetComponent<EnemyHealth>(); presentation = GetComponent<EnemyAnimationPresenter>();
        ranged = GetComponent<RangedEnemyAttack>();
    }
    private void Update()
    {
        if (Time.timeScale <= 0) return;
        damageTimer = Mathf.Max(0, damageTimer - Time.deltaTime);
        if (health == null || !health.IsAlive || health.Status.MoveMultiplier <= 0)
        { attack.Cancel(); return; }
        if (attack.Tick(Time.deltaTime) && victim != null && victim.IsAlive
            && Time.time - lastContact <= Mathf.Max(.06f, Time.fixedDeltaTime * 3)
            && victim.GetComponent<PlayerDodge>()?.IsDodging != true)
            victim.ReceiveDamage(CombatBalance.Damage(damage) * health.Status.OutgoingMultiplier);
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (player == null || !player.IsAlive || health == null || !health.IsAlive || Time.timeScale <= 0) return;
        victim = player; lastContact = Time.time;
        if (attack.Active || damageTimer > 0 || health.Status.MoveMultiplier <= 0
            || player.GetComponent<PlayerDodge>()?.IsDodging == true || ranged != null && ranged.Phase == RangedAttackPhase.Aim) return;
        damageTimer = Mathf.Max(.05f, damageCooldown);
        AttackDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        if (presentation != null && presentation.IsConfigured)
            attack.Start(presentation.Data.attack.Duration, presentation.Data.attack.ImpactTime);
        else player.ReceiveDamage(CombatBalance.Damage(damage) * health.Status.OutgoingMultiplier);
    }
    private void OnCollisionExit2D(Collision2D collision)
    { if (collision.gameObject.GetComponentInParent<PlayerHealth>() == victim) victim = null; }
    private void OnDisable() { attack.Cancel(); victim = null; }
}

