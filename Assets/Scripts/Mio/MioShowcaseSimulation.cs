using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Showcase
{
    // Local deterministic display model. Never instantiates battle components,
    // spends charges, registers enemies, unlocks Q or writes progression.
    public sealed partial class ShowcaseCombatSimulation
    {
        public sealed class MioEffect { public string key;public Vector2 position,direction;public float radius,age,duration; }
        public readonly List<MioEffect> mioEffects=new List<MioEffect>();
        public string MioClipKey {get;private set;}="normal.idle";
        public float MioClipAge {get;private set;}
        public float MioClipDuration {get;private set;}
        public Vector2 MioAvatarOffset {get;private set;}
        public bool MioReleased {get;private set;}
        public float MioChargeProgress {get;private set;}
        public bool MioInvulnerable {get;private set;}
        private readonly HashSet<int> mioRecorded=new HashSet<int>();
        private int mioFinisherCount;
        private void ConfigureMioTargets()
        {
            targets.Clear();var data=Plan.mio.tuning;
            if(Plan.pattern==ShowcasePattern.Melee){for(int i=0;i<5;i++)targets.Add(new Target{position=hero+Vector2.right*(1.6f+i*1.25f)});return;}
            int count=Plan.pattern==ShowcasePattern.DashSpin?7:6;
            Vector2 center=Plan.pattern==ShowcasePattern.DashSpin?hero+Vector2.right*Plan.mioEDistance:hero;
            for(int i=0;i<count;i++){float a=i*Mathf.PI/3,r=Plan.pattern==ShowcasePattern.DashSpin?(i==6?data.eRadius+1.5f:data.eRadius*.52f):2.1f;targets.Add(new Target{position=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r});}
        }
        private void TickMio(float dt)
        {
            float before=Time;elapsed+=dt;float t=Time;var data=Plan.mio.tuning;
            foreach(var p in pulses)p.age+=dt;pulses.RemoveAll(p=>p.age>=.45f);
            foreach(var h in hits)h.age+=dt;hits.RemoveAll(h=>h.age>=.75f);
            foreach(var fx in mioEffects)fx.age+=dt;mioEffects.RemoveAll(fx=>fx.age>=fx.duration);
            MioAvatarOffset=Vector2.zero;MioReleased=MioInvulnerable=false;MioChargeProgress=0;
            if(Plan.pattern==ShowcasePattern.DashSpin)
            {
                float age=t-.2f;MioAvatarOffset=Vector2.right*(age<0?0:Plan.mioEDistance*Mathf.SmoothStep(0,1,Mathf.Clamp01(age/data.eDuration)));
                if(age<0)MioPose("normal.idle",t,0);
                else if(age<data.eDuration){MioPose("normal.dash",age,data.eDuration);MioInvulnerable=true;}
                else if(age<data.eDuration+data.spinDuration){MioPose("normal.spin",age-data.eDuration,data.spinDuration);MioInvulnerable=true;}
                else MioPose("normal.idle",age,0);
                if(Crossed(before,t,.2f+data.eDuration+.08f)){MioCircle(hero+Vector2.right*Plan.mioEDistance,Plan.radius,Plan.damage,true,1,data.ePush);MioEffectAt("normal.spin_fx",hero+MioAvatarOffset,Plan.radius,data.spinDuration);}
                if(Plan.mioEnhancedE&&Crossed(before,t,.2f+data.eDuration+data.enhancedEDelay))
                {Vector2 to=hero+Vector2.right*Plan.mioEDistance;foreach(var target in targets)if(Vector2.Distance(Closest(hero,to,target.position),target.position)<=.7f)MioDamage(target,Plan.damage*data.enhancedECoefficient,2);MioEffectAt("normal.slash",(hero+to)*.5f,Plan.radius*.5f,.35f);}
            }
            else if(Plan.pattern==ShowcasePattern.Empower)
            {
                float start=.2f+data.qEnterSeconds,end=start+data.qSeconds;
                if(t<.2f)MioPose("normal.idle",t,0);
                else if(t<start)MioPose("q.enter",t-.2f,data.qEnterSeconds);
                else if(t<end){MioReleased=true;MioReleasedSequence(t-start,before-start);}
                else if(t<end+data.qExitSeconds)MioPose("q.exit",t-end,data.qExitSeconds);
                else MioPose("normal.idle",t,0);
                if(Crossed(before,t,.2f)){MioEffectAt("fx.pulse",hero,data.qPushRadius,data.qEnterSeconds);foreach(var target in targets)if(Vector2.Distance(hero,target.position)<=data.qPushRadius)target.position+=(target.position-hero).normalized*data.qPushDistance;}
                if(Crossed(before,t,end))MioEffectAt("fx.fade",hero,1.2f,data.qExitSeconds);
                if(Plan.mioEnhancedQ){if(Crossed(before,t,end))mioFinisherCount=Math.Min(6,mioRecorded.Count);for(int i=0;i<mioFinisherCount;i++)if(Crossed(before,t,end+i*.075f))MioShot(Vector2.right,Plan.mioChargeDamage*data.enhancedQCoefficient,0,true);}
            }
            else MioBasic(t,before,false);
            for(int i=shots.Count-1;i>=0;i--)
            {
                var shot=shots[i];shot.age+=dt;Vector2 next=shot.position+shot.direction*Plan.speed*dt;
                var order=new List<int>();for(int n=0;n<targets.Count;n++)order.Add(n);order.Sort((a,b)=>(targets[a].position-shot.position).sqrMagnitude.CompareTo((targets[b].position-shot.position).sqrMagnitude));
                foreach(int n in order){var target=targets[n];if(target.health<=0||shot.hit.Contains(n)||Vector2.Distance(Closest(shot.position,next,target.position),target.position)>shot.mioWidth*.5f+.25f)continue;shot.hit.Add(n);MioDamage(target,shot.damage,shot.mioCastId);if(shot.hit.Count>=shot.mioMaxTargets)break;}
                shot.position=next;if(shot.hit.Count>=shot.mioMaxTargets||shot.age>=Plan.lifetime)shots.RemoveAt(i);
            }
        }
        private void MioBasic(float time,float previous,bool released)
        {
            var data=Plan.mio.tuning;float start=data.tapSeconds,end=start+Math.Max(.12f,Plan.mioChargeSeconds),hold=end+.8f,length=hold+data.releaseSeconds+.25f;
            int cycle=Math.Max(0,(int)(time/length));float local=Math.Max(0,time-cycle*length);bool moving=cycle%2==1;string form=released?"rage.":"normal.";
            if(local<start)MioPose(form+"tap",local,data.tapSeconds);
            else if(local<end){float age=local-start;MioChargeProgress=Mathf.Clamp01(age/Plan.mioChargeSeconds);float first=Plan.mioChargeSeconds*.27f;MioPose(form+(moving?(age<first?"move_charge_start":"move_charge"):"charge"),moving&&age>=first?age-first:age,moving?(age<first?first:0):Plan.mioChargeSeconds);}
            else if(local<hold){MioChargeProgress=1;MioPose(form+(moving?"move_hold":"hold"),local-end,0);}
            else if(local<hold+data.releaseSeconds)MioPose(form+(moving?"move_release":"release"),local-hold,data.releaseSeconds);
            else MioPose(form+"idle",local,0);
            if(Crossed(previous,time,cycle*length+data.impactDelay)){MioCircle(hero,data.slashRadius,Plan.damage,false,cycle*2+1);MioEffectAt(form+"slash",hero,data.slashRadius,.35f);if(released)MioShot(Vector2.right,Plan.damage*data.rageTapWaveCoefficient,cycle*2+1,true);}
            if(Crossed(previous,time,cycle*length+hold+data.impactDelay)){int count=released?6:1;for(int i=0;i<count;i++)MioShot(Quaternion.Euler(0,0,i*360f/count)*Vector2.right,Plan.mioChargeDamage*(released?data.rageRadialCoefficient:1),cycle*2+2,released);}
        }
        private void MioReleasedSequence(float time,float previous)
        {
            var d=Plan.mio.tuning;
            string[] poses={"move","tap","move_charge_start","move_charge","move_hold","move_release","e","move","tap","charge","hold","release"};
            float[] spans={.5f,.42f,Plan.mioChargeSeconds*.27f,Plan.mioChargeSeconds*.73f,.35f,.5f,d.eDuration+d.spinDuration,.5f,.42f,Plan.mioChargeSeconds,.33f,.5f};
            float begin=0;for(int i=0;i<poses.Length;i++)
            {
                float end=begin+spans[i];if(time>=begin&&time<end)
                {
                    string pose=poses[i];float age=time-begin;
                    if(pose=="e")
                    {
                        MioInvulnerable=true;MioPose(age<d.eDuration?"rage.dash":"rage.spin",age<d.eDuration?age:age-d.eDuration,age<d.eDuration?d.eDuration:d.spinDuration);
                        if(Crossed(previous,time,begin+d.eDuration+.08f)){float radius=d.eRadius*d.rageSpinMultiplier;MioCircle(hero,radius,Plan.damage,true,90,d.ePush+d.rageSpinPush);MioEffectAt("rage.spin_fx",hero,radius,d.spinDuration);}
                    }
                    else
                    {
                        MioPose("rage."+pose,age,pose=="move_charge_start"?Plan.mioChargeSeconds*.27f:pose=="charge"?Plan.mioChargeSeconds:pose=="tap"?d.tapSeconds:pose=="move_release"||pose=="release"?d.releaseSeconds:0);
                        if(pose=="move_charge_start")MioChargeProgress=Mathf.Clamp01(age/Plan.mioChargeSeconds);
                        if(pose=="move_charge")MioChargeProgress=Mathf.Clamp01((age+Plan.mioChargeSeconds*.27f)/Plan.mioChargeSeconds);
                        if(pose=="charge")MioChargeProgress=Mathf.Clamp01(age/Plan.mioChargeSeconds);
                        if(pose=="hold"||pose=="move_hold")MioChargeProgress=1;
                        if(pose=="tap"&&Crossed(previous,time,begin+d.impactDelay)){MioCircle(hero,d.slashRadius,Plan.damage,false,10+i);MioEffectAt("rage.slash",hero,d.slashRadius,.35f);MioShot(Vector2.right,Plan.damage*d.rageTapWaveCoefficient,10+i,true);}
                        if((pose=="release"||pose=="move_release")&&Crossed(previous,time,begin+d.impactDelay))for(int n=0;n<6;n++)MioShot(Quaternion.Euler(0,0,n*60)*Vector2.right,Plan.mioChargeDamage*d.rageRadialCoefficient,10+i,true);
                    }
                    return;
                }
                begin=end;
            }
            MioPose("rage.idle",0,0);
        }
        private static bool Crossed(float before,float now,float at)=>before<at&&now>=at;
        private void MioPose(string key,float age,float duration){MioClipKey=key;MioClipAge=Math.Max(0,age);MioClipDuration=duration;}
        private void MioShot(Vector2 direction,float damage,int castId=0,bool released=false)
        {if(shots.Count<64)shots.Add(new Shot{position=hero,direction=direction,damage=damage,mioCastId=castId,mioMaxTargets=Plan.maxTargets,mioWidth=Plan.mio.tuning.waveWidth,mioReleased=released});}
        private void MioEffectAt(string key,Vector2 at,float radius,float duration){if(mioEffects.Count<64)mioEffects.Add(new MioEffect{key=key,position=at,direction=Vector2.right,radius=radius,duration=duration});}
        private void MioCircle(Vector2 at,float radius,float damage,bool all,int castId=0,float push=0)
        {foreach(var target in targets)if(target.health>0&&Vector2.Distance(at,target.position)<=radius+.25f&&(all||Vector2.Angle(Vector2.right,target.position-at)<=30)){MioDamage(target,damage,castId);if(push>0)target.position+=(target.position-at).normalized*push;}}
        private void MioDamage(Target target,float amount,int castId=0)
        {if(MioReleased&&castId>0)mioRecorded.Add(castId);target.health=Math.Max(0,target.health-amount);target.lastHit=Time;if(target.health<=0)target.deathAt=Time;if(hits.Count<64)hits.Add(new Hit{position=target.position,amount=amount});MioEffectAt((MioReleased?"rage.":"normal.")+"hit",target.position,.35f,.25f);}
    }
}
