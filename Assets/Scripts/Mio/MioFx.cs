using AbyssBar.Upgrades;
using UnityEngine;

namespace AbyssBar.Mio
{
    public sealed class MioFx : MonoBehaviour
    {
        private MioCharacterDefinition definition;
        private SpriteRenderer view, echo;
        private Vector3 position;
        private Vector2 direction;
        private string key;
        private float age,lifetime,radius,lineWidth;
        private bool line;
        private static int liveCount;
        public static MioFx Play(MioCharacterDefinition data,string key,Vector2 position,Vector2 direction,float radius,float seconds,Transform owner)
        {
            if(data==null||owner==null||data.Find(key)==null||liveCount>=128)return null;
            var go=new GameObject("Mio "+key);go.transform.SetParent(owner,false);go.layer=owner.gameObject.layer;go.AddComponent<RunOwnedObject>();
            var fx=go.AddComponent<MioFx>();liveCount++;fx.definition=data;fx.key=key;fx.radius=radius;fx.lifetime=Mathf.Max(.05f,seconds);
            fx.position=new Vector3(position.x,position.y,owner.position.z);fx.direction=direction;
            fx.view=go.AddComponent<SpriteRenderer>();var source=owner.GetComponent<SpriteRenderer>();
            if(source!=null){fx.view.sharedMaterial=source.sharedMaterial;fx.view.sortingLayerID=source.sortingLayerID;fx.view.sortingOrder=source.sortingOrder+(MioVisuals.Behind(key)?-1:3);}
            if(key.EndsWith("slash")){var child=new GameObject("Trailing edge");child.layer=go.layer;child.transform.SetParent(go.transform,false);fx.echo=child.AddComponent<SpriteRenderer>();fx.echo.sharedMaterial=fx.view.sharedMaterial;fx.echo.sortingLayerID=fx.view.sortingLayerID;fx.echo.sortingOrder=fx.view.sortingOrder-1;}
            fx.Draw();return fx;
        }
        public static void Line(MioCharacterDefinition data,bool released,Vector2 from,Vector2 to,float seconds,Transform owner)
        {var fx=Play(data,released?"rage.slash":"normal.slash",(from+to)*.5f,to-from,1,seconds,owner);if(fx!=null){fx.line=true;fx.lineWidth=Mathf.Max(.3f,Vector2.Distance(from,to));fx.Draw();}}
        private void Update(){if(Time.timeScale<=0)return;age+=Time.deltaTime;if(age>=lifetime){Destroy(gameObject);return;}Draw();}
        private void Draw()
        {
            MioVisuals.Draw(view,definition,key,age,lifetime,position,direction,radius);
            if(line&&view.sprite!=null){view.transform.position=position;view.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);var parent=transform.parent.lossyScale;view.transform.localScale=new Vector3(lineWidth/view.sprite.bounds.size.x/Mathf.Abs(parent.x),.22f/view.sprite.bounds.size.y/Mathf.Abs(parent.y),1);}
            if(echo!=null){echo.enabled=!line; if(!line){MioVisuals.Draw(echo,definition,key,age,lifetime,position,direction,radius*.9f,.32f);echo.transform.rotation*=Quaternion.Euler(0,0,-5);}}
        }
        private void OnDestroy(){liveCount=Mathf.Max(0,liveCount-1);}
    }
}
