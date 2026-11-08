using System;
using UnityEngine;

namespace AbyssBar.Hub
{
    [Serializable] public sealed class HubArtEntry
    {
        public string id;
        public Sprite sprite;
        public Vector2 opaqueSize;
        public Vector2 originOffset;
    }
    [CreateAssetMenu(menuName="Abyss Bar/Hub/Art Catalog")]
    public sealed class HubArtCatalog:ScriptableObject
    {
        public HubArtEntry[] entries=Array.Empty<HubArtEntry>();
        public Material spriteMaterial,outlineMaterial;
        public CharacterSizeProfile[] characterSizes=Array.Empty<CharacterSizeProfile>();
        public SpriteBodyMeasure[] bodyMeasures=Array.Empty<SpriteBodyMeasure>();
        public HubForegroundPart[] chairForeground=Array.Empty<HubForegroundPart>();
        public HubArtEntry Find(string id)=>Array.Find(entries,e=>e!=null&&e.id==id);
        public static HubArtCatalog Load()=>Resources.Load<HubArtCatalog>("Hub/HubArtCatalog");
    }
    [Serializable] public sealed class HubForegroundPart
    {
        public string id,sourceArtId;
        public Mesh mesh;
        public int sortingOrder=HubRenderLayers.ChairFront;
    }
    [Serializable] public sealed class CharacterSizeProfile
    {
        public string characterId;
        public float hubHeight=1.85f,battleHeight=1.85f;
        public float mioHubReferenceHeight=1,mioBattleReferenceHeight=1;
    }
    [Serializable] public sealed class SpriteBodyMeasure
    {
        public Sprite sprite;
        public Rect localVisibleBounds;
    }
    public sealed class HubSceneBounds:MonoBehaviour
    {
        public Rect area=new Rect(-124,-5,176,12.5f);
        public Bounds WorldBounds=>new Bounds(new Vector3(area.center.x,area.center.y,0),new Vector3(area.width,area.height,1));
    }
    public static class HubRenderLayers
    {
        public const int Background=-1000,BackFurniture=-800,Bartender=-500,Counter=-300,Chair=0,SeatedActor=100,ChairBack=150,ChairFront=160,WalkingActor=300,Foreground=500;
    }
}
