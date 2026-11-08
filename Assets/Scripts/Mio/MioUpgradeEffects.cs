using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Mio
{
    // Personal shape/timing modifiers are consumed by the greatsword controller.
    // Shared hit/action/support behaviors remain in the common run-effect pipeline.
    public sealed class MioUpgradeEffects : BuiltinUpgradeEffect
    {
        public static readonly string[] Keys={
            "mio.grindstone","mio.halfmoon_step","mio.grip_wrap","mio.straight_answer",
            "mio.turning_cut","mio.borrowed_footing","mio.patience","mio.warm_blade",
            "mio.three_beats","mio.moon_return","mio.cross_step","mio.circular_space","mio.unending_chime",
            "mio.unsheathed","mio.rift_path","mio.before_sheathing","mio.crossed_moons","mio.open_the_way",
            "support.mio_whetstone","support.mio_step","support.mio_cover","support.mio_note",
            "support.mio_moonbeam","support.mio_retreat","support.mio_clear_path"
        };
        private bool moonbeam;
        public MioUpgradeEffects(string key):base(key){}
        private static void Speed(RunUpgradeRuntime run,float fraction,float seconds)
        { run.scoutSpeed=Time.time<run.scoutSpeedUntil?Mathf.Max(run.scoutSpeed,fraction):fraction;run.scoutSpeedUntil=Mathf.Max(run.scoutSpeedUntil,Time.time+seconds); }
        public override void AfterHit(RunHit hit)
        {
            if(Code=="mio.halfmoon_step"&&hit.action.kind==RunActionKind.Attack&&hit.melee&&hit.action.Once(Data.id))
                Speed(C,N("fraction",.15f),N("duration",1));
        }
        public override void ModifyDamage(RunHit hit)
        {
            if(Code=="support.mio_whetstone"&&!hit.generated&&hit.action!=null&&hit.action.kind==RunActionKind.Attack
                &&hit.target.CurrentHealth>hit.target.MaxHealth*N("healthThreshold",.8f))hit.damage*=1+N("fraction",.2f);
        }
        public override void BeforeAction(RunAction action)
        {
            if(Code=="support.mio_cover"&&action.kind==RunActionKind.E&&Gate("shield",N("cooldown",6)))
                C.Health.AddShield(C.Health.MaxHealth*N("fraction",.04f),N("duration",2));
            if(Code=="support.mio_moonbeam"&&action.kind==RunActionKind.Attack&&moonbeam)
            {
                moonbeam=false;
                MioWave.Spawn(MioCharacterDefinition.Load(),C,C.Position,action.direction,C.AttackBasis*N("coefficient",.8f),
                    Mathf.RoundToInt(N("targets",4)),N("range",9),null,true);
            }
        }
        public override void AfterAction(RunAction action)
        {
            if(Code=="support.mio_moonbeam"&&action.kind==RunActionKind.Attack&&Count("basics",Mathf.RoundToInt(N("count",5))))moonbeam=true;
            if(Code=="support.mio_step"&&action.kind==RunActionKind.Dodge)Speed(C,N("fraction",.2f),N("duration",1.5f));
            if(Code=="support.mio_clear_path"&&action.kind==RunActionKind.Q)
                MioWave.Spawn(MioCharacterDefinition.Load(),C,C.Position,action.direction,C.AttackBasis*N("coefficient",5),64,
                    N("range",10),null,true,false,false,null,N("push",1.2f),1.3f);
        }
        public override void DodgeEnded(Vector2 start,Vector2 end)
        {
            if(Code!="support.mio_retreat"||!Gate("retreat",N("cooldown",4)))return;
            float damage=C.AttackBasis*N("coefficient",1.2f),radius=N("radius",1.5f);
            C.Later(N("delay",.5f),()=>{
                foreach(var enemy in C.Targets(start,radius))if(!RunProjectile.Blocked(start,enemy.transform.position))C.Hit(enemy,damage,null,true,false,start);
                MioFx.Play(MioCharacterDefinition.Load(),"normal.slash",start,Vector2.right,radius,.3f,C.transform);
            });
        }
        public override float Reduction()=>Code=="support.mio_note"&&C.Health.CurrentHealth/C.Health.MaxHealth<N("healthThreshold",.3f)?N("fraction",.1f):0;
    }
}
