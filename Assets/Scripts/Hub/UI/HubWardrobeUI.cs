using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HubWardrobeUI : MonoBehaviour
{
    [SerializeField] private GameObject wardrobePanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private TMP_Text equippedText;
    [SerializeField] private TMP_Text skinTicketsText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Image previewImage;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button equipButton;

    private readonly List<CharacterSkinData> visibleSkins = new List<CharacterSkinData>();
    private CharacterData currentCharacter;
    private int currentIndex;

    public bool IsConfigured => wardrobePanel != null && characterNameText != null
        && skinNameText != null && equippedText != null && skinTicketsText != null && pageText != null
        && statusText != null && previewImage != null && previousButton != null
        && nextButton != null && equipButton != null;
    public bool IsOpen => wardrobePanel != null && wardrobePanel.activeSelf;

    private void Awake()
    {
        if (!IsConfigured)
        {
            Debug.LogError("Assign every HubWardrobeUI reference on Canvas.", this);
            return;
        }

        previewImage.preserveAspect = true;
        Hide();
    }

    public bool Show(CharacterData character)
    {
        if (!IsConfigured || character == null) return false;

        currentCharacter = character;
        visibleSkins.Clear();
        foreach (CharacterSkinData skin in character.Skins)
        {
            if (skin != null && !string.IsNullOrWhiteSpace(skin.SkinId))
                visibleSkins.Add(skin);
        }

        CharacterSkinData equipped = CharacterSkinSave.GetEquippedSkin(character);
        int equippedIndex = visibleSkins.IndexOf(equipped);
        currentIndex = equippedIndex >= 0 ? equippedIndex : 0;
        wardrobePanel.SetActive(true);
        Refresh();
        return true;
    }

    public void Hide()
    {
        if (wardrobePanel != null)
            wardrobePanel.SetActive(false);
        currentCharacter = null;
    }

    public void PreviousSkin()
    {
        if (!IsConfigured || !IsOpen || currentIndex <= 0) return;

        currentIndex--;
        Refresh();
    }

    public void NextSkin()
    {
        if (!IsConfigured || !IsOpen || currentIndex >= visibleSkins.Count - 1) return;

        currentIndex++;
        Refresh();
    }

    public void EquipPreview()
    {
        if (!IsConfigured || !IsOpen || currentCharacter == null || visibleSkins.Count == 0)
            return;

        CharacterSkinData skin = visibleSkins[currentIndex];
        bool equipped = CharacterSkinSave.TryEquip(currentCharacter, skin);
        Refresh();
        statusText.text = equipped ? $"已套用「{skin.DisplayName}」。" : "這套造型尚未解鎖。";
    }

    private void Refresh()
    {
        characterNameText.text = currentCharacter.DisplayName;
        skinTicketsText.text = $"持有造型券：{CurrencySave.SkinTickets}";
        CharacterSkinData equipped = CharacterSkinSave.GetEquippedSkin(currentCharacter);
        equippedText.text = $"目前使用：{(equipped != null ? equipped.DisplayName : "預設外觀")}";

        if (visibleSkins.Count == 0)
        {
            skinNameText.text = "預設外觀";
            pageText.text = "0 / 0";
            statusText.text = "此角色目前沒有可更換的造型。";
            previousButton.interactable = false;
            nextButton.interactable = false;
            equipButton.interactable = false;
            previewImage.gameObject.SetActive(false);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, visibleSkins.Count - 1);
        CharacterSkinData preview = visibleSkins[currentIndex];
        bool isOwned = CharacterSkinSave.IsOwned(currentCharacter, preview);
        bool isEquipped = preview == equipped;
        skinNameText.text = preview.DisplayName;
        pageText.text = $"{currentIndex + 1} / {visibleSkins.Count}";
        previousButton.interactable = currentIndex > 0;
        nextButton.interactable = currentIndex < visibleSkins.Count - 1;
        equipButton.interactable = isOwned && !isEquipped;
        statusText.text = !isOwned ? "尚未解鎖這套造型。" : isEquipped ? "目前使用中。" : "可套用。";

        Sprite sprite = preview.Portrait;
        bool isPortrait = sprite != null;
        if (sprite == null) sprite = preview.GameplaySprite;
        if (sprite == null) sprite = currentCharacter.GameplaySprite;
        if (sprite == null)
        {
            sprite = currentCharacter.Portrait;
            isPortrait = sprite != null;
        }

        previewImage.sprite = sprite;
        previewImage.color = isPortrait ? Color.white : preview.Tint;
        previewImage.gameObject.SetActive(sprite != null);
    }
}
