#if UNITY_EDITOR
using AbyssBar.Showcase;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

internal static class ShowcaseUIBuilder
{
    public static HubSelectionShowcase Build(GameObject panel, HubCharacterSelection hub, HubCameraFollow camera,
        TMP_FontAsset font, ShowcasePresentationData presentation)
    {
        var root = new GameObject("ShowcaseLayout", typeof(RectTransform)); root.layer = panel.layer;
        root.transform.SetParent(panel.transform, false); Box((RectTransform)root.transform, Vector2.zero, Vector2.one);
        Undo.RegisterCreatedObjectUndo(root, "Create character showcase");
        var left = Block("Portrait panel", root.transform, new Vector2(.015f, .025f), new Vector2(.285f, .975f));
        var right = Block("Information panel", root.transform, new Vector2(.655f, .025f), new Vector2(.985f, .975f));
        var portraitObject = new GameObject("Skin portrait", typeof(RectTransform), typeof(Image)); portraitObject.layer = panel.layer;
        portraitObject.transform.SetParent(left, false); var portrait = portraitObject.GetComponent<Image>();
        portrait.preserveAspect = true; portrait.raycastTarget = false;
        Box(portrait.rectTransform, new Vector2(.01f, .2f), new Vector2(.99f, .90f));
        var name = Text("Character name", left, font, 30, new Vector2(.04f, .91f), new Vector2(.96f, .985f)); name.richText = false;
        var skin = Text("Skin name", left, font, 20, new Vector2(.04f, .145f), new Vector2(.96f, .19f)); skin.richText = false;
        var status = Text("Skin ownership", left, font, 15, new Vector2(.04f, .1f), new Vector2(.96f, .14f));
        var previous = Button("Previous skin", "‹", left, font, new Vector2(.025f, .025f), new Vector2(.165f, .09f));
        var next = Button("Next skin", "›", left, font, new Vector2(.835f, .025f), new Vector2(.975f, .09f));
        var apply = Button("Apply skin", "套用造型", left, font, new Vector2(.2f, .025f), new Vector2(.8f, .09f));
        string[] labels = { "簡介", "普攻", "E", "Q" }; var tabs = new Button[4];
        for (int i = 0; i < 4; i++) tabs[i] = Button("Page " + i, labels[i], right, font,
            new Vector2(.04f + i * .235f, .925f), new Vector2(.255f + i * .235f, .985f));
        var title = Text("Page title", right, font, 25, new Vector2(.06f, .83f), new Vector2(.94f, .9f));
        var body = Text("Page body", right, font, 20, new Vector2(.06f, .08f), new Vector2(.94f, .8f));
        body.alignment = TextAlignmentOptions.TopLeft; body.fontSizeMin = 15;
        var demoFrame = Block("Battle demonstration", right, new Vector2(.05f, .115f), new Vector2(.95f, .53f));
        var viewport = new GameObject("Viewport", typeof(RectTransform)); viewport.layer = panel.layer; viewport.transform.SetParent(demoFrame, false);
        Box((RectTransform)viewport.transform, new Vector2(0, .18f), Vector2.one);
        var surface = new GameObject("Render surface", typeof(RectTransform), typeof(RawImage)); surface.layer = panel.layer;
        surface.transform.SetParent(viewport.transform, false); var output = surface.GetComponent<RawImage>(); output.raycastTarget = false;
        Box(output.rectTransform, Vector2.zero, Vector2.one);
        var fitter = surface.AddComponent<AspectRatioFitter>(); fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fitter.aspectRatio = 800f / 480;
        var caption = Text("Demonstration caption", demoFrame, font, 12, new Vector2(.02f, .015f), new Vector2(.61f, .16f));
        var replay = Button("Replay", "重播", demoFrame, font, new Vector2(.65f, .02f), new Vector2(.98f, .15f));
        var demo = surface.AddComponent<ShowcaseBattlePreview>(); var ds = new SerializedObject(demo);
        Ref(ds, "output", output); Ref(ds, "presentation", presentation); Ref(ds, "font", font); ds.ApplyModifiedPropertiesWithoutUndo();
        var view = root.AddComponent<HubSelectionShowcase>(); var so = new SerializedObject(view);
        Ref(so, "hub", hub); Ref(so, "cameraFollow", camera); Ref(so, "portrait", portrait); Ref(so, "characterName", name);
        Ref(so, "skinName", skin); Ref(so, "skinStatus", status); Ref(so, "applyLabel", apply.GetComponentInChildren<TMP_Text>(true));
        Ref(so, "pageTitle", title); Ref(so, "body", body); Ref(so, "demoCaption", caption);
        Ref(so, "previousSkin", previous); Ref(so, "nextSkin", next); Ref(so, "applySkin", apply); Ref(so, "replay", replay);
        Ref(so, "demoFrame", demoFrame.gameObject); Ref(so, "demonstration", demo);
        var array = so.FindProperty("tabs"); array.arraySize = 4; for (int i = 0; i < 4; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = tabs[i];
        so.ApplyModifiedPropertiesWithoutUndo(); return view;
    }
    private static RectTransform Block(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>(); image.color = new Color(.035f, .05f, .075f, .96f);
        Box(image.rectTransform, min, max); return image.rectTransform;
    }
    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>(); text.font = font; text.fontSize = size; text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(11, size - 3); text.fontSizeMax = size; text.color = new Color(.94f, .95f, .98f);
        text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false; Box(text.rectTransform, min, max); return text;
    }
    private static Button Button(string name, string label, Transform parent, TMP_FontAsset font, Vector2 min, Vector2 max)
    {
        var rect = Block(name, parent, min, max); var image = rect.GetComponent<Image>(); image.color = new Color(.15f, .3f, .34f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Text("Label", rect, font, 18, new Vector2(.04f, .02f), new Vector2(.96f, .98f)).text = label; return button;
    }
    internal static void Box(RectTransform rect, Vector2 min, Vector2 max)
    { Undo.RecordObject(rect, "Showcase layout"); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one; }
    private static void Ref(SerializedObject so, string field, UnityEngine.Object value) => so.FindProperty(field).objectReferenceValue = value;
}
#endif
