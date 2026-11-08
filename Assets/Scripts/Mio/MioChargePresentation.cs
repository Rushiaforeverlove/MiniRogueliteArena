using TMPro;
using UnityEngine;

namespace AbyssBar.Mio
{
    [DefaultExecutionOrder(600)]
    public sealed class MioChargePresentation : MonoBehaviour
    {
        private MioCharacterDefinition definition;private MioCombat combat;
        private SpriteRenderer aura,background,fill;private TextMeshPro label;private GameObject root;private Sprite white;
        public void Configure(MioCharacterDefinition value){definition=value;if(value==null&&root!=null)root.SetActive(false);}
        private SpriteRenderer Renderer(string name,SpriteRenderer source,int offset)
        {var go=new GameObject(name);go.transform.SetParent(root.transform,false);var s=go.AddComponent<SpriteRenderer>();s.sharedMaterial=source.sharedMaterial;s.sortingLayerID=source.sortingLayerID;s.sortingOrder=source.sortingOrder+offset;return s;}
        private void LateUpdate()
        {
            if(combat==null)combat=GetComponent<MioCombat>();bool active=definition!=null&&combat!=null&&combat.enabled&&combat.IsCharging;
            if(!active){if(root!=null)root.SetActive(false);return;}
            var source=GetComponent<SpriteRenderer>();if(root==null)
            {
                root=new GameObject("Mio charge feedback");root.transform.SetParent(transform,false);
                white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
                aura=Renderer("Charge aura",source,-1);background=Renderer("Charge meter",source,50);fill=Renderer("Charge progress",source,51);background.sprite=fill.sprite=white;
                var text=new GameObject("Charge label");text.transform.SetParent(root.transform,false);label=text.AddComponent<TextMeshPro>();label.fontSize=2;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(1,.3f);label.GetComponent<MeshRenderer>().sortingLayerID=source.sortingLayerID;label.GetComponent<MeshRenderer>().sortingOrder=source.sortingOrder+52;
            }
            root.SetActive(true);float p=combat.ChargeProgress;bool full=p>=.999f;var color=full?new Color(1,.85f,.4f):combat.Released?new Color(1,.35f,.5f):new Color(.5f,.85f,1);
            var collider=GetComponent<Collider2D>();float floor=collider!=null?collider.bounds.min.y+.025f:transform.position.y;
            var at=new Vector3(transform.position.x,floor+2.45f,transform.position.z);
            SetRect(background,at,new Vector2(1.02f,.12f),new Color(.04f,.08f,.14f));SetRect(fill,at+Vector3.left*(1-p)*.49f,new Vector2(.98f*p,.075f),color);
            label.transform.position=at+Vector3.up*.2f;var parent=transform.lossyScale;label.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(parent.x)),1/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);label.text=full?"MAX":Mathf.RoundToInt(p*100)+"%";label.color=color;
            MioVisuals.Draw(aura,definition,(combat.Released?"rage.":"normal.")+(full?"charge_full":"charge_gather"),Time.time,0,new Vector3(transform.position.x,floor,transform.position.z),Vector2.right,full?1.2f:.85f+p*.25f,full?.9f:.4f+p*.4f);
        }
        private static void SetRect(SpriteRenderer s,Vector3 at,Vector2 size,Color color){var parent=s.transform.parent.lossyScale;s.transform.position=at;s.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,Mathf.Abs(parent.x)),size.y/Mathf.Max(.001f,Mathf.Abs(parent.y)),1);s.color=color;}
        private void OnDisable(){if(root!=null)root.SetActive(false);}
        private void OnDestroy(){if(white!=null)Destroy(white);}
    }
}
