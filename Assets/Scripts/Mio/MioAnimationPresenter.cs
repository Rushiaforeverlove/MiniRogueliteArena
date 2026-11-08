using UnityEngine;

namespace AbyssBar.Mio
{
    [DefaultExecutionOrder(500),DisallowMultipleComponent]
    public sealed class MioAnimationPresenter : MonoBehaviour
    {
        private MioCharacterDefinition definition;
        private SpriteRenderer original,view;
        private bool hub,terminal;
        private float clipAge,clipUntil,clipDuration,idleAge,terminalAge;
        private int priority;
        private string key;
        private Vector3 previous;
        private PlayerHealth health;
        private MioCombat combat;
        private AbyssBar.Upgrades.RunUpgradeRuntime run;
        private float previousHealth;
        private Collider2D groundCollider;
        private bool suppliedAge;
        private PlayerController movement;
        private MioChargePresentation gauge;
        private string lastSelected;
        private float presentationScale=1;
        private float? floorOverride;
        private AbyssBar.Presentation.HubMotionPresenter hubMotion;
        public void SetPresentationScale(float value,float? floor=null){presentationScale=Mathf.Max(.01f,value);floorOverride=floor;}
        public void SetDefinition(MioCharacterDefinition data,bool inHub)
        {
            original=GetComponent<SpriteRenderer>();definition=data;hub=inHub;previous=transform.position;presentationScale=1;floorOverride=null;
            key=lastSelected=null;priority=0;terminal=false;idleAge=0;
            hubMotion=GetComponent<AbyssBar.Presentation.HubMotionPresenter>();
            var hubData=inHub&&data!=null?Resources.Load<AbyssBar.Presentation.HubMotionDefinition>("HubMotion/mio_01"):null;
            if(hubData!=null)
            {
                if(hubMotion==null)hubMotion=gameObject.AddComponent<AbyssBar.Presentation.HubMotionPresenter>();
                var sizes=AbyssBar.Hub.HubArtCatalog.Load()?.characterSizes;
                var size=sizes!=null?System.Array.Find(sizes,s=>s.characterId==data.character.CharacterId):null;
                hubMotion.enabled=true;hubMotion.Configure(hubData,size!=null?size.hubHeight:hubData.referenceHeight);
            }
            else if(hubMotion!=null){hubMotion.Configure(null,1);hubMotion.enabled=false;}
            if(data==null){gauge?.Configure(null);if(view!=null)view.enabled=false;if(original!=null)original.enabled=true;enabled=false;return;}
            enabled=true;
            if(view==null){var go=new GameObject("Mio visual");go.transform.SetParent(transform,false);view=go.AddComponent<SpriteRenderer>();}
            original.enabled=false;view.enabled=true;view.sharedMaterial=original.sharedMaterial;
            view.spriteSortPoint=SpriteSortPoint.Pivot;
            health=GetComponent<PlayerHealth>();combat=GetComponent<MioCombat>();run=GetComponent<AbyssBar.Upgrades.RunUpgradeRuntime>();
            groundCollider=GetComponent<Collider2D>();
            movement=GetComponent<PlayerController>();
            if(!hub){gauge=GetComponent<MioChargePresentation>()??gameObject.AddComponent<MioChargePresentation>();gauge.Configure(data);}
            previousHealth=health!=null?health.CurrentHealth:0;
        }
        public void Play(string action,bool released,float seconds,float duration,int newPriority)
            =>PlayKey((released?"rage.":"normal.")+action,seconds,newPriority,duration);
        public void PlayKey(string name,float seconds,int newPriority,float duration=0)
        {
            if(definition==null||terminal||priority>newPriority&&clipUntil>0)return;
            key=name;suppliedAge=false;clipAge=0;clipUntil=Mathf.Max(.01f,seconds);clipDuration=duration>0?duration:seconds;priority=newPriority;idleAge=0;
        }
        public void ShowCharging(bool released,float age,float required)
        {
            if(combat!=null&&!combat.Busy)ReleaseMotionPose();
            if(priority==25&&clipAge>=definition.tuning.impactDelay+.04f)ReleaseMotionPose(true);
            if(terminal||priority>15&&clipUntil>0)return;
            bool moving=movement!=null&&movement.MovementInput.sqrMagnitude>.001f,full=age>=required;
            string action=full?(moving?"move_hold":"hold"):moving?(age<required*.27f?"move_charge_start":"move_charge"):"charge";
            key=(released?"rage.":"normal.")+action;suppliedAge=true;
            clipAge=full?age-required:action=="move_charge"?age-required*.27f:age;
            clipDuration=full||action=="move_charge"?0:action=="move_charge_start"?required*.27f:required;
            clipUntil=float.PositiveInfinity;priority=15;idleAge=0;
        }
        public void CancelCharge(){if(priority==15){key=null;clipUntil=0;priority=0;}}
        public void ReleaseMotionPose(bool all=false){if(!terminal&&(all||key!=null&&(key.EndsWith(".spin")||key.EndsWith(".dash")||key.EndsWith(".dodge")||key.EndsWith(".hurt")))){key=null;clipUntil=0;priority=0;suppliedAge=false;}}
        private void LateUpdate()
        {
            if(definition==null||view==null)return;
            if(combat==null)combat=GetComponent<MioCombat>();
            if(run==null&&!hub)run=GetComponent<AbyssBar.Upgrades.RunUpgradeRuntime>();
            bool dead=!hub&&health!=null&&!health.IsAlive;
            bool ended=!hub&&run!=null&&run.Ready&&!run.Active;
            if((dead||ended)&&!terminal){terminal=true;terminalAge=0;key=(combat!=null&&(combat.Released||combat.LastEndedReleased)?"rage.":"normal.")+(dead?"defeat":"victory");priority=100;}
            float dt=terminal?Time.unscaledDeltaTime:Time.deltaTime;
            if(terminal){terminalAge+=dt;clipAge=terminalAge;clipDuration=0;}
            else if(Time.timeScale>0){if(!suppliedAge)clipAge+=dt;clipUntil=Mathf.Max(0,clipUntil-dt);if(clipUntil<=0){priority=0;key=null;}}
            Vector3 delta=transform.position-previous;previous=transform.position;
            bool moving=delta.sqrMagnitude>.000001f&&delta.sqrMagnitude<16;
            if(!terminal&&Time.timeScale>0)
            {
                if(moving)idleAge=0;else idleAge+=dt;
                if(health!=null&&health.CurrentHealth<previousHealth)combat?.NotifyHurt();
                if(health!=null)previousHealth=health.CurrentHealth;
            }
            bool released=combat!=null&&combat.Released;
            if(hub&&hubMotion!=null&&hubMotion.enabled)
            {
                if(Mathf.Abs(delta.x)>.0001f)view.flipX=delta.x<0;
                float hubFloor=floorOverride??(groundCollider!=null&&groundCollider.enabled?groundCollider.bounds.min.y+.025f:transform.position.y);
                if(hubMotion.Draw(view,original,moving,dt,new Vector3(transform.position.x,hubFloor,transform.position.z)))
                {original.enabled=false;lastSelected="hub."+hubMotion.Clock.Current.ToString().ToLowerInvariant();return;}
            }
            if(!hub&&!terminal&&!released&&key==null&&moving&&lastSelected=="normal.sleep")PlayKey("normal.wakeup",.16f,5,.16f);
            string selected=key;
            if(string.IsNullOrEmpty(selected))selected=hub?(moving?"hub.walk":idleAge>=5?"hub.sleep":"hub.idle")
                :(moving?(released?"rage.move":"normal.move"):idleAge>=5&&!released?"normal.sleep":released?"rage.idle":"normal.idle");
            var clip=definition.Find(selected);float age=key!=null?clipAge:selected=="hub.sleep"||selected=="normal.sleep"?idleAge-5:Time.time;
            var frame=clip?.At(age,key!=null?clipDuration:0);
            if(frame==null||frame.sprite==null)return;
            // Hub overrides bind their atlas explicitly. Restore the original idle/battle binding on return.
            view.SetPropertyBlock(null);view.sharedMaterial=original.sharedMaterial;
            view.sprite=frame.sprite;view.color=original.color;view.sortingLayerID=original.sortingLayerID;view.sortingOrder=original.sortingOrder;
            if(hub&&Mathf.Abs(delta.x)>.0001f)view.flipX=delta.x<0;else if(!hub)view.flipX=original.flipX;
            var parent=transform.lossyScale;float scale=definition.visualScale*Mathf.Max(.01f,frame.scale)*presentationScale;
            view.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(parent.x)),scale/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
            // Existing player roots are centered above their feet. Use the body's
            // bottom for every clip so replacing artwork never changes physics.
            float floor=floorOverride??(groundCollider!=null&&groundCollider.enabled?groundCollider.bounds.min.y+.025f:transform.position.y);
            view.transform.position=new Vector3(transform.position.x,floor,transform.position.z);original.enabled=false;
            lastSelected=selected;
            DrawGhosts(selected,dt);
        }
        private readonly SpriteRenderer[] ghosts=new SpriteRenderer[3];private readonly Vector3[] ghostAt=new Vector3[3];private readonly float[] ghostUntil=new float[3];private int ghostIndex;private float nextGhost;
        private void DrawGhosts(string selected,float dt)
        {
            if(!hub&&Time.timeScale>0&&(selected.EndsWith(".dash")||selected.EndsWith(".dodge"))&&Time.time>=nextGhost)
            {
                nextGhost=Time.time+.045f;int i=ghostIndex++%3;
                if(ghosts[i]==null){var go=new GameObject("Mio afterimage");go.transform.SetParent(transform,false);ghosts[i]=go.AddComponent<SpriteRenderer>();}
                var g=ghosts[i];g.sprite=view.sprite;g.flipX=view.flipX;g.sharedMaterial=view.sharedMaterial;g.sortingLayerID=view.sortingLayerID;g.sortingOrder=view.sortingOrder-1;g.transform.localScale=view.transform.localScale;ghostAt[i]=view.transform.position;ghostUntil[i]=Time.time+.16f;
            }
            for(int i=0;i<3;i++)if(ghosts[i]!=null){var g=ghosts[i];g.enabled=ghostUntil[i]>Time.time&&!terminal;g.transform.position=ghostAt[i];g.color=new Color(view.color.r,view.color.g,view.color.b,.25f*Mathf.Clamp01((ghostUntil[i]-Time.time)/.16f));}
        }
        private void OnDisable(){hubMotion?.HideLower();if(view!=null)view.enabled=false;foreach(var g in ghosts)if(g!=null)g.enabled=false;if(original!=null)original.enabled=true;}
    }
}
