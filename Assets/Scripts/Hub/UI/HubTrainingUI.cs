using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HubTrainingUI : MonoBehaviour
{
    // Existing header/reference names are retained so they can be reused.
    [SerializeField] private GameObject trainingPanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text baseStatsText;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text trainingStatusText;

    [Header("Five-Node Training")]
    [SerializeField] private Button[] nodeButtons = new Button[5];
    [SerializeField] private TMP_Text nodeDetailsText;
    [SerializeField] private Button purchaseButton;

    private CharacterData currentCharacter;
    private int selectedNode;
    private int displayedPurchases;
    private int lastPurchaseFrame = -1;
    public bool IsOpen => trainingPanel != null && trainingPanel.activeSelf;
    public bool IsConfigured
    {
        get
        {
            if (trainingPanel == null || characterNameText == null || baseStatsText == null
                || currencyText == null || trainingStatusText == null || nodeDetailsText == null
                || purchaseButton == null || purchaseButton.GetComponentInChildren<TMP_Text>(true) == null
                || nodeButtons == null || nodeButtons.Length != 5) return false;
            for (int i = 0; i < nodeButtons.Length; i++)
            {
                if (nodeButtons[i] == null || nodeButtons[i].GetComponentInChildren<TMP_Text>(true) == null)
                    return false;
                for (int j = 0; j < i; j++) if (nodeButtons[j] == nodeButtons[i]) return false;
            }
            return true;
        }
    }

    private void Awake()
    {
        if (!IsConfigured)
        {
            Debug.LogError("Assign HubTrainingUI's five distinct node buttons, details and purchase button.", this);
            return;
        }
        for (int i = 0; i < nodeButtons.Length; i++)
        {
            int node = i;
            nodeButtons[i].onClick.AddListener(() => SelectNode(node));
        }
        Hide();
    }

    public bool Show(CharacterData character)
    {
        if (!IsConfigured || character == null) return false;
        currentCharacter = character;
        int completed = CharacterProgressionSave.GetCompletedPurchases(character);
        selectedNode = completed >= 5 ? 3 : completed >= 4 ? 2 : completed >= 1 ? 1 : 0;
        CharacterProgressionSave.MigrateLegacyTraining(character);
        int total = CharacterProgressionSave.GetLegacyRefundTotal(character);
        int pending = CharacterProgressionSave.GetPendingRefund(character);
        trainingStatusText.text = total > 0
            ? $"舊生命培養已轉換，累計退還 {total - pending} 補給幣。" : "五節點依序培養；節點 2 需要購買三次，其餘各一次。";
        if (pending > 0) trainingStatusText.text += $"\n另有 {pending} 待退，餘額有空間後再次開啟培養可領取。";
        trainingPanel.SetActive(true);
        Refresh();
        return true;
    }

    public void SelectNode(int node)
    {
        if (!IsConfigured || !IsOpen || currentCharacter == null || node < 0 || node >= 5) return;
        selectedNode = node;
        Refresh();
    }

    private void Refresh()
    {
        CharacterData c = currentCharacter;
        bool unlocked = CharacterProgressionSave.IsQUnlocked(c);
        displayedPurchases = CharacterProgressionSave.GetCompletedPurchases(c);
        CharacterTrainingBonuses bonuses = CharacterProgressionSave.GetBonuses(c);
        characterNameText.text = c.DisplayName;
        currencyText.text = $"補給幣：{CurrencySave.SupplyCoins}";
        string interval = c.StartingWeapon != null
            ? $"{Mathf.Max(0.15f, c.StartingWeapon.AttackInterval / (1f + bonuses.AttackSpeed)):0.00} 秒" : "未設定";
        baseStatsText.text = $"出戰生命：{CurrencySave.GetStartingMaxHealth(c)}　傷害加成：+{bonuses.Damage}\n"
            + $"普攻間隔：{interval}　移速：{c.BaseMoveSpeed:0.#}\n"
            + $"閃避距離加成：+{bonuses.DodgeDistance:0.##}\n"
            + $"E：{(CharacterProgressionSave.IsEEnhanced(c) ? "已強化" : "基礎")}　Q：{(CharacterProgressionSave.IsQEnhanced(c) ? "已強化" : unlocked ? "已解鎖" : "未解鎖")}　培養：{displayedPurchases} / {CharacterProgressionSave.TotalPurchases} 次";

        for (int i = 0; i < nodeButtons.Length; i++)
        {
            int size = CharacterProgressionSave.GetNodeSize(i);
            int progress = CharacterProgressionSave.GetNodeProgress(c, i);
            string dots = "";
            for (int j = 0; j < size; j++) dots += (j < progress ? "●" : "○") + (j + 1 < size ? " " : "");
            string title = $"{i + 1} {CharacterProgressionSave.GetNodeTitle(i)}";
            if (selectedNode == i) title = "【" + title + "】";
            nodeButtons[i].GetComponentInChildren<TMP_Text>(true).text = title + "\n" + dots
                + "\n" + (progress >= size ? "已完成" : i == 0 ? "可培養"
                    : i == 1 ? (unlocked ? "可培養" : "前置未完成")
                    : i == 2 ? (CharacterProgressionSave.GetNodeProgress(c, 1) == 3 ? "可培養" : "前置未完成")
                    : i == 3 ? (CharacterProgressionSave.IsEEnhanced(c) ? "可培養" : "前置未完成") : (CharacterProgressionSave.IsQEnhanced(c) ? "可培養" : "前置未完成"));
            // Every node remains clickable for inspection, including locked future nodes.
            nodeButtons[i].interactable = true;
        }

        nodeDetailsText.text = GetDescription(c, selectedNode);
        purchaseButton.interactable = false;
        string purchaseLabel;
        CharacterProgressionSave.PurchaseResult status = CharacterProgressionSave.GetPurchaseStatus(c, selectedNode);
        int cost = CharacterProgressionSave.GetPurchaseCost(c, selectedNode);
        switch (status)
        {
            case CharacterProgressionSave.PurchaseResult.Ready:
                purchaseLabel = $"培養：{cost} 補給幣";
                purchaseButton.interactable = true; break;
            case CharacterProgressionSave.PurchaseResult.AlreadyCompleted:
                purchaseLabel = $"節點 {selectedNode + 1} 已完成"; break;
            case CharacterProgressionSave.PurchaseResult.PrerequisiteNotMet:
                purchaseLabel = PrerequisiteText(selectedNode); break;
            case CharacterProgressionSave.PurchaseResult.MissingTrainingData:
                purchaseLabel = "請設定三段數值培養資料"; break;
            case CharacterProgressionSave.PurchaseResult.MissingQSkill:
                purchaseLabel = "請先設定 Q 技能"; break;
            case CharacterProgressionSave.PurchaseResult.MissingEnhancedESkill:
                purchaseLabel = "請設定獨立的強化 E 資料"; break;
            case CharacterProgressionSave.PurchaseResult.MissingEnhancedQSkill:
                purchaseLabel = "請設定獨立的強化 Q 資料"; break;
            case CharacterProgressionSave.PurchaseResult.NotEnoughCoins:
                purchaseLabel = $"補給幣不足（差 {cost - CurrencySave.SupplyCoins}）"; break;
            case CharacterProgressionSave.PurchaseResult.NotImplemented:
                purchaseLabel = "後續小節開放"; break;
            default: purchaseLabel = "角色或費用設定錯誤"; break;
        }
        purchaseButton.GetComponentInChildren<TMP_Text>(true).text = purchaseLabel;
    }

    private static string SkillName(ActiveSkillData data) => data != null ? data.DisplayName : "未設定";
    private static string GetDescription(CharacterData c, int node)
    {
        string heading = $"節點 {node + 1}：{CharacterProgressionSave.GetNodeTitle(node)}\n";
        switch (node)
        {
            case 0:
                return heading + $"永久解鎖 Q「{SkillName(c.UltimateQSkill)}」。\n"
                    + $"費用：{c.QUnlockCost} 補給幣。此節點只需培養一次。\n"
                    + "完成後下次出戰即可使用；依 Character Id 分別存檔。";
            case 1:
                int progress = CharacterProgressionSave.GetNodeProgress(c, 1);
                string text = heading + $"目前 {progress} / 3；需先完成節點 1。\n";
                for (int i = 0; i < 3; i++)
                {
                    CharacterStatTrainingStep step = c.GetStatTrainingStep(i);
                    string mark = i < progress ? "已完成" : i == progress ? "下一次" : "未完成";
                    text += $"{i + 1}（{mark}）：{c.GetStatTrainingPreview(i)}"
                        + (step != null ? $"／{step.Cost} 幣" : "") + "\n";
                }
                return text + (progress >= 3 ? "三次培養已完成，可前往節點 3 強化 E。" : "每次購買一段，完成三段才完成節點 2。");
            case 2:
                return heading + $"前置：節點 2 完成三次；費用 {c.EEnhanceCost} 補給幣。\n"
                    + $"基礎 E：{SkillName(c.StartingESkill)}\n{SkillSummary(c.StartingESkill)}\n{c.IdentityProfile?.baseEHint}\n"
                    + $"強化 E：{SkillName(c.EnhancedESkill)}\n{SkillSummary(c.EnhancedESkill)}\n{c.IdentityProfile?.enhancedEHint}\n"
                    + (!c.HasConfiguredEnhancedESkill ? "請指定獨立且有效的強化 E，出戰暫用基礎 E。"
                        : CharacterProgressionSave.IsEEnhanced(c) ? "已完成，下次出戰使用強化 E。" : "一次培養完成，下次出戰生效。" );
            case 3:
                return heading + $"前置：完成節點 3；費用 {c.QEnhanceCost} 補給幣。\n"
                    + $"基礎 Q：{SkillName(c.UltimateQSkill)}\n{SkillSummary(c.UltimateQSkill)}\n{c.IdentityProfile?.baseQHint}\n"
                    + $"強化 Q：{SkillName(c.EnhancedQSkill)}\n{SkillSummary(c.EnhancedQSkill)}\n{c.IdentityProfile?.enhancedQHint}\n"
                    + (!c.HasConfiguredEnhancedQSkill ? "請指定獨立且有效的強化 Q，出戰暫用基礎 Q。"
                        : CharacterProgressionSave.IsQEnhanced(c) ? "已完成，下次出戰使用強化 Q。" : "一次培養完成，下次出戰生效。");
            default:
                return heading + "完成節點 4 後，再培養一次。\n解鎖更多角色專屬局內升級選項，加入該角色選項池。\n"
                    + "費用：" + c.NodeFiveCost + " 補給幣。\n滿培獎勵：" + (c.MasteryRewardSkin != null ? c.MasteryRewardSkin.name : "待指定造型；完成紀錄保留，設定後領取") + ".";
        }
    }

    private static string SkillSummary(ActiveSkillData skill)
    {
        if (skill == null || !skill.IsConfigured) return "技能資料尚未設定完成";
        if(skill.Kind==ActiveSkillKind.MioDash)return $"位移後 360° 旋斬／基礎充能 {skill.BaseCharges}／逐次回復 {skill.Cooldown:0.#} 秒";
        if(skill.Kind==ActiveSkillKind.MioRelease)return $"紅瞳解放 6 秒／點按追加劍氣／蓄力六向劍氣／入場只擊退／冷卻 {skill.Cooldown:0.#} 秒";
        string effect = skill.Kind == ActiveSkillKind.Shockwave
            ? $"{skill.ShockwaveCount} 次／基礎傷害 {skill.Damage}／半徑 {skill.Radius:0.#}"
            : skill.Kind == ActiveSkillKind.FanShot
                ? $"{skill.ProjectileCount} 發／扇形 {skill.SpreadDegrees:0.#} 度" : $"環狀 {skill.ProjectileCount} 發";
        if (skill.Kind == ActiveSkillKind.Shockwave && skill.ShockwaveCount > 1)
            effect += $"／間隔 {skill.ShockwaveInterval:0.##} 秒";
        if (skill.Kind != ActiveSkillKind.Shockwave)
            effect += skill.ExplodeOnEnemyHit ? $"／爆炸半徑 {skill.ExplosionRadius:0.#}"
                : skill.ProjectileMaxTargets > 1 ? $"／最多命中 {skill.ProjectileMaxTargets} 名" : "";
        return effect + $"／冷卻 {skill.Cooldown:0.#} 秒";
    }

    private static string PrerequisiteText(int node)
    {
        return node == 4 ? "請先完成節點 4" : node == 3 ? "請先完成節點 3" : node == 2 ? "請先完成節點 2 的三次培養" : "請先完成節點 1";
    }

    // Purchase button calls this once; the save service also rejects repeated events.
    public void BuySelectedNode()
    {
        if (!IsConfigured || !IsOpen || currentCharacter == null || lastPurchaseFrame == Time.frameCount) return;
        lastPurchaseFrame = Time.frameCount;
        CharacterProgressionSave.PurchaseResult result = CharacterProgressionSave.TryPurchaseNode(currentCharacter, selectedNode, displayedPurchases);
        switch (result)
        {
            case CharacterProgressionSave.PurchaseResult.Purchased:
                trainingStatusText.text = selectedNode == 0 ? "培養完成！Q 已永久解鎖，下次出戰即可使用。"
                    : selectedNode == 2 ? "節點 3 完成！下次出戰使用強化 E。"
                    : selectedNode == 3 ? "節點 4 完成！下次出戰使用強化 Q。"
                    : selectedNode == 4 ? "節點 5 完成！追加專屬白／紫／金各一張，下次出戰生效。"
                    : $"數值培養 {CharacterProgressionSave.GetNodeProgress(currentCharacter, 1)} / 3 完成，下次出戰套用。"; break;
            case CharacterProgressionSave.PurchaseResult.AlreadyCompleted:
                trainingStatusText.text = "此節點已完成，不會重複扣款。"; break;
            case CharacterProgressionSave.PurchaseResult.NotEnoughCoins:
                trainingStatusText.text = "補給幣不足，請先完成出戰取得補給幣。"; break;
            case CharacterProgressionSave.PurchaseResult.NotImplemented:
                trainingStatusText.text = "此節點將於後續小節開放，目前不扣款。"; break;
            case CharacterProgressionSave.PurchaseResult.PrerequisiteNotMet:
                trainingStatusText.text = PrerequisiteText(selectedNode); break;
            case CharacterProgressionSave.PurchaseResult.ProgressChanged:
                trainingStatusText.text = "進度已更新，請確認最新的下一段效果與費用。"; break;
            case CharacterProgressionSave.PurchaseResult.MissingTrainingData:
                trainingStatusText.text = "請先設定角色三段數值培養的種類、數值與費用。"; break;
            case CharacterProgressionSave.PurchaseResult.MissingEnhancedESkill:
                trainingStatusText.text = "請先指定有效且不同於基礎版的強化 E 技能資產。"; break;
            case CharacterProgressionSave.PurchaseResult.MissingEnhancedQSkill:
                trainingStatusText.text = "請先指定有效且不同於基礎版的強化 Q 技能資產。"; break;
            default:
                trainingStatusText.text = "尚未購買：請檢查角色 ID、Q 技能與培養費用設定。"; break;
        }
        Refresh();
    }

    public void Hide()
    {
        if (trainingPanel != null) trainingPanel.SetActive(false);
        currentCharacter = null;
    }
}


