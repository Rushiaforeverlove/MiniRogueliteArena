using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackInterval = 0.6f;
    [SerializeField] private float attackRange = 8f;

    private float attackTimer;

    private void Update()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            AttackNearestEnemy();
        }
    }

    private void AttackNearestEnemy()
    {
        GameObject nearestEnemy = FindNearestEnemy();

        if (nearestEnemy == null)
        {
            return;
        }

        Vector2 direction = nearestEnemy.transform.position - transform.position;

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            transform.position,
            Quaternion.identity
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(direction);
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        GameObject nearestEnemy = null;
        float nearestDistance = attackRange;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestEnemy = enemy;
            }
        }

        return nearestEnemy;
    }

    public void ReduceAttackInterval(float amount)
    {
        attackInterval -= amount;
        attackInterval = Mathf.Max(0.15f, attackInterval);

        Debug.Log("Attack Interval: " + attackInterval);
    }
}