using AbyssBar.Hub;
using UnityEngine;
namespace AbyssBar.Presentation
{
    [DefaultExecutionOrder(550),DisallowMultipleComponent]
    public sealed class CharacterSeatPresenter:MonoBehaviour
    {
        private CharacterPresentationDefinition data;private SpriteRenderer source,upper,lower;
        private CharacterSkinData viewed;private float age;
        public SpriteRenderer Upper=>upper;public SpriteRenderer Lower=>lower;
        public void Configure(CharacterData character)
        {
            data=CharacterPresentationDefinition.Load(character);source=GetComponent<SpriteRenderer>();viewed=null;
            if(data==null){enabled=false;if(upper!=null)upper.enabled=false;if(lower!=null)lower.enabled=false;return;}
            enabled=true;age=0;
            if(upper==null){upper=Create("Seated upper body");lower=Create("Fixed seated lower body");}
            Draw();
        }
        public void PreviewSkin(CharacterSkinData skin){viewed=skin;}
        private SpriteRenderer Create(string label){var go=new GameObject(label);go.layer=gameObject.layer;go.transform.SetParent(transform,false);return go.AddComponent<SpriteRenderer>();}
        private void OnEnable(){age=0;}
        private void LateUpdate(){age+=Time.deltaTime;Draw();}
        private void Draw()
        {
            if(data==null||source==null||upper==null)return;
            var layout=HubLayoutData.Load();float hip=(layout!=null?layout.floorY+layout.seatSurfaceHeight:-2.21f);
            var at=new Vector3(transform.position.x,hip,transform.position.z);var tint=viewed!=null?viewed.Tint:source.color;
            float scale=data.hubHeight/1.85f*data.seatedScale;
            CharacterPresentationView.Apply(lower,data.seatLower,at+(Vector3)(data.seatLowerOffset*scale),scale,false,tint,source.sortingOrder,source.sortingLayerID,data.MaterialFor(data.seatLower.texture));
            float phase=Mathf.Max(0,age-5);
            bool special=age>=5;
            if(special&&data.seatedRestSeconds>0)
            {
                phase=Mathf.Repeat(phase,data.seatSpecialUpper.duration+data.seatedRestSeconds);
                special=phase<data.seatSpecialUpper.duration;
            }
            var clip=special?data.seatSpecialUpper:data.seatUpper;
            var frame=clip.At(special?phase:age);
            CharacterPresentationView.Apply(upper,frame,at,scale,false,tint,source.sortingOrder+1,source.sortingLayerID,data.MaterialFor(frame.texture));
            source.enabled=false;
        }
        private void OnDisable(){if(upper!=null)upper.enabled=false;if(lower!=null)lower.enabled=false;}
    }
}
