using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to the always-active Canvas. The hub controller owns opening/closing
// so that player movement and the other interaction panels stay coordinated.
[DisallowMultipleComponent]
public class HubBulletinUI : MonoBehaviour
{
    [SerializeField] private GameObject bulletinPanel;
    [SerializeField] private TMP_Text entryTitleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text pageText;
    [SerializeField] private ScrollRect contentScroll;
    [SerializeField] private Button announcementsButton;
    [SerializeField] private Button eventsButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private HubBulletinData[] bulletins = new HubBulletinData[0];

    private readonly List<HubBulletinData> visibleEntries = new List<HubBulletinData>();
    private HubBulletinCategory currentCategory;
    private int currentIndex;

    public bool IsConfigured => bulletinPanel != null && entryTitleText != null
        && bodyText != null && pageText != null && contentScroll != null
        && contentScroll.content != null && announcementsButton != null
        && eventsButton != null && previousButton != null && nextButton != null;
    public bool IsOpen => bulletinPanel != null && bulletinPanel.activeSelf;

    private void Awake()
    {
        if (!IsConfigured)
        {
            Debug.LogError("Assign every HubBulletinUI reference, including the Scroll View's Content.", this);
            return;
        }

        bodyText.richText = false;
        Hide();
    }

    public bool Show()
    {
        if (!IsConfigured) return false;

        bulletinPanel.SetActive(true);
        ShowAnnouncements();
        return true;
    }

    public void Hide()
    {
        if (bulletinPanel != null)
            bulletinPanel.SetActive(false);
    }

    public void ShowAnnouncements()
    {
        SelectCategory(HubBulletinCategory.Announcement);
    }

    public void ShowEvents()
    {
        SelectCategory(HubBulletinCategory.Event);
    }

    private void SelectCategory(HubBulletinCategory category)
    {
        if (!IsConfigured || !IsOpen) return;

        currentCategory = category;
        visibleEntries.Clear();
        if (bulletins != null)
        {
            foreach (HubBulletinData entry in bulletins)
            {
                if (entry != null && entry.Category == category)
                    visibleEntries.Add(entry);
            }
        }

        currentIndex = 0;
        Refresh();
    }

    public void PreviousBulletin()
    {
        if (!IsConfigured || !IsOpen || currentIndex <= 0) return;

        currentIndex--;
        Refresh();
    }

    public void NextBulletin()
    {
        if (!IsConfigured || !IsOpen || currentIndex >= visibleEntries.Count - 1) return;

        currentIndex++;
        Refresh();
    }

    private void Refresh()
    {
        announcementsButton.interactable = currentCategory != HubBulletinCategory.Announcement;
        eventsButton.interactable = currentCategory != HubBulletinCategory.Event;

        if (visibleEntries.Count == 0)
        {
            entryTitleText.text = currentCategory == HubBulletinCategory.Announcement
                ? "酒吧公告" : "活動情報";
            bodyText.text = currentCategory == HubBulletinCategory.Announcement
                ? "目前沒有公告。" : "目前沒有活動公告。";
            pageText.text = "0 / 0";
            previousButton.interactable = false;
            nextButton.interactable = false;
        }
        else
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, visibleEntries.Count - 1);
            HubBulletinData entry = visibleEntries[currentIndex];
            entryTitleText.text = entry.Title;
            bodyText.text = entry.Body;
            pageText.text = $"{currentIndex + 1} / {visibleEntries.Count}";
            previousButton.interactable = currentIndex > 0;
            nextButton.interactable = currentIndex < visibleEntries.Count - 1;
        }

        // Rebuild wrapped text height, then start each article at its top.
        Canvas.ForceUpdateCanvases();
        contentScroll.StopMovement();
        contentScroll.verticalNormalizedPosition = 1f;
    }
}
