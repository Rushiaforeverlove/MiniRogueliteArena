using AbyssBar.Hub;
using AbyssBar.Upgrades;
using UnityEngine;
namespace AbyssBar.Presentation
{
    [DefaultExecutionOrder(550),DisallowMultipleComponent]
    public sealed class CharacterAnimationPresenter:MonoBehaviour
    {
        private CharacterPresentationDefinition data;
        private SpriteRenderer source,view;private Collider2D ground;
        private RunUpgradeRuntime run;private PlayerHealth health;private PlayerAutoAttack attack;
        private CharacterEffectPool feedback;
        private Vector3 previous;private float idleAge,age,remaining,overrideDuration,previousHealth;
        private string action;private int priority;private bool hub,terminal,left,castLeft;
        public string CurrentKey{get;private set;}
        public SpriteRenderer View=>view;
        public CharacterPresentationDefinition Definition=>data;
        public void Configure(CharacterPresentationDefinition definition,bool inHub)
        {
            Unsubscribe();data=definition;hub=inHub;source=GetComponent<SpriteRenderer>();ground=GetComponent<Collider2D>();
            if(data==null||source==null){enabled=false;if(view!=null)view.enabled=false;feedback?.Clear();return;}
            action=null;priority=0;terminal=false;age=idleAge=remaining=0;previous=transform.position;
            if(view==null){var go=new GameObject("Animated character");go.layer=gameObject.layer;go.transform.SetParent(transform,false);view=go.AddComponent<SpriteRenderer>();}
            run=GetComponent<RunUpgradeRuntime>();health=GetComponent<PlayerHealth>();attack=GetComponent<PlayerAutoAttack>();
            previousHealth=health!=null?health.CurrentHealth:0;
            if(!hub&&run!=null)
            {
                run.ActionCompleted+=OnAction;run.PulseEmitted+=OnPulse;run.ProjectileCreated+=OnProjectile;run.HitResolved+=OnHit;
                feedback=GetComponent<CharacterEffectPool>()??gameObject.AddComponent<CharacterEffectPool>();
                feedback.Configure(data,source.sortingLayerID,source.sortingOrder+5);
                run.AreaVisualOverride+=OwnsAreaVisual;
            }
            Draw(0);enabled=true;
        }
        private void Unsubscribe()
        {if(run!=null){run.ActionCompleted-=OnAction;run.PulseEmitted-=OnPulse;run.ProjectileCreated-=OnProjectile;run.HitResolved-=OnHit;run.AreaVisualOverride-=OwnsAreaVisual;}run=null;}
        private bool OwnsAreaVisual(RunAction a)
        {
            if(!isActiveAndEnabled||hub||data==null||!data.circularSkills||feedback==null||!feedback.isActiveAndEnabled||a==null||!a.shockwave)return false;
            if(a.kind!=RunActionKind.E&&a.kind!=RunActionKind.Q)return false;
            var clip=data.Find(a.kind==RunActionKind.E?"fx.e":"fx.q");
            return clip?.frames!=null&&clip.frames.Length>0&&System.Array.TrueForAll(clip.frames,f=>f!=null&&f.texture!=null);
        }
        private void OnEnable()
        {
            // Fast Play can preserve this instance; remove then re-add rather than duplicate delegates.
            if(data!=null&&source!=null)Configure(data,hub);
        }
        public void Play(string key,float duration,int rank)
        {if(data==null||terminal||remaining>0&&priority>rank)return;action=key;age=0;remaining=duration;overrideDuration=duration;priority=rank;idleAge=0;}
        private void OnAction(RunAction a)
        {
            if(a==null||data==null)return;
            if(a.kind==RunActionKind.Attack)
            {
                // Always release skill/hurt recovery visually when the combat system accepts a new shot.
                action=Moving?"combat.moveShoot":"combat.shoot";age=0;overrideDuration=remaining=Mathf.Clamp(attack!=null?attack.AttackInterval:.5f,.12f,.5f);priority=10;idleAge=0;
                var at=transform.position+Vector3.up*data.aimHeight+(Vector3)a.direction*data.muzzleReach;
                feedback?.Emit("fx.muzzle",at,new Vector2(.7f,.7f),Mathf.Atan2(a.direction.y,a.direction.x)*Mathf.Rad2Deg,.15f);
            }
            else if(a.kind==RunActionKind.Dodge)Play("combat.dodge",.16f,50);
            else if(!a.shockwave)
            {
                castLeft=a.direction.x<0;
                Play(a.kind==RunActionKind.E?"skill.e":"skill.q",a.kind==RunActionKind.E?.65f:.8f,30);
                feedback?.Emit("fx.muzzle",transform.position+Vector3.up*data.aimHeight+(Vector3)a.direction*data.muzzleReach,
                    Vector2.one*.85f,Mathf.Atan2(a.direction.y,a.direction.x)*Mathf.Rad2Deg,.2f);
            }
        }
        private void OnPulse(RunAction a,Vector2 center,float radius)
        {
            if(a==null||data==null)return;bool q=a.kind==RunActionKind.Q;
            Play(q?"skill.q":"skill.e",q?.48f:.65f,30);
            feedback?.Emit(q?"fx.q":"fx.e",center,new Vector2(radius*2,radius*1.1f),0,.45f);
        }
        private void OnProjectile(RunProjectile projectile,RunAction a,bool generated)
        {if(!generated&&projectile!=null&&a!=null)projectile.gameObject.AddComponent<CharacterProjectileView>().Configure(data,projectile.Direction);}
        private void OnHit(RunHit hit)
        {
            if(hit==null||hit.generated||hit.target==null||hit.shockwave)return;
            bool explosion=hit.action!=null&&hit.action.kind==RunActionKind.Q&&!data.circularSkills&&run.Skills.UltimateSkill.ExplodeOnEnemyHit;
            float radius=explosion?run.Skills.UltimateSkill.ExplosionRadius: .4f;
            feedback?.Emit(explosion?"fx.q":"fx.hit",hit.target.transform.position+Vector3.up*data.aimHeight,Vector2.one*radius*2,0,.4f);
        }
        private bool Moving=>(transform.position-previous).sqrMagnitude>.000001f&&(transform.position-previous).sqrMagnitude<16;
        private void LateUpdate()=>Draw(terminal?Time.unscaledDeltaTime:Time.deltaTime);
        private void Draw(float dt)
        {
            if(data==null||source==null||view==null)return;
            bool moving=Moving;var delta=transform.position-previous;previous=transform.position;
            if(hub&&Mathf.Abs(delta.x)>.0001f)left=delta.x<0;else if(!hub)left=source.flipX;
            if(!hub&&!terminal&&health!=null&&(!health.IsAlive||run!=null&&run.Ready&&!run.Active))
            {terminal=true;action=!health.IsAlive?"combat.death":"action.victory";age=0;priority=100;remaining=float.PositiveInfinity;overrideDuration=0;}
            if(dt>0)
            {
                age+=dt;
                if(!terminal)
                {
                    if(moving)idleAge=0;else idleAge+=dt;
                    remaining=Mathf.Max(0,remaining-dt);if(remaining<=0){action=null;priority=0;}
                    if(health!=null&&health.CurrentHealth<previousHealth)Play("combat.hurt",.22f,40);
                    if(health!=null)previousHealth=health.CurrentHealth;
                }
            }
            CurrentKey=action??(hub?(moving?"hub.walk":idleAge>=5?"hub.special":"hub.idle"):(moving?"combat.walk":"combat.idle"));
            if(!terminal&&action!=null&&(action=="combat.shoot"||action=="combat.moveShoot"))CurrentKey=moving?"combat.moveShoot":"combat.shoot";
            float clipAge=action!=null?age:CurrentKey=="hub.special"?idleAge-5:Time.time;
            float floor=hub?(HubLayoutData.Load()?.WalkingFloorY??-3.55f):ground!=null&&ground.enabled?ground.bounds.min.y+.025f:transform.position.y;
            bool facing=CurrentKey=="skill.e"&&!data.circularSkills?castLeft:left;
            CharacterPresentationView.Draw(view,data,CurrentKey,clipAge,new Vector3(transform.position.x,floor,transform.position.z),hub?data.hubHeight:data.battleHeight,facing,source.color,source.sortingOrder,source.sortingLayerID,action!=null?overrideDuration:0);
            source.enabled=false;
        }
        private void OnDisable(){Unsubscribe();if(view!=null)view.enabled=false;feedback?.Clear();}
    }
}
