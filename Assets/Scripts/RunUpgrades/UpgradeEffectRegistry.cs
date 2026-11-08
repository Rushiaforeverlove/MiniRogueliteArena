using System;
using System.Collections.Generic;
namespace AbyssBar.Upgrades {
public static class UpgradeEffectRegistry {
private static readonly Dictionary<string, Func<RunEffect>> factories = new Dictionary<string, Func<RunEffect>>(StringComparer.Ordinal) {
    { "stat.max_health", () => new GeneralUpgradeEffects("GEN-W01") },
    { "stat.attack_speed", () => new GeneralUpgradeEffects("GEN-W02") },
    { "stat.move_speed", () => new GeneralUpgradeEffects("GEN-W03") },
    { "stat.all_damage", () => new GeneralUpgradeEffects("GEN-W04") },
    { "stat.pickup_radius", () => new GeneralUpgradeEffects("GEN-W05") },
    { "stat.e_recharge", () => new GeneralUpgradeEffects("GEN-W06") },
    { "proc.slow_on_attack", () => new GeneralUpgradeEffects("GEN-W07") },
    { "buff.after_dodge", () => new GeneralUpgradeEffects("GEN-W08") },
    { "attack.extra_projectiles", () => new GeneralUpgradeEffects("GEN-P01") },
    { "charge.add_e", () => new GeneralUpgradeEffects("GEN-P02") },
    { "charge.add_dodge", () => new GeneralUpgradeEffects("GEN-P03") },
    { "heal.damage_counter", () => new GeneralUpgradeEffects("GEN-P04") },
    { "charge.e_to_q", () => new GeneralUpgradeEffects("GEN-P05") },
    { "survival.last_drink", () => new GeneralUpgradeEffects("GEN-G01") },
    { "charge.q_encore", () => new GeneralUpgradeEffects("GEN-G02") },
    { "item.molotov", () => new ItemUpgradeEffects("ITM-W01") },
    { "item.ice_bucket", () => new ItemUpgradeEffects("ITM-W02") },
    { "item.glass_shards", () => new ItemUpgradeEffects("ITM-W03") },
    { "item.snack", () => new ItemUpgradeEffects("ITM-W04") },
    { "item.flash_coaster", () => new ItemUpgradeEffects("ITM-W05") },
    { "item.escort_drone", () => new ItemUpgradeEffects("ITM-P01") },
    { "item.magnetic_device", () => new ItemUpgradeEffects("ITM-P02") },
    { "item.bouncing_barrel", () => new ItemUpgradeEffects("ITM-P03") },
    { "item.all_night_turret", () => new ItemUpgradeEffects("ITM-G01") },
    { "item.emergency_bell", () => new ItemUpgradeEffects("ITM-G02") },
    { "scout.scouting_attack", () => new ScoutUpgradeEffects("SCT-W01") },
    { "scout.projectile_extension", () => new ScoutUpgradeEffects("SCT-W02") },
    { "scout.long_range_damage", () => new ScoutUpgradeEffects("SCT-W03") },
    { "scout.dodge_reload", () => new ScoutUpgradeEffects("SCT-W04") },
    { "scout.close_pass", () => new ScoutUpgradeEffects("SCT-W05") },
    { "scout.range_lens", () => new ScoutUpgradeEffects("SCT-W06") },
    { "scout.turning_shot", () => new ScoutUpgradeEffects("SCT-W07") },
    { "scout.focus_counter", () => new ScoutUpgradeEffects("SCT-W08") },
    { "scout.crossfire", () => new ScoutUpgradeEffects("SCT-P01") },
    { "scout.scan_shutter", () => new ScoutUpgradeEffects("SCT-P02") },
    { "scout.pierce_recharge", () => new ScoutUpgradeEffects("SCT-P03") },
    { "scout.firing_lane", () => new ScoutUpgradeEffects("SCT-P04") },
    { "scout.distance_pass", () => new ScoutUpgradeEffects("SCT-P05") },
    { "scout.satellite_network", () => new ScoutUpgradeEffects("SCT-G01") },
    { "scout.route_zero", () => new ScoutUpgradeEffects("SCT-G02") },
    { "scout.field_notes", () => new ScoutUpgradeEffects("SCT-N5-W01") },
    { "scout.delayed_exposure", () => new ScoutUpgradeEffects("SCT-N5-P01") },
    { "scout.dawn_observation", () => new ScoutUpgradeEffects("SCT-N5-G01") },
    { "guard.e_barrier", () => new GuardUpgradeEffects("GRD-W01") },
    { "guard.close_resistance", () => new GuardUpgradeEffects("GRD-W02") },
    { "guard.heavy_beat", () => new GuardUpgradeEffects("GRD-W03") },
    { "guard.shock_stagger", () => new GuardUpgradeEffects("GRD-W04") },
    { "guard.steady_boots", () => new GuardUpgradeEffects("GRD-W05") },
    { "guard.close_shock_damage", () => new GuardUpgradeEffects("GRD-W06") },
    { "guard.polished_guard", () => new GuardUpgradeEffects("GRD-W07") },
    { "guard.keep_out", () => new GuardUpgradeEffects("GRD-W08") },
    { "guard.barrier_counter", () => new GuardUpgradeEffects("GRD-P01") },
    { "guard.echo_floor", () => new GuardUpgradeEffects("GRD-P02") },
    { "guard.outer_ring", () => new GuardUpgradeEffects("GRD-P03") },
    { "guard.dodge_line", () => new GuardUpgradeEffects("GRD-P04") },
    { "guard.shield_recharge", () => new GuardUpgradeEffects("GRD-P05") },
    { "guard.refuse_exit", () => new GuardUpgradeEffects("GRD-G01") },
    { "guard.final_downbeat", () => new GuardUpgradeEffects("GRD-G02") },
    { "guard.night_inspection", () => new GuardUpgradeEffects("GRD-N5-W01") },
    { "guard.shelter_code", () => new GuardUpgradeEffects("GRD-N5-P01") },
    { "guard.open_fortress", () => new GuardUpgradeEffects("GRD-N5-G01") },
    { "support.scout_note", () => new SupportUpgradeEffects("SUP-SCT-W01") },
    { "support.scout_sight", () => new SupportUpgradeEffects("SUP-SCT-W02") },
    { "support.scout_light_kit", () => new SupportUpgradeEffects("SUP-SCT-W03") },
    { "support.scout_chalk", () => new SupportUpgradeEffects("SUP-SCT-W04") },
    { "support.scout_callout", () => new SupportUpgradeEffects("SUP-SCT-P01") },
    { "support.scout_return_fire", () => new SupportUpgradeEffects("SUP-SCT-P02") },
    { "support.scout_pathfinder", () => new SupportUpgradeEffects("SUP-SCT-G01") },
    { "support.guard_sleeve", () => new SupportUpgradeEffects("SUP-GRD-W01") },
    { "support.guard_lamp", () => new SupportUpgradeEffects("SUP-GRD-W02") },
    { "support.guard_boots", () => new SupportUpgradeEffects("SUP-GRD-W03") },
    { "support.guard_tap", () => new SupportUpgradeEffects("SUP-GRD-W04") },
    { "support.guard_escort", () => new SupportUpgradeEffects("SUP-GRD-P01") },
    { "support.guard_repair", () => new SupportUpgradeEffects("SUP-GRD-P02") },
    { "support.guard_nightwatch", () => new SupportUpgradeEffects("SUP-GRD-G01") },
    { "item.orbiting_blades", () => new ItemUpgradeEffects("ITM-W06") },
    { "item.orbiting_bottles", () => new ItemUpgradeEffects("ITM-P04") },
};
private static readonly Dictionary<string, string[]> required = new Dictionary<string, string[]>(StringComparer.Ordinal) {
{ "stat.max_health", new string[] {"healPerRank","healthPerRank"} },
{ "stat.attack_speed", new string[] {"fractionPerRank"} },
{ "stat.move_speed", new string[] {"fractionPerRank"} },
{ "stat.all_damage", new string[] {"fractionPerRank"} },
{ "stat.pickup_radius", new string[] {"fractions"} },
{ "stat.e_recharge", new string[] {"reductions"} },
{ "proc.slow_on_attack", new string[] {"duration","slow","chances","targetIcd"} },
{ "buff.after_dodge", new string[] {"duration","reductions"} },
{ "attack.extra_projectiles", new string[] {"coefficient","extraCounts"} },
{ "charge.add_e", new string[] {"charges"} },
{ "charge.add_dodge", new string[] {"charges"} },
{ "heal.damage_counter", new string[] {"heal","icd","damageThreshold"} },
{ "charge.e_to_q", new string[] {"advanceSeconds","eSpends"} },
{ "survival.last_drink", new string[] {"uses","invulnerability","healthFraction","advanceSeconds","survivalHealthFloor"} },
{ "charge.q_encore", new string[] {"restoreE","qCharges","icd"} },
{ "item.molotov", new string[] {"duration","attackThresholds","tick","coefficient","radius"} },
{ "item.ice_bucket", new string[] {"durations","radius","slow"} },
{ "item.glass_shards", new string[] {"coefficient","icd","counts"} },
{ "item.snack", new string[] {"killThresholds","heal","lifetime"} },
{ "item.flash_coaster", new string[] {"coefficient","icd","stuns"} },
{ "item.escort_drone", new string[] {"duration","shots","interval","shield","coefficient"} },
{ "item.magnetic_device", new string[] {"tick","pull","duration","coefficient","activeLimit","radius"} },
{ "item.bouncing_barrel", new string[] {"coefficient","slow","targets","dodgeThreshold","duration"} },
{ "item.all_night_turret", new string[] {"duration","extraRoundIcd","activeLimit","interval","coefficient"} },
{ "item.emergency_bell", new string[] {"healthThreshold","stun","knockback","healPerPack","packs","cooldown","radius"} },
{ "scout.scouting_attack", new string[] {"vulnerability","durations"} },
{ "scout.projectile_extension", new string[] {"explosionRadiusBonus","extraTargets"} },
{ "scout.long_range_damage", new string[] {"bonuses","minimumDistance"} },
{ "scout.dodge_reload", new string[] {"bonuses","window","storedAttacks"} },
{ "scout.close_pass", new string[] {"duration","markDuration","speedBonuses","pathRadius"} },
{ "scout.range_lens", new string[] {"rangePerRank"} },
{ "scout.turning_shot", new string[] {"coefficients","cooldown","turnDegrees","moveTime"} },
{ "scout.focus_counter", new string[] {"coefficients","attackHits","resetAfter"} },
{ "scout.crossfire", new string[] {"coefficient","perTargetPerCast","sideShots"} },
{ "scout.scan_shutter", new string[] {"markDuration","radius","damageBonus"} },
{ "scout.pierce_recharge", new string[] {"perActionLimit","icd","extraAttackTargets","advanceSeconds"} },
{ "scout.firing_lane", new string[] {"duration","markDuration","width","damageBonus"} },
{ "scout.distance_pass", new string[] {"minimumDistance","killThreshold","capacity","initialTokens"} },
{ "scout.satellite_network", new string[] {"shotInterval","initialMarks","killExtension","maxDuration","coefficient","duration","satellites"} },
{ "scout.route_zero", new string[] {"markDuration","icd","targets","duration","pathRadius","coefficient"} },
{ "scout.field_notes", new string[] {"markDuration","pickupIcd","nextDamageBonus","storedAttacks","nextRange"} },
{ "scout.delayed_exposure", new string[] {"coefficient","delay","markedCoefficient","targetsPerCast"} },
{ "scout.dawn_observation", new string[] {"markDuration","markRadius","icd","immediateShot","maxTargets","window","coefficient"} },
{ "guard.e_barrier", new string[] {"duration","amounts"} },
{ "guard.close_resistance", new string[] {"maxEnemies","radius","perEnemy"} },
{ "guard.heavy_beat", new string[] {"knockback","attackThresholds","stun"} },
{ "guard.shock_stagger", new string[] {"durations","enemyDamageReduction"} },
{ "guard.steady_boots", new string[] {"stabilityDurations","distanceBonuses"} },
{ "guard.close_shock_damage", new string[] {"bonuses","distance"} },
{ "guard.polished_guard", new string[] {"interval","shield","damageBonuses"} },
{ "guard.keep_out", new string[] {"knockback","slow","duration","distance","targetIcd"} },
{ "guard.barrier_counter", new string[] {"coefficient","knockback","cooldown","shield","shieldInterval","radius"} },
{ "guard.echo_floor", new string[] {"duration","radius","normalCoefficient","comboCoefficient","zonesPerCast"} },
{ "guard.outer_ring", new string[] {"knockback","radiusBonus","outerFraction","outerDamageBonus"} },
{ "guard.dodge_line", new string[] {"duration","radius","staggerDuration","damageReduction"} },
{ "guard.shield_recharge", new string[] {"icd","shield","absorbedThreshold","advanceSeconds"} },
{ "guard.refuse_exit", new string[] {"duration","shieldHealthFraction","shieldPerAction"} },
{ "guard.final_downbeat", new string[] {"totalBaseCoefficient","delay","pull","radiusBonus"} },
{ "guard.night_inspection", new string[] {"killThresholds","heal","shield"} },
{ "guard.shelter_code", new string[] {"radius","shieldPerPickup","staggerDuration","maxPickupsPerCast"} },
{ "guard.open_fortress", new string[] {"duration","radius","activeLimit","shieldPerSecond","extraECoefficient"} },
{ "support.scout_note", new string[] {"interval","markDuration"} },
{ "support.scout_sight", new string[] {"coefficient","attackThresholds","maxTargets"} },
{ "support.scout_light_kit", new string[] {"rangeBonus","moveTime","damageBonus"} },
{ "support.scout_chalk", new string[] {"duration","markDuration","targets","pickupBonus"} },
{ "support.scout_callout", new string[] {"coefficient","markDuration","targets"} },
{ "support.scout_return_fire", new string[] {"coefficient","markDuration","maxTargets","cooldown"} },
{ "support.scout_pathfinder", new string[] {"freeECapacity","markDuration","killThreshold","supportShots","coefficient","radius"} },
{ "support.guard_sleeve", new string[] {"cooldown","shields"} },
{ "support.guard_lamp", new string[] {"staggerDuration","distance","targetsPerAction"} },
{ "support.guard_boots", new string[] {"duration","reductions","hits"} },
{ "support.guard_tap", new string[] {"coefficient","attackThresholds","radius"} },
{ "support.guard_escort", new string[] {"stability","shield","cooldown"} },
{ "support.guard_repair", new string[] {"countIcd","hurtEvents","dodgeCharges","shield"} },
{ "support.guard_nightwatch", new string[] {"activeLimit","pulseTimes","duration","coefficient","shieldPerPulse","radius"} },
{ "item.orbiting_blades", new string[] {"counts","orbitRadius","orbitPeriod","hitRadius","coefficient","sharedTargetIcd"} },
{ "item.orbiting_bottles", new string[] {"count","orbitRadius","orbitPeriod","hitRadius","contactCoefficient","perBottleTargetIcd","burnCoefficient","burnDuration","burnInterval","slow","slowDuration","shield","shieldIcd","dodgeOrbitSpeedBonus","speedDuration"} },
};
public static IReadOnlyList<string> RequiredParameters(string key) => key != null && required.TryGetValue(key, out var names) ? names : Array.Empty<string>();
static UpgradeEffectRegistry() {
    required.Add("mio.grindstone", new string[] { "fraction", "range" });
    required.Add("mio.halfmoon_step", new string[] { "fraction", "duration" });
    required.Add("mio.grip_wrap", new string[] { "fraction" });
    required.Add("mio.straight_answer", new string[] { "targets", "range" });
    required.Add("mio.turning_cut", new string[] { "fraction", "angle", "duration" });
    required.Add("mio.borrowed_footing", new string[] { "seconds" });
    required.Add("mio.patience", new string[] { "fraction", "window" });
    required.Add("mio.warm_blade", new string[] { "fraction", "duration", "cooldown", "count" });
    required.Add("mio.three_beats", new string[] { "coefficient", "count", "range" });
    required.Add("mio.moon_return", new string[] { "coefficient" });
    required.Add("mio.cross_step", new string[] { "window" });
    required.Add("mio.circular_space", new string[] { "push", "pull", "window" });
    required.Add("mio.unending_chime", new string[] { "seconds", "cap" });
    required.Add("mio.unsheathed", new string[] { "count", "coefficient" });
    required.Add("mio.rift_path", new string[] { "duration", "count", "coefficient", "width" });
    required.Add("mio.before_sheathing", new string[] { "fraction", "window" });
    required.Add("mio.crossed_moons", new string[] { "coefficient" });
    required.Add("mio.open_the_way", new string[] { "coefficient" });
    required.Add("support.mio_whetstone", new string[] { "fraction", "healthThreshold" });
    required.Add("support.mio_step", new string[] { "fraction", "duration" });
    required.Add("support.mio_cover", new string[] { "fraction", "duration", "cooldown" });
    required.Add("support.mio_note", new string[] { "fraction", "healthThreshold" });
    required.Add("support.mio_moonbeam", new string[] { "coefficient", "count", "targets", "range" });
    required.Add("support.mio_retreat", new string[] { "coefficient", "cooldown", "delay", "radius" });
    required.Add("support.mio_clear_path", new string[] { "coefficient", "range", "push" });
    foreach (string name in AbyssBar.Mio.MioUpgradeEffects.Keys) {
        string key = name;
        factories.Add(key, () => new AbyssBar.Mio.MioUpgradeEffects(key));
    }
}
public static bool Supports(string key) => key != null && factories.ContainsKey(key);
public static RunEffect Create(string key) => Supports(key) ? factories[key]() : null;
public static void Register(string key, Func<RunEffect> factory) { if (string.IsNullOrWhiteSpace(key) || factory == null || factories.ContainsKey(key)) throw new ArgumentException("Duplicate or invalid effect key."); factories.Add(key, factory); }
}}


