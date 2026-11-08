using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AbyssBar.Inputs
{
    public enum GameAction { Left, Right, Up, Down, Primary, Skill, Ultimate, Dodge, Interact, Menu, Upgrade1, Upgrade2, Upgrade3, Upgrade4, Confirm }
    [Flags] public enum ControlContext { Hub = 1, Battle = 2, Menu = 4 }

    public sealed class ActionDefinition
    {
        public readonly GameAction Id;
        public readonly string Label;
        public readonly ControlContext Context;
        public readonly KeyCode First, Second;
        public ActionDefinition(GameAction id, string label, ControlContext context, KeyCode first, KeyCode second = KeyCode.None)
        { Id=id; Label=label; Context=context; First=first; Second=second; }
    }

    public static class ControlCatalog
    {
        private const ControlContext Play = ControlContext.Hub | ControlContext.Battle;
        public static readonly ActionDefinition[] Actions = {
            new ActionDefinition(GameAction.Left,"向左移動／選單向左",Play|ControlContext.Menu,KeyCode.A,KeyCode.LeftArrow),
            new ActionDefinition(GameAction.Right,"向右移動／選單向右",Play|ControlContext.Menu,KeyCode.D,KeyCode.RightArrow),
            new ActionDefinition(GameAction.Up,"向上移動／選單向上",ControlContext.Battle|ControlContext.Menu,KeyCode.W,KeyCode.UpArrow),
            new ActionDefinition(GameAction.Down,"向下移動／選單向下",ControlContext.Battle|ControlContext.Menu,KeyCode.S,KeyCode.DownArrow),
            new ActionDefinition(GameAction.Primary,"普攻（點按／長按）",ControlContext.Battle,KeyCode.Mouse0),
            new ActionDefinition(GameAction.Skill,"E 技能",ControlContext.Battle,KeyCode.E),
            new ActionDefinition(GameAction.Ultimate,"Q 大招",ControlContext.Battle,KeyCode.Q),
            new ActionDefinition(GameAction.Dodge,"閃避",ControlContext.Battle,KeyCode.LeftShift,KeyCode.RightShift),
            new ActionDefinition(GameAction.Interact,"大廳互動",ControlContext.Hub,KeyCode.E),
            new ActionDefinition(GameAction.Menu,"設定／返回",Play | ControlContext.Menu,KeyCode.Escape),
            new ActionDefinition(GameAction.Upgrade1,"升級卡片一",ControlContext.Menu,KeyCode.Alpha1,KeyCode.Keypad1),
            new ActionDefinition(GameAction.Upgrade2,"升級卡片二",ControlContext.Menu,KeyCode.Alpha2,KeyCode.Keypad2),
            new ActionDefinition(GameAction.Upgrade3,"升級卡片三",ControlContext.Menu,KeyCode.Alpha3,KeyCode.Keypad3),
            new ActionDefinition(GameAction.Upgrade4,"升級卡片四",ControlContext.Menu,KeyCode.Alpha4,KeyCode.Keypad4),
            new ActionDefinition(GameAction.Confirm,"選單確認",ControlContext.Menu,KeyCode.Return,KeyCode.Space)
        };
        public static ActionDefinition Find(GameAction action) => Array.Find(Actions,a=>a.Id==action);
        public static string KeyName(KeyCode key)
        {
            if(key>=KeyCode.Alpha0&&key<=KeyCode.Alpha9)return ((int)key-(int)KeyCode.Alpha0).ToString();
            if(key>=KeyCode.Keypad0&&key<=KeyCode.Keypad9)return "小鍵盤 "+((int)key-(int)KeyCode.Keypad0);
            if(key==KeyCode.Return)return "Enter";
            switch(key){case KeyCode.None:return "未設定";case KeyCode.Mouse0:return "滑鼠左鍵";case KeyCode.Mouse1:return "滑鼠右鍵";case KeyCode.Mouse2:return "滑鼠中鍵";case KeyCode.LeftShift:return "左 Shift";case KeyCode.RightShift:return "右 Shift";case KeyCode.LeftControl:return "左 Ctrl";case KeyCode.RightControl:return "右 Ctrl";case KeyCode.Escape:return "Esc";case KeyCode.Space:return "空白鍵";case KeyCode.LeftArrow:return "←";case KeyCode.RightArrow:return "→";case KeyCode.UpArrow:return "↑";case KeyCode.DownArrow:return "↓";default:return key.ToString();}
        }
    }

    [Serializable] public sealed class ControlBinding
    { public GameAction action; public KeyCode first,second; }
    [Serializable] public sealed class ControlProfile
    {
        public int version=1;
        public List<ControlBinding> bindings=new List<ControlBinding>();
        public static ControlProfile Defaults()
        {
            var profile=new ControlProfile();foreach(var a in ControlCatalog.Actions)profile.bindings.Add(new ControlBinding{action=a.Id,first=a.First,second=a.Second});return profile;
        }
        public KeyCode Get(GameAction action,int slot=0)
        { var binding=bindings.Find(b=>b.action==action);var fallback=ControlCatalog.Find(action);return binding!=null?(slot==0?binding.first:binding.second):slot==0?fallback.First:fallback.Second; }
        public bool TryBind(GameAction action,int slot,KeyCode key,out string error)
        {
            error=null;
            if(slot<0||slot>1||!Enum.IsDefined(typeof(KeyCode),key)||slot==0&&key==KeyCode.None){error="主要按鍵不能留空。";return false;}
            if(key!=KeyCode.None)
            {
                if(Get(action,1-slot)==key){error="此操作已使用這個按鍵。";return false;}
                var definition=ControlCatalog.Find(action);
                foreach(var other in ControlCatalog.Actions)
                    if(other.Id!=action&&(other.Context&definition.Context)!=0&&(Get(other.Id)==key||Get(other.Id,1)==key))
                    {error="此鍵與「"+other.Label+"」衝突，請選另一個按鍵。";return false;}
            }
            var entry=bindings.Find(b=>b.action==action);if(entry==null){entry=new ControlBinding{action=action};bindings.Add(entry);}if(slot==0)entry.first=key;else entry.second=key;return true;
        }
        public string ToJson()=>JsonUtility.ToJson(this);
        public static ControlProfile FromJson(string json)
        {
            var defaults=Defaults();if(string.IsNullOrWhiteSpace(json))return defaults;
            try
            {
                var saved=JsonUtility.FromJson<ControlProfile>(json);if(saved==null||saved.version!=1||saved.bindings==null)return defaults;
                var candidate=Defaults();var seen=new HashSet<GameAction>();
                foreach(var binding in saved.bindings)
                {
                    if(binding==null||!Enum.IsDefined(typeof(GameAction),binding.action)||!seen.Add(binding.action)||binding.first==KeyCode.None
                        ||!Enum.IsDefined(typeof(KeyCode),binding.first)||!Enum.IsDefined(typeof(KeyCode),binding.second))return defaults;
                    var entry=candidate.bindings.Find(b=>b.action==binding.action);entry.first=binding.first;entry.second=binding.second;
                }
                // Validate the final profile, rather than a partially loaded profile.
                foreach(var a in candidate.bindings)
                {
                    if(a.first==a.second&&a.second!=KeyCode.None)return defaults;
                    foreach(var b in candidate.bindings)
                    {
                        if(a.action==b.action||(ControlCatalog.Find(a.action).Context&ControlCatalog.Find(b.action).Context)==0)continue;
                        if(a.first==b.first||a.first==b.second||a.second!=KeyCode.None&&(a.second==b.first||a.second==b.second))return defaults;
                    }
                }
                return candidate;
            }catch(ArgumentException){return defaults;}
        }
    }

    public interface IControlInput
    { bool Held(KeyCode key); bool Down(KeyCode key); bool Up(KeyCode key); }
    public sealed class UnityControlInput:IControlInput
    {
        public bool Held(KeyCode key)=>key!=KeyCode.None&&Input.GetKey(key);
        public bool Down(KeyCode key)=>key!=KeyCode.None&&Input.GetKeyDown(key);
        public bool Up(KeyCode key)=>key!=KeyCode.None&&Input.GetKeyUp(key);
    }

    public sealed class ControlRouter
    {
        public ControlProfile Profile {get;private set;}
        public readonly IControlInput Input;
        private bool blocked,awaitRelease;
        public bool Blocked=>blocked||awaitRelease;
        public ControlRouter(ControlProfile profile,IControlInput input){Profile=profile;Input=input;}
        public void Replace(ControlProfile profile){Profile=profile;awaitRelease=true;}
        public void SetBlocked(bool value){blocked=value;if(!value)awaitRelease=true;}
        public void TickReleaseGate()
        {if(awaitRelease&&!ControlCatalog.Actions.Any(a=>RawHeld(a.Id)))awaitRelease=false;}
        public bool RawHeld(GameAction action)=>Input.Held(Profile.Get(action))||Input.Held(Profile.Get(action,1));
        public bool RawDown(GameAction action)=>Input.Down(Profile.Get(action))||Input.Down(Profile.Get(action,1));
        public bool Held(GameAction action)=>!Blocked&&RawHeld(action);
        public bool Down(GameAction action)=>!Blocked&&RawDown(action);
        public bool Up(GameAction action)=>!Blocked&&(Input.Up(Profile.Get(action))||Input.Up(Profile.Get(action,1)));
        public float Axis(GameAction negative,GameAction positive)=>(Held(positive)?1:0)-(Held(negative)?1:0);
    }

    public static class GameInput
    {
        private const string SaveKey="AbyssBar.Settings.Controls.v1";
        private static ControlRouter router;
        public static event Action BindingsChanged;
        public static ControlRouter Router=>router??(router=new ControlRouter(ControlProfile.FromJson(PlayerPrefs.GetString(SaveKey,"")),new UnityControlInput()));
        public static bool Blocked=>Router.Blocked;
        public static bool Down(GameAction action)=>Router.Down(action);
        public static bool Held(GameAction action)=>Router.Held(action);
        public static bool Up(GameAction action)=>Router.Up(action);
        public static float Horizontal=>Router.Axis(GameAction.Left,GameAction.Right);
        public static float Vertical=>Router.Axis(GameAction.Down,GameAction.Up);
        public static string BindingName(GameAction action)=>ControlCatalog.KeyName(Router.Profile.Get(action));
        public static void Save(){PlayerPrefs.SetString(SaveKey,Router.Profile.ToJson());PlayerPrefs.Save();BindingsChanged?.Invoke();}
        public static void RestoreDefaults(){Router.Replace(ControlProfile.Defaults());Save();}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics(){router=null;BindingsChanged=null;}
    }
}
