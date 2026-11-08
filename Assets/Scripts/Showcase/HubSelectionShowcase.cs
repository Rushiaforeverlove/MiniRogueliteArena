using AbyssBar.Characters;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssBar.Showcase
{
    public sealed class HubSelectionShowcase : MonoBehaviour
    {
        [SerializeField] private HubCharacterSelection hub;
        [SerializeField] private HubCameraFollow cameraFollow;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text characterName, skinName, skinStatus, applyLabel, pageTitle, body, demoCaption;
        [SerializeField] private Button previousSkin, nextSkin, applySkin, replay;
        [SerializeField] private Button[] tabs;
        [SerializeField] private GameObject demoFrame;
        [SerializeField] private ShowcaseBattlePreview demonstration;
        [SerializeField, Min(.5f)] private float focusSize = 1.8f;
        [SerializeField, Range(0, 31)] private int previewLayer = 31;
        [SerializeField, Range(.75f,.96f)] private float informationTopAnchor=.90f;
        private readonly ShowcaseSkinBrowser browser = new ShowcaseSkinBrowser();
        private HubCharacterSeat seat;
        private int page;
        private Camera hubCamera;
        private int savedCullingMask;
        private bool capturedCamera;
        public bool IsConfigured => hub != null && cameraFollow != null && portrait != null && characterName != null && skinName != null
            && skinStatus != null && applyLabel != null && pageTitle != null && body != null && demoCaption != null
            && previousSkin != null && nextSkin != null && applySkin != null && replay != null
            && tabs != null && tabs.Length == 4 && System.Array.TrueForAll(tabs, b => b != null)
            && demonstration != null && demonstration.IsConfigured && demoFrame != null;
        private void Awake()
        {
            if (!IsConfigured) { Debug.LogError("Run Selection Showcase installer in BarHub first.", this); return; }
            // Leave the persistent settings button its own top-right space.
            if(tabs[0].transform.parent is RectTransform information)
                information.anchorMax=new Vector2(information.anchorMax.x,informationTopAnchor);
            previousSkin.onClick.AddListener(() => BrowseSkin(-1)); nextSkin.onClick.AddListener(() => BrowseSkin(1));
            applySkin.onClick.AddListener(ApplySkin); replay.onClick.AddListener(demonstration.Replay);
            for (int i = 0; i < tabs.Length; i++) { int selected = i; tabs[i].onClick.AddListener(() => SelectPage(selected)); }
        }
        public void Show(HubCharacterSeat selected)
        {
            Hide();
            if (!IsConfigured || selected == null || selected.Character == null) return;
            seat = selected; browser.Open(selected.Character); page = 0;
            hubCamera = cameraFollow.GetComponent<Camera>();
            if (hubCamera != null) { savedCullingMask = hubCamera.cullingMask; capturedCamera = true; hubCamera.cullingMask &= ~(1 << previewLayer); }
            cameraFollow.FocusCharacter(seat.transform, focusSize);
            characterName.text = selected.Character.DisplayName; RenderSkin();
        }
        public void BrowseSkin(int direction)
        { if (seat == null) return; browser.Move(direction); RenderSkin(); }
        public void ApplySkin()
        {
            if (seat == null || !browser.Apply()) return;
            hub.RefreshCharacterAppearances(); RenderSkin();
        }
        public void SelectPage(int selected)
        { if (seat == null || selected < 0 || selected > 3) return; page = selected; RenderPage(); }
        private void RenderSkin()
        {
            var skin = browser.Current; var character = browser.Character;
            portrait.sprite = skin != null && skin.Portrait != null ? skin.Portrait : character.Portrait;
            portrait.color = Color.white; portrait.enabled = portrait.sprite != null;
            skinName.text = (skin != null ? skin.DisplayName : "預設造型") + (browser.Count > 0 ? $"  {browser.Index + 1}/{browser.Count}" : string.Empty);
            previousSkin.interactable = nextSkin.interactable = browser.Count > 1;
            applySkin.interactable = browser.CanApply && !browser.IsEquipped;
            applyLabel.text = browser.IsEquipped ? "使用中" : browser.CanApply ? "套用造型" : "尚未解鎖";
            skinStatus.text = browser.IsEquipped ? "目前使用的造型" : browser.CanApply ? "已擁有・可套用" : "僅供預覽・出戰仍使用已裝備造型";
            // Only the seat's temporary display changes while browsing. Hub and
            // battle appearance still read the saved equipped skin.
            var renderer = seat.GetComponent<SpriteRenderer>();
            if (renderer != null && skin != null)
            {
                var sprite = skin.SeatedSprite != null ? skin.SeatedSprite : skin.GameplaySprite != null ? skin.GameplaySprite : character.GameplaySprite;
                if (sprite != null) renderer.sprite = sprite;
                renderer.color = skin.Tint;
            }
            seat.GetComponent<AbyssBar.Presentation.CharacterSeatPresenter>()?.PreviewSkin(skin);
            RenderPage();
        }
        private void RenderPage()
        {
            var character = browser.Character;
            string[] titles = { "角色簡介", "出戰數值・普攻", "E 技能", "Q 大招" };
            pageTitle.text = titles[page];
            body.text = page == 0 ? CharacterProfileText.Overview(character, false) : page == 1 ? CharacterProfileText.Stats(character)
                : CharacterProfileText.Skill(character, page == 3);
            var rect = body.rectTransform;
            rect.anchorMin = new Vector2(.06f, page == 0 ? .08f : .58f); rect.anchorMax = new Vector2(.94f, .80f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            for (int i = 0; i < tabs.Length; i++)
            {
                var colors = tabs[i].colors; colors.normalColor = i == page ? new Color(.4f, .8f, .82f) : Color.white; tabs[i].colors = colors;
            }
            bool showDemo = page > 0; demoFrame.SetActive(showDemo);
            if (showDemo)
            {
                demonstration.Play(character, browser.Current, (ShowcasePage)page, previewLayer);
                demoCaption.text = page == 3 && !CharacterProgressionSave.IsQUnlocked(character)
                    ? "未解鎖 Q・能力預覽，不會解鎖技能" : "演示目標生命 1,000・不含局內升級";
            }
            else demonstration.Stop();
        }
        public void Hide()
        {
            if (demonstration != null) demonstration.Stop();
            if (seat != null) seat.RefreshAppearance(); seat = null;
            if (cameraFollow != null) cameraFollow.EndCharacterFocus();
            if (capturedCamera && hubCamera != null) hubCamera.cullingMask = savedCullingMask;
            capturedCamera = false; hubCamera = null;
        }
        private void OnDisable() { Hide(); }
    }
}
