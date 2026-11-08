using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AbyssBar.Inputs
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GameSettingsUI:MonoBehaviour
    {
        private static GameSettingsUI instance;
        private static readonly KeyCode[] Keys=((KeyCode[])Enum.GetValues(typeof(KeyCode))).Distinct().Where(k=>k!=KeyCode.None).ToArray();
        private readonly Dictionary<string,Button> bindingButtons=new Dictionary<string,Button>();
        private GameObject panel;
        private Button entryButton,cancelCapture;
        private TMP_Text message;
        private TMP_FontAsset font;
        private GameAction captureAction;
        private int captureSlot;
        private bool capturing,captureReleased;
        private float previousScale;
        public bool IsOpen=>panel!=null&&panel.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics(){instance=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(instance!=null)return;
            var go=new GameObject("Game settings",typeof(RectTransform));DontDestroyOnLoad(go);instance=go.AddComponent<GameSettingsUI>();
        }
        private void Awake()
        {
            if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;
            Build();SceneManager.sceneLoaded+=SceneLoaded;SceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);
        }
        private void OnEnable()
        {
            SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;
            EnsureRuntimeBindings();
        }
        // Unity can restore generated UI objects but discard dictionaries/listeners when
        // entering fast Play after compilation. Reconnect the existing UI, without
        // adding a second Canvas or losing its open/paused state.
        private void EnsureRuntimeBindings()
        {
            if(bindingButtons.Count==ControlCatalog.Actions.Length*2)return;
            var buttons=GetComponentsInChildren<Button>(true);
            if(buttons.Length==0)return;
            foreach(var definition in ControlCatalog.Actions)for(int slot=0;slot<2;slot++)
            {
                var action=definition.Id;int capturedSlot=slot;
                var button=buttons.FirstOrDefault(b=>b.name==action+" "+slot);
                if(button==null)continue;
                bindingButtons[action+":"+slot]=button;button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>BeginCapture(action,capturedSlot));
            }
            entryButton=buttons.FirstOrDefault(b=>b.name=="Settings button");
            if(entryButton!=null){entryButton.onClick.RemoveAllListeners();entryButton.onClick.AddListener(()=>{if(IsOpen)Close();else Open();});}
            var reset=buttons.FirstOrDefault(b=>b.name=="Reset");
            if(reset!=null){reset.onClick.RemoveAllListeners();reset.onClick.AddListener(()=>{if(capturing)return;GameInput.RestoreDefaults();message.text="已還原預設操作。";Refresh();});}
            cancelCapture=buttons.FirstOrDefault(b=>b.name=="Cancel capture");
            if(cancelCapture!=null){cancelCapture.onClick.RemoveAllListeners();cancelCapture.onClick.AddListener(()=>{EndCapture();message.text="已取消改鍵。";});}
            var close=buttons.FirstOrDefault(b=>b.name=="Close");
            if(close!=null){close.onClick.RemoveAllListeners();close.onClick.AddListener(Close);}
        }
        private void SceneLoaded(Scene scene,LoadSceneMode mode)
        {
            if(IsOpen)Close();
            var eventSystem=UnityEngine.EventSystems.EventSystem.current;
            if(eventSystem!=null&&eventSystem.GetComponent<GameUIInputBridge>()==null)eventSystem.gameObject.AddComponent<GameUIInputBridge>();
            foreach(var text in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
                if(!text.transform.IsChildOf(transform)&&text.font!=null&&text.font.HasCharacter('設',true,true)){font=text.font;break;}
            if(font!=null)foreach(var text in GetComponentsInChildren<TMP_Text>(true))text.font=font;
        }
        private void Update()
        {
            EnsureRuntimeBindings();
            GameInput.Router.TickReleaseGate();
            if(!capturing)
            {
                if(GameInput.Router.RawDown(GameAction.Menu))
                {
                    if(IsOpen)Close();
                    else if(!GameInput.Blocked&&!HasOpenModal())Open();
                }
                return;
            }
            if(!captureReleased){if(!Keys.Any(key=>GameInput.Router.Input.Held(key)))captureReleased=true;return;}
            foreach(var key in Keys)
            {
                if(!GameInput.Router.Input.Down(key))continue;
                if(GameInput.Router.Profile.TryBind(captureAction,captureSlot,key,out string error))
                {GameInput.Save();message.text="已更新「"+ControlCatalog.Find(captureAction).Label+"」。";EndCapture();}
                else message.text=error+"\n可繼續按另一個鍵，或按取消。";
                break;
            }
        }
        // Existing hub modals own their return key; settings can always be opened by its mouse button.
        private bool HasOpenModal()
        {
            var hub=FindAnyObjectByType<HubCharacterSelection>();
            if(hub!=null)return hub.HasOpenPanel;
            return false;
        }
        public void Open()
        {
            EnsureRuntimeBindings();
            if(IsOpen)return;previousScale=Time.timeScale;GameInput.Router.SetBlocked(true);Time.timeScale=0;
            panel.SetActive(true);message.text="選擇要更改的按鍵。點按與長按共用同一個普攻鍵。";Refresh();
        }
        public void Close()
        {
            if(!IsOpen)return;capturing=false;panel.SetActive(false);Time.timeScale=previousScale;GameInput.Router.SetBlocked(false);Refresh();
        }
        public void BeginCapture(GameAction action,int slot)
        {
            if(!IsOpen)return;captureAction=action;captureSlot=slot;capturing=true;captureReleased=false;
            message.text="請按「"+ControlCatalog.Find(action).Label+"」的新按鍵或滑鼠鍵。";Refresh();
        }
        private void EndCapture(){capturing=false;Refresh();}
        private void Refresh()
        {
            EnsureRuntimeBindings();
            foreach(var action in ControlCatalog.Actions)for(int slot=0;slot<2;slot++)
            {
                var button=bindingButtons[action.Id+":"+slot];button.interactable=!capturing;
                button.GetComponentInChildren<TMP_Text>().text=ControlCatalog.KeyName(GameInput.Router.Profile.Get(action.Id,slot));
            }
            cancelCapture.gameObject.SetActive(capturing);
        }
        private RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;return r;
        }
        private TMP_Text Text(string value,RectTransform parent,float size=22)
        {
            var r=Rect("Label",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var text=r.gameObject.AddComponent<TextMeshProUGUI>();text.text=value;text.font=font;text.fontSize=size;text.color=new Color(.95f,.95f,.98f);text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;return text;
        }
        private Button Button(string label,RectTransform parent,UnityEngine.Events.UnityAction clicked)
        {
            var image=parent.gameObject.AddComponent<Image>();image.color=new Color(.18f,.23f,.31f,.98f);var button=parent.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(clicked);Text(label,parent,20).alignment=TextAlignmentOptions.Center;return button;
        }
        private void Build()
        {
            var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=30000;
            var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;gameObject.AddComponent<GraphicRaycaster>();
            var entry=Rect("Settings button",transform,Vector2.one,Vector2.one,new Vector2(-112,-52),new Vector2(-12,-12));entryButton=Button("設定",entry,()=>{if(IsOpen)Close();else Open();});
            var shade=Rect("Settings modal",transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);panel=shade.gameObject;var background=shade.gameObject.AddComponent<Image>();background.color=new Color(.025f,.035f,.055f,.97f);
            var content=Rect("Content",shade,new Vector2(.08f,.05f),new Vector2(.92f,.95f),Vector2.zero,Vector2.zero);
            Text("遊戲設定 · 操作設定",Rect("Title",content,new Vector2(0,.9f),Vector2.one,Vector2.zero,Vector2.zero),30);
            Text("操作",Rect("Action column",content,new Vector2(0,.84f),new Vector2(.42f,.9f),Vector2.zero,Vector2.zero),20);
            Text("主要按鍵",Rect("Primary column",content,new Vector2(.43f,.84f),new Vector2(.69f,.9f),Vector2.zero,Vector2.zero),20).alignment=TextAlignmentOptions.Center;
            Text("備用按鍵",Rect("Alternate column",content,new Vector2(.71f,.84f),new Vector2(.97f,.9f),Vector2.zero,Vector2.zero),20).alignment=TextAlignmentOptions.Center;
            var scrollRoot=Rect("Bindings scroll",content,new Vector2(0,.23f),new Vector2(1,.83f),Vector2.zero,Vector2.zero);var viewport=Rect("Viewport",scrollRoot,Vector2.zero,Vector2.one,Vector2.zero,new Vector2(-20,0));viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.08f);viewport.gameObject.AddComponent<Mask>().showMaskGraphic=false;
            var list=Rect("Rows",viewport,new Vector2(0,1),new Vector2(1,1),new Vector2(0,-ControlCatalog.Actions.Length*46),Vector2.zero);list.pivot=new Vector2(.5f,1);
            var scroll=scrollRoot.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=list;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;
            var track=Rect("Scrollbar",scrollRoot,new Vector2(1,0),Vector2.one,new Vector2(-12,0),Vector2.zero);track.gameObject.AddComponent<Image>().color=new Color(.15f,.19f,.25f,.8f);
            var handle=Rect("Handle",track,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=new Color(.6f,.65f,.72f);
            var scrollbar=track.gameObject.AddComponent<Scrollbar>();scrollbar.direction=Scrollbar.Direction.BottomToTop;scrollbar.handleRect=handle;scrollbar.targetGraphic=handleImage;scroll.verticalScrollbar=scrollbar;
            for(int i=0;i<ControlCatalog.Actions.Length;i++)
            {
                var action=ControlCatalog.Actions[i];float top=-i*46;
                Text(action.Label,Rect(action.Id+" label",list,new Vector2(0,1),new Vector2(.42f,1),new Vector2(0,top-42),new Vector2(-12,top)),20);
                for(int slot=0;slot<2;slot++)
                {
                    int capturedSlot=slot;var binding=Rect(action.Id+" "+slot,list,new Vector2(.43f+slot*.28f,1),new Vector2(.69f+slot*.28f,1),new Vector2(0,top-40),new Vector2(0,top));
                    bindingButtons[action.Id+":"+slot]=Button("",binding,()=>BeginCapture(action.Id,capturedSlot));
                }
            }
            message=Text("",Rect("Message",content,new Vector2(0,.08f),new Vector2(.74f,.22f),Vector2.zero,Vector2.zero),19);
            Button("還原預設",Rect("Reset",content,Vector2.zero,new Vector2(.22f,.075f),Vector2.zero,Vector2.zero),()=>{if(capturing)return;GameInput.RestoreDefaults();message.text="已還原預設操作。";Refresh();});
            cancelCapture=Button("取消改鍵",Rect("Cancel capture",content,new Vector2(.76f,.12f),new Vector2(1,.21f),Vector2.zero,Vector2.zero),()=>{EndCapture();message.text="已取消改鍵。";});
            Button("返回遊戲",Rect("Close",content,new Vector2(.76f,0),new Vector2(1,.075f),Vector2.zero,Vector2.zero),Close);
            panel.SetActive(false);Refresh();
        }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded-=SceneLoaded;
            if(instance==this){if(IsOpen){Time.timeScale=previousScale;GameInput.Router.SetBlocked(false);}instance=null;}
        }
    }
}
