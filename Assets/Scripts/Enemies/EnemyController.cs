using UnityEngine;
using AbyssBar.Enemies;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private Transform target;

    private Rigidbody2D rb;
    private EnemyHealth health;
    private IEnemyMovementPolicy movementPolicy;
    private EnemyDamage contactAttack;
    private IEnemyPhysicsAction physicsAction;
    private Vector2 previousPhysicsPosition;
    private float stationarySeconds;
    public bool IsMoving { get; private set; }
    public Vector2 MovementDirection { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        movementPolicy = GetComponent<IEnemyMovementPolicy>();
        contactAttack = GetComponent<EnemyDamage>();
        physicsAction = GetComponent<IEnemyPhysicsAction>();
        previousPhysicsPosition = rb.position;
    }

    private void FixedUpdate()
    {
        // Observe actual movement once per physics tick, then retain the result
        // for every rendered frame until the next tick. Render frames may have
        // zero physics updates, especially at 120/144/240 Hz.
        Vector2 traveled = rb.position - previousPhysicsPosition;
        previousPhysicsPosition = rb.position;
        if (target == null || Time.timeScale <= 0 || health != null && !health.IsAlive)
        {
            ClearMovementPresentation();
            return;
        }

        if (physicsAction != null && physicsAction.TryGetStep(target, rb.position, Time.fixedDeltaTime, out Vector2 actionStep))
        {
            UpdateMovementPresentation(actionStep.sqrMagnitude > .000001f && rb.simulated, traveled);
            rb.MovePosition(rb.position + actionStep);
            return;
        }
        Vector2 direction = target.position - transform.position;
        direction = direction.normalized;
        if (movementPolicy != null) direction = movementPolicy.MovementDirection(target, rb.position);
        if (contactAttack != null && contactAttack.IsAttacking) direction = Vector2.zero;

        float speed = moveSpeed * (health?.Status.MoveMultiplier ?? 1);
        UpdateMovementPresentation(direction.sqrMagnitude > .0001f && speed > .0001f && rb.simulated, traveled);
        Vector2 nextPosition = rb.position + direction * speed * Time.fixedDeltaTime;
        rb.MovePosition(nextPosition);
    }

    private void UpdateMovementPresentation(bool wantsToMove, Vector2 traveled)
    {
        if (!wantsToMove) { ClearMovementPresentation(); return; }
        float minimumStep = .05f * Mathf.Max(.0001f, Time.fixedDeltaTime);
        if (traveled.sqrMagnitude > minimumStep * minimumStep)
        {
            IsMoving = true; MovementDirection = traveled.normalized; stationarySeconds = 0;
        }
        else
        {
            // Ignore brief contact jitter, but genuinely blocked bodies return
            // to idle. This delay is measured in physics time, never render frames.
            stationarySeconds += Time.fixedDeltaTime;
            if (stationarySeconds >= .08f) ClearMovementPresentation();
        }
    }
    private void ClearMovementPresentation()
    { IsMoving = false; MovementDirection = Vector2.zero; stationarySeconds = 0; }
    private void OnEnable()
    { if (rb != null) previousPhysicsPosition = rb.position; ClearMovementPresentation(); }
    private void OnDisable() { ClearMovementPresentation(); }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}

