using UnityEngine;

// Optional hazard for checking both solid and trigger damage sources.
// PlayerHealth decides whether contact damage is currently allowed.
public class DamageZone2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 1;

    private void OnTriggerStay2D(Collider2D other) { Damage(other); }
    private void OnCollisionStay2D(Collision2D collision) { Damage(collision.collider); }

    private void Damage(Collider2D other)
    {
        if (other == null) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.TakeDamage(damage);
    }
}
