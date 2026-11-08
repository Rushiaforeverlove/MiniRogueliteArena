using AbyssBar.Upgrades;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController), typeof(PlayerHealth))]
public class PlayerDodge : MonoBehaviour
{
    public enum UseResult { None, Used, NotConfigured, Paused, Defeated, CoolingDown, AlreadyDodging, Blocked, Disabled }

    [SerializeField, Min(0.1f)] private float dodgeDistance = 2.5f;
    [SerializeField, Min(0.05f)] private float dodgeDuration = 0.16f;
    [SerializeField, Min(0.1f)] private float cooldown = 1.2f;
    [Tooltip("Enemy and Hazard layers only. Do not include the player's or walls' layer.")]
    [SerializeField] private LayerMask passThroughLayers;

    private PlayerController controller;
    private PlayerHealth health;
    private Vector2 direction;
    private float remainingDuration;
    private float activeSpeed;
    private readonly SequentialCharges charges = new SequentialCharges();
    private Vector2 startPosition;
    private RunUpgradeRuntime Run => GetComponent<RunUpgradeRuntime>();
    private float distanceBonus;
    private bool isDodging;

    public bool IsDodging => isDodging;
    public int PassThroughLayers => passThroughLayers.value;
    public float CooldownRemaining => charges.Available > 0 ? 0 : charges.Remaining;
    public float Distance => dodgeDistance + distanceBonus + (Run != null ? Run.Stats.dodgeDistance : 0);
    public UseResult LastResult { get; private set; }
    public bool IsConfigured => controller != null && controller.HasPhysicsBody && health != null
        && passThroughLayers.value != 0 && (passThroughLayers.value & (1 << gameObject.layer)) == 0
        && IsPositiveFinite(Distance) && IsPositiveFinite(dodgeDuration) && IsPositiveFinite(cooldown);

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        charges.Reset(cooldown);
    }

    // Reserved for node 2's saved dodge-distance bonus; no progression is changed here.
    public void SetStartingDistanceBonus(float bonus)
    {
        distanceBonus = float.IsNaN(bonus) || float.IsInfinity(bonus) ? 0f : Mathf.Max(0f, bonus);
    }

    public UseResult TryDodge(Vector2 requestedDirection)
    {
        if (!isActiveAndEnabled) return LastResult = UseResult.Disabled;
        if (!IsConfigured || !controller.isActiveAndEnabled) return LastResult = UseResult.NotConfigured;
        if (Time.timeScale <= 0f) return LastResult = UseResult.Paused;
        if (!health.IsAlive) return LastResult = UseResult.Defeated;
        if (IsDodging) return LastResult = UseResult.AlreadyDodging;
        if (CooldownRemaining > 0f) return LastResult = UseResult.CoolingDown;
        if (float.IsNaN(requestedDirection.x) || float.IsNaN(requestedDirection.y)
            || float.IsInfinity(requestedDirection.x) || float.IsInfinity(requestedDirection.y))
            return LastResult = UseResult.NotConfigured;
        Vector2 chosen = requestedDirection.sqrMagnitude > 0.0001f ? requestedDirection.normalized : Vector2.right;
        if (controller.GetAllowedDodgeDistance(chosen, Distance) <= 0.0001f)
            return LastResult = UseResult.Blocked;

        direction = chosen;
        GetComponent<AbyssBar.Mio.MioCombat>()?.InterruptForDodge();
        remainingDuration = dodgeDuration;
        activeSpeed = Distance / dodgeDuration;
        charges.Spend();
        startPosition = transform.position;
        var action = Run?.Begin(RunActionKind.Dodge, 0, direction);
        if (action != null) Run.EndAction(action);
        isDodging = true;
        controller.BeginDodgeCollision(passThroughLayers.value);
        // Separate from hurt immunity, so ending a dodge cannot erase its remaining time.
        health.SetDodgeInvulnerability(true);
        return LastResult = UseResult.Used;
    }

    // PlayerController is the sole Rigidbody mover and calls this once per physics step.
    public Vector2 GetDisplacement(float deltaTime)
    {
        if (!IsDodging || Time.timeScale <= 0f || deltaTime <= 0f
            || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return Vector2.zero;
        if (health == null || !health.IsAlive)
        {
            CancelDodge();
            return Vector2.zero;
        }
        if (remainingDuration <= 0.000001f)
        {
            // Restore before the next movement step, after the final dash step was simulated.
            CancelDodge();
            Run?.EndDodge(startPosition);
            return Vector2.zero;
        }
        float stepTime = Mathf.Min(deltaTime, remainingDuration);
        remainingDuration = Mathf.Max(0f, remainingDuration - stepTime);
        return direction * (activeSpeed * stepTime);
    }

    public void StopAtObstacle() { remainingDuration = 0f; }
    public void CancelDodge()
    {
        remainingDuration = 0f;
        isDodging = false;
        controller?.EndDodgeCollision();
        health?.SetDodgeInvulnerability(false);
    }
    public int Charges => charges.Available;
    public int ChargeCapacity => charges.Capacity;
    public float RechargeRemaining => charges.Remaining;
    public void RefreshCharges(RunStats stats) => charges.Configure(1 + stats.dodgeCharges, cooldown);
    public void Advance(float seconds) => charges.AdvanceCurrent(seconds);
    public void Restore(int amount = 1) => charges.Restore(amount);
    private void Update() { if (Time.timeScale > 0 && health != null && health.IsAlive) charges.Tick(Time.deltaTime); }
    private void OnDisable() { CancelDodge(); }
    private static bool IsPositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
}


