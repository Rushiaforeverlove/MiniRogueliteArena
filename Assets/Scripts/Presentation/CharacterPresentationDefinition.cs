using System;
using System.Collections.Generic;
using UnityEngine;

namespace AbyssBar.Presentation
{
    // Render metadata only. Stable role IDs select assets; this class owns no combat or save state.
    [Serializable] public sealed class CharacterFrame
    {
        public Texture2D texture;
        public Rect rect;
        public Vector2 pivot;
        public float pixelsPerUnit;
        [NonSerialized] private Sprite cached;
        public Sprite Sprite
        {
            get
            {
                if(cached==null&&texture!=null)
                    cached=UnityEngine.Sprite.Create(texture,rect,pivot,Mathf.Max(1,pixelsPerUnit),0,SpriteMeshType.FullRect);
                return cached;
            }
        }
        public void Release()
        {
            if(cached!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(cached);else UnityEngine.Object.DestroyImmediate(cached);}
            cached=null;
        }
    }
    [Serializable] public sealed class CharacterMotion
    {
        public string key;
        public float duration=1;
        public bool loop;
        public CharacterFrame[] frames=Array.Empty<CharacterFrame>();
        public CharacterFrame At(float seconds,float overrideDuration=0)
        {
            if(frames==null||frames.Length==0)return null;
            float length=Mathf.Max(.01f,overrideDuration>0?overrideDuration:duration);
            float phase=loop?Mathf.Repeat(Mathf.Max(0,seconds),length):Mathf.Clamp(seconds,0,length);
            return frames[Mathf.Clamp(Mathf.FloorToInt(phase/length*frames.Length),0,frames.Length-1)];
        }
    }
    [CreateAssetMenu(menuName="Abyss Bar/Character Presentation")]
    public sealed class CharacterPresentationDefinition:ScriptableObject
    {
        public string characterId;
        public float hubHeight=1.85f,battleHeight=1.85f;
        public float aimHeight=.65f,muzzleReach=.65f;
        public bool circularSkills;
        public Material characterMaterial,effectMaterial;
        public CharacterMotion[] motions=Array.Empty<CharacterMotion>();
        public CharacterMotion seatUpper,seatSpecialUpper;
        public CharacterFrame seatLower;
        [Min(.01f)] public float seatedScale=1;
        [Min(0)] public float seatedRestSeconds;
        public Vector2 seatLowerOffset;
        [NonSerialized]private readonly Dictionary<Texture,Material> characterMaterials=new Dictionary<Texture,Material>();
        [NonSerialized]private readonly Dictionary<Texture,Material> effectMaterials=new Dictionary<Texture,Material>();
        public Material MaterialFor(Texture texture,bool effect=false)
        {
            if(texture==null)return effect?effectMaterial:characterMaterial;
            var cache=effect?effectMaterials:characterMaterials;
            if(!cache.TryGetValue(texture,out var material)||material==null)
            {
                // Explicit bindings avoid runtime-sprite texture batching differences on DX12.
                material=new Material(effect?effectMaterial:characterMaterial){name=name+" / "+texture.name,mainTexture=texture};
                cache[texture]=material;
            }
            return material;
        }
        public CharacterMotion Find(string key)=>Array.Find(motions,m=>m!=null&&m.key==key);
        public static CharacterPresentationDefinition Load(CharacterData role)
        {
            if(role==null)return null;
            var catalog=Resources.Load<CharacterPresentationCatalog>("CharacterPresentation/Catalog");
            return catalog!=null?catalog.Find(role.CharacterId):null;
        }
        private void OnDisable()
        {
            foreach(var m in motions)if(m?.frames!=null)foreach(var f in m.frames)f?.Release();
            foreach(var m in new[]{seatUpper,seatSpecialUpper})if(m?.frames!=null)foreach(var f in m.frames)f?.Release();
            seatLower?.Release();
            foreach(var cache in new[]{characterMaterials,effectMaterials})
            {foreach(var material in cache.Values)if(material!=null){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}cache.Clear();}
        }
    }
}
