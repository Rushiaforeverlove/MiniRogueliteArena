using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public enum RunActionKind { Attack, E, Q, Dodge }
    public sealed class RunAction
    {
        public long id;
        public RunActionKind kind;
        public bool free, shockwave;
        public bool melee, charged;
        public Vector2 origin, direction;
        public float basis, multiplier = 1, rangeMultiplier = 1, rangeBonus, radius;
        // Positive only on directly controlled normal projectile attacks.
        // Captured before BeforeAction consumes one-shot range bonuses.
        public float projectileRange;
        public int pulseCount = 1, pulseIndex;
        public readonly HashSet<string> once = new HashSet<string>();
        public readonly Dictionary<string, HashSet<int>> targets = new Dictionary<string, HashSet<int>>();
        public bool Once(string key) => once.Add(key);
        public bool TargetOnce(string key, EnemyHealth target)
        {
            if (!targets.TryGetValue(key, out var ids)) targets[key] = ids = new HashSet<int>();
            return ids.Add(target.RuntimeId);
        }
    }
    public struct RunStats
    {
        public float attackSpeed, moveSpeed, allDamage, pickupRadius, eCooldownReduction, range, dodgeDistance;
        public int extraProjectiles, eCharges, qCharges, dodgeCharges, extraTargets;
        public int eExtraTargets;
        public float explosionRadiusBonus;
    }
    public sealed class RunHit
    {
        public EnemyHealth target;
        public RunAction action;
        public float damage, actualDamage, distanceFromOrigin, areaRadius;
        public bool generated, shockwave, melee;
        public int targetOrdinal;
        public Vector2 origin;
    }
    public abstract class RunEffect
    {
        public UpgradeCardData Data { get; internal set; }
        public int Rank { get; internal set; }
        protected RunUpgradeRuntime C;
        public void Bind(RunUpgradeRuntime context) { C = context; }
        protected float N(string key, float fallback = 0) => Data.Number(key, Rank, fallback);
        public virtual void Acquired(int previousRank) { }
        public virtual void Stats(ref RunStats stats) { }
        public virtual void Tick(float dt) { }
        public virtual void BeforeAction(RunAction a) { }
        public virtual void AfterAction(RunAction a) { }
        public virtual void BeforeHit(RunHit h) { }
        public virtual void ModifyDamage(RunHit h) { }
        public virtual void AfterAnyHit(RunHit h) { }
        public virtual void AfterHit(RunHit h) { }
        public virtual void Kill(EnemyHealth enemy, Vector2 position, bool marked, float distance) { }
        public virtual void DodgeEnded(Vector2 start, Vector2 end) { }
        public virtual float Reduction() => 0;
        public virtual void Hurt(float actual) { }
        public virtual void IncomingHit() { }
        public virtual float ETargetRangeMultiplier() => 1;
        public virtual void ShieldAbsorbed(float amount) { }
        public virtual void ShieldBroken() { }
        public virtual bool PreventFatal() => false;
        public virtual bool FreeE() => false;
        public virtual void Pulse(RunAction a, Vector2 center, float radius, float basis, bool last) { }
    }
    // One instance per battle player; definitions and permanent saves never store run ranks/timers.
    [DisallowMultipleComponent]
    public sealed class RunUpgradeRuntime : MonoBehaviour
    {
        public UpgradeDatabase Database { get; private set; }
        public event Action<RunHit> HitResolved;
        // Presentation observers receive accepted actions, actual pulses and existing projectiles.
        // They cannot change cadence, charges, hit tests or upgrade execution.
        public event Action<RunAction> ActionCompleted;
        public event Action<RunAction,Vector2,float> PulseEmitted;
        // Presentation can claim a primary area effect; damage and upgrade-generated areas stay independent.
        public event Func<RunAction,bool> AreaVisualOverride;
        public event Action<RunProjectile,RunAction,bool> ProjectileCreated;
        public void NotifyProjectileCreated(RunProjectile projectile,RunAction action,bool generated)
            =>ProjectileCreated?.Invoke(projectile,action,generated);
        public CharacterData Character { get; private set; }
        public PlayerHealth Health { get; private set; }
        public PlayerActiveSkill Skills { get; private set; }
        public PlayerAutoAttack Attack { get; private set; }
        public PlayerDodge Dodge { get; private set; }
        public RunStats Stats { get; private set; }
        public Vector2 Position => transform.position;
        public float AttackBasis => Attack != null ? Attack.BaseDamage : 100;
        public float EBase => Skills != null ? Skills.EBaseDamage : AttackBasis;
        public float ETargetRangeMultiplier => effects.Aggregate(1f, (value, e) => value * e.ETargetRangeMultiplier());
        public bool Ready => Character != null && Database != null;
        public bool Active => Ready && enabled && Health != null && Health.IsAlive && !battleEnded;
        public IReadOnlyDictionary<string, int> Ranks => ranks;
        public float MovingSeconds { get; private set; }
        public Vector2 MoveDirection { get; private set; }
        public float StabilityUntil, SlowUntil, SlowFraction;
        public float NextAttackRange;
        public float OrbitBoostUntil;
        public float TemporaryMoveSpeed => (Time.time < scoutSpeedUntil ? scoutSpeed : 0);
        public float scoutSpeedUntil, scoutSpeed;
        public float supportPickupUntil, supportPickupBonus;
        public float PickupBonus => Stats.pickupRadius + (Time.time < supportPickupUntil ? supportPickupBonus : 0);
        public void ResetMovingTime() { MovingSeconds = 0; }
        public float ExtraProjectileCoefficient = .6f;
        public bool Has(string key) => effects.Exists(e => e.Data.effectKey == key);
        public float Param(string key, string number, float fallback = 0)
        { var e = effects.Find(v => v.Data.effectKey == key); return e != null ? e.Data.Number(number, e.Rank, fallback) : fallback; }
        private readonly Dictionary<string, int> ranks = new Dictionary<string, int>();
        private readonly List<RunEffect> effects = new List<RunEffect>();
        private readonly List<RunZone> zones = new List<RunZone>();
        private readonly List<DelayedEffect> delayed = new List<DelayedEffect>();
        internal readonly List<DamageNumberView> damageNumberPool = new List<DamageNumberView>();
        private long nextAction;
        private Vector2 previousPosition;
        private bool battleEnded;
        private int damageDepth;
        public void Initialize(CharacterData character)
        {
            Character = character;
            Database = Resources.Load<UpgradeDatabase>("RunUpgradeDatabase");
            Health = GetComponent<PlayerHealth>(); Skills = GetComponent<PlayerActiveSkill>();
            Attack = GetComponent<PlayerAutoAttack>(); Dodge = GetComponent<PlayerDodge>();
            previousPosition = Position;
            if (Database == null) Debug.LogError("Run upgrade database missing: run Abyss Bar/Run Upgrades/Install in the editor.", this);
        }
        public bool Apply(UpgradeCardData card)
        {
            if (!Active || card == null || !Database.cards.Contains(card)
                || !UpgradeDraft.Eligible(card, Character.CharacterId, Skills != null && Skills.IsQUnlocked,
                    CharacterProgressionSave.IsNodeFiveComplete(Character), ranks, Database.IsRoleUnlocked)) return false;
            return ApplyValidated(card);
        }
#if UNITY_EDITOR
        public bool ApplyForTesting(UpgradeCardData card)
        {
            if (!Application.isEditor || !Active || card == null || !Database.cards.Contains(card)) return false;
            ranks.TryGetValue(card.id, out int rank);
            return (card.maxRank == 0 || rank < card.maxRank) && ApplyValidated(card);
        }
#endif
        private bool ApplyValidated(UpgradeCardData card)
        {
            ranks.TryGetValue(card.id, out int before);
            if (before == int.MaxValue) return false;
            RunEffect effect = effects.Find(e => e.Data.id == card.id);
            if (effect == null)
            {
                effect = UpgradeEffectRegistry.Create(card.effectKey);
                if (effect == null) return false;
                effect.Data = card; effect.Bind(this); effects.Add(effect);
            }
            ranks[card.id] = effect.Rank = before + 1;
            Recalculate(); effect.Acquired(before); return true;
        }
        public void Recalculate()
        {
            var stats = new RunStats();
            foreach (var e in effects) e.Stats(ref stats);
            Stats = stats;
            Skills?.RefreshCharges(stats); Dodge?.RefreshCharges(stats);
        }
        public RunAction Begin(RunActionKind kind, float basis, Vector2 direction, bool free = false, bool shockwave = false, int pulses = 1, bool melee = false, bool charged = false)
        {
            var a = new RunAction { id = ++nextAction, kind = kind, basis = basis, direction = direction,
                origin = Position, free = free, shockwave = shockwave, pulseCount = pulses, melee = melee, charged = charged };
            foreach (var e in effects) e.BeforeAction(a);
            return a;
        }
        public void EndAction(RunAction a) { foreach (var e in effects) e.AfterAction(a); ActionCompleted?.Invoke(a); }
        public bool TryFreeE() { foreach (var e in effects) if (e.FreeE()) return true; return false; }
        public string DescribeUpgrade(UpgradeCardData card)
            => Attack?.CurrentWeapon?.PrimaryAttackStyle is IUpgradeCardDescription adapter ? adapter.DescribeUpgrade(card) : card?.description ?? string.Empty;
        public void EndDodge(Vector2 start) { foreach (var e in effects) e.DodgeEnded(start, Position); }
        public float DamageReduction => Mathf.Clamp(effects.Sum(e => e.Reduction()), 0, .6f);
        public void NotifyHurt(float amount) { if (Health.IsAlive) foreach (var e in effects) e.Hurt(amount); }
        public void NotifyIncomingHit() { foreach (var e in effects) e.IncomingHit(); }
        public void NotifyAbsorbed(float amount) { foreach (var e in effects) e.ShieldAbsorbed(amount); }
        public void NotifyBroken() { foreach (var e in effects) e.ShieldBroken(); }
        public bool TryPreventFatal() { foreach (var e in effects) if (e.PreventFatal()) return true; return false; }
        public void Pulse(RunAction a, Vector2 position, float radius, float basis, bool last)
        { foreach (var e in effects) e.Pulse(a, position, radius, basis, last); PulseEmitted?.Invoke(a,position,radius); }
        public float Hit(EnemyHealth enemy, float amount, RunAction a = null, bool generated = true, bool shockwave = false, Vector2? origin = null, int targetOrdinal = 1, float areaRadius = 0, bool? melee = null)
        {
            if (!Active || enemy == null || !enemy.IsAlive || amount <= 0 || damageDepth > 16) return 0;
            damageDepth++;
            try
            {
                var h = new RunHit { target = enemy, action = a, damage = amount, generated = generated, shockwave = shockwave, melee = melee ?? (a != null && a.melee), origin = origin ?? Position, targetOrdinal = targetOrdinal, areaRadius = areaRadius };
                h.distanceFromOrigin = Vector2.Distance(h.origin, enemy.transform.position);
                if (!generated && a != null) foreach (var e in effects) e.BeforeHit(h);
                foreach (var e in effects) e.ModifyDamage(h);
                if (!generated && enemy.Status.Marked) h.damage *= 1.1f;
                h.damage *= 1 + Stats.allDamage;
                h.actualDamage = enemy.ReceiveDamage(h.damage);
                bool killed = !enemy.IsAlive && enemy.TryClaimRunKill();
                Vector2 killPosition = enemy.transform.position;
                bool killedMarked = enemy.Status.Marked;
                DamageNumberView.Show(enemy.transform.position, enemy.LastHitDamage, this);
                foreach (var e in effects) e.AfterAnyHit(h);
                if (!generated && a != null) foreach (var e in effects) e.AfterHit(h);
                if (killed)
                {
                    foreach (var e in effects) e.Kill(enemy, killPosition, killedMarked, Vector2.Distance(Position, killPosition));
                }
                HitResolved?.Invoke(h);
                return h.actualDamage;
            }
            finally { damageDepth--; }
        }
        public List<EnemyHealth> Targets(Vector2 center, float range = 8, bool farthest = false, bool markedOnly = false)
        {
            var list = EnemyHealth.Living.Where(e => e != null && e.IsAlive && (!markedOnly || e.Status.Marked)
                && Vector2.Distance(center, e.transform.position) <= range).ToList();
            list.Sort((a, b) => ((Vector2)a.transform.position - center).sqrMagnitude.CompareTo(((Vector2)b.transform.position - center).sqrMagnitude));
            if (farthest) list.Reverse();
            return list;
        }
        public EnemyHealth Nearest(Vector2? center = null, float range = 8)
        {
            Vector2 origin = center ?? Position; float nearest = range * range; EnemyHealth result = null;
            foreach (var enemy in EnemyHealth.Living)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                float distance = ((Vector2)enemy.transform.position - origin).sqrMagnitude;
                if (distance <= nearest) { nearest = distance; result = enemy; }
            }
            return result;
        }
        public void Area(Vector2 center, float radius, float damage, RunAction a = null, bool generated = true, bool shock = false, Action<EnemyHealth> status = null)
        {
            if (generated && shock) radius *= 1 + Param("guard.outer_ring", "radiusBonus");
            foreach (var enemy in Targets(center, radius))
            { status?.Invoke(enemy); if (damage > 0) Hit(enemy, damage, a, generated, shock, center, 1, radius); }
            if(generated||!HasAreaVisualOverride(a))RunVisual.Circle(this, center, radius, .18f, new Color(.8f, .7f, .3f, .35f));
        }
        private bool HasAreaVisualOverride(RunAction action)
        {
            if(action==null||AreaVisualOverride==null)return false;
            foreach(Func<RunAction,bool> provider in AreaVisualOverride.GetInvocationList())if(provider(action))return true;
            return false;
        }
        public void Shot(Vector2 origin, Vector2 direction, float damage, int targets = 1, RunAction action = null,
            bool generated = true, float mark = 0, Action<EnemyHealth> onHit = null, ProjectileEffects? mechanics = null)
        {
            RunProjectile.Spawn(this, origin, direction, damage, targets, action, generated, mark, onHit, mechanics);
        }
        public void NearestShot(float coefficient, int targets = 1, float stun = 0)
        {
            var enemy = Nearest(); if (enemy == null) return;
            Shot(Position, (Vector2)enemy.transform.position - Position, coefficient * AttackBasis, targets, null, true, 0,
                e => { if (stun > 0) e.Status.Stun(stun); });
        }
        public void MarkArea(Vector2 center, float radius, float duration)
        { foreach (var e in Targets(center, radius)) e.Status.Mark(duration); }
        public void Later(float seconds, Action callback)
        { delayed.Add(new DelayedEffect { at = Time.time + Mathf.Max(0, seconds), callback = callback }); }
        public RunZone Zone(string key, Vector2 center, float radius, float duration, float interval,
            Action<Vector2> tick, Action<Vector2> expiry = null, bool follow = false)
        {
            foreach (var old in zones.Where(z => z.key == key).ToArray()) RemoveZone(old);
            var z = new RunZone { key = key, center = center, radius = radius, end = Time.time + duration,
                interval = Mathf.Max(.05f, interval), next = Time.time + Mathf.Max(.05f, interval), tick = tick, expiry = expiry, follow = follow };
            z.visual = RunVisual.Circle(this, center, radius, duration, new Color(.3f, .7f, 1, .16f));
            zones.Add(z); tick?.Invoke(center); return z;
        }
        public RunZone FindZone(string key) => zones.Find(z => z.key == key && z.end > Time.time);
        public void Fireline(Vector2 start, Vector2 end, float duration, float boost, float markRadius)
        {
            var z = Zone("shared.fireline", start, .3f, duration, .1f, p => {
                foreach (var e in Targets((start + end) * .5f, Vector2.Distance(start, end) * .5f + markRadius))
                    if (Vector2.Distance(RunProjectile.Closest(start, end, e.transform.position), e.transform.position) <= markRadius) e.Status.Mark(4);
            });
            z.endPoint = end; z.damageBoost = boost;
            if (z.visual != null) { Destroy(z.visual); z.visual = null; }
            z.visual = RunVisual.Line(this, start, end, .6f, duration, new Color(1, .5f, .2f, .35f));
        }
        public bool FirelineIntersects(Vector2 a, Vector2 b, out float boost)
        {
            boost = 0; var z = FindZone("shared.fireline"); if (z == null) return false;
            for (int i = 0; i <= 8; i++)
                if (Vector2.Distance(RunProjectile.Closest(z.center, z.endPoint, Vector2.Lerp(a, b, i / 8f)), Vector2.Lerp(a, b, i / 8f)) <= .3f)
                { boost = z.damageBoost; return true; }
            return false;
        }
        public bool StandingOnFireline => FirelineIntersects(Position, Position, out _);
        public void RemoveZone(RunZone z)
        { zones.Remove(z); if (z.visual != null) Destroy(z.visual); }
        public void Pickup(Vector2 center, float heal = 0, float shield = 0, Action collected = null)
        { RunPickup.Spawn(this, center, heal, shield, collected); }
        private void Update()
        {
            if (!Active || Time.timeScale <= 0) return;
            Vector2 displacement = Position - previousPosition;
            if (Dodge == null || !Dodge.IsDodging)
            {
                MoveDirection = displacement.sqrMagnitude > .000001f ? displacement.normalized : Vector2.zero;
                MovingSeconds = MoveDirection == Vector2.zero ? 0 : MovingSeconds + Time.deltaTime;
            }
            previousPosition = Position;
            foreach (var e in effects) e.Tick(Time.deltaTime);
            foreach (var job in delayed.ToArray())
                if (Time.time >= job.at) { delayed.Remove(job); if (Active) job.callback(); }
            foreach (var z in zones.ToArray())
            {
                if (!zones.Contains(z)) continue;
                if (z.follow) { z.center = Position; if (z.visual != null) z.visual.GetComponent<RunVisual>()?.SetWorldPosition(Position); }
                int caughtUp = 0;
                while (z.next < z.end - .00001f && Time.time >= z.next && caughtUp++ < 128)
                { z.next += z.interval; z.tick?.Invoke(z.center); }
                if (Time.time >= z.end) { RemoveZone(z); z.expiry?.Invoke(z.center); }
            }
        }
        public void EndBattle()
        {
            if (battleEnded) return;
            battleEnded = true; Skills?.CancelRunSequences(); Dodge?.CancelDodge();
            delayed.Clear(); foreach (var z in zones.ToArray()) RemoveZone(z);
            foreach (var owned in GetComponentsInChildren<RunOwnedObject>(true)) if (owned != null) Destroy(owned.gameObject);
        }
        private void OnDisable() { EndBattle(); }
        private sealed class DelayedEffect { public float at; public Action callback; }
    }
    public sealed class RunZone
    {
        public string key;
        public Vector2 center, endPoint;
        public float damageBoost;
        public float radius, end, next, interval, created;
        public bool follow;
        public GameObject visual;
        public Action<Vector2> tick, expiry;
    }

}


