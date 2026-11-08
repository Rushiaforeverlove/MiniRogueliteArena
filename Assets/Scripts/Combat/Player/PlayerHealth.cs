using System.Collections.Generic;
using UnityEngine;
using AbyssBar.Upgrades;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5; // Legacy fallback; CharacterData overrides at Start.
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;
    [SerializeField, Min(0)] private float invincibilityDuration = .8f;
    private float currentHealth, nextDamageTime;
    private bool dodgeInvulnerable;
        private bool skillInvulnerable;
    private RunUpgradeRuntime Runtime => GetComponent<RunUpgradeRuntime>();
    private sealed class ShieldPart { public float amount, until; }
    private readonly List<ShieldPart> shields = new List<ShieldPart>();
    public bool IsAlive => currentHealth > 0;
    public bool IsInvincible => dodgeInvulnerable || skillInvulnerable || Time.time < nextDamageTime;
    public float CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float Shield { get { RemoveExpired(); float total = 0; foreach (var s in shields) total += s.amount; return total; } }
    private void Awake() { maxHealth = CombatBalance.PlayerHealth(maxHealth); currentHealth = maxHealth; }
    private void Start() { Refresh(); }
    public void TakeDamage(int damage) => ReceiveDamage(damage);
    public void ReceiveDamage(float damage)
    {
        if (Time.timeScale <= 0 || !IsAlive || IsInvincible || damage <= 0) return;
        var run = Runtime;
        damage *= 1 - (run != null ? run.DamageReduction : 0);
        run?.NotifyIncomingHit();
        nextDamageTime = Time.time + invincibilityDuration;
        float before = Shield, absorbed = 0;
        // Expire independently; earliest expiry absorbs first.
        shields.Sort((a, b) => a.until.CompareTo(b.until));
        foreach (var part in shields)
        { float used = Mathf.Min(part.amount, damage); part.amount -= used; damage -= used; absorbed += used; if (damage <= 0) break; }
        shields.RemoveAll(s => s.amount <= 0);
        if (absorbed > 0) run?.NotifyAbsorbed(absorbed);
        bool broken = before > 0 && before - absorbed <= .0001f;
        float healthDamage = Mathf.Min(currentHealth, damage);
        if (damage >= currentHealth && run != null && run.TryPreventFatal())
        { if (broken) run.NotifyBroken(); Refresh(); return; }
        currentHealth = Mathf.Max(0, currentHealth - damage);
        GetComponent<DamageFlash>()?.Play();
        Refresh();
        if (!IsAlive) { run?.EndBattle(); gameResultUI?.ShowGameOver(); return; }
        if (broken) run?.NotifyBroken();
        if (healthDamage > 0) run?.NotifyHurt(healthDamage);
    }
    public void AddShield(float amount, float duration = 6)
    {
        if (!IsAlive || amount <= 0 || duration <= 0) return;
        float space = Mathf.Max(0, maxHealth * .5f - Shield);
        amount = Mathf.Min(space, amount);
        if (amount > 0) { shields.Add(new ShieldPart { amount = amount, until = Time.time + duration }); Refresh(); }
    }
    private void RemoveExpired() { shields.RemoveAll(s => s.until <= Time.time || s.amount <= 0); }
    public void Heal(float amount) { if (IsAlive && amount > 0) { currentHealth = Mathf.Min(maxHealth, currentHealth + amount); Refresh(); } }
    public void SurviveFatal(float floor, float fraction, float immunity)
    { currentHealth = Mathf.Min(maxHealth, Mathf.Max(1, floor) + maxHealth * fraction); GrantInvincibility(immunity); Refresh(); }
    public void GrantInvincibility(float duration)
    { if (IsAlive && duration > 0 && !float.IsInfinity(duration)) nextDamageTime = Mathf.Max(nextDamageTime, Time.time + duration); }
    public void SetDodgeInvulnerability(bool value) { dodgeInvulnerable = value; }
    public void SetSkillInvulnerability(bool value) { skillInvulnerable = value; }
    public void AddMaxHealth(int amount) => AddMaxHealth(amount, amount);
    public void AddMaxHealth(int amount, float heal)
    {
        if (amount <= 0) return;
        int next = (int)System.Math.Min(int.MaxValue, (long)maxHealth + amount);
        maxHealth = next; currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0, heal)); Refresh();
    }
    public void SetStartingHealth(int health)
    { maxHealth = Mathf.Max(1, health); currentHealth = maxHealth; shields.Clear(); nextDamageTime = 0; dodgeInvulnerable = skillInvulnerable = false; Refresh(); }
    private void Update() { if (shields.Count > 0 && Time.timeScale > 0) { RemoveExpired(); gameHUD?.UpdateShield(Shield); } }
    private void Refresh() { gameHUD?.UpdateHealth(Mathf.CeilToInt(currentHealth), maxHealth); gameHUD?.UpdateShield(Shield); }
}

