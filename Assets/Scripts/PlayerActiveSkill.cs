using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerActiveSkill : MonoBehaviour
{
    public enum UseResult { None, Used, NotConfigured, Paused, Defeated, CoolingDown, NoTarget, Locked, SequenceActive, Disabled }

    [Header("Editor Testing")]
    [Tooltip("Temporarily previews Q in the Unity Editor only. Has no effect in a player build or on saves.")]
    [SerializeField] private bool previewQInEditor;

    // Each slot owns its battle state, even if both use the same data asset.
    private sealed class SkillSlot
    {
        public ActiveSkillData Data;
        public float NextReadyTime;
        public UseResult LastResult;
        public int PendingPulses;
        public float UntilPulse, PulseInterval, PulseRadius;
        public int PulseDamage;
        public SkillPulseVisual PulseVisual;
        public bool IsConfigured => Data != null && Data.IsConfigured;
        public float Remaining => Mathf.Max(0f, NextReadyTime - Time.time);

        public void Configure(ActiveSkillData data)
        {
            Data = data;
            NextReadyTime = Time.time;
            LastResult = UseResult.None;
            PendingPulses = 0;
        }
    }

    private readonly SkillSlot eSlot = new SkillSlot();
    private readonly SkillSlot qSlot = new SkillSlot();
    private PlayerHealth health;
    private bool qUnlocked;
    private int startingDamageBonus;
    public void SetStartingDamageBonus(int bonus) { startingDamageBonus = Mathf.Max(0, bonus); }

    // Existing E API is preserved for callers from the previous lesson.
    public ActiveSkillData Skill => eSlot.Data;
    public bool IsConfigured => eSlot.IsConfigured;
    public float CooldownRemaining => eSlot.Remaining;
    public UseResult LastResult => eSlot.LastResult;
    public bool IsSequenceRunning => eSlot.PendingPulses > 0;

    public ActiveSkillData UltimateSkill => qSlot.Data;
    public bool IsUltimateConfigured => qSlot.IsConfigured;
    public bool IsQPreviewActive => Application.isEditor && previewQInEditor && !qUnlocked;
    public bool IsQUnlocked => qUnlocked || IsQPreviewActive;
    public float UltimateCooldownRemaining => qSlot.Remaining;
    public UseResult UltimateLastResult => qSlot.LastResult;
    public bool IsUltimateSequenceRunning => qSlot.PendingPulses > 0;

    private void Awake() { health = GetComponent<PlayerHealth>(); }

    public void SetStartingSkill(ActiveSkillData startingSkill)
    {
        eSlot.Configure(startingSkill);
        if (!eSlot.IsConfigured)
            Debug.LogWarning("Assign a valid Starting E Skill on this character's CharacterData.", this);
    }

    public void SetStartingSkills(ActiveSkillData startingE, ActiveSkillData ultimateQ, bool isQUnlocked)
    {
        SetStartingSkill(startingE);
        qSlot.Configure(ultimateQ);
        qUnlocked = isQUnlocked;
    }

    // Changing availability never resets a running cooldown. Persistence belongs to progression.
    public void SetQUnlocked(bool value)
    {
        qUnlocked = value;
        if (!IsQUnlocked) qSlot.PendingPulses = 0;
    }

    private void Update()
    {
        TickSequences(Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.E)) TryUseSkill();
        if (Input.GetKeyDown(KeyCode.Q)) TryUseUltimate();
    }

    public UseResult TryUseSkill()
    {
        return eSlot.LastResult = TryUse(eSlot);
    }

    public UseResult TryUseUltimate()
    {
        if (!IsQUnlocked) return qSlot.LastResult = UseResult.Locked;
        return qSlot.LastResult = TryUse(qSlot);
    }

    private UseResult TryUse(SkillSlot slot)
    {
        if (!isActiveAndEnabled) return UseResult.Disabled;
        if (!slot.IsConfigured) return UseResult.NotConfigured;
        if (Time.timeScale <= 0f) return UseResult.Paused;
        if (health == null || !health.IsAlive) return UseResult.Defeated;
        if (slot.Remaining > 0f) return UseResult.CoolingDown;
        if (slot.PendingPulses > 0) return UseResult.SequenceActive;

        ActiveSkillData data = slot.Data;
        if (data.Kind == ActiveSkillKind.RadialShot)
        {
            // i/count gives distinct directions: the final shot never duplicates 0 degrees.
            for (int i = 0; i < data.ProjectileCount; i++)
                SpawnShot(data, DirectionAtAngle(360f * i / data.ProjectileCount));
        }
        else
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>();
            if (data.Kind == ActiveSkillKind.FanShot)
            {
                EnemyHealth nearest = FindNearest(enemies, data.TargetRange);
                if (nearest == null) return UseResult.NoTarget;
                FireFan(data, (Vector2)(nearest.transform.position - transform.position));
            }
            else if (data.Kind == ActiveSkillKind.Shockwave)
            {
                // Snapshot cast values. Remaining pulses follow the player's current position.
                slot.PulseDamage = CombatDamage.WithBonus(data.Damage, startingDamageBonus);
                slot.PulseRadius = data.Radius;
                slot.PulseVisual = data.PulsePrefab;
                slot.PulseInterval = data.ShockwaveInterval;
                slot.PendingPulses = data.ShockwaveCount - 1;
                slot.UntilPulse = data.ShockwaveInterval;
                EmitPulse(slot);
            }
            else return UseResult.NotConfigured;
        }

        slot.NextReadyTime = Time.time + data.Cooldown;
        return UseResult.Used;
    }

    private EnemyHealth FindNearest(EnemyHealth[] enemies, float range)
    {
        EnemyHealth nearest = null;
        float nearestDistance = range;
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = enemy;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    private void FireFan(ActiveSkillData data, Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        direction.Normalize();
        int count = data.ProjectileCount;
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : -data.SpreadDegrees * 0.5f
                + data.SpreadDegrees * i / (count - 1);
            Vector2 rotation = DirectionAtAngle(angle);
            SpawnShot(data, new Vector2(direction.x * rotation.x - direction.y * rotation.y,
                direction.x * rotation.y + direction.y * rotation.x));
        }
    }

    private static Vector2 DirectionAtAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private void SpawnShot(ActiveSkillData data, Vector2 direction)
    {
        GameObject shot = Instantiate(data.ProjectilePrefab, transform.position, Quaternion.identity);
        shot.GetComponent<Projectile>().Launch(direction, startingDamageBonus, data.CreateProjectileEffects());
    }

    private void EmitPulse(SkillSlot slot)
    {
        AreaDamage2D.Deal(transform.position, slot.PulseRadius, slot.PulseDamage);
        SkillPulseVisual pulse = Instantiate(slot.PulseVisual, transform.position, Quaternion.identity);
        pulse.Play(slot.PulseRadius);
    }

    private void TickSequences(float deltaTime)
    {
        if (health == null || !health.IsAlive)
        {
            CancelSequences();
            return;
        }
        if (!IsQUnlocked) qSlot.PendingPulses = 0;
        if (Time.timeScale <= 0f || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        AdvanceSlot(eSlot, deltaTime);
        AdvanceSlot(qSlot, deltaTime);
    }

    private void AdvanceSlot(SkillSlot slot, float deltaTime)
    {
        if (slot.PendingPulses <= 0) return;
        slot.UntilPulse -= deltaTime;
        while (slot.PendingPulses > 0 && slot.UntilPulse <= 0.00001f)
        {
            EmitPulse(slot);
            slot.PendingPulses--;
            slot.UntilPulse += slot.PulseInterval;
        }
    }

    private void CancelSequences() { eSlot.PendingPulses = qSlot.PendingPulses = 0; }
    private void OnDisable() { CancelSequences(); }
}
