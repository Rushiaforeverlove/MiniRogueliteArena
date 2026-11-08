using UnityEngine;

namespace AbyssBar.Combat
{
    [CreateAssetMenu(fileName = "ProjectileAttack", menuName = "Abyss Bar/Primary Attacks/Projectile")]
    public sealed class ProjectilePrimaryAttackStyle : PrimaryAttackStyle
    { public override PrimaryAttackSession CreateSession() => new ProjectilePrimaryAttackSession(); }
}
