using System;
using UnityEngine;

namespace AbyssBar.Hub
{
    [Serializable] public sealed class HubPropPlacement
    {
        public string id,artId,legacyName;
        public Vector2 position;
        public int sortingOrder=HubRenderLayers.BackFurniture;
    }
    [Serializable] public sealed class HubSeatPlacement
    {
        public string id,legacySeatName;
        public CharacterData character;
        public float x;
        public bool alignSeatedContacts;
        public Vector2 localHip;
        public Vector2 localTableContact=new Vector2(0,.726f);
    }
    [Serializable] public sealed class HubCounterRun
    {public string id;public float start,end;}
    [Serializable] public sealed class HubContentSlotData
    {
        public string id,decorationArtId="event";
        public Vector2 position;
        public GameObject contentPrefab;
    }
    [CreateAssetMenu(menuName="Abyss Bar/Hub/Layout")]
    public sealed class HubLayoutData:ScriptableObject
    {
        public Rect bounds=new Rect(-106,-5,130,12.5f);
        public float floorY=-3,playerY=-2.75f,returnX=12;
        public float counterStart=-10,counterEnd=10,counterFloorY=-3;
        public float counterHeight=1.35f,seatSurfaceHeight=.79f,walkingSpeed=6.5f;
        public float walkingFloorOffset=-.55f;
        public float WalkingFloorY=>floorY+walkingFloorOffset;
        public HubCounterRun[] counterRuns=Array.Empty<HubCounterRun>();
        public HubPropPlacement[] props=Array.Empty<HubPropPlacement>();
        public HubSeatPlacement[] seats=Array.Empty<HubSeatPlacement>();
        public HubContentSlotData[] contentSlots=Array.Empty<HubContentSlotData>();
        public static HubLayoutData Load()=>Resources.Load<HubLayoutData>("Hub/HubLayout");
    }
    public sealed class HubContentSlot:MonoBehaviour
    {
        public string SlotId {get;private set;}
        public GameObject Content {get;private set;}
        public void Configure(string id,GameObject prefab)
        {
            SlotId=id;if(Content!=null)Destroy(Content);Content=prefab!=null?Instantiate(prefab,transform,false):null;
        }
    }
}
