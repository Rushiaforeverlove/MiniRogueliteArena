using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace AbyssBar.Upgrades
{
    public sealed class UpgradeChoiceUI : MonoBehaviour
    {
        [SerializeField] private List<CardView> views = new List<CardView>();
        [Serializable] private sealed class CardView { public Button button; public Image icon; public TMP_Text title, meta, description, rank; [NonSerialized] public string rankSuffix; }
        private Action<int> requestChoice;
        private Func<bool> canChoose;
        private Func<int> inputEpoch;
        private int viewVersion;
        public int InputEpoch => inputEpoch != null ? inputEpoch() : viewVersion;
        public bool CanReceiveInput(int version) => isActiveAndEnabled && version == viewVersion
            && Application.isFocused && (canChoose == null || canChoose());
        public void RequestChoice(int slot, int version)
        { if (CanReceiveInput(version)) requestChoice?.Invoke(slot); }
        public void SetInputReady(bool ready)
        { foreach (var view in views) if (view.button != null) view.button.interactable = ready; }
        public static UpgradeChoiceUI Build(Canvas canvas, TMP_FontAsset font)
        {
            var root = Rect("Run Upgrade Choices", canvas.transform);
            Stretch(root); root.gameObject.AddComponent<Image>().color = new Color(.025f, .035f, .065f, .96f);
            var ui = root.gameObject.AddComponent<UpgradeChoiceUI>();
            var heading = Text("Heading", root, font, 38); heading.text = "成長抉擇 · 選擇一項升級";
            Box(heading.rectTransform, new Vector2(.08f, .87f), new Vector2(.92f, .96f));
            for (int i = 0; i < 4; i++)
            {
                var card = Rect("Card " + (i + 1), root);
                Box(card, new Vector2(.04f + .24f * i, .09f), new Vector2(.24f + .24f * i, .82f));
                var bg = card.gameObject.AddComponent<Image>(); bg.color = new Color(.12f, .15f, .22f, 1);
                var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = bg;
                var iconRect = Rect("Icon", card); Box(iconRect, new Vector2(.32f, .67f), new Vector2(.68f, .84f));
                var image = iconRect.gameObject.AddComponent<Image>(); image.preserveAspect = true;
                var meta = Text("Category and quality", card, font, 23); Box(meta.rectTransform, new Vector2(.07f, .87f), new Vector2(.93f, .96f));
                var title = Text("Name", card, font, 32); Box(title.rectTransform, new Vector2(.07f, .53f), new Vector2(.93f, .66f));
                var description = Text("Details", card, font, 24); Box(description.rectTransform, new Vector2(.08f, .14f), new Vector2(.92f, .52f));
                description.alignment = TextAlignmentOptions.TopLeft; description.enableAutoSizing = true; description.fontSizeMin = 17; description.fontSizeMax = 24;
                var rank = Text("Rank", card, font, 21); Box(rank.rectTransform, new Vector2(.06f, .03f), new Vector2(.94f, .12f));
                ui.views.Add(new CardView { button = button, icon = image, title = title, meta = meta, description = description, rank = rank });
            }
            root.gameObject.SetActive(false); return ui;
        }
        public void Show(IReadOnlyList<UpgradeCardData> offers, IReadOnlyDictionary<string, int> ranks, Action<int> choose,
            Func<bool> mayChoose = null, Func<int> epoch = null, Func<UpgradeCardData,string> describe = null)
        {
            viewVersion++; requestChoice = choose; canChoose = mayChoose; inputEpoch = epoch;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            gameObject.SetActive(true);
            for (int i = 0; i < views.Count; i++)
            {
                var v = views[i]; v.button.gameObject.SetActive(i < offers.Count); if (i >= offers.Count) continue;
                var card = offers[i]; ranks.TryGetValue(card.id, out int rank);
                v.button.onClick.RemoveAllListeners();
                var guard = v.button.GetComponent<UpgradeChoiceInputGuard>() ?? v.button.gameObject.AddComponent<UpgradeChoiceInputGuard>();
                guard.Bind(this, i, viewVersion);
                v.title.text = card.title; v.description.text = describe!=null?describe(card):card.description;
                v.meta.text = Category(card.category) + " · " + Quality(card.quality);
                v.meta.color = card.quality == UpgradeQuality.Gold ? new Color(1, .8f, .3f)
                    : card.quality == UpgradeQuality.Purple ? new Color(.8f, .6f, 1) : Color.white;
                v.icon.sprite = card.icon != null ? card.icon : RunVisual.CircleSprite;
                v.icon.color = card.icon != null ? Color.white : v.meta.color;
                v.rankSuffix = $"  Lv.{rank} → {rank + 1}" + (card.maxRank > 0 ? $" / {card.maxRank}" : "");
            }
            RefreshShortcutLabels();
            SetInputReady(mayChoose == null);
        }
        private void RefreshShortcutLabels()
        {
            for(int i=0;i<views.Count;i++)if(views[i].rank!=null&&views[i].rankSuffix!=null)
                views[i].rank.text="["+AbyssBar.Inputs.GameInput.BindingName((AbyssBar.Inputs.GameAction)((int)AbyssBar.Inputs.GameAction.Upgrade1+i))+"]"+views[i].rankSuffix;
        }
        private void OnEnable(){AbyssBar.Inputs.GameInput.BindingsChanged+=RefreshShortcutLabels;}
        private void OnDisable() { AbyssBar.Inputs.GameInput.BindingsChanged-=RefreshShortcutLabels;viewVersion++; requestChoice = null; SetInputReady(false); }
        private static string Quality(UpgradeQuality q) => q == UpgradeQuality.Gold ? "金色" : q == UpgradeQuality.Purple ? "紫色" : "白色";
        private static string Category(UpgradeCategory c) => c == UpgradeCategory.General ? "通用" : c == UpgradeCategory.Item ? "道具" : c == UpgradeCategory.Personal ? "角色專屬" : "角色外援";
        private static RectTransform Rect(string name, Transform parent)
        { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return go.GetComponent<RectTransform>(); }
        private static void Stretch(RectTransform rect) => Box(rect, Vector2.zero, Vector2.one);
        private static void Box(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size)
        {
            var rect = Rect(name, parent); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.fontSize = size; text.color = Color.white; text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; return text;
        }
    }
}

