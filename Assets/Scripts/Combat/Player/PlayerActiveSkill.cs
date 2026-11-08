using UnityEngine;
using AbyssBar.Upgrades;

[DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
public class PlayerActiveSkill : MonoBehaviour
{
    public enum UseResult { None, Used, NotConfigured, Paused, Defeated, CoolingDown, NoTarget, Locked, SequenceActive, Disabled, Blocked }
    [SerializeField] private bool previewQInEditor;
    private sealed class Slot
    {
        public ActiveSkillData data;
        public readonly SequentialCharges charges = new SequentialCharges();
        public RunAction action;
        public int pending;
        public float until, radius;
        public UseResult result;
    }
    private readonly Slot e = new Slot(), q = new Slot();
    private bool qUnlocked;
    private int damageBonus;
    private PlayerHealth health;
    private RunUpgradeRuntime Run => GetComponent<RunUpgradeRuntime>();
    public ActiveSkillData Skill => e.data;
    public ActiveSkillData UltimateSkill => q.data;
    public bool IsConfigured => e.data != null && e.data.IsConfigured;
    public bool IsUltimateConfigured => q.data != null && q.data.IsConfigured;
    public bool IsQPreviewActive => Application.isEditor && previewQInEditor && !qUnlocked;
    public bool IsQUnlocked => qUnlocked || IsQPreviewActive;
    public float CooldownRemaining => e.charges.Available > 0 ? 0 : e.charges.Remaining;
    public float UltimateCooldownRemaining => q.charges.Available > 0 ? 0 : q.charges.Remaining;
    public int ECharges => e.charges.Available;
    public int QCharges => q.charges.Available;
    public int EChargeCapacity => e.charges.Capacity;
    public int QChargeCapacity => q.charges.Capacity;
    public float ERechargeRemaining => e.charges.Remaining;
    public float QRechargeRemaining => q.charges.Remaining;
    public bool IsSequenceRunning => e.pending > 0;
    public bool IsUltimateSequenceRunning => q.pending > 0;
    public UseResult LastResult => e.result;
    public UseResult UltimateLastResult => q.result;
    public float EBaseDamage => Basis(e.data);
    public void SetStartingDamageBonus(int bonus) { damageBonus = Mathf.Max(0, bonus); }
    private void Awake() { health = GetComponent<PlayerHealth>(); }
    public void SetStartingSkill(ActiveSkillData data) { e.data = data; e.pending = 0; e.charges.Reset(data != null ? data.Cooldown : 1); }
    public void SetStartingSkills(ActiveSkillData startingE, ActiveSkillData ultimateQ, bool unlocked)
    { SetStartingSkill(startingE); q.data = ultimateQ; q.charges.Reset(ultimateQ != null ? ultimateQ.Cooldown : 1); q.pending = 0; qUnlocked = unlocked; RefreshCharges(Run != null ? Run.Stats : new RunStats()); }
    public void SetQUnlocked(bool value) { qUnlocked = value; if (!IsQUnlocked) q.pending = 0; }
    public void RefreshCharges(RunStats stats)
    {
        e.charges.Configure((e.data != null ? e.data.BaseCharges : 1) + stats.eCharges, (e.data != null ? e.data.Cooldown : 1) * Mathf.Max(.1f, 1 - stats.eCooldownReduction));
        q.charges.Configure((q.data != null ? q.data.BaseCharges : 1) + stats.qCharges, q.data != null ? q.data.Cooldown : 1);
    }
    public void AdvanceE(float seconds) => e.charges.AdvanceCurrent(seconds);
    public void AdvanceQ(float seconds) => q.charges.AdvanceCurrent(seconds);
    public void RestoreE(int count = 1) => e.charges.Restore(count);
    private void Update()
    {
        if (health == null || !health.IsAlive) { CancelRunSequences(); return; }
        if (Time.timeScale <= 0) return;
        e.charges.Tick(Time.deltaTime); q.charges.Tick(Time.deltaTime);
        Tick(e); if (IsQUnlocked) Tick(q); else q.pending = 0;
        if (AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Skill)) TryUseSkill();
        if (AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Ultimate)) TryUseUltimate();
    }
    public UseResult TryUseSkill() => e.result = Use(e, RunActionKind.E);
    public UseResult TryUseUltimate() => q.result = IsQUnlocked ? Use(q, RunActionKind.Q) : UseResult.Locked;
    private float Basis(ActiveSkillData data)
    {
        if (data == null) return 0;
        var extension = GetComponent<AbyssBar.Mio.IPlayerSkillAction>();
        if (extension != null && extension.Accepts(data)) return extension.Basis(data);
        if(data.Kind==ActiveSkillKind.MioDash||data.Kind==ActiveSkillKind.MioRelease)return 0;
        return (data.Kind == ActiveSkillKind.Shockwave ? data.Damage
            : data.ProjectilePrefab.GetComponent<Projectile>().Damage) + damageBonus;
    }
    private UseResult Use(Slot slot, RunActionKind kind)
    {
        if (!isActiveAndEnabled) return UseResult.Disabled;
        var data = slot.data;
        if (data == null || !data.IsConfigured || Run == null || !Run.Active) return UseResult.NotConfigured;
        if (Time.timeScale <= 0) return UseResult.Paused;
        if (health == null || !health.IsAlive) return UseResult.Defeated;
        if (slot.pending > 0) return UseResult.SequenceActive;
        var extension = GetComponent<AbyssBar.Mio.IPlayerSkillAction>();
        if (data.Kind == ActiveSkillKind.MioDash || data.Kind == ActiveSkillKind.MioRelease)
        {
            if (extension == null || !extension.Accepts(data) || extension is Behaviour behaviour && !behaviour.isActiveAndEnabled) return UseResult.NotConfigured;
            var check = extension.CanExecute(data); if (check != UseResult.Used) return check;
            bool token = kind == RunActionKind.E && Run.TryFreeE();
            if (!token && slot.charges.Available == 0) return UseResult.CoolingDown;
            if (!token) slot.charges.Spend();
            Vector2 aim = Run.Attack != null ? Run.Attack.AimDirection : Vector2.right;
            var customAction = Run.Begin(kind, extension.Basis(data), aim, token, data.Kind == ActiveSkillKind.MioDash);
            extension.Execute(data,customAction); Run.EndAction(customAction); return UseResult.Used;
        }
        float rangeMultiplier = kind == RunActionKind.E ? Run.ETargetRangeMultiplier : 1;
        var nearest = Run.Nearest(null, (data.TargetRange + Run.Stats.range) * rangeMultiplier);
        if (data.Kind == ActiveSkillKind.FanShot && nearest == null) return UseResult.NoTarget;
        // A free token may be used even with zero filled charges; failures above never consume it.
        bool free = kind == RunActionKind.E && Run.TryFreeE();
        if (!free && slot.charges.Available == 0) return UseResult.CoolingDown;
        if (!free) slot.charges.Spend();
        Vector2 direction = nearest != null ? ((Vector2)nearest.transform.position - Run.Position).normalized : Vector2.right;
        slot.action = Run.Begin(kind, Basis(data), direction, free, data.Kind == ActiveSkillKind.Shockwave, data.ShockwaveCount);
        slot.radius = data.Radius * slot.action.rangeMultiplier;
        if (Run.Has("guard.outer_ring")) slot.radius *= 1 + Run.Param("guard.outer_ring", "radiusBonus");
        if (data.Kind == ActiveSkillKind.Shockwave)
        {
            slot.pending = data.ShockwaveCount - 1; slot.until = data.ShockwaveInterval;
            Emit(slot, slot.pending == 0);
        }
        else
        {
            int count = data.ProjectileCount;
            for (int i = 0; i < count; i++)
            {
                float angle = data.Kind == ActiveSkillKind.RadialShot ? 360f * i / count
                    : count == 1 ? 0 : -data.SpreadDegrees * .5f + data.SpreadDegrees * i / (count - 1);
                Vector2 dir = data.Kind == ActiveSkillKind.RadialShot ? (Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.right)
                    : (Vector2)(Quaternion.Euler(0, 0, angle) * direction);
                var mechanics = data.CreateProjectileEffects();
                int bonusTargets = kind == RunActionKind.E ? Run.Stats.eExtraTargets : 0;
                if (mechanics.ExplosionRadius > 0) mechanics.ExplosionRadius += Run.Stats.explosionRadiusBonus;
                Run.Shot(Run.Position, dir, slot.action.basis * slot.action.multiplier,
                    mechanics.MaxTargets + bonusTargets, slot.action, false, 0, null, mechanics);
            }
        }
        Run.EndAction(slot.action); return UseResult.Used;
    }
    private void Emit(Slot slot, bool last)
    {
        var run = Run; if (run == null || !run.Active) return;
        var a = slot.action; a.radius = slot.radius;
        run.Area(run.Position, slot.radius, a.basis * a.multiplier, a, false, true);
        run.Pulse(a, run.Position, slot.radius, a.basis, last); a.pulseIndex++;
    }
    private void Tick(Slot slot)
    {
        if (slot.pending <= 0) return;
        slot.until -= Time.deltaTime;
        while (slot.pending > 0 && slot.until <= 0 && health.IsAlive)
        { slot.pending--; Emit(slot, slot.pending == 0); slot.until += slot.data.ShockwaveInterval; }
    }
    public void CancelRunSequences() { e.pending = q.pending = 0; GetComponent<AbyssBar.Mio.IPlayerSkillAction>()?.CancelSkillActions(); }
    private void OnDisable() { CancelRunSequences(); }
}

