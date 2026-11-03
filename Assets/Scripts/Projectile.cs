using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;

    private Rigidbody2D rb;
    private int launchedDamage;
    private bool hasLaunched;
    private bool spent;
    private readonly HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();
    private ProjectileEffects effects;
    public int Damage => hasLaunched ? launchedDamage : Mathf.Max(0, damage);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 direction, int damageBonus = 0)
    {
        Launch(direction, damageBonus, ProjectileEffects.Default);
    }

    public void Launch(Vector2 direction, int damageBonus, ProjectileEffects shotEffects)
    {
        // Compute from this instance's base field, never mutate the shared prefab.
        launchedDamage = CombatDamage.WithBonus(damage, damageBonus);
        hasLaunched = true;
        spent = false;
        hitEnemies.Clear();
        effects = shotEffects;
        effects.MaxTargets = Mathf.Clamp(effects.MaxTargets, 1, 16);
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (spent) return;
        // Walls always consume the shot, including piercing and explosive shots.
        if (other.CompareTag("Wall"))
        {
            spent = true;
            Destroy(gameObject);
            return;
        }

        EnemyHealth enemy = other.GetComponentInParent<EnemyHealth>();
        if (enemy == null || !enemy.IsAlive || !hitEnemies.Add(enemy)) return;
        if (effects.ExplosionRadius > 0f && effects.ExplosionVisual != null)
        {
            // Replace direct damage with one explosion, not direct damage plus explosion.
            spent = true;
            Vector3 center = enemy.transform.position;
            AreaDamage2D.Deal(center, effects.ExplosionRadius, Damage);
            SkillPulseVisual visual = Instantiate(effects.ExplosionVisual, center, Quaternion.identity);
            visual.Play(effects.ExplosionRadius);
        }
        else
        {
            spent = hitEnemies.Count >= effects.MaxTargets;
            enemy.TakeDamage(Damage);
        }
        if (spent) Destroy(gameObject);
    }
}
