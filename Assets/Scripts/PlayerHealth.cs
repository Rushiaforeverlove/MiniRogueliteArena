using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;

    private int currentHealth;

    [SerializeField, Min(0f)] private float invincibilityDuration = 0.8f;

    private float nextDamageTime;

    private void Awake()
    {
        Time.timeScale = 1f;
        currentHealth = maxHealth;
    }

    private void Start()
    {
        gameHUD.UpdateHealth(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        if (Time.time < nextDamageTime)
        {
            return;
        }

        nextDamageTime = Time.time + invincibilityDuration;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        GetComponent<DamageFlash>()?.Play();

        gameHUD.UpdateHealth(currentHealth, maxHealth);
        Debug.Log("Player HP: " + currentHealth + " / " + maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void AddMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount;

        gameHUD.UpdateHealth(currentHealth, maxHealth);

        Debug.Log("Max HP: " + maxHealth);
        Debug.Log("Current HP: " + currentHealth);
    }

    private void Die()
    {
        gameResultUI.ShowGameOver();
    }

    public void SetStartingHealth(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
        nextDamageTime = 0f;
        gameHUD.UpdateHealth(currentHealth, maxHealth);
    }
}