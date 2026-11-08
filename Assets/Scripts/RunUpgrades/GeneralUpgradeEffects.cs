using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Upgrades
{
    public sealed class GeneralUpgradeEffects : BuiltinUpgradeEffect
    {
        public GeneralUpgradeEffects(string code) : base(code) { }
        private float afterDodgeUntil, damageBank;
        private bool fatalUsed;
        private readonly Dictionary<int, float> slowChecks = new Dictionary<int, float>();
        public override void Acquired(int previousRank)
        {
            if (Code == "GEN-W01") C.Health.AddMaxHealth(Mathf.RoundToInt(N("healthPerRank")), N("healPerRank"));
            if (Code == "GEN-P01") C.ExtraProjectileCoefficient = N("coefficient", .6f);
        }
        public override void Stats(ref RunStats s)
        {
            switch (Code)
            {
                case "GEN-W02": s.attackSpeed += N("fractionPerRank") * Rank; break;
                case "GEN-W03": s.moveSpeed += N("fractionPerRank") * Rank; break;
                case "GEN-W04": s.allDamage += N("fractionPerRank") * Rank; break;
                case "GEN-W05": s.pickupRadius += N("fractions"); break;
                case "GEN-W06": s.eCooldownReduction += N("reductions"); break;
                case "GEN-P01": s.extraProjectiles += (int)N("extraCounts"); break;
                case "GEN-P02": s.eCharges += (int)N("charges"); break;
                case "GEN-P03": s.dodgeCharges += (int)N("charges"); break;
                case "GEN-G02": s.qCharges += (int)N("qCharges"); break;
            }
        }
        public override void AfterHit(RunHit h)
        {
            if (Code == "GEN-W07" && h.action.kind == RunActionKind.Attack)
            {
                int id = h.target.RuntimeId;
                if (slowChecks.TryGetValue(id, out float next) && Time.time < next) return;
                slowChecks[id] = Time.time + N("targetIcd");
                if (Random.value < N("chances")) h.target.Status.Slow(N("slow"), N("duration"));
            }
            if (Code == "GEN-P04")
            {
                if (timers.TryGetValue("heal", out float at) && Time.time < at) return;
                damageBank += h.actualDamage;
                if (damageBank >= N("damageThreshold")) { damageBank = 0; Gate("heal", N("icd")); C.Health.Heal(N("heal")); }
            }
        }
        public override void BeforeAction(RunAction a)
        {
            if (Code == "GEN-P05" && Spending(a, RunActionKind.E) && Count("e", (int)N("eSpends"))) C.Skills.AdvanceQ(N("advanceSeconds"));
            if (Code == "GEN-G02" && Spending(a, RunActionKind.Q) && Gate("restore", N("icd"))) C.Skills.RestoreE((int)N("restoreE"));
        }
        public override void DodgeEnded(Vector2 start, Vector2 end) { if (Code == "GEN-W08") afterDodgeUntil = Time.time + N("duration"); }
        public override float Reduction() => Code == "GEN-W08" && Time.time < afterDodgeUntil ? N("reductions") : 0;
        public override bool PreventFatal()
        {
            if (Code != "GEN-G01" || fatalUsed) return false;
            fatalUsed = true;
            C.Health.SurviveFatal(N("survivalHealthFloor"), N("healthFraction"), N("invulnerability"));
            C.Skills.AdvanceE(N("advanceSeconds")); C.Skills.AdvanceQ(N("advanceSeconds")); C.Dodge?.Advance(N("advanceSeconds"));
            return true;
        }
        public override void Tick(float dt)
        {
            if (slowChecks.Count > 0 && Gate("cleanup", 5))
            {
                var dead = new List<int>(); foreach (var pair in slowChecks) if (pair.Value < Time.time) dead.Add(pair.Key);
                foreach (int id in dead) slowChecks.Remove(id);
            }
        }
    }
}

