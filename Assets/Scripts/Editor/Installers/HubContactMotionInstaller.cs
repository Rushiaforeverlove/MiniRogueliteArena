#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AbyssBar.Hub;
using AbyssBar.Presentation;

public static class HubContactMotionInstaller
{
    const string Art="Assets/Art/Characters/HubContactV1/",Data="Assets/Resources/HubMotion/";
    public const string SpecPath=Art+"import-spec.json";
    [Serializable] sealed class Spec{public int version;public MioSpec mio;public SeatSpec chitose;}
    [Serializable] sealed class MioSpec{public float referenceHeight,splitHeight;public Motion walk,fallAsleep,wake;public Frame restingLower;}
    [Serializable] sealed class SeatSpec{public float seatedScale,seatedRestSeconds;public Vector2 seatLowerOffset;public Motion idle,drink;public Frame lower;}
    [Serializable] sealed class Motion{public string key;public float duration;public bool loop;public Frame[] frames;}
    [Serializable] sealed class Frame{public string texture;public Box rect;public Vector2 pivot;public float pixelsPerUnit;}
    [Serializable] sealed class Box{public float x,y,width,height;public Rect Rect=>new Rect(x,y,width,height);}
    [MenuItem("Abyss Bar/Hub/Build Approved Contact and Motion")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before rebuilding hub presentation.");
        AssetDatabase.Refresh();
        var spec=JsonUtility.FromJson<Spec>(File.ReadAllText(SpecPath));
        if(spec?.version!=1||spec.mio==null||spec.chitose==null)throw new InvalidDataException("Invalid hub motion specification.");
        Folder(Data.TrimEnd('/'));
        foreach(var path in Directory.GetFiles(Art,"*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Point;
            importer.isReadable=false;importer.SaveAndReimport();
        }
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CharacterPresentation/Character.mat");
        if(material==null)throw new InvalidDataException("Existing character presentation material is required.");
        var mio=Asset<HubMotionDefinition>(Data+"mio_01.asset");var m=spec.mio;
        mio.referenceHeight=m.referenceHeight;mio.splitHeight=m.splitHeight;mio.sleepDelay=5;mio.wakeSeconds=.16f;
        mio.material=material;mio.walk=Convert(m.walk);mio.fallAsleep=Convert(m.fallAsleep);mio.wake=Convert(m.wake);mio.restingLower=Convert(m.restingLower);
        EditorUtility.SetDirty(mio);
        var guard=AssetDatabase.LoadAssetAtPath<CharacterPresentationDefinition>("Assets/Resources/CharacterPresentation/chitose.asset");
        if(guard==null||guard.characterId!="guard_01")throw new InvalidDataException("Existing Chitose definition is required.");
        var s=spec.chitose;guard.seatUpper=Convert(s.idle);guard.seatSpecialUpper=Convert(s.drink);guard.seatLower=Convert(s.lower);
        guard.seatedScale=s.seatedScale;guard.seatedRestSeconds=s.seatedRestSeconds;guard.seatLowerOffset=s.seatLowerOffset;EditorUtility.SetDirty(guard);
        var catalog=HubArtCatalog.Load();if(catalog?.Find("stool")?.sprite==null)throw new InvalidDataException("Shared stool reference is required.");
        var mesh=BuildFront(catalog.Find("stool").sprite);
        var existing=(catalog.chairForeground??Array.Empty<HubForegroundPart>()).Where(p=>p.id!="contact-front").ToList();
        existing.Add(new HubForegroundPart{id="contact-front",sourceArtId="stool",mesh=mesh,sortingOrder=HubRenderLayers.ChairFront});
        catalog.chairForeground=existing.ToArray();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
        Debug.Log("HUB CONTACT MOTION READY: approved Mio hub motion, centered Chitose seat and shared chair-front mask. Combat and saves unchanged.");
    }
    static CharacterMotion Convert(Motion motion)
    {
        if(motion?.frames==null||motion.frames.Length!=6||motion.duration<=0)throw new InvalidDataException("Incomplete hub motion.");
        return new CharacterMotion{key=motion.key,duration=motion.duration,loop=motion.loop,frames=motion.frames.Select(Convert).ToArray()};
    }
    static CharacterFrame Convert(Frame frame)
    {
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+frame.texture+".png");var rect=frame.rect.Rect;
        if(texture==null||rect.width<=0||rect.height<=0||rect.xMin<0||rect.yMin<0||rect.xMax>texture.width+.01f||rect.yMax>texture.height+.01f||frame.pixelsPerUnit<=0)
            throw new InvalidDataException("Invalid frame rectangle: "+frame.texture);
        if(frame.pivot.x<0||frame.pivot.x>1||frame.pivot.y<-.0001f||frame.pivot.y>1.0001f)throw new InvalidDataException("Frame pivot outside slice.");
        return new CharacterFrame{texture=texture,rect=rect,pivot=frame.pivot,pixelsPerUnit=frame.pixelsPerUnit};
    }
    // Geometry clips the original stool texture; no extra painted overlay or modified PNG is used.
    static Mesh BuildFront(Sprite sprite)
    {
        string path=Data+"ChairFront.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        var polygons=new[]{
            new[]{new Vector2(351,390),new Vector2(409,390),new Vector2(409,645),new Vector2(351,645)},
            new[]{new Vector2(617,390),new Vector2(667,390),new Vector2(667,645),new Vector2(617,645)},
            new[]{new Vector2(274,574),new Vector2(303,601),new Vector2(370,621),new Vector2(511,631),new Vector2(658,619),new Vector2(728,602),new Vector2(750,577),new Vector2(750,652),new Vector2(706,685),new Vector2(610,704),new Vector2(402,704),new Vector2(303,680),new Vector2(274,652)}};
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        float anchorX=sprite.rect.x+sprite.pivot.x,anchorY=sprite.rect.y+sprite.pivot.y;
        foreach(var polygon in polygons)
        {
            int start=vertices.Count;
            foreach(var point in polygon)
            {
                vertices.Add(new Vector3((point.x-anchorX)/sprite.pixelsPerUnit,(sprite.texture.height-point.y-anchorY)/sprite.pixelsPerUnit,0));
                uv.Add(new Vector2(point.x/sprite.texture.width,1-point.y/sprite.texture.height));
            }
            indices.AddRange(Triangulate(polygon).Select(i=>i+start));
        }
        mesh.name="Approved shared stool posts and cushion front";mesh.SetVertices(vertices);mesh.SetUVs(0,uv);
        mesh.SetColors(Enumerable.Repeat(Color.white,vertices.Count).ToList());mesh.SetTriangles(indices,0);mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }
    static float Cross(Vector2 a,Vector2 b,Vector2 c)=>Vector3.Cross(b-a,c-a).z;
    static int[] Triangulate(Vector2[] points)
    {
        var remaining=Enumerable.Range(0,points.Length).ToList();var triangles=new List<int>();
        float area=0;for(int i=0;i<points.Length;i++)area+=points[i].x*points[(i+1)%points.Length].y-points[(i+1)%points.Length].x*points[i].y;
        if(area<0)remaining.Reverse();
        while(remaining.Count>3)
        {
            bool clipped=false;
            for(int i=0;i<remaining.Count;i++)
            {
                int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];
                if(Cross(points[a],points[b],points[c])<=0)continue;
                if(remaining.Any(p=>p!=a&&p!=b&&p!=c&&Cross(points[a],points[b],points[p])>=0&&Cross(points[b],points[c],points[p])>=0&&Cross(points[c],points[a],points[p])>=0))continue;
                triangles.AddRange(new[]{a,b,c});remaining.RemoveAt(i);clipped=true;break;
            }
            if(!clipped)throw new InvalidDataException("Invalid foreground mask polygon.");
        }
        triangles.AddRange(remaining);return triangles.ToArray();
    }
    static T Asset<T>(string path)where T:ScriptableObject
    {var value=AssetDatabase.LoadAssetAtPath<T>(path);if(value==null){value=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(value,path);}return value;}
    static void Folder(string path){if(AssetDatabase.IsValidFolder(path))return;int slash=path.LastIndexOf('/');Folder(path.Substring(0,slash));AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1));}
}
#endif
