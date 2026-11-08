#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssBar.Hub;
using AbyssBar.Mio;
using UnityEditor;
using UnityEngine;

public static class HubAssetBuilder
{
    const string Art="Assets/Art/Hub/V1/",ResourcesPath="Assets/Resources/Hub/";
    static readonly Dictionary<string,Texture2D> readbacks=new Dictionary<string,Texture2D>();
    [MenuItem("Abyss Bar/Hub/Build Modular Art Data")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before rebuilding hub data.");
        Folder(ResourcesPath.TrimEnd('/'));
        var catalog=Asset<HubArtCatalog>(ResourcesPath+"HubArtCatalog.asset");
        var entries=new List<HubArtEntry>();
        try
        {
            foreach(var pair in new[]{new Spec("wall",12.5f),new Spec("counter",1.2f),new Spec("stool",1.15f),new Spec("backbar",2.3f),new Spec("board",2.3f),new Spec("wardrobe",2.45f),new Spec("gacha",2.05f),new Spec("door",2.9f),new Spec("event",2.1f),new Spec("bartender",1.9f),new Spec("bench",1.15f)})
            {
                string path=Art+pair.id+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Missing hub artwork: "+path);
                importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=4096;importer.isReadable=true;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Rect visible=Opaque(texture,new Rect(0,0,texture.width,texture.height));
                float ppu=visible.height/pair.height;
                Rect rect=new Rect(Mathf.Max(0,visible.xMin-72),Mathf.Max(0,visible.yMin-72),0,0);
                rect.width=Mathf.Min(texture.width,visible.xMax+72)-rect.x;rect.height=Mathf.Min(texture.height,visible.yMax+72)-rect.y;
                var pivot=new Vector2((visible.center.x-rect.x)/rect.width,(visible.yMin-rect.y)/rect.height);
                var sprite=SpriteAsset(catalog,pair.id,texture,rect,pivot,ppu);
                entries.Add(new HubArtEntry{id=pair.id,sprite=sprite,opaqueSize=new Vector2(visible.width/ppu,pair.height)});
                if(pair.id=="stool")
                {
                    float cut=Mathf.Floor(visible.yMin+visible.height*.72f);var upper=new Rect(rect.x,cut,rect.width,rect.yMax-cut);
                    var back=SpriteAsset(catalog,"stool_back",texture,upper,new Vector2(pivot.x,0),ppu);
                    entries.Add(new HubArtEntry{id="stool_back",sprite=back,opaqueSize=new Vector2(visible.width/ppu,(visible.yMax-cut)/ppu),originOffset=new Vector2(0,(cut-visible.yMin)/ppu)});
                }
            }
            catalog.entries=entries.ToArray();
            catalog.spriteMaterial=Material(ResourcesPath+"HubSprite.mat","AbyssBar/HubSprite");catalog.outlineMaterial=Material(ResourcesPath+"HubOutline.mat","AbyssBar/HubOutline");
            var scout=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/Character_Scout.asset");
            var guard=AssetDatabase.LoadAssetAtPath<CharacterData>("Assets/Data/Characters/Character_Guard.asset");
            var mio=MioCharacterDefinition.Load();if(scout==null||guard==null||mio==null)throw new InvalidOperationException("Existing character data required.");
            var measures=new List<SpriteBodyMeasure>();
            foreach(var sprite in new[]{scout.GameplaySprite,guard.GameplaySprite}.Where(s=>s!=null).Distinct())measures.Add(new SpriteBodyMeasure{sprite=sprite,localVisibleBounds=Measure(sprite)});
            catalog.bodyMeasures=measures.ToArray();
            var hubFrame=mio.Find("hub.idle").frames[0];var battleFrame=mio.Find("normal.idle").frames[0];
            catalog.characterSizes=new[]{new CharacterSizeProfile{characterId=scout.CharacterId,hubHeight=1.85f,battleHeight=1.85f},new CharacterSizeProfile{characterId=guard.CharacterId,hubHeight=2.05f,battleHeight=1.85f},new CharacterSizeProfile{characterId=MioCharacterDefinition.RoleId,hubHeight=1.63f,battleHeight=1.85f,mioHubReferenceHeight=Measure(hubFrame.sprite).height*hubFrame.scale*mio.visualScale,mioBattleReferenceHeight=Measure(battleFrame.sprite).height*battleFrame.scale*mio.visualScale}};
            EditorUtility.SetDirty(catalog);
            var layout=Asset<HubLayoutData>(ResourcesPath+"HubLayout.asset");
            layout.bounds=new Rect(-106,-5,130,12.5f);layout.floorY=-3;layout.walkingFloorOffset=-.55f;layout.playerY=-2.75f;layout.returnX=12;layout.counterStart=-10;layout.counterEnd=10;layout.counterFloorY=-3;
            layout.counterHeight=1.35f;layout.seatSurfaceHeight=.79f;layout.walkingSpeed=6.5f;
            layout.counterRuns=new[]{new HubCounterRun{id="reserved",start=-80,end=-44},new HubCounterRun{id="main",start=-10,end=10}};
            layout.props=new[]{Prop("notice","board","OtherFeatures",-28,HubRenderLayers.BackFurniture),Prop("wardrobe","wardrobe","Wardrobe",-22,HubRenderLayers.BackFurniture),Prop("draw","gacha","SkinDrawStation",-16,HubRenderLayers.BackFurniture),Prop("exit","door","ExitDoor",16,HubRenderLayers.BackFurniture),Prop("bartender","bartender","Bartender",6.5f,HubRenderLayers.Bartender),Prop("lounge","bench",null,-103,HubRenderLayers.BackFurniture)};
            float[] anchors={-76,-72.4f,-68.8f,-65.2f,-61.6f,-58,-54.4f,-50.8f,-47.2f,-6,-2,2};layout.seats=anchors.Select((x,i)=>new HubSeatPlacement{id="seat_"+i,x=x,character=i==9?scout:i==10?guard:i==11?mio.character:null,legacySeatName=i==9?"SeatedScout":i==10?"SeatedGuard":i==11?"SeatedMio":null,alignSeatedContacts=i==11}).ToArray();
            float[] events={-98,-92,-86,-40,-36,-32};
            layout.contentSlots=events.Select((x,i)=>new HubContentSlotData{id="event_"+i,position=new Vector2(x,-3),decorationArtId=i==5?"":"event"}).ToArray();
            EditorUtility.SetDirty(layout);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            foreach(var material in new[]{catalog.spriteMaterial,catalog.outlineMaterial})
                if(ShaderUtil.ShaderHasError(material.shader))throw new InvalidOperationException("Hub shader has errors: "+material.shader.name+"\n"+string.Join("\n",ShaderUtil.GetShaderMessages(material.shader).Select(m=>m.message+" line "+m.line)));
            Debug.Log("HUB ART DATA READY: "+entries.Count+" sprites; "+layout.seats.Length+" matching chairs; "+layout.contentSlots.Length+" future content slots.");
        }
        finally{foreach(var texture in readbacks.Values)UnityEngine.Object.DestroyImmediate(texture);readbacks.Clear();}
    }
    static HubPropPlacement Prop(string id,string art,string legacy,float x,int order)=>new HubPropPlacement{id=id,artId=art,legacyName=legacy,position=new Vector2(x,-3),sortingOrder=order};
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int index=path.LastIndexOf('/');Folder(path.Substring(0,index));AssetDatabase.CreateFolder(path.Substring(0,index),path.Substring(index+1));}
    static T Asset<T>(string path)where T:ScriptableObject
    {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
    static Material Material(string path,string name)
    {
        var shader=Shader.Find(name);if(shader==null)throw new InvalidOperationException("Missing shader "+name);
        var value=AssetDatabase.LoadAssetAtPath<Material>(path);if(value==null){value=new Material(shader);AssetDatabase.CreateAsset(value,path);}else value.shader=shader;
        EditorUtility.SetDirty(value);return value;
    }
    static Sprite SpriteAsset(HubArtCatalog owner,string id,Texture2D texture,Rect rect,Vector2 pivot,float ppu)
    {
        string name="Hub_"+id;var previous=AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(owner)).OfType<Sprite>().FirstOrDefault(s=>s.name==name);
        if(previous!=null&&previous.rect==rect&&Mathf.Abs(previous.pixelsPerUnit-ppu)<.001f)return previous;
        if(previous!=null)UnityEngine.Object.DestroyImmediate(previous,true);
        var sprite=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.AddObjectToAsset(sprite,owner);return sprite;
    }
    public static Rect Measure(Sprite sprite)
    {
        Texture2D texture=sprite.texture;
        if(!texture.isReadable)
        {
            string path=AssetDatabase.GetAssetPath(texture);
            if(!readbacks.TryGetValue(path,out texture)){texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(path));readbacks.Add(path,texture);}
        }
        Rect visible=Opaque(texture,sprite.rect);
        return new Rect((visible.x-sprite.rect.x-sprite.pivot.x)/sprite.pixelsPerUnit,(visible.y-sprite.rect.y-sprite.pivot.y)/sprite.pixelsPerUnit,visible.width/sprite.pixelsPerUnit,visible.height/sprite.pixelsPerUnit);
    }
    static Rect Opaque(Texture2D texture,Rect rect)
    {
        var pixels=texture.GetPixels32();int minX=(int)rect.xMax,minY=(int)rect.yMax,maxX=-1,maxY=-1;
        for(int y=(int)rect.yMin;y<(int)rect.yMax;y++)for(int x=(int)rect.xMin;x<(int)rect.xMax;x++)if(pixels[y*texture.width+x].a>=100){minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        if(maxX<minX||maxY<minY)throw new InvalidOperationException("Artwork contains no visible pixels: "+texture.name);
        return new Rect(minX,minY,maxX-minX+1,maxY-minY+1);
    }
    readonly struct Spec{public readonly string id;public readonly float height;public Spec(string name,float h){id=name;height=h;}}
}
#endif
