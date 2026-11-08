using UnityEngine;

[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class HubCharacterSeat : MonoBehaviour
{
    [SerializeField] private CharacterData character;
    [SerializeField] private HubCharacterSelection selectionController;
    [SerializeField, TextArea(2, 4)] private string greeting = "今天也要出戰嗎？";
    [SerializeField, TextArea(2, 4)] private string dailyDialogue = "準備好裝備後，我們就出發吧。";

    public CharacterData Character => character;
    public string Greeting => greeting;
    public string DailyDialogue => dailyDialogue;
    public void Configure(CharacterData data,HubCharacterSelection owner){character=data;selectionController=owner;RefreshAppearance();}

    private SpriteRenderer seatRenderer;
    private Sprite originalSprite;
    private Color originalColor;

    private void Awake()
    {
        seatRenderer = GetComponent<SpriteRenderer>();
        originalSprite = seatRenderer.sprite;
        originalColor = seatRenderer.color;
    }

    private void OnEnable()
    {
        RefreshAppearance();
    }

    public void RefreshAppearance()
    {
        if (seatRenderer == null) return;
        GetComponent<AbyssBar.Presentation.CharacterSeatPresenter>()?.PreviewSkin(null);

        Sprite sprite = CharacterSkinSave.GetSeatedSprite(character);
        seatRenderer.sprite = sprite != null ? sprite : originalSprite;
        seatRenderer.color = CharacterSkinSave.GetEquippedSkin(character) != null
            ? CharacterSkinSave.GetTint(character) : originalColor;
    }

    private void OnMouseUpAsButton()
    {
        if (character == null || selectionController == null)
        {
            Debug.LogWarning("Assign Character and Selection Controller on this seat.", this);
            return;
        }

        selectionController.RequestSeatPointerSelection(this);
    }
}
