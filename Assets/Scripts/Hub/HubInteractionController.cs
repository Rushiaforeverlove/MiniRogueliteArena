using TMPro;
using UnityEngine;
using AbyssBar.Inputs;

namespace AbyssBar.Hub
{
    public sealed class HubInteractionController:MonoBehaviour
    {
        private TMP_Text hint;
        private UnityEngine.UI.Image hintBackground;
        public HubInteractionTarget Focus {get;private set;}
        public void Configure(TMP_Text label)
        {
            hint=label;
            if(hint!=null&&hintBackground==null)
            {
                var go=new GameObject("Interaction hint background",typeof(RectTransform));go.transform.SetParent(hint.transform.parent,false);
                hintBackground=go.AddComponent<UnityEngine.UI.Image>();hintBackground.color=new Color(.025f,.03f,.045f,.9f);hintBackground.raycastTarget=false;
                go.transform.SetSiblingIndex(hint.transform.GetSiblingIndex());hint.alignment=TextAlignmentOptions.Center;hint.textWrappingMode=TextWrappingModes.NoWrap;
            }
            Clear();
        }
        public void Tick(Vector2 player,bool enabled,bool pressed)
        {
            if(!enabled||GameInput.Blocked){Clear();return;}
            HubInteractionTarget nearest=null;float best=float.PositiveInfinity;
            foreach(var target in HubInteractionTarget.Registered)
            {
                if(target==null||target.gameObject.scene!=gameObject.scene||!target.Available)continue;
                float distance=Mathf.Abs(player.x-target.InteractionPoint.x);
                float range=target==Focus?target.ExitRange:target.EnterRange;
                if(distance>range)continue;
                // Keep the current target near an exact tie; never execute two callbacks.
                float score=distance-(target == Focus ? .15f : 0);
                if(score<best){nearest=target;best=score;}
            }
            if(nearest!=Focus){if(Focus!=null)Focus.SetFocused(false);Focus=nearest;if(Focus!=null)Focus.SetFocused(true);}
            if(hint!=null)
            {
                hint.gameObject.SetActive(Focus!=null);
                if(hintBackground!=null)hintBackground.gameObject.SetActive(Focus!=null);
                if(Focus!=null)
                {
                    hint.text="按 "+GameInput.BindingName(GameAction.Interact)+(Focus.Verb==InteractionVerb.Enter?" 進入":" 與")+Focus.DisplayName+(Focus.Verb==InteractionVerb.With?"互動":"");
                    var preferred=hint.GetPreferredValues(hint.text);hint.rectTransform.sizeDelta=new Vector2(preferred.x+24,preferred.y+12);
                    PlaceHint(Focus.PromptPoint);
                }
            }
            if(pressed&&Focus!=null){var chosen=Focus;Clear();chosen.Interact();}
        }
        private void PlaceHint(Vector3 point)
        {
            var canvas=hint.canvas;var camera=Camera.main;if(canvas==null||camera==null)return;
            var parent=hint.rectTransform.parent as RectTransform;if(parent==null)return;
            var screen=RectTransformUtility.WorldToScreenPoint(camera,point);
            var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,uiCamera,out var local))
            {
                var rect=parent.rect;Vector2 half=hint.rectTransform.rect.size*.5f;
                local.x=Mathf.Clamp(local.x,rect.xMin+half.x+12,rect.xMax-half.x-12);
                local.y=Mathf.Clamp(local.y,rect.yMin+half.y+12,rect.yMax-half.y-12);
                hint.rectTransform.localPosition=new Vector3(local.x,local.y,0);
                if(hintBackground!=null){hintBackground.rectTransform.sizeDelta=hint.rectTransform.rect.size+new Vector2(16,8);hintBackground.rectTransform.localPosition=hint.rectTransform.localPosition;}
            }
        }
        public void Clear(){if(Focus!=null)Focus.SetFocused(false);Focus=null;if(hint!=null)hint.gameObject.SetActive(false);if(hintBackground!=null)hintBackground.gameObject.SetActive(false);}
        private void OnDisable()=>Clear();
    }
}
