using UnityEngine;

namespace AbyssBar.Combat
{
    // Definitions contain no charging/timer state. Each player/weapon creates
    // its own session. New melee/charge styles can consume pressed/held/released
    // and heldSeconds without changing the pointer or UI-safety pipeline.
    public abstract class PrimaryAttackStyle : ScriptableObject
    { public abstract PrimaryAttackSession CreateSession(); }

    public abstract class PrimaryAttackSession
    {
        public abstract void Tick(PlayerAutoAttack actor, PrimaryAttackInput input);
        public virtual void Cancel(PlayerAutoAttack actor) { }
    }

    public sealed class ProjectilePrimaryAttackSession : PrimaryAttackSession
    {
        public override void Tick(PlayerAutoAttack actor, PrimaryAttackInput input)
        { if (input.pressed || input.held) actor.TryFireProjectileAttack(); }
    }
}
