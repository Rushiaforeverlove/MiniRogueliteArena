using System;
using System.Collections.Generic;
using AbyssBar.Upgrades;
using UnityEngine;
using AbyssBar.Combat;

namespace AbyssBar.Mio
{
    [DisallowMultipleComponent]
    public sealed class MioCombat : MonoBehaviour, IPlayerSkillAction
    {
        private MioCharacterDefinition definition;
        private MioAnimationPresenter visual;
        private PlayerAutoAttack attack;
        private PlayerController movement;
        private PlayerActiveSkill skills;
        private PlayerHealth health;
        private PlayerDodge dodge;
        private RunUpgradeRuntime run;
        private readonly MioChargeGesture charge = new MioChargeGesture();
        public readonly MioActionClock Ultimate = new MioActionClock();
        private bool dash, spinning, cancelled;
        private bool primaryPending, pendingCharged;
        private float pendingAge;
        private float dashRemaining, spinRemaining, dashSpeed, lastAttackAt = -100;
        private Vector2 dashDirection, dashStart;
        private RunAction eAction;
        private int actionGeneration, taps;
        private float nextTapUntil, nextChargeUntil, preservedChargeUntil;
        private bool tapBuff, chargeBuff, mirrorReady;
        private readonly List<Rift> rifts = new List<Rift>();
        private static readonly IPrimaryEmissionPattern fanPattern=new FanEmissionPattern();
        private static readonly IPrimaryEmissionPattern releasedChargePattern=new RadialEmissionPattern(6);
        private enum WaveKind { Charged, ReleasedTap, Combo }
        private sealed class Rift { public Vector2 start, end; public float until; }
        public MioCharacterDefinition Definition => definition;
        public bool Released => Ultimate.IsReleased;
        public bool LastEndedReleased { get; private set; }
        public bool IsDashing => dash;
        public bool Busy => dash || spinning || dodge != null && dodge.IsDodging || Ultimate.Phase == MioActionClock.QPhase.Enter;
        public bool IsEInvulnerable => dash || spinning;
        public float ChargeProgress => charge.Active ? Mathf.Clamp01(charge.Seconds / ChargeSeconds) : 0;
        public bool IsCharging => charge.Active;
        public bool FullCharge => charge.Full(ChargeSeconds);
        public float ChargeSeconds => Mathf.Max(.12f, (definition.tuning.chargeSeconds
            - CharacterProgressionSave.GetBonuses(run.Character).ChargeTimeReduction)
            * (1 - P("mio.grip_wrap", "fraction"))
            * (chargeBuff && Time.time < nextChargeUntil ? 1-P("mio.before_sheathing","fraction",.2f) : 1));
        public float EBaseDamage => definition != null ? definition.tuning.eDamage + (attack != null ? attack.BaseDamage - definition.tuning.tapDamage : 0) : 180;
        private float P(string key, string number, float fallback = 0) => run != null ? run.Param(key, number, fallback) : fallback;
        public void Configure(MioCharacterDefinition data, MioAnimationPresenter presenter)
        {
            definition = data; visual = presenter; attack = GetComponent<PlayerAutoAttack>(); movement = GetComponent<PlayerController>();
            skills = GetComponent<PlayerActiveSkill>(); health = GetComponent<PlayerHealth>(); dodge = GetComponent<PlayerDodge>(); run = GetComponent<RunUpgradeRuntime>();
            cancelled = false; LastEndedReleased = false;
            Ultimate.Activated -= BecameReleased; Ultimate.Finished -= EndedReleased;
            Ultimate.Activated += BecameReleased; Ultimate.Finished += EndedReleased;
            if (run != null) { run.HitResolved -= HitResolved; run.HitResolved += HitResolved; }
        }
        private void Update()=>Tick(Time.deltaTime);
        public void Tick(float seconds)
        {
            if (definition == null || run == null) return;
            if (!run.Active) { if (!cancelled) CancelSkillActions(); return; }
            if (Time.timeScale <= 0) return;
            if(seconds<=0||float.IsNaN(seconds)||float.IsInfinity(seconds))return;
            Ultimate.Tick(seconds);
            charge.Tick(seconds);
            if (preservedChargeUntil>0 && Time.time>preservedChargeUntil) CancelPrimary();
            rifts.RemoveAll(r => r.until <= Time.time);
            if (spinning)
            {
                spinRemaining -= seconds;
                if (spinRemaining <= 0) { spinning = false; health?.SetSkillInvulnerability(false); visual?.ReleaseMotionPose(); }
            }
        }
        public void SamplePrimary(AbyssBar.Combat.PrimaryAttackInput input)
        {
            if (definition == null || run == null || !run.Active) return;
            if (input.cancelled) { CancelPrimary(); return; }
            if (input.pressed) { charge.Press(); primaryPending = false; pendingAge = 0; }
            if (charge.Active && !Busy) visual?.ShowCharging(Released, charge.Seconds, ChargeSeconds);
            if (input.released && charge.Active)
            {
                pendingCharged = charge.Full(ChargeSeconds); primaryPending = true; pendingAge = 0;
                charge.Consume(); visual?.CancelCharge();
            }
            if (primaryPending && !Busy)
            {
                if (FirePrimary(pendingCharged)) primaryPending = false;
                else { pendingAge += Mathf.Max(0,input.deltaTime); if(pendingAge>definition.tuning.primaryBufferSeconds) CancelPrimary(); }
            }
        }
        public void CancelPrimary() { charge.Cancel(); primaryPending = false; pendingAge=0; preservedChargeUntil=0; visual?.CancelCharge(); }
        public void InterruptForDodge()
        {
            actionGeneration++; CancelDash(); spinning = false; health?.SetSkillInvulnerability(false);
            visual?.Play("dodge", Released, .22f, .22f, 40);
        }
        public void NotifyHurt() { if(!IsCharging&&!Busy)visual?.Play("hurt", Released, .18f, .18f, 20); }
        private bool FirePrimary(bool charged)
        {
            if (Busy || run == null || !run.Active) return false;
            float waveRange = (definition.tuning.waveRange + run.Stats.range + run.NextAttackRange)
                * (1 + (charged ? P("mio.straight_answer", "range") : 0));
            if (!attack.TryBeginBasicAttack(out var action,melee:!charged,charged:charged)) return false;
            visual?.ReleaseMotionPose(true);
            try
            {
                action.projectileRange = waveRange * action.rangeMultiplier + action.rangeBonus;
                bool mirror = mirrorReady && Released; mirrorReady = false;
                float mirrorCoefficient=P("mio.open_the_way","coefficient",.6f);
                int stamp = actionGeneration;
                if (charged)
                {
                    float amount = (action.basis+definition.tuning.chargedDamage-definition.tuning.tapDamage) * action.multiplier;
                    if (run.Has("mio.patience") && Time.time - lastAttackAt >= P("mio.patience","window",2)) amount *= 1 + P("mio.patience", "fraction", .4f);
                    bool rage = Released;
                    bool moving=movement!=null&&movement.MovementInput.sqrMagnitude>.001f;
                    visual?.Play(moving?"move_release":"release", rage, definition.tuning.releaseSeconds, definition.tuning.releaseSeconds, 25);
                    float damage = amount;var volley=(rage?releasedChargePattern:fanPattern).Build(run.Stats.extraProjectiles,run.ExtraProjectileCoefficient);
                    bool returning=run.Has("mio.moon_return"),rift=run.Has("mio.rift_path");
                    run.Later(definition.tuning.impactDelay, () => {
                        if (!Valid(stamp)) return;
                        foreach(var emission in volley)
                        {
                            Vector2 direction = Quaternion.Euler(0, 0, emission.Angle) * action.direction;
                            float emittedDamage=damage*emission.DamageCoefficient*(rage?definition.tuning.rageRadialCoefficient:1);
                            LaunchWave(run.Position,direction,emittedDamage,action,WaveKind.Charged,generated:false,released:rage,returning:returning,rift:rift);
                            if(mirror)LaunchWave(run.Position,direction,emittedDamage*mirrorCoefficient,action,WaveKind.Charged,generated:true,released:rage);
                        }
                    });
                    chargeBuff = false;
                }
                else
                {
                    taps++;
                    int tapIndex=taps;
                    bool fullCircle = run.Has("mio.unsheathed") && taps % Mathf.Max(1,Mathf.RoundToInt(P("mio.unsheathed","count",3))) == 0;
                    float radius = (definition.tuning.slashRadius + run.Stats.range + action.rangeBonus)
                        * action.rangeMultiplier * (1 + P("mio.grindstone", "range"));
                    float cone = fullCircle ? 360 : Mathf.Min(180, definition.tuning.slashDegrees + (tapBuff && Time.time < nextTapUntil ? P("mio.turning_cut","angle",30) : 0));
                    float amount = action.basis * action.multiplier * (1 + P("mio.grindstone", "fraction")) * (fullCircle ? P("mio.unsheathed","coefficient",1.6f) : 1);
                    if (tapBuff && Time.time < nextTapUntil) amount *= 1+P("mio.turning_cut","fraction",.25f);
                    tapBuff = false;
                    bool pull = run.Has("mio.circular_space") && Time.time < pullUntil;
                    pullUntil = 0;
                    bool rage = Released;
                    float tapDuration=Mathf.Min(definition.tuning.tapSeconds,attack.AttackInterval);
                    var volley=fanPattern.Build(run.Stats.extraProjectiles,run.ExtraProjectileCoefficient);
                    bool thirdBeat=run.Has("mio.three_beats")&&tapIndex%Mathf.Max(1,Mathf.RoundToInt(P("mio.three_beats","count",3)))==0;
                    visual?.Play("tap", rage,tapDuration,tapDuration,25);
                    run.Later(definition.tuning.impactDelay*tapDuration/definition.tuning.tapSeconds, () => {
                        if (!Valid(stamp)) return;
                        var hitTargets=new HashSet<int>();var pulledTargets=new HashSet<int>();
                        foreach(var emission in volley)
                        {
                            Vector2 direction=Quaternion.Euler(0,0,emission.Angle)*action.direction;
                            Slash(run.Position,direction,radius,cone,amount*emission.DamageCoefficient,action,false,pull,hitTargets,pulledTargets);
                            if(rage)LaunchWave(run.Position,direction,action.basis*definition.tuning.rageTapWaveCoefficient*action.multiplier*emission.DamageCoefficient,action,WaveKind.ReleasedTap,generated:false,released:rage);
                            if(mirror){Slash(run.Position,direction,radius,cone,amount*emission.DamageCoefficient*mirrorCoefficient,action,true,false);if(rage)LaunchWave(run.Position,direction,action.basis*definition.tuning.rageTapWaveCoefficient*action.multiplier*emission.DamageCoefficient*mirrorCoefficient,action,WaveKind.ReleasedTap,generated:true,released:rage);}
                        }
                        int hits=hitTargets.Count;
                        MioFx.Play(definition, (rage ? "rage." : "normal.")+(fullCircle?"spin_fx":"slash"), run.Position, action.direction, radius, .28f, run.transform);
                        if (hits >= P("mio.warm_blade","count",3) && run.Has("mio.warm_blade") && Time.time >= shieldReadyAt)
                        { shieldReadyAt = Time.time + P("mio.warm_blade","cooldown",5); health.AddShield(health.MaxHealth * P("mio.warm_blade","fraction",.03f), P("mio.warm_blade","duration",3)); }
                        if(thirdBeat)LaunchWave(run.Position,action.direction,action.basis*P("mio.three_beats","coefficient",.8f),action,WaveKind.Combo,generated:true,released:rage,range:P("mio.three_beats","range",3));
                    });
                }
                lastAttackAt = Time.time; charge.Consume(); preservedChargeUntil=0;
            }
            finally { attack.CompleteBasicAttack(action); }
            return true;
        }
        private float shieldReadyAt, pullUntil;
        private bool Valid(int generation) => generation == actionGeneration && isActiveAndEnabled && run != null && run.Active;
        public int Slash(Vector2 center, Vector2 direction, float radius, float degrees, float damage, RunAction action, bool generated, bool pull, ISet<int> hitTargets = null, ISet<int> pulledTargets = null)
        {
            int count = 0;
            foreach (var enemy in run.Targets(center, radius + 1))
            {
                if (enemy == null || !enemy.IsAlive) continue;
                Vector2 at = Closest(enemy, center), offset = at - center;
                if (offset.magnitude > radius || degrees < 359 && offset.sqrMagnitude > .0001f && Vector2.Angle(direction, offset) > degrees * .5f
                    || RunProjectile.Blocked(center, at)) continue;
                if (pull&&(pulledTargets==null||pulledTargets.Add(enemy.RuntimeId))) enemy.Status.Push(center + direction * radius * .65f, -P("mio.circular_space","pull",.45f));
                if (run.Hit(enemy, damage, action, generated, action != null && action.kind == RunActionKind.E, center, count + 1, radius, melee:true) > 0){count++;hitTargets?.Add(enemy.RuntimeId);}
            }
            return count;
        }
        private static Vector2 Closest(EnemyHealth enemy, Vector2 center)
        {
            Vector2 result = enemy.transform.position; float distance = (result - center).sqrMagnitude;
            foreach (var collider in enemy.GetComponentsInChildren<Collider2D>())
                if (collider.enabled) { var point = collider.ClosestPoint(center); float d = (point - center).sqrMagnitude; if (d < distance) { result = point; distance = d; } }
            return result;
        }
        private void LaunchWave(Vector2 origin, Vector2 direction, float damage, RunAction action, WaveKind kind, bool generated, bool released, bool returning=false, bool rift=false, float range=0)
        {
            int targets = definition.tuning.waveTargets + run.Stats.extraTargets + (kind==WaveKind.Charged?Mathf.RoundToInt(P("mio.straight_answer", "targets")):0);
            MioWave.Spawn(definition, run, origin, direction, damage, targets, range > 0 ? range : action.projectileRange,
                action, generated, released, returning, rift ? (Action<Vector2, Vector2>)((a,b) => MakeRift(a,b,damage,action)) : null);
        }
        public bool Accepts(ActiveSkillData data) => definition != null && data != null && (data.Kind == ActiveSkillKind.MioDash || data.Kind == ActiveSkillKind.MioRelease);
        public float Basis(ActiveSkillData data) => data.Kind == ActiveSkillKind.MioRelease ? 0 : EBaseDamage;
        public PlayerActiveSkill.UseResult CanExecute(ActiveSkillData data)
        {
            if (dash || spinning || dodge != null && dodge.IsDodging || Ultimate.Phase == MioActionClock.QPhase.Enter) return PlayerActiveSkill.UseResult.SequenceActive;
            if (data.Kind == ActiveSkillKind.MioRelease && Ultimate.Phase != MioActionClock.QPhase.Off) return PlayerActiveSkill.UseResult.SequenceActive;
            if (data.Kind == ActiveSkillKind.MioDash && movement.GetAllowedDodgeDistance(ChosenDirection(), .1f) <= .0001f) return PlayerActiveSkill.UseResult.Blocked;
            return PlayerActiveSkill.UseResult.Used;
        }
        public void Execute(ActiveSkillData data, RunAction action)
        {
            if (data.Kind == ActiveSkillKind.MioRelease)
            {
                actionGeneration++; attack.SuspendInput(); mirrorReady = false;
                Ultimate.Begin(definition.tuning.qSeconds, definition.tuning.qEnterSeconds, definition.tuning.qExitSeconds);
                visual?.PlayKey("q.enter", definition.tuning.qEnterSeconds, 35);
                // The pulse has NO Hit/Area/ReceiveDamage path. Upgrade Q procs are separate effects.
                PushNearby(run.Position, definition.tuning.qPushRadius, definition.tuning.qPushDistance);
                MioFx.Play(definition, "fx.pulse", run.Position, Vector2.right, definition.tuning.qPushRadius, .4f, run.transform);
                if (run.Has("mio.open_the_way")) skills.RestoreE(skills.EChargeCapacity);
                return;
            }
            bool preserve = run.Has("mio.cross_step") && charge.Full(ChargeSeconds);
            if (!preserve) attack.SuspendInput();
            else preservedChargeUntil=Time.time+P("mio.cross_step","window",2);
            actionGeneration++; eAction = action; dashStart = run.Position; dashDirection = ChosenDirection();
            float bonus = CharacterProgressionSave.GetBonuses(run.Character).EDistanceFraction;
            float distance = definition.tuning.eDistance * (1 + bonus) * (Released ? definition.tuning.rageDashMultiplier : 1) * action.rangeMultiplier;
            dashRemaining = definition.tuning.eDuration; dashSpeed = distance / Mathf.Max(.05f, dashRemaining); dash = true;
            health?.SetSkillInvulnerability(true);
            movement.BeginDodgeCollision(dodge != null ? dodge.PassThroughLayers : 0);
            visual?.Play("dash", Released, dashRemaining, dashRemaining, 30);
            if (Released && run.Has("mio.open_the_way")) mirrorReady = true;
        }
        private Vector2 ChosenDirection()
        {
            // Sample with the E press so changing direction and pressing E in the
            // same frame cannot depend on MonoBehaviour Update order.
            return attack!=null?attack.SampleAimDirection():Vector2.right;
        }
        // PlayerController remains the only owner of Rigidbody movement and wall casts.
        public bool TryMovementStep(float dt, out Vector2 step)
        {
            step = Vector2.zero; if (!dash) return false;
            if (run == null || !run.Active) { CancelDash(); return false; }
            if (dashRemaining <= .00001f) { FinishDash(); return true; }
            float used = Mathf.Min(dt, dashRemaining); dashRemaining -= used; step = dashDirection * dashSpeed * used; return true;
        }
        public void StopAtWall() { dashRemaining = 0; }
        private void FinishDash()
        {
            CancelDash(); spinning = true; spinRemaining = definition.tuning.spinDuration;
            bool rage = Released; var action = eAction;
            float radius = (definition.tuning.eRadius + run.Stats.range + action.rangeBonus) * action.rangeMultiplier * (rage ? definition.tuning.rageSpinMultiplier : 1);
            visual?.Play("spin", rage, spinRemaining, spinRemaining, 30);
            float damage = action.basis * action.multiplier;
            int stamp = actionGeneration;
            run.Later(.08f, () => {
                if (!Valid(stamp)) return;
                Slash(run.Position, dashDirection, radius, 360, damage, action, false, false);
                float push = definition.tuning.ePush + (rage ? definition.tuning.rageSpinPush : 0) + (run.Has("mio.circular_space") ? P("mio.circular_space","push",.6f) : 0);
                if (push > 0) PushNearby(run.Position, radius, push);
                MioFx.Play(definition, rage ? "rage.spin_fx" : "normal.spin_fx", run.Position, dashDirection, radius, .35f, run.transform);
            });
            if (CharacterProgressionSave.IsEEnhanced(run.Character))
            {
                Vector2 from = dashStart, to = run.Position;
                run.Later(definition.tuning.enhancedEDelay, () => {
                    SegmentHit(from, to, .45f, damage * definition.tuning.enhancedECoefficient, action);
                    MioFx.Line(definition, rage, from, to, .35f, run.transform);
                });
            }
        }
        private void CancelDash() { if (!dash) return; dash = false; movement?.EndDodgeCollision(); dashRemaining = 0; }
        public void PushNearby(Vector2 center, float radius, float distance)
        { foreach (var enemy in run.Targets(center, radius)) if (!RunProjectile.Blocked(center, enemy.transform.position)) enemy.Status.Push(center, distance); }
        private void BecameReleased() { visual?.CancelCharge(); }
        private void EndedReleased()
        {
            int blades = Ultimate.Blades;
            if (CharacterProgressionSave.IsQEnhanced(run.Character) && blades > 0 && run.Active)
            {
                Vector2 direction = attack.AimDirection;
                for (int i = 0; i < blades; i++)
                {
                    float delay = i * .075f;
                    run.Later(delay, () => MioWave.Spawn(definition, run, run.Position, direction,
                        (definition.tuning.chargedDamage + attack.BaseDamage - definition.tuning.tapDamage) * definition.tuning.enhancedQCoefficient,
                        1, definition.tuning.waveRange, null, true, true));
                }
            }
            mirrorReady = false; visual?.PlayKey("q.exit", definition.tuning.qExitSeconds, 10);
            MioFx.Play(definition, "fx.fade", run.Position, Vector2.right, 1.4f, .4f, run.transform);
        }
        private void HitResolved(RunHit hit)
        {
            if(hit.actualDamage>0&&hit.target!=null&&Time.time>=nextHitFx){nextHitFx=Time.time+.03f;MioFx.Play(definition,Released?"rage.hit":"normal.hit",hit.target.transform.position,Vector2.right,.35f,.25f,transform);}
            var a = hit.action;
            if (a == null || hit.generated || hit.actualDamage <= 0 || a.kind != RunActionKind.Attack && a.kind != RunActionKind.E) return;
            if (a.Once("mio.record")) Ultimate.RecordAttack();
            if (!hit.target.IsAlive && run.Has("mio.unending_chime")) Ultimate.Extend(P("mio.unending_chime","seconds",.15f), P("mio.unending_chime","cap",3));
            if (a.kind == RunActionKind.E)
            {
                if (run.Has("mio.turning_cut")) { tapBuff = true; nextTapUntil = Time.time + P("mio.turning_cut","duration",3); }
                if (run.Has("mio.before_sheathing")) { chargeBuff = true; nextChargeUntil = Time.time + P("mio.before_sheathing","window",3); }
                if (run.Has("mio.circular_space")) pullUntil = Time.time + P("mio.circular_space","window",2);
                if (a.Once("mio.advance")) skills.AdvanceE(P("mio.borrowed_footing", "seconds"));
                if (run.Has("mio.crossed_moons") && InRift(hit.target.transform.position) && a.Once("mio.crossed"))
                    Slash(run.Position, Vector2.right, definition.tuning.slashRadius, 360, a.basis * a.multiplier * P("mio.crossed_moons","coefficient",1), a, true, false);
            }
        }
        private float nextHitFx;
        private bool InRift(Vector2 point)
        { foreach (var r in rifts) if (r.until > Time.time && Vector2.Distance(RunProjectile.Closest(r.start, r.end, point), point) <= .35f) return true; return false; }
        private void MakeRift(Vector2 from, Vector2 to, float damage, RunAction action)
        {
            if (!run.Active) return; if (rifts.Count >= 24) rifts.RemoveAt(0);
            float duration=P("mio.rift_path","duration",1.5f),width=P("mio.rift_path","width",.3f),coefficient=P("mio.rift_path","coefficient",.25f);
            int count=Mathf.Clamp(Mathf.RoundToInt(P("mio.rift_path","count",3)),1,16);
            rifts.Add(new Rift { start = from, end = to, until = Time.time + duration });
            for (int i = 0; i < count; i++) run.Later(i * duration/count, () => SegmentHit(from, to, width, damage * coefficient, action));
            MioFx.Line(definition, Released, from, to, duration, run.transform);
        }
        private void SegmentHit(Vector2 from, Vector2 to, float width, float damage, RunAction action)
        {
            if (run == null || !run.Active) return;
            foreach (var enemy in run.Targets((from+to)*.5f, Vector2.Distance(from,to)*.5f+width+1))
            {
                Vector2 point = Closest(enemy, RunProjectile.Closest(from,to,enemy.transform.position));
                if (Vector2.Distance(RunProjectile.Closest(from,to,point),point) <= width && !RunProjectile.Blocked(from,point))
                    run.Hit(enemy,damage,action,true,false,from);
            }
        }
        public void CancelSkillActions()
        {
            if (!cancelled) LastEndedReleased = Released;
            cancelled = true; actionGeneration++; CancelPrimary(); CancelDash(); spinning = false;
            health?.SetSkillInvulnerability(false);
            Ultimate.Cancel(); mirrorReady = false; rifts.Clear();
        }
        private void OnDisable() { CancelSkillActions(); if (run != null) run.HitResolved -= HitResolved; }
    }
}
