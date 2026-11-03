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
        string eText = FormatSkill("E", playerSkill.Skill, playerSkill.IsConfigured,
            playerSkill.CooldownRemaining, playerSkill.LastResult, playerSkill.IsSequenceRunning);
        string qText;
        if (!playerSkill.IsQUnlocked)
            qText = "Q：未解鎖\n於角色培養節點 1 解鎖";
        else
        {
            qText = FormatSkill("Q", playerSkill.UltimateSkill, playerSkill.IsUltimateConfigured,
                playerSkill.UltimateCooldownRemaining, playerSkill.UltimateLastResult, playerSkill.IsUltimateSequenceRunning);
            if (playerSkill.IsQPreviewActive) qText += "\n（編輯器測試）";
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
