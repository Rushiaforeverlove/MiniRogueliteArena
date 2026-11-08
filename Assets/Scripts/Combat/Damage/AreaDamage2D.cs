using UnityEngine;

public static class AreaDamage2D
{
    // One health component per enemy: multiple colliders do not multiply a single hit.
    public static void Deal(Vector2 center, float radius, int damage)
    {
        foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>())
            if (enemy != null && enemy.IsAlive && Vector2.Distance(center, enemy.transform.position) <= radius)
                enemy.TakeDamage(damage);
    }
}

