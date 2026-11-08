using System.Collections.Generic;
using UnityEngine;
using AbyssBar.Upgrades;
using AbyssBar.EnemyAnimations;
using AbyssBar.Enemies;

public class EnemyHealth : MonoBehaviour
{
    public event System.Action<EnemyHealth> Defeated;
    [SerializeField] private int maxHealth = 3; // Serialized assets stay in legacy units.
    [SerializeField] private GameObject experienceGemPrefab;
    [SerializeField] private bool isBoss;
    private float currentHealth;
    private int battleMaxHealth;
    private bool runKillClaimed;
    public bool TryClaimRunKill() { if (IsAlive || runKillClaimed) return false; runKillClaimed = true; return true; }
    private EnemyStatus2D status;
    private IEnemyDamageMitigation[] defenses;
    private IEnemyHitReaction[] hitReactions;
    private static readonly HashSet<EnemyHealth> living = new HashSet<EnemyHealth>();
    private static int nextRuntimeId;
    public int RuntimeId { get; private set; }
    public static IEnumerable<EnemyHealth> Living => living;
    public bool IsAlive => currentHealth > 0;
    public bool IsFullHealth => currentHealth >= battleMaxHealth;
    public bool IsBoss => isBoss;
    public float CurrentHealth => currentHealth;
    public int MaxHealth => battleMaxHealth;
    // Display the resolved hit, preserving large overkill numbers while
    // ReceiveDamage still returns HP actually removed for lifesteal/accounting.
    public float LastHitDamage { get; private set; }
    public bool HasValidStartingHealth => maxHealth > 0;
    public EnemyStatus2D Status => status;
    private void Awake()
    {
        RuntimeId = ++nextRuntimeId;
        battleMaxHealth = CombatBalance.EnemyHealth(maxHealth); currentHealth = battleMaxHealth;
        status = GetComponent<EnemyStatus2D>() ?? gameObject.AddComponent<EnemyStatus2D>();
        defenses = GetComponents<IEnemyDamageMitigation>();
        hitReactions = GetComponents<IEnemyHitReaction>();
    }
    private void OnEnable() { living.Add(this); }
    private void OnDisable() { living.Remove(this); }
    public void TakeDamage(int damage) { ReceiveDamage(damage); }
    public float ReceiveDamage(float damage)
    {
        LastHitDamage = 0;
        if (!IsAlive) return 0;
        float mitigated = EnemyDamageMitigation.Resolve(damage, defenses);
        if (mitigated <= 0) return 0;
        LastHitDamage = mitigated;
        float actual = Mathf.Min(currentHealth, mitigated); currentHealth -= actual;
        GetComponent<DamageFlash>()?.Play();
        if (currentHealth <= 0)
        {
            living.Remove(this);
            // Separate visual death from combat ownership and immediate kill accounting.
            GetComponent<EnemyAnimationPresenter>()?.PlayDeath();
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            var body = GetComponent<Rigidbody2D>(); if (body != null) body.simulated = false;
            Defeated?.Invoke(this);
            if (experienceGemPrefab != null) Instantiate(experienceGemPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else
        {
            bool handled = false;
            foreach (var reaction in hitReactions) if (reaction != null) handled |= reaction.OnHitReceived(damage, actual);
            if (!handled) GetComponent<EnemyAnimationPresenter>()?.PlayHurt();
        }
        return actual;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { living.Clear(); nextRuntimeId = 0; }
}

