using UnityEngine;

namespace AbyssBar.Mio
{
    // Shared by live combat and the detached character-select demonstration.
    public static class MioVisuals
    {
        public static bool Behind(string key) => key.EndsWith("spin_fx") || key.EndsWith("gather") || key.EndsWith("full") || key=="fx.pulse" || key=="fx.fade";
        public static void Draw(SpriteRenderer view, MioCharacterDefinition data, string key, float age, float duration,
            Vector3 anchor, Vector2 direction, float radius, float alpha=1)
        {
            var frame=data.Find(key)?.At(age,duration); view.enabled=frame?.sprite!=null;
            if(!view.enabled)return;view.sprite=frame.sprite;view.color=new Color(1,1,1,Mathf.Clamp01(alpha));
            if(data.energyMaterial!=null)view.sharedMaterial=data.energyMaterial;
            Vector2 size=Vector2.one*radius*2, offset=new Vector2(0,data.tuning.visualAimHeight);float angle=0;
            if(key.EndsWith("slash")){size=new Vector2(radius*.8f,radius*1.4f*data.tuning.groundDepth);offset+=direction.normalized*radius*.65f;angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;}
            else if(key.EndsWith("spin_fx")||key=="fx.pulse"){size.y*=data.tuning.groundDepth;offset.y=.1f;}
            else if(key.EndsWith("gather")||key.EndsWith("full")||key=="fx.fade"){size.y*=.8f;offset.y=.45f;}
            view.transform.position=anchor+(Vector3)offset;view.transform.rotation=Quaternion.Euler(0,0,angle);
            var parent=view.transform.parent!=null?view.transform.parent.lossyScale:Vector3.one;
            var bounds=view.sprite.bounds.size;
            view.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,bounds.x*Mathf.Abs(parent.x)),size.y/Mathf.Max(.001f,bounds.y*Mathf.Abs(parent.y)),1);
        }
    }
}
