using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HubSkinDrawUI : MonoBehaviour
{
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private SkinDrawPoolData pool;
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private TMP_Text poolInfoText;
    [SerializeField] private TMP_Text poolEntriesText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Image resultImage;
    [SerializeField] private Button drawButton;
    [SerializeField] private ScrollRect poolScroll;

    public bool IsConfigured => drawPanel != null && pool != null && currencyText != null
        && poolInfoText != null && poolEntriesText != null && resultText != null
        && resultImage != null && drawButton != null && poolScroll != null
        && poolScroll.content != null;
    public bool IsOpen => drawPanel != null && drawPanel.activeSelf;

    private void Awake()
    {
        if (!IsConfigured)
        {
            Debug.LogError("Assign every HubSkinDrawUI field, including Pool and the Scroll View's Content.", this);
            return;
        }

        resultImage.preserveAspect = true;
        poolEntriesText.richText = false;
        Hide();
    }

    public bool Show()
    {
        if (!IsConfigured) return false;

        drawPanel.SetActive(true);
        resultImage.gameObject.SetActive(false);
        resultText.text = "抽取後，造型會加入所屬角色的衣櫃。";
        Refresh();
        poolScroll.StopMovement();
        poolScroll.verticalNormalizedPosition = 1f;
        return true;
    }

    public void Hide()
    {
        if (drawPanel != null) drawPanel.SetActive(false);
    }

    public void DrawOnce()
    {
        if (!IsConfigured || !IsOpen) return;

        SkinDrawService.DrawResult result = SkinDrawService.TryDraw(pool, out SkinDrawEntry reward);
        Refresh();
        resultImage.gameObject.SetActive(false);

        switch (result)
        {
            case SkinDrawService.DrawResult.Drawn:
                resultText.text = $"取得！\n{reward.Character.DisplayName}・{reward.Skin.DisplayName}\n"
                    + "已加入衣櫃，可切換該角色後套用。";
                ShowRewardImage(reward);
                break;
            case SkinDrawService.DrawResult.NotEnoughTickets:
                resultText.text = $"造型券不足，還需要 {pool.DrawCost - CurrencySave.SkinTickets} 張。";
                break;
            case SkinDrawService.DrawResult.CollectedAll:
                resultText.text = "本獎池造型已全部取得。";
                break;
            case SkinDrawService.DrawResult.InvalidPool:
                resultText.text = "目前無法抽取，請稍後再試。";
                break;
        }
    }

    private void Refresh()
    {
        int balance = CurrencySave.SkinTickets;
        List<SkinDrawEntry> entries = SkinDrawService.GetPoolEntries(pool);
        var catalogue = new StringBuilder();
        int remaining = 0;
        foreach (SkinDrawEntry entry in entries)
        {
            bool owned = CharacterSkinSave.IsOwned(entry.Character, entry.Skin);
            if (!owned) remaining++;
            if (catalogue.Length > 0) catalogue.Append("\n\n");
            catalogue.Append(entry.Character.DisplayName).Append("／").Append(entry.Skin.DisplayName)
                .Append(owned ? "（已取得）" : "（未取得）");
        }

        currencyText.text = $"持有造型券：{balance}";
        bool validPool = pool.DrawCost > 0 && entries.Count > 0;
        poolInfoText.text = validPool
            ? $"每次費用：{pool.DrawCost} 張造型券\n尚未取得：{remaining} / {entries.Count} 套\n"
                + (remaining > 0 ? $"每套未取得造型機率：1 / {remaining}" : "本獎池已集齊。")
            : "目前沒有可抽取的造型。";
        if (validPool && remaining > 0 && balance < pool.DrawCost)
            poolInfoText.text += $"\n造型券不足，還需要 {pool.DrawCost - balance} 張。";
        poolEntriesText.text = entries.Count > 0 ? catalogue.ToString() : "目前沒有獎池內容。";
        drawButton.interactable = validPool && remaining > 0 && balance >= pool.DrawCost;
        Canvas.ForceUpdateCanvases();
    }

    private void ShowRewardImage(SkinDrawEntry reward)
    {
        Sprite sprite = reward.Skin.Portrait;
        bool isPortrait = sprite != null;
        if (sprite == null) sprite = reward.Skin.GameplaySprite;
        if (sprite == null) sprite = reward.Character.GameplaySprite;
        if (sprite == null)
        {
            sprite = reward.Character.Portrait;
            isPortrait = sprite != null;
        }

        resultImage.sprite = sprite;
        resultImage.color = isPortrait ? Color.white : reward.Skin.Tint;
        resultImage.gameObject.SetActive(sprite != null);
    }
}
