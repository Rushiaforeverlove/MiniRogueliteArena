using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace AbyssBar.Hub
{
    public enum InteractionVerb { With, Enter }
    public interface IHubInteractable
    {
        bool Available {get;}
        Vector3 InteractionPoint {get;}
        Vector3 PromptPoint {get;}
        float EnterRange {get;}
        float ExitRange {get;}
        string DisplayName {get;}
        InteractionVerb Verb {get;}
        void Interact();
        void SetFocused(bool focused);
    }
    public interface IHubHighlightView { void SetFocused(bool focused); }

    [DisallowMultipleComponent]
    public sealed class HubInteractionTarget:MonoBehaviour,IHubInteractable
    {
        [SerializeField] private string displayName;
        [SerializeField] private InteractionVerb verb;
        [SerializeField] private Transform interactionAnchor,promptAnchor;
        [SerializeField,Min(.1f)] private float enterRange=1.2f,exitRange=1.5f;
        [SerializeField] private UnityEvent onInteract=new UnityEvent();
        private Action action;
        private Func<bool> availability;
        private Func<string> nameProvider;
        private HubOutlineView outline;
        private static readonly HashSet<HubInteractionTarget> registered=new HashSet<HubInteractionTarget>();
        public static IEnumerable<HubInteractionTarget> Registered=>registered;
        public bool HasRuntimeBinding=>action!=null;
        public bool Available=>isActiveAndEnabled&&gameObject.activeInHierarchy&&(action!=null||onInteract.GetPersistentEventCount()>0)&&(availability==null||availability());
        public Vector3 InteractionPoint=>interactionAnchor!=null?interactionAnchor.position:transform.position;
        public Vector3 PromptPoint
        {
            get
            {
                if(promptAnchor!=null)return promptAnchor.position;
                float y=transform.position.y;
                foreach(var renderer in GetComponentsInChildren<Renderer>())
                    if(renderer.enabled&&!renderer.name.StartsWith("__HubOutline")&&(renderer is SpriteRenderer||renderer is MeshRenderer))y=Mathf.Max(y,renderer.bounds.max.y);
                return new Vector3(transform.position.x,y+.45f,transform.position.z);
            }
        }
        public float EnterRange=>Mathf.Max(.1f,enterRange);
        public float ExitRange=>Mathf.Max(EnterRange,exitRange);
        public string DisplayName=>nameProvider!=null?nameProvider():displayName;
        public InteractionVerb Verb=>verb;
        public void Configure(string title,InteractionVerb actionVerb,Action callback,Func<bool> predicate=null,Func<string> titleProvider=null)
        {displayName=title;verb=actionVerb;action=callback;availability=predicate;nameProvider=titleProvider;}
        public void Interact(){if(!Available)return;if(action!=null)action();else onInteract.Invoke();}
        public void SetFocused(bool focused)
        {
            if(!focused&&outline==null)return;
            if(outline==null)outline=GetComponent<HubOutlineView>()??gameObject.AddComponent<HubOutlineView>();
            outline.SetFocused(focused);
        }
        private void OnEnable()=>registered.Add(this);
        private void OnDisable(){registered.Remove(this);SetFocused(false);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()=>registered.Clear();
    }
}
