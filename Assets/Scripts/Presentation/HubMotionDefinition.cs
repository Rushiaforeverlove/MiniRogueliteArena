using System;
using UnityEngine;

namespace AbyssBar.Presentation
{
    // Optional hub-only animation override. Combat and the existing standing idle stay independent.
    [CreateAssetMenu(menuName="Abyss Bar/Hub/Actor Motion")]
    public sealed class HubMotionDefinition:ScriptableObject
    {
        public float referenceHeight=1.63f,sleepDelay=5,wakeSeconds=.16f,splitHeight=.7009f;
        public Material material;
        public CharacterMotion walk,fallAsleep,wake;
        public CharacterFrame restingLower;
        [NonSerialized] private Material boundMaterial;
        public Material BoundMaterial
        {
            get
            {
                if(boundMaterial==null)boundMaterial=new Material(material){name=name+" hub atlas",mainTexture=walk.frames[0].texture};
                return boundMaterial;
            }
        }
        private void OnDisable()
        {
            foreach(var clip in new[]{walk,fallAsleep,wake})if(clip?.frames!=null)foreach(var frame in clip.frames)frame?.Release();
            restingLower?.Release();
            if(boundMaterial!=null){if(Application.isPlaying)Destroy(boundMaterial);else DestroyImmediate(boundMaterial);}boundMaterial=null;
        }
    }
    // Has no input or movement ownership: a waking pose must never lock player movement.
    public sealed class HubIdleMotionClock
    {
        public enum Pose{Idle,Walk,Sleep,Wake}
        public Pose Current{get;private set;}
        public float Age{get;private set;}
        private float stationary;
        public void Reset(){Current=Pose.Idle;Age=stationary=0;}
        public void Tick(bool moving,float dt,HubMotionDefinition data)
        {
            dt=Mathf.Max(0,dt);var next=Current;
            if(moving)
            {
                stationary=0;
                if(Current==Pose.Sleep)next=Pose.Wake;
                else if(Current!=Pose.Wake||Age+dt>=data.wakeSeconds)next=Pose.Walk;
            }
            else
            {
                stationary+=dt;
                if(Current==Pose.Wake&&Age+dt<data.wakeSeconds)next=Pose.Wake;
                else next=stationary>=data.sleepDelay?Pose.Sleep:Pose.Idle;
            }
            if(next!=Current){Current=next;Age=0;}else Age+=dt;
        }
    }
}
