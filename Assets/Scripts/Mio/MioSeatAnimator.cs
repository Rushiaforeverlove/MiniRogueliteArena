using System.Collections.Generic;
using UnityEngine;
using AbyssBar.Presentation;

namespace AbyssBar.Mio
{
    [DisallowMultipleComponent]
    public sealed class MioSeatAnimator : MonoBehaviour
    {
        [SerializeField] private MioCharacterDefinition definition;
        [SerializeField] private RisingBubbleSettings sleepBubbles = new RisingBubbleSettings();
        private SpriteRenderer source;private MeshRenderer view;private Mesh mesh;private Material material;
        private Vector3[] resting,vertices;private float[] fractions;private RisingBubbleEmitter bubbleEmitter;private float age;
        private void Awake(){source=GetComponent<SpriteRenderer>();}
        private void OnEnable(){age=0;if(bubbleEmitter!=null){bubbleEmitter.ResetClock();bubbleEmitter.SetVisible(true);}}
        private void Build(Sprite sprite)
        {
            var go=new GameObject("Mio seated breathing");go.layer=gameObject.layer;go.transform.SetParent(transform,false);view=go.AddComponent<MeshRenderer>();var filter=go.AddComponent<MeshFilter>();
            material=definition.seatedMaterial!=null?new Material(definition.seatedMaterial):new Material(Shader.Find("Sprites/Default"));material.mainTexture=sprite.texture;view.sharedMaterial=material;
            var rows=new List<float>();for(int i=0;i<=50;i++)rows.Add(i/50f);rows.Add(.27f);rows.Sort();fractions=rows.ToArray();
            resting=new Vector3[rows.Count*2];vertices=new Vector3[resting.Length];var uv=new Vector2[resting.Length];var colors=new Color[resting.Length];var triangles=new int[(rows.Count-1)*6];
            float w=sprite.rect.width/sprite.pixelsPerUnit,h=sprite.rect.height/sprite.pixelsPerUnit;Vector2 pivot=new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height);
            for(int r=0;r<rows.Count;r++)for(int c=0;c<2;c++){int n=r*2+c;resting[n]=new Vector3((c-pivot.x)*w,(1-rows[r]-pivot.y)*h,0);uv[n]=new Vector2((sprite.rect.x+c*sprite.rect.width)/sprite.texture.width,(sprite.rect.y+(1-rows[r])*sprite.rect.height)/sprite.texture.height);colors[n]=Color.white;}
            for(int r=0;r<rows.Count-1;r++){int n=r*6,v=r*2;triangles[n]=v;triangles[n+1]=v+2;triangles[n+2]=v+1;triangles[n+3]=v+1;triangles[n+4]=v+2;triangles[n+5]=v+3;}
            mesh=new Mesh{name="Mio pinned seated mesh"};mesh.vertices=resting;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();filter.sharedMesh=mesh;
            var bubbles=new GameObject("Independent rising sleep bubbles");bubbles.layer=gameObject.layer;bubbles.transform.SetParent(transform,false);
            bubbleEmitter=bubbles.AddComponent<RisingBubbleEmitter>();bubbleEmitter.Initialize(source,sprite,material,sleepBubbles);
        }
        private void LateUpdate()
        {
            if(definition==null||source==null)return;var sprite=definition.Find("seat.sleep")?.frames[0].sprite;if(sprite==null)return;if(view==null)Build(sprite);
            age+=Time.deltaTime;float breath=(.5f-.5f*Mathf.Cos(age*Mathf.PI*2/3.6f))*.014f;
            for(int r=0;r<fractions.Length;r++){float n=fractions[r],offset=n<.6f?breath*Mathf.Sin(Mathf.PI*n/.6f)*(1-.92f*Mathf.Exp(-Mathf.Pow((n-.27f)/.025f,2))):0;for(int c=0;c<2;c++){int i=r*2+c;vertices[i]=resting[i]+Vector3.up*offset;}}
            mesh.vertices=vertices;mesh.RecalculateBounds();view.enabled=true;view.sortingLayerID=source.sortingLayerID;view.sortingOrder=source.sortingOrder;material.color=source.color;source.enabled=false;
            bubbleEmitter.Advance(Time.deltaTime);
        }
        private void OnDisable(){if(view!=null)view.enabled=false;if(bubbleEmitter!=null)bubbleEmitter.SetVisible(false);if(source!=null)source.enabled=true;}
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
