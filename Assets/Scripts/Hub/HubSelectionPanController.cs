using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AbyssBar.Inputs;

namespace AbyssBar.Hub
{
    public interface IHubSelectionPointer
    {
        Vector2 Position {get;}
        bool Pressed {get;}
        bool Held {get;}
        bool Released {get;}
        bool OverUI {get;}
    }

    public sealed class UnityHubSelectionPointer:IHubSelectionPointer
    {
        private readonly List<RaycastResult> hits=new List<RaycastResult>();
        public Vector2 Position=>Input.mousePosition;
        public bool Pressed=>Input.GetMouseButtonDown(0);
        public bool Held=>Input.GetMouseButton(0);
        public bool Released=>Input.GetMouseButtonUp(0);
        public bool OverUI
        {
            get
            {
                var events=EventSystem.current;if(events==null)return false;
                hits.Clear();events.RaycastAll(new PointerEventData(events){position=Position},hits);
                foreach(var hit in hits)if(hit.module is GraphicRaycaster)return true;
                return false;
            }
        }
    }

    // Pure pointer gesture state: a drag must never turn into a seat click on release.
    public sealed class HubSelectionDragGesture
    {
        private Vector2 origin,previous;
        private readonly float threshold;
        public bool Active {get;private set;}
        public bool IsDrag {get;private set;}
        public bool CanPan {get;private set;}
        public bool CanClick {get;private set;}
        public HubSelectionDragGesture(float pixels){threshold=Mathf.Max(1,pixels);}
        public void Begin(Vector2 point,bool canPan,bool overUI)
        {origin=previous=point;Active=!overUI;IsDrag=false;CanPan=canPan&&!overUI;CanClick=!overUI;}
        public float Move(Vector2 point)
        {
            if(!Active)return 0;
            if(!IsDrag&&(point-origin).sqrMagnitude>=threshold*threshold){IsDrag=true;CanClick=false;}
            if(!IsDrag)return 0;
            float delta=previous.x-point.x;previous=point;return CanPan?delta:0;
        }
        public float End(Vector2 point){float delta=Move(point);Active=false;return delta;}
        public void Cancel(){Active=false;CanClick=false;CanPan=false;}
    }

    [DefaultExecutionOrder(-9000),DisallowMultipleComponent]
    public sealed class HubSelectionPanController:MonoBehaviour
    {
        [SerializeField] private HubCharacterSelection owner;
        [SerializeField] private HubCameraFollow cameraFollow;
        [SerializeField,Min(1)] private float thresholdPixels=8;
        private IHubSelectionPointer pointer;
        private HubSelectionDragGesture gesture;
        private HubCharacterSeat queuedSeat;
        private int releasedFrame=-1;
        private bool releaseCanClick;
        public void Configure(HubCharacterSelection selection,HubCameraFollow follow){owner=selection;cameraFollow=follow;}
        private void Update()=>TickPointer();
        public void TickPointer()
        {
            if(pointer==null)pointer=new UnityHubSelectionPointer();
            if(gesture==null)gesture=new HubSelectionDragGesture(thresholdPixels);
            if(owner==null||GameInput.Blocked||owner.HasOpenPanel){gesture.Cancel();queuedSeat=null;return;}
            if(pointer.Pressed){gesture.Begin(pointer.Position,owner.CanPanBeforeSelection,pointer.OverUI);releaseCanClick=false;}
            if(gesture.Active)
            {
                if(gesture.CanPan&&!owner.CanPanBeforeSelection){gesture.Cancel();return;}
                float pixels=pointer.Released?gesture.End(pointer.Position):pointer.Held?gesture.Move(pointer.Position):0;
                if(pixels!=0)cameraFollow?.PanSelectionByPixels(pixels);
            }
            if(pointer.Released){releasedFrame=Time.frameCount;releaseCanClick=gesture.CanClick&&!pointer.OverUI;}
        }
        public void QueueSeatClick(HubCharacterSeat seat){queuedSeat=seat;}
        private void LateUpdate()
        {
            if(queuedSeat==null)return;
            var seat=queuedSeat;queuedSeat=null;
            if(releasedFrame==Time.frameCount&&releaseCanClick&&owner!=null&&!owner.HasOpenPanel&&!GameInput.Blocked)
            {releaseCanClick=false;owner.OpenPreview(seat);}
        }
        private void OnDisable(){gesture?.Cancel();queuedSeat=null;releaseCanClick=false;releasedFrame=-1;}
        private void OnApplicationFocus(bool focused){if(!focused)OnDisable();}
    }
}
