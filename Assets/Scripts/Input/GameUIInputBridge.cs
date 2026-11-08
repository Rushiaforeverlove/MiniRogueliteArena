using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssBar.Inputs
{
    // Pointer input remains with Unity; keyboard navigation uses the same player bindings.
    [DefaultExecutionOrder(-8000)]
    public sealed class GameUIInputBridge:MonoBehaviour
    {
        private EventSystem events;
        private void Awake(){events=GetComponent<EventSystem>();if(events!=null)events.sendNavigationEvents=false;}
        private bool IsMenuContext()
        {
            if(Time.timeScale<=0||UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu")return true;
            var hub=FindAnyObjectByType<HubCharacterSelection>();return hub!=null&&hub.HasOpenPanel;
        }
        private void Update()
        {
            if(events==null||GameInput.Blocked||!IsMenuContext())return;
            var target=events.currentSelectedGameObject;if(target==null||!target.activeInHierarchy)return;
            if(GameInput.Down(GameAction.Confirm)){ExecuteEvents.Execute(target,new BaseEventData(events),ExecuteEvents.submitHandler);return;}
            Vector2 direction=new Vector2(GameInput.Down(GameAction.Right)?1:GameInput.Down(GameAction.Left)?-1:0,GameInput.Down(GameAction.Up)?1:GameInput.Down(GameAction.Down)?-1:0);
            if(direction==Vector2.zero)return;
            var data=new AxisEventData(events){moveVector=direction,moveDir=direction.x>0?MoveDirection.Right:direction.x<0?MoveDirection.Left:direction.y>0?MoveDirection.Up:MoveDirection.Down};
            ExecuteEvents.Execute(target,data,ExecuteEvents.moveHandler);
        }
    }
}
