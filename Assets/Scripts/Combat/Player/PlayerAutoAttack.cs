using UnityEngine;
using AbyssBar.Upgrades;
using AbyssBar.Combat;

[DefaultExecutionOrder(200)]
public class PlayerAutoAttack : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float attackInterval = .6f;
    [SerializeField] private float attackRange = 8;
    [Header("Mouse Primary Attack")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private bool faceMouse = true;
    [SerializeField] private bool showReticle = true;
    [SerializeField, Min(.05f)] private float reticleSize = .28f;
    [SerializeField] private Material reticleMaterial;
    private float permanentSpeed;
    private int startingDamageBonus;
    private RunUpgradeRuntime run;
    private UpgradeManager upgrades;
    private SpriteRenderer body;
    private MouseAttackReticle reticle;
    private PrimaryAttackSession session;
    private RunAction pendingAction;
    private readonly PrimaryAttackGesture gesture = new PrimaryAttackGesture();
    private readonly PrimaryAttackCadence cadence = new PrimaryAttackCadence();
    private readonly PrimaryAttackUiQuery ui = new PrimaryAttackUiQuery();
    private bool allowAttack, wasBlocked = true, focused = true, applicationPaused;
    public event System.Action<RunAction> BasicAttackExecuted;
    public WeaponData CurrentWeapon { get; private set; }
    public PrimaryAttackInput CurrentInput { get; private set; }
    public Vector2 AimDirection { get; private set; } = Vector2.right;
    public Vector2 AimWorldPoint { get; private set; }
    public bool HasMouseAim { get; private set; }
    public RunUpgradeRuntime Runtime => run;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float BaseDamage => (CurrentWeapon != null && CurrentWeapon.PrimaryAttackStyle is AbyssBar.Mio.MioPrimaryAttackStyle sword
        ? sword.BaseDamage : (float)(projectilePrefab != null ? projectilePrefab.GetComponent<Projectile>()?.Damage ?? 100 : 100)) + startingDamageBonus;
    public float AttackInterval => Mathf.Max(.02f, attackInterval / (1 + permanentSpeed + (run != null ? run.Stats.attackSpeed : 0)));
    public float AttackRange => Mathf.Max(.1f, attackRange + (run != null ? run.Stats.range + run.NextAttackRange : 0));
    private void Awake()
    { run = GetComponent<RunUpgradeRuntime>(); body = GetComponent<SpriteRenderer>(); focused = Application.isFocused; session = new ProjectilePrimaryAttackSession(); }
    private void LateUpdate()
    {
        if (run == null) run = GetComponent<RunUpgradeRuntime>();
        if (upgrades == null) upgrades = FindAnyObjectByType<UpgradeManager>();
        if (aimCamera == null) aimCamera = Camera.main;
        bool down = AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Primary), held = AbyssBar.Inputs.GameInput.Held(AbyssBar.Inputs.GameAction.Primary), up = AbyssBar.Inputs.GameInput.Up(AbyssBar.Inputs.GameAction.Primary);
        bool battleActive = isActiveAndEnabled && run != null && run.Active && Time.timeScale > 0;
        if (battleActive) cadence.Tick(Time.deltaTime);
        bool projected = MouseAttackAim.TryProject(aimCamera, Input.mousePosition, transform.position, AimDirection, out Vector2 point, out Vector2 direction);
        bool blocked = AbyssBar.Inputs.GameInput.Blocked || !battleActive || session == null || projectilePrefab == null || !focused || !Application.isFocused || applicationPaused || !projected
            || upgrades != null && upgrades.BlocksPrimaryAttack || ui.IsBlocked(Input.mousePosition);
        var frame = gesture.Sample(down, held, up, blocked, battleActive ? Time.deltaTime : 0);
        frame.direction = direction; frame.worldPoint = point; CurrentInput = frame;
        allowAttack = !blocked; HasMouseAim = !blocked;
        if (blocked)
        {
            if (!wasBlocked || frame.cancelled) session?.Cancel(this);
            wasBlocked = true; if (reticle != null) reticle.Hide(); return;
        }
        wasBlocked = false; AimDirection = direction; AimWorldPoint = point;
        if (faceMouse && body != null && Mathf.Abs(direction.x) > .1f) body.flipX = direction.x < 0;
        if (showReticle)
        {
            if (reticle == null) reticle = GetComponent<MouseAttackReticle>() ?? gameObject.AddComponent<MouseAttackReticle>();
            reticle.Show(point, transform.position.z, reticleSize, reticleMaterial != null ? reticleMaterial : body != null ? body.sharedMaterial : null,
                body != null ? body.sortingLayerID : 0, frame.held);
        }
        else if (reticle != null) reticle.Hide();
        session?.Tick(this, frame);
    }
    // Style sessions use the same cadence, upgrade action kind and damage path.
    // Begin and Complete must pair within one Tick; charging state lives in the session.
    public bool TryBeginBasicAttack(out RunAction action, bool melee = false, bool charged = false)
    {
        action = null;
        if (!allowAttack || !isActiveAndEnabled || run == null || !run.Active || Time.timeScale <= 0 || pendingAction != null
            || !cadence.TrySpend(AttackInterval)) return false;
        float range = AttackRange; // Snapshot before BeforeAction consumes one-shot bonuses.
        pendingAction = run.Begin(RunActionKind.Attack, BaseDamage, AimDirection, melee: melee, charged: charged);
        pendingAction.projectileRange = Mathf.Max(.1f, range * pendingAction.rangeMultiplier + pendingAction.rangeBonus);
        action = pendingAction; return true;
    }
    public void CompleteBasicAttack(RunAction action)
    {
        if (action == null || action != pendingAction) return;
        pendingAction = null; run.EndAction(action); BasicAttackExecuted?.Invoke(action);
    }
    public bool TryFireProjectileAttack()
    {
        if (projectilePrefab == null || !TryBeginBasicAttack(out var action)) return false;
        try
        {
            int count = Mathf.Max(0, run.Stats.extraProjectiles);
            for (int i = 0; i <= count; i++)
            {
                float angle = i == 0 ? 0 : (i % 2 == 0 ? 1 : -1) * 10 * ((i + 1) / 2);
                Vector2 direction = Quaternion.Euler(0, 0, angle) * action.direction;
                run.Shot(run.Position, direction, action.basis * action.multiplier * (i == 0 ? 1 : run.ExtraProjectileCoefficient),
                    1 + run.Stats.extraTargets, action, false);
            }
        }
        finally { CompleteBasicAttack(action); }
        return true;
    }
    public void SuspendInput()
    {
        gesture.Interrupt(); session?.Cancel(this); allowAttack = false; HasMouseAim = false; wasBlocked = true;
        if (reticle != null) reticle.Hide();
    }
    public Vector2 SampleAimDirection()
    {
        if (aimCamera == null) aimCamera = Camera.main;
        return MouseAttackAim.TryProject(aimCamera, Input.mousePosition, transform.position, AimDirection, out _, out var direction)
            ? direction : AimDirection;
    }
    public void ReduceAttackInterval(float amount) { attackInterval = Mathf.Max(.02f, attackInterval - amount); }
    public void SetStartingWeapon(WeaponData weapon, int damageBonus = 0, float attackSpeedBonus = 0)
    {
        if (weapon == null || weapon.ProjectilePrefab == null || weapon.ProjectilePrefab.GetComponent<Projectile>() == null
            || weapon.AttackInterval <= 0 || float.IsNaN(weapon.AttackInterval) || float.IsInfinity(weapon.AttackInterval)
            || weapon.AttackRange <= 0 || float.IsNaN(weapon.AttackRange) || float.IsInfinity(weapon.AttackRange))
        { SuspendInput(); session = null; projectilePrefab = null; CurrentWeapon = null; Debug.LogError("Missing/invalid weapon, projectile, interval or range.", this); return; }
        SuspendInput(); CurrentWeapon = weapon;
        projectilePrefab = weapon.ProjectilePrefab; startingDamageBonus = Mathf.Max(0, damageBonus);
        permanentSpeed = Mathf.Max(0, attackSpeedBonus); attackInterval = weapon.AttackInterval; attackRange = weapon.AttackRange;
        session = weapon.PrimaryAttackStyle != null ? weapon.PrimaryAttackStyle.CreateSession() : new ProjectilePrimaryAttackSession();
        if (session == null) Debug.LogError("Primary attack style returned no runtime session.", this);
        cadence.Reset();
    }
    private void OnApplicationFocus(bool value) { focused = value; SuspendInput(); }
    private void OnApplicationPause(bool value) { applicationPaused = value; SuspendInput(); }
    private void OnEnable() { gesture.Interrupt(); }
    private void OnDisable() { SuspendInput(); }
}

