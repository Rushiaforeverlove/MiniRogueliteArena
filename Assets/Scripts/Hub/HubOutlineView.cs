using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Hub
{
    // Renderer adapter: one target may contain a sprite hierarchy or a breathing mesh.
    public sealed class HubOutlineView:MonoBehaviour,IHubHighlightView
    {
        private sealed class Part
        { public Renderer source,outline;public Material material;public GameObject root; }
        private readonly List<Part> parts=new List<Part>();
        private bool focused;
        public void SetFocused(bool value)
        {
            focused=value;
            if(value&&parts.Count==0)Build();
            foreach(var part in parts)if(part.root!=null)part.root.SetActive(value);
            if(value)Draw();
        }
        private void Build()
        {
            var template=HubArtCatalog.Load()?.outlineMaterial;var shader=Shader.Find("AbyssBar/HubOutline");if(template==null&&shader==null)return;
            foreach(var source in GetComponentsInChildren<Renderer>())
            {
                if(!source.enabled||source.name.StartsWith("__HubOutline"))continue;
                if(!(source is SpriteRenderer)&&!(source is MeshRenderer&&source.GetComponent<MeshFilter>()!=null))continue;
                var root=new GameObject("__HubOutline "+source.name);root.layer=gameObject.layer;root.transform.SetParent(source.transform,false);
                var material=template!=null?new Material(template):new Material(shader);Renderer outline;
                if(source is SpriteRenderer)outline=root.AddComponent<SpriteRenderer>();
                else{root.AddComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;outline=root.AddComponent<MeshRenderer>();}
                outline.sharedMaterial=material;parts.Add(new Part{source=source,outline=outline,material=material,root=root});
            }
        }
        private void LateUpdate(){if(focused)Draw();}
        private void Draw()
        {
            foreach(var part in parts)
            {
                if(part.source==null||part.outline==null)continue;
                part.outline.enabled=focused&&part.source.enabled;
                part.outline.sortingLayerID=part.source.sortingLayerID;part.outline.sortingOrder=part.source.sortingOrder+1;
                Sprite sprite=null;
                if(part.source is SpriteRenderer sr)
                {
                    var target=(SpriteRenderer)part.outline;target.sprite=sr.sprite;target.flipX=sr.flipX;target.flipY=sr.flipY;target.drawMode=sr.drawMode;target.size=sr.size;sprite=sr.sprite;
                }
                else
                {
                    part.outline.GetComponent<MeshFilter>().sharedMesh=part.source.GetComponent<MeshFilter>().sharedMesh;
                    sprite=GetComponent<SpriteRenderer>()?.sprite;
                }
                Texture texture=sprite!=null?sprite.texture:part.source.sharedMaterial!=null?part.source.sharedMaterial.mainTexture:null;
                if(texture==null)continue;part.material.mainTexture=texture;
                Rect uv=sprite!=null?new Rect(sprite.rect.x/texture.width,sprite.rect.y/texture.height,sprite.rect.width/texture.width,sprite.rect.height/texture.height):new Rect(0,0,1,1);
                part.material.SetVector("_UvRect",new Vector4(uv.xMin,uv.yMin,uv.xMax,uv.yMax));
                // Texture resolution must not make large source artwork lose its interaction cue.
                float pixelsPerUnit=sprite!=null?sprite.pixelsPerUnit:100;
                part.material.SetFloat("_Radius",Mathf.Clamp(.03f*pixelsPerUnit/Mathf.Max(.01f,Mathf.Abs(part.source.transform.lossyScale.y)),1,36));
            }
        }
        private void OnDisable()=>SetFocused(false);
        private void OnDestroy(){foreach(var p in parts)if(p.material!=null)Destroy(p.material);}
    }
}
