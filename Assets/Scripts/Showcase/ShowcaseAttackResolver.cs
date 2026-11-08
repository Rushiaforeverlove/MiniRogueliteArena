using UnityEngine;

namespace AbyssBar.Showcase
{
    public static class ShowcaseAttackResolver
    {
        public static ShowcaseAttackPlan Resolve(CharacterData character, ShowcasePage page)
        {
            if (character == null || page == ShowcasePage.Overview) return null;
            var bonus = CharacterProgressionSave.GetBonuses(character);
            if (character.StartingWeapon != null && character.StartingWeapon.PrimaryAttackStyle is AbyssBar.Mio.MioPrimaryAttackStyle sword && sword.definition != null)
            {
                var data=sword.definition;
                return new ShowcaseAttackPlan { mio=data, pattern=page==ShowcasePage.Attack?ShowcasePattern.Melee:page==ShowcasePage.E?ShowcasePattern.DashSpin:ShowcasePattern.Empower,
                    damage=(page==ShowcasePage.E?data.tuning.eDamage:data.tuning.tapDamage)+bonus.Damage,
                    radius=page==ShowcasePage.E?data.tuning.eRadius:data.tuning.slashRadius,attackInterval=character.StartingWeapon.AttackInterval,
                    maxTargets=data.tuning.waveTargets,mioEDistance=data.tuning.eDistance*(1+bonus.EDistanceFraction),
                    speed=data.tuning.waveSpeed,lifetime=data.tuning.waveRange/data.tuning.waveSpeed,
                    mioChargeSeconds=Mathf.Max(.12f,data.tuning.chargeSeconds-bonus.ChargeTimeReduction),mioChargeDamage=data.tuning.chargedDamage+bonus.Damage,
                    projectileSprite=data.Find("normal.wave")?.frames[0].sprite,
                    mioEnhancedE=CharacterProgressionSave.IsEEnhanced(character),mioEnhancedQ=CharacterProgressionSave.IsQEnhanced(character) };
            }
            if (page == ShowcasePage.Attack)
            {
                var weapon = character.StartingWeapon;
                var projectile = weapon != null && weapon.ProjectilePrefab != null ? weapon.ProjectilePrefab.GetComponent<Projectile>() : null;
                if (projectile == null) return null;
                return new ShowcaseAttackPlan { repeatBasic = true, pattern = ShowcasePattern.Straight,
                    damage = (float)projectile.Damage + bonus.Damage, speed = projectile.Speed, lifetime = projectile.Lifetime,
                    attackInterval = Mathf.Max(.02f, weapon.AttackInterval / (1 + Mathf.Max(0, bonus.AttackSpeed))),
                    projectileSprite = weapon.ProjectilePrefab.GetComponent<SpriteRenderer>()?.sprite };
            }
            // Locked Q is allowed ONLY in this detached presentation model.
            // No call to SetQUnlocked, progression purchases or real combat.
            var skill = page == ShowcasePage.Q ? CharacterProgressionSave.GetBattleQSkill(character) : CharacterProgressionSave.GetBattleESkill(character);
            if (skill == null || !skill.IsConfigured) return null;
            var shot = skill.ProjectilePrefab != null ? skill.ProjectilePrefab.GetComponent<Projectile>() : null;
            var plan = new ShowcaseAttackPlan { damage = (float)(skill.Kind == ActiveSkillKind.Shockwave ? skill.Damage : shot.Damage) + bonus.Damage,
                radius = skill.Radius, projectileCount = skill.ProjectileCount, spreadDegrees = skill.SpreadDegrees,
                maxTargets = skill.ProjectileMaxTargets, explosionRadius = skill.ExplodeOnEnemyHit ? skill.ExplosionRadius : 0,
                pulseCount = skill.ShockwaveCount, pulseInterval = skill.ShockwaveInterval,
                speed = shot != null ? shot.Speed : 8, lifetime = shot != null ? shot.Lifetime : 3,
                projectileSprite = skill.ProjectilePrefab != null ? skill.ProjectilePrefab.GetComponent<SpriteRenderer>()?.sprite : null };
            switch (skill.Kind)
            {
                case ActiveSkillKind.FanShot: plan.pattern = ShowcasePattern.Fan; break;
                case ActiveSkillKind.RadialShot: plan.pattern = ShowcasePattern.Radial; break;
                case ActiveSkillKind.Shockwave: plan.pattern = ShowcasePattern.Shockwave; break;
                default: return null;
            }
            return plan;
        }
    }
}
