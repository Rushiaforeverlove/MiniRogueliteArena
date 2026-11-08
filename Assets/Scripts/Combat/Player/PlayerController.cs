using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Min(0.001f)] private float dodgeCollisionSkin = 0.02f;

    private Rigidbody2D rb;
    private PlayerDodge dodge;
    private Vector2 moveInput;
    private Vector2 lastMoveDirection = Vector2.right;
    private readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(8);
    private bool dodgeCollisionActive;
    private LayerMask originalExcludedLayers;
    private Vector2 pendingKnockback;
    public bool HasPhysicsBody => rb != null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        dodge = GetComponent<PlayerDodge>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || AbyssBar.Inputs.GameInput.Blocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        moveInput = new Vector2(AbyssBar.Inputs.GameInput.Horizontal, AbyssBar.Inputs.GameInput.Vertical).normalized;
        if (moveInput.sqrMagnitude > 0.001f) lastMoveDirection = moveInput;

        // Sample movement and dodge together so script execution order cannot change direction.
        if (dodge != null && AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Dodge))
            dodge.TryDodge(lastMoveDirection);
    }

    public Vector2 MovementInput => moveInput;
    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f) return;
        if (pendingKnockback.sqrMagnitude > .000001f)
        {
            Vector2 direction = pendingKnockback.normalized;
            rb.MovePosition(rb.position + direction * GetAllowedDodgeDistance(direction, pendingKnockback.magnitude));
            pendingKnockback = Vector2.zero; return;
        }
        if (dodge != null && dodge.IsDodging)
        {
            Vector2 step = dodge.GetDisplacement(Time.fixedDeltaTime);
            float desired = step.magnitude;
            if (desired <= 0f)
            {
                rb.MovePosition(rb.position);
                return;
            }
            Vector2 direction = step / desired;
            float allowed = GetAllowedDodgeDistance(direction, desired);
            rb.MovePosition(rb.position + direction * allowed);
            if (allowed + 0.0001f < desired) dodge.StopAtObstacle();
            return;
        }
        var run = GetComponent<AbyssBar.Upgrades.RunUpgradeRuntime>();
        var mio = GetComponent<AbyssBar.Mio.MioCombat>();
        if (mio != null && mio.isActiveAndEnabled && mio.TryMovementStep(Time.fixedDeltaTime, out Vector2 actionStep))
        {
            float desired = actionStep.magnitude;
            if (desired > .0001f) { float allowed = GetAllowedDodgeDistance(actionStep.normalized,desired); rb.MovePosition(rb.position + actionStep.normalized*allowed); if (allowed+.0001f<desired) mio.StopAtWall(); }
            return;
        }
        float bonus = run != null ? run.Stats.moveSpeed + run.TemporaryMoveSpeed : 0;
        float slow = run != null && Time.time < run.SlowUntil ? 1 - run.SlowFraction : 1;
        rb.MovePosition(rb.position + moveInput * moveSpeed * (1 + bonus) * slow * Time.fixedDeltaTime);
    }

    // Cast the Rigidbody's own collider shapes before moving; triggers do not block a dodge.
    public float GetAllowedDodgeDistance(Vector2 direction, float distance)
    {
        if (rb == null || distance <= 0f || direction.sqrMagnitude < 0.0001f) return 0f;
        ContactFilter2D filter = new ContactFilter2D();
        // Cast uses its own filter: exclude pass-through layers here as well as on the body.
        int passThrough = dodge != null ? dodge.PassThroughLayers : 0;
        int mask = (Physics2D.GetLayerCollisionMask(gameObject.layer) | rb.includeLayers.value)
            & ~rb.excludeLayers.value & ~passThrough;
        filter.SetLayerMask(mask);
        filter.useTriggers = false;
        float skin = Mathf.Max(0.001f, dodgeCollisionSkin);
        castHits.Clear();
        rb.Cast(direction.normalized, filter, castHits, distance + skin);
        float allowed = distance;
        foreach (RaycastHit2D hit in castHits)
        {
            if (hit.collider != null)
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - skin));
        }
        return allowed;
    }

    // Only this player's body is changed; the global collision matrix stays untouched.
    public void BeginDodgeCollision(int passThroughLayers)
    {
        if (rb == null || dodgeCollisionActive) return;
        originalExcludedLayers = rb.excludeLayers;
        rb.excludeLayers = originalExcludedLayers.value | passThroughLayers;
        dodgeCollisionActive = true;
    }

    public void EndDodgeCollision()
    {
        if (!dodgeCollisionActive) return;
        if (rb != null)
        {
            rb.excludeLayers = originalExcludedLayers;
            rb.linearVelocity = Vector2.zero;
        }
        dodgeCollisionActive = false;
    }

    private void OnDisable()
    {
        dodge?.CancelDodge();
        EndDodgeCollision();
        moveInput = Vector2.zero;
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(0.1f, moveSpeed + amount);
        Debug.Log("Move Speed: " + moveSpeed);
    }
    // Damage sources can use this API; stability buffs and dodge immunity apply automatically.
    public bool TryApplyKnockback(Vector2 direction, float distance)
    {
        var run = GetComponent<AbyssBar.Upgrades.RunUpgradeRuntime>();
        if (run == null || !run.Active || Time.timeScale <= 0 || Time.time < run.StabilityUntil
            || (dodge != null && dodge.IsDodging) || GetComponent<AbyssBar.Mio.MioCombat>()?.IsEInvulnerable == true || distance <= 0 || float.IsNaN(distance) || float.IsInfinity(distance)
            || float.IsNaN(direction.x) || float.IsNaN(direction.y) || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)) return false;
        pendingKnockback += direction.normalized * Mathf.Min(5, distance); return true;
    }

    public void SetStartingMoveSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
    }
}


