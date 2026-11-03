using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private GameObject experienceGemPrefab;

    private int currentHealth;
    public bool IsAlive => currentHealth > 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // Several fan-shot projectiles can arrive before Destroy removes this object.
        if (damage <= 0 || !IsAlive) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        GetComponent<DamageFlash>()?.Play();
        Debug.Log(gameObject.name + " HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (experienceGemPrefab != null)
        {
            Instantiate(experienceGemPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}
