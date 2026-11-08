using System.Collections.Generic;
using UnityEngine;
namespace AbyssBar.Presentation
{
    // Bounded, reused renderers; no gameplay collider, target, damage or loot ownership.
    public sealed class CharacterEffectPool:MonoBehaviour
    {
        private sealed class Entry{public SpriteRenderer view;public string key;public float age,duration;public Vector3 at;public Vector2 size;public float angle;public bool active;}
        private readonly List<Entry> entries=new List<Entry>();
        private CharacterPresentationDefinition data;
        private int layerId,order;
        public void Configure(CharacterPresentationDefinition definition,int sortingLayer,int sortingOrder)
        {data=definition;layerId=sortingLayer;order=sortingOrder;Clear();}
        public void Emit(string key,Vector3 at,Vector2 size,float angle=0,float duration=.4f)
        {
            if(data==null||data.Find(key)==null)return;
            var entry=entries.Find(e=>!e.active);
            if(entry==null)
            {
                if(entries.Count>=64)return;
                var go=new GameObject("Character feedback");go.layer=gameObject.layer;go.transform.SetParent(transform,false);
                entry=new Entry{view=go.AddComponent<SpriteRenderer>()};entries.Add(entry);
            }
            entry.key=key;entry.age=0;entry.duration=duration;entry.at=at;entry.size=size;entry.angle=angle;entry.active=true;
            Draw(entry);
        }
        private void Draw(Entry e)
        {e.view.sortingLayerID=layerId;e.view.sortingOrder=order;CharacterPresentationView.Effect(e.view,data,e.key,e.age,e.at,e.size,e.angle,e.duration);}
        private void LateUpdate()
        {
            foreach(var e in entries)if(e.active)
            {e.age+=Time.deltaTime;if(e.age>=e.duration){e.active=false;e.view.enabled=false;}else Draw(e);}
        }
        public void Clear(){foreach(var e in entries){e.active=false;if(e.view!=null)e.view.enabled=false;}}
        private void OnDisable()=>Clear();
    }
}
