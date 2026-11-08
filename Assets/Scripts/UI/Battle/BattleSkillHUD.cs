using TMPro;
using UnityEngine;

public class BattleSkillHUD : MonoBehaviour
{
    [SerializeField] private PlayerActiveSkill playerSkill;
    [SerializeField] private TMP_Text skillText;

    private void Awake()
    {
        if (playerSkill == null || skillText == null)
        {
            Debug.LogError("Assign Player Skill and Skill Text on BattleSkillHUD.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        string eText = FormatSkill(AbyssBar.Inputs.GameInput.BindingName(AbyssBar.Inputs.GameAction.Skill), playerSkill.Skill, playerSkill.IsConfigured,
            playerSkill.CooldownRemaining, playerSkill.LastResult, playerSkill.IsSequenceRunning);
        string qText;
        if (!playerSkill.IsQUnlocked)
            qText = AbyssBar.Inputs.GameInput.BindingName(AbyssBar.Inputs.GameAction.Ultimate)+"：未解鎖\n於角色培養節點 1 解鎖";
        else
        {
            qText = FormatSkill(AbyssBar.Inputs.GameInput.BindingName(AbyssBar.Inputs.GameAction.Ultimate), playerSkill.UltimateSkill, playerSkill.IsUltimateConfigured,
                playerSkill.UltimateCooldownRemaining, playerSkill.UltimateLastResult, playerSkill.IsUltimateSequenceRunning);
            if (playerSkill.IsQPreviewActive) qText += "\n（編輯器測試）";
        }
        eText += $"\n充能：{playerSkill.ECharges}/{playerSkill.EChargeCapacity}"
            + (playerSkill.ERechargeRemaining > 0 ? $"（回充 {Mathf.CeilToInt(playerSkill.ERechargeRemaining)} 秒）" : "");
        if (playerSkill.IsQUnlocked) qText += $"\n充能：{playerSkill.QCharges}/{playerSkill.QChargeCapacity}"
            + (playerSkill.QRechargeRemaining > 0 ? $"（回充 {Mathf.CeilToInt(playerSkill.QRechargeRemaining)} 秒）" : "");
        var mio=playerSkill.GetComponent<AbyssBar.Mio.MioCombat>();
        if(mio!=null&&mio.isActiveAndEnabled)
        {
            if(mio.Released)qText+=$"\n解放剩餘：{mio.Ultimate.Remaining:0.0} 秒";
            else if(mio.Ultimate.Phase==AbyssBar.Mio.MioActionClock.QPhase.Enter)qText+="\n力量解放中";
            else if(mio.Ultimate.Phase==AbyssBar.Mio.MioActionClock.QPhase.Exit)qText+="\n恢復原形中";
            if(mio.IsCharging)eText+=mio.FullCharge?"\n劍氣蓄滿・放開"+AbyssBar.Inputs.GameInput.BindingName(AbyssBar.Inputs.GameAction.Primary)+"釋放":"\n巨劍蓄力中";
        }
        skillText.text = eText + "\n\n" + qText;
    }

    private static string FormatSkill(string key, ActiveSkillData data, bool configured,
        float remaining, PlayerActiveSkill.UseResult lastResult, bool sequenceRunning)
    {
        if (!configured) return key + "：技能尚未設定";
        string status = remaining > 0f ? $"冷卻 {Mathf.CeilToInt(remaining)} 秒" : "可使用";
        if (remaining <= 0f && lastResult == PlayerActiveSkill.UseResult.NoTarget)
            status = "範圍內沒有目標";
        if (sequenceRunning) status = "連續施放中／" + status;
        return $"{key}：{data.DisplayName}\n{status}";
    }
}
