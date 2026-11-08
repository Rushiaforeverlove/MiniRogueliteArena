using TMPro;
using UnityEngine;

public class BattleDodgeHUD : MonoBehaviour
{
    [SerializeField] private PlayerDodge playerDodge;
    [SerializeField] private TMP_Text dodgeText;

    private void Awake()
    {
        if (playerDodge == null || dodgeText == null)
        {
            Debug.LogError("Assign Player Dodge and Dodge Text on BattleDodgeHUD.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        string status;
        if (!playerDodge.IsConfigured || !playerDodge.isActiveAndEnabled) status = "尚未設定或停用";
        else if (playerDodge.IsDodging) status = "閃避中";
        else if (playerDodge.CooldownRemaining > 0f)
        {
            float shown = Mathf.CeilToInt(playerDodge.CooldownRemaining * 10f) / 10f;
            status = $"冷卻 {shown:0.0} 秒";
        }
        else status = "可使用";
        dodgeText.text = AbyssBar.Inputs.GameInput.BindingName(AbyssBar.Inputs.GameAction.Dodge)+"：閃避\n" + status + $"\n充能：{playerDodge.Charges}/{playerDodge.ChargeCapacity}"
            + (playerDodge.RechargeRemaining > 0 ? $"（回充 {Mathf.CeilToInt(playerDodge.RechargeRemaining * 10f) / 10f:0.0} 秒）" : "");
    }
}
