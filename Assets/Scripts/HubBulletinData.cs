using UnityEngine;

public enum HubBulletinCategory
{
    Announcement,
    Event
}

// Editable local content. An Event entry is information, not an event/reward system.
[CreateAssetMenu(fileName = "HubBulletin", menuName = "Abyss Bar/Hub Bulletin")]
public class HubBulletinData : ScriptableObject
{
    [SerializeField] private HubBulletinCategory category;
    [SerializeField] private string title = "酒吧公告";
    [SerializeField, TextArea(4, 12)] private string body = "";

    public HubBulletinCategory Category => category;
    public string Title => string.IsNullOrWhiteSpace(title) ? "未命名公告" : title;
    public string Body => body ?? "";
}
