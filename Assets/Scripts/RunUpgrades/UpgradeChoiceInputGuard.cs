using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssBar.Upgrades
{
    // Button continues to render its normal hover/pressed visuals. Its onClick
    // has no selection callback: this component owns fresh pointer/submit input.
    public sealed class UpgradeChoiceInputGuard : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISubmitHandler
    {
        private UpgradeChoiceUI owner;
        private int slot, viewVersion, pointerEpoch, pointerId;
        private bool pressedReady;
        public void Bind(UpgradeChoiceUI ui, int index, int version)
        { owner = ui; slot = index; viewVersion = version; pressedReady = false; }
        public void OnPointerDown(PointerEventData data)
        {
            pressedReady = data.button == PointerEventData.InputButton.Left && owner != null
                && owner.CanReceiveInput(viewVersion);
            if (pressedReady) { pointerEpoch = owner.InputEpoch; pointerId = data.pointerId; }
        }
        public void OnPointerClick(PointerEventData data)
        {
            bool valid = pressedReady && data.button == PointerEventData.InputButton.Left && data.pointerId == pointerId
                && owner != null && owner.InputEpoch == pointerEpoch && owner.CanReceiveInput(viewVersion);
            pressedReady = false;
            if (valid) owner.RequestChoice(slot, viewVersion);
        }
        public void OnSubmit(BaseEventData data)
        {
            if (owner == null || !owner.CanReceiveInput(viewVersion)) return;
            if (AbyssBar.Inputs.GameInput.Down(AbyssBar.Inputs.GameAction.Confirm))
                owner.RequestChoice(slot, viewVersion);
        }
        private void OnDisable() { pressedReady = false; }
    }
}
