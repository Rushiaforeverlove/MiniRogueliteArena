using UnityEngine;
namespace AbyssBar.Presentation
{
    // Shared by native combat, hub actors and the isolated selection demonstration.
    public static class CharacterPresentationView
    {
        private static readonly MaterialPropertyBlock textureBinding=new MaterialPropertyBlock();
        private static void BindTexture(SpriteRenderer renderer,Texture texture)
        {
            renderer.GetPropertyBlock(textureBinding);textureBinding.SetTexture("_MainTex",texture);renderer.SetPropertyBlock(textureBinding);textureBinding.Clear();
        }
        public static void Draw(SpriteRenderer view,CharacterPresentationDefinition data,string key,float age,
            Vector3 feet,float height,bool left,Color tint,int order,int layer,float duration=0)
        {
            var frame=data?.Find(key)?.At(age,duration);
            Apply(view,frame,feet,height/1.85f,left,tint,order,layer,data!=null?data.MaterialFor(frame?.texture):null);
        }
        public static void Apply(SpriteRenderer view,CharacterFrame frame,Vector3 at,float scale,bool left,Color tint,int order,int layer,Material material)
        {
            view.sprite=frame?.Sprite;view.enabled=view.sprite!=null;if(!view.enabled)return;
            view.sharedMaterial=material;view.color=tint;view.flipX=left;view.sortingOrder=order;view.sortingLayerID=layer;
            BindTexture(view,frame.texture);
            view.spriteSortPoint=SpriteSortPoint.Pivot;view.transform.position=at;view.transform.rotation=Quaternion.identity;
            var parent=view.transform.parent!=null?view.transform.parent.lossyScale:Vector3.one;
            view.transform.localScale=new Vector3(scale/Mathf.Max(.001f,Mathf.Abs(parent.x)),scale/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);
        }
        public static void Effect(SpriteRenderer view,CharacterPresentationDefinition data,string key,float age,
            Vector3 at,Vector2 size,float angle=0,float duration=0)
        {
            var frame=data?.Find(key)?.At(age,duration);view.sprite=frame?.Sprite;view.enabled=view.sprite!=null;if(!view.enabled)return;
            view.sharedMaterial=data.MaterialFor(frame.texture,true);view.color=Color.white;view.flipX=false;
            BindTexture(view,frame.texture);
            view.transform.position=at;view.transform.rotation=Quaternion.Euler(0,0,angle);
            var p=view.transform.parent!=null?view.transform.parent.lossyScale:Vector3.one;var b=view.sprite.bounds.size;
            view.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,b.x*Mathf.Abs(p.x)),size.y/Mathf.Max(.001f,b.y*Mathf.Abs(p.y)),1);
        }
    }
}
