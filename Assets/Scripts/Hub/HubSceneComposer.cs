using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssBar.Hub
{
    // Builds visuals around existing feature roots. IDs and feature ownership remain unchanged.
    public sealed class HubSceneComposer:MonoBehaviour
    {
        public const string RootName="BarHub modular art";
        private HubArtCatalog art;
        private HubLayoutData layout;
        private GameObject[] sceneRoots;
        private bool visualPreview;
        private readonly List<Renderer> suppressedRenderers=new List<Renderer>();
        private readonly Dictionary<Texture,Material> materials=new Dictionary<Texture,Material>();
        [SerializeField,HideInInspector] private List<Material> ownedMaterials=new List<Material>();
        [SerializeField,HideInInspector] private List<Sprite> ownedSprites=new List<Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Subscribe(){SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;}
        private void OnEnable(){if(Application.isPlaying)Subscribe();}
        private static void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(scene.name!="BarHub"||scene.GetRootGameObjects().Any(g=>g.name==RootName))return;
            var catalog=HubArtCatalog.Load();var data=HubLayoutData.Load();if(catalog==null||data==null)return;
            var root=new GameObject(RootName);SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<HubSceneComposer>().Compose(scene,catalog,data);
        }
        public void Compose(Scene scene,HubArtCatalog catalog,HubLayoutData definition,bool previewOnly=false)
        {
            visualPreview=previewOnly;art=catalog;layout=definition;sceneRoots=scene.GetRootGameObjects();
            var bounds=gameObject.AddComponent<HubSceneBounds>();bounds.area=layout.bounds;
            HideOld("BarBackground");HideOld("Walkway");HideOld("BarCounter");
            foreach(var go in sceneRoots)if(go.name.StartsWith("Stool_"))HideOld(go.name);
            var wall=art.Find("wall");
            if(wall?.sprite!=null)
            {
                float width=wall.opaqueSize.x;
                for(float x=layout.bounds.xMin;x<layout.bounds.xMax;x+=width)
                    Make("wall",new Vector2(x+width*.5f,layout.bounds.yMin),HubRenderLayers.Background,transform,"Wall module");
            }
            var counter=art.Find("counter");
            var back=art.Find("backbar");
            var runs=layout.counterRuns!=null&&layout.counterRuns.Length>0?layout.counterRuns:new[]{new HubCounterRun{id="bar",start=layout.counterStart,end=layout.counterEnd}};
            foreach(var run in runs)
            {
                MakeStrip("counter",run,layout.counterFloorY,layout.counterHeight,HubRenderLayers.Counter,"Counter module");
                if(back!=null)MakeStrip("backbar",run,layout.counterFloorY+layout.counterHeight,back.opaqueSize.y,HubRenderLayers.BackFurniture,"Bottle shelf module");
            }
            foreach(var prop in layout.props)
            {
                var root=visualPreview?null:FindRoot(prop.legacyName);
                if(visualPreview)HideOld(prop.legacyName);
                if(root==null){root=new GameObject(prop.id);root.transform.SetParent(transform,false);}
                var old=root.GetComponent<SpriteRenderer>();if(old!=null)old.enabled=false;
                root.transform.localScale=Vector3.one;root.transform.position=new Vector3(prop.position.x,prop.position.y,0);
                Make(prop.artId,prop.position,prop.sortingOrder,root.transform,"Visual "+prop.id);
            }
            var owner=sceneRoots.SelectMany(g=>g.GetComponentsInChildren<HubCharacterSelection>(true)).FirstOrDefault();
            foreach(var slot in layout.seats)
            {
                Make("stool",new Vector2(slot.x,layout.floorY),HubRenderLayers.Chair,transform,"Shared chair "+slot.id);
                Make("stool_back",new Vector2(slot.x,layout.floorY),HubRenderLayers.ChairBack,transform,"Shared chair back "+slot.id);
                MakeChairForeground(new Vector2(slot.x,layout.floorY),slot.id);
                if(slot.character==null)continue;
                if(visualPreview){HideOld(slot.legacySeatName);MakePreviewSeat(slot);continue;}
                var seatRoot=FindRoot(slot.legacySeatName);
                if(seatRoot==null)
                {
                    seatRoot=new GameObject("Seated "+slot.id);seatRoot.transform.SetParent(transform,false);
                    var seat=seatRoot.AddComponent<HubCharacterSeat>();seat.Configure(slot.character,owner);owner?.RegisterSeat(seat);
                }
                seatRoot.transform.position=new Vector3(slot.x,seatRoot.transform.position.y,0);
                var sr=seatRoot.GetComponent<SpriteRenderer>();if(sr!=null)sr.sortingOrder=HubRenderLayers.SeatedActor;
                var characterArt=AbyssBar.Presentation.CharacterPresentationDefinition.Load(slot.character);
                if(characterArt!=null)
                {
                    seatRoot.transform.localScale=Vector3.one;
                    seatRoot.transform.position=new Vector3(slot.x,layout.floorY+layout.seatSurfaceHeight,0);
                    (seatRoot.GetComponent<AbyssBar.Presentation.CharacterSeatPresenter>()??seatRoot.AddComponent<AbyssBar.Presentation.CharacterSeatPresenter>()).Configure(slot.character);
                    continue;
                }
                // Mio retains her approved pinned breathing and rising bubbles.
                if(slot.alignSeatedContacts)
                {
                    float rise=slot.localTableContact.y-slot.localHip.y;
                    float scale=(layout.counterFloorY+layout.counterHeight-layout.floorY-layout.seatSurfaceHeight)/Mathf.Max(.01f,rise);
                    seatRoot.transform.localScale=Vector3.one*scale;
                    seatRoot.transform.position=new Vector3(slot.x-slot.localHip.x*scale,layout.floorY+layout.seatSurfaceHeight-slot.localHip.y*scale,0);
                }
                else
                {
                    seatRoot.transform.localScale=Vector3.one*.7f;
                    seatRoot.transform.position=new Vector3(slot.x,layout.floorY+1.25f,0);
                }
            }
            foreach(var slot in layout.contentSlots)
            {
                var go=new GameObject("Content slot "+slot.id);go.transform.SetParent(transform,false);go.transform.position=slot.position;
                if(!visualPreview)go.AddComponent<HubContentSlot>().Configure(slot.id,slot.contentPrefab);
                if(slot.contentPrefab==null&&!string.IsNullOrWhiteSpace(slot.decorationArtId))Make(slot.decorationArtId,slot.position,HubRenderLayers.BackFurniture,go.transform,"Vacant event stand");
            }
            if(visualPreview){HideOld("HubPlayer");return;}
            var player=FindRoot("HubPlayer");
            if(player!=null)
            {
                player.transform.position=new Vector3(player.transform.position.x,layout.playerY,0);
                var move=player.GetComponent<HubPlayerMovement>();move?.SetBounds(layout.bounds.xMin+2,layout.bounds.xMax-2);
                move?.SetWalkingSpeed(layout.walkingSpeed);
                var sr=player.GetComponent<SpriteRenderer>();if(sr!=null)sr.sortingOrder=HubRenderLayers.WalkingActor;
            }
            var spawn=FindRoot("HubReturnPoint");if(spawn!=null)spawn.transform.position=new Vector3(layout.returnX,layout.playerY,0);
        }
        private GameObject FindRoot(string name)=>string.IsNullOrEmpty(name)?null:sceneRoots.FirstOrDefault(g=>g.name==name);
        // Static render-only stand-ins. No gameplay, unlock, UI or actor components execute in edit mode.
        private void MakePreviewSeat(HubSeatPlacement slot)
        {
            var go=new GameObject("Preview seated "+slot.character.DisplayName);go.transform.SetParent(transform,false);
            var data=AbyssBar.Presentation.CharacterPresentationDefinition.Load(slot.character);
            var at=new Vector3(slot.x,layout.floorY+layout.seatSurfaceHeight,0);
            var tint=CharacterSkinSave.GetTint(slot.character);
            if(data!=null)
            {
                float scale=data.hubHeight/1.85f*data.seatedScale;
                var low=new GameObject("Fixed lower body");low.transform.SetParent(go.transform,false);
                var upper=new GameObject("Seated upper body");upper.transform.SetParent(go.transform,false);
                AbyssBar.Presentation.CharacterPresentationView.Apply(low.AddComponent<SpriteRenderer>(),data.seatLower,at+(Vector3)(data.seatLowerOffset*scale),scale,false,tint,HubRenderLayers.SeatedActor,0,data.MaterialFor(data.seatLower.texture));
                var frame=data.seatUpper.At(0);
                AbyssBar.Presentation.CharacterPresentationView.Apply(upper.AddComponent<SpriteRenderer>(),frame,at,scale,false,tint,HubRenderLayers.SeatedActor+1,0,data.MaterialFor(frame.texture));
                return;
            }
            var mio=AbyssBar.Mio.MioCharacterDefinition.Load();
            var sprite=mio!=null&&mio.Matches(slot.character)?mio.Find("seat.sleep")?.frames[0].sprite:CharacterSkinSave.GetSeatedSprite(slot.character);
            if(sprite==null)return;
            float actorScale=.7f;
            if(slot.alignSeatedContacts)
            {
                actorScale=(layout.counterFloorY+layout.counterHeight-layout.floorY-layout.seatSurfaceHeight)/Mathf.Max(.01f,slot.localTableContact.y-slot.localHip.y);
                at=new Vector3(slot.x-slot.localHip.x*actorScale,layout.floorY+layout.seatSurfaceHeight-slot.localHip.y*actorScale,0);
            }
            else at=new Vector3(slot.x,layout.floorY+1.25f,0);
            go.transform.position=at;go.transform.localScale=Vector3.one*actorScale;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=SceneSprite(sprite);sr.color=tint;sr.sortingOrder=HubRenderLayers.SeatedActor;
            sr.sharedMaterial=SpriteMaterial(sprite.texture,mio!=null&&mio.Matches(slot.character)?mio.seatedMaterial:null);
        }
        private void MakeChairForeground(Vector2 at,string seatId)
        {
            foreach(var part in art.chairForeground)
            {
                var entry=art.Find(part.sourceArtId);
                if(part.mesh==null||entry?.sprite==null)continue;
                var go=new GameObject("Chair front "+seatId+" / "+part.id);
                go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(at.x+entry.originOffset.x,at.y+entry.originOffset.y,0);
                go.AddComponent<MeshFilter>().sharedMesh=part.mesh;
                var renderer=go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial=SpriteMaterial(entry.sprite.texture);renderer.sortingOrder=part.sortingOrder;
                var binding=new MaterialPropertyBlock();binding.SetTexture("_MainTex",entry.sprite.texture);renderer.SetPropertyBlock(binding);
            }
        }
        private void HideOld(string name)
        {
            var go=FindRoot(name);if(go==null)return;var sr=go.GetComponent<SpriteRenderer>();if(sr==null)return;
            if(!visualPreview){sr.enabled=false;return;}
            if(!sr.forceRenderingOff){suppressedRenderers.Add(sr);sr.forceRenderingOff=true;}
        }
        private SpriteRenderer Make(string id,Vector2 at,int order,Transform parent,string name)
        {
            var entry=art.Find(id);if(entry?.sprite==null)return null;
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=new Vector3(at.x+entry.originOffset.x,at.y+entry.originOffset.y,0);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=SceneSprite(entry.sprite);renderer.sharedMaterial=SpriteMaterial(renderer.sprite.texture);renderer.sortingOrder=order;
            return renderer;
        }
        private void MakeStrip(string id,HubCounterRun run,float floor,float height,int order,string label)
        {
            var entry=art.Find(id);if(entry?.sprite==null||run.end<=run.start)return;
            var template=entry.sprite;float ppu=template.pixelsPerUnit;
            // Crop only the final module, preserving pixels rather than stretching
            // a complete shelf. Both strips terminate at the exact same run.end.
            for(float x=run.start;x<run.end-.001f;)
            {
                float width=Mathf.Min(entry.opaqueSize.x,run.end-x);
                string spriteName=template.name+" strip "+Mathf.RoundToInt(width*ppu);
                var sprite=ownedSprites.FirstOrDefault(s=>s!=null&&s.name==spriteName);
                if(sprite==null)
                {
                    float left=template.rect.x+template.pivot.x-entry.opaqueSize.x*ppu*.5f;
                    var rect=new Rect(left,template.rect.y,width*ppu,template.rect.height);
                    sprite=Sprite.Create(template.texture,rect,new Vector2(.5f,template.pivot.y/template.rect.height),ppu,0,SpriteMeshType.FullRect);
                    sprite.name=spriteName;ownedSprites.Add(sprite);
                }
                var go=new GameObject(label+" "+run.id);go.transform.SetParent(transform,false);
                go.transform.position=new Vector3(x+width*.5f,floor,0);go.transform.localScale=new Vector3(1,height/entry.opaqueSize.y,1);
                var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=SpriteMaterial(sprite.texture);renderer.sortingOrder=order;
                x+=width;
            }
        }
        private Sprite SceneSprite(Sprite template)
        {
            var sprite=ownedSprites.FirstOrDefault(s=>s!=null&&s.name==template.name);
            if(sprite!=null)return sprite;
            // Recreate native sprite geometry/UVs from its portable slice metadata.
            // Do not depend on a .asset sub-sprite's imported render-data cache from
            // another project/Library; every matching chair still shares one sprite.
            var rect=template.rect;
            sprite=Sprite.Create(template.texture,rect,new Vector2(template.pivot.x/rect.width,template.pivot.y/rect.height),template.pixelsPerUnit,0,SpriteMeshType.FullRect);
            sprite.name=template.name;ownedSprites.Add(sprite);return sprite;
        }
        private Material SpriteMaterial(Texture texture,Material template=null)
        {
            if(!materials.TryGetValue(texture,out var material))
            {
                material=ownedMaterials.FirstOrDefault(m=>m!=null&&m.mainTexture==texture);
                if(material==null){material=new Material(template!=null?template:art.spriteMaterial){name="Hub texture "+texture.name,mainTexture=texture};ownedMaterials.Add(material);}
                materials.Add(texture,material);
            }
            return material;
        }
        private void OnDestroy()=>ReleaseVisuals();
        public void ReleaseVisuals()
        {
            foreach(var renderer in suppressedRenderers)if(renderer!=null)renderer.forceRenderingOff=false;
            suppressedRenderers.Clear();
            foreach(var material in ownedMaterials)if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
            foreach(var sprite in ownedSprites)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
            ownedMaterials.Clear();ownedSprites.Clear();materials.Clear();
        }
    }
}
