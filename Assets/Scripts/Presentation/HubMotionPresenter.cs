using UnityEngine;

namespace AbyssBar.Presentation
{
    [DisallowMultipleComponent]
    public sealed class HubMotionPresenter:MonoBehaviour
    {
        private HubMotionDefinition data;
        private SpriteRenderer lower;
        private float height;
        public HubIdleMotionClock Clock{get;}=new HubIdleMotionClock();
        public SpriteRenderer Lower=>lower;
        public void Configure(HubMotionDefinition definition,float desiredHeight)
        {data=definition;height=desiredHeight;Clock.Reset();HideLower();}
        public bool Draw(SpriteRenderer view,SpriteRenderer source,bool moving,float dt,Vector3 feet)
        {
            if(data==null){HideLower();return false;}
            Clock.Tick(moving,dt,data);
            if(Clock.Current==HubIdleMotionClock.Pose.Idle){HideLower();return false;}
            bool walking=Clock.Current==HubIdleMotionClock.Pose.Walk;
            var clip=walking?data.walk:Clock.Current==HubIdleMotionClock.Pose.Wake?data.wake:data.fallAsleep;
            var frame=clip.At(Clock.Age,Clock.Current==HubIdleMotionClock.Pose.Wake?data.wakeSeconds:0);
            float scale=height/Mathf.Max(.01f,data.referenceHeight);
            var at=walking?feet:feet+Vector3.up*(data.splitHeight*scale);
            CharacterPresentationView.Apply(view,frame,at,scale,view.flipX,source.color,source.sortingOrder+1,source.sortingLayerID,data.BoundMaterial);
            if(walking){HideLower();return true;}
            if(lower==null)
            {
                var go=new GameObject("Pinned hub resting lower body");go.transform.SetParent(transform,false);lower=go.AddComponent<SpriteRenderer>();
            }
            CharacterPresentationView.Apply(lower,data.restingLower,feet,scale,view.flipX,source.color,source.sortingOrder,source.sortingLayerID,data.BoundMaterial);
            return true;
        }
        public void HideLower(){if(lower!=null)lower.enabled=false;}
        private void OnDisable(){HideLower();Clock.Reset();}
    }
}
