using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;

    private int currentHealth;

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
        currentHealth -= damage;
        Debug.Log("Player HP: " + currentHealth + " / " + maxHealth);

        gameHUD.UpdateHealth(currentHealth, maxHealth);

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
}