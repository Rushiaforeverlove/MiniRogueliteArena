#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AbyssBar.EnemyAnimations;
using AbyssBar.Showcase;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SelectionShowcaseInstaller
{
    private const string Art = "Assets/Art/Characters/Showcase/";
    private const string Data = "Assets/Data/Showcase/SelectionPresentation.asset";
    [Serializable] private sealed class PortraitSeed { public PortraitRow[] portraits; }
    [Serializable] private sealed class PortraitRow { public string characterId, skinId, file; }
    [Serializable] private sealed class Atlas { public string file; public int width, height; public Slice[] frames; }
    [Serializable] private sealed class Slice { public int row, col, x, y, width, height; public float pivotX, pivotY; }

    [MenuItem("Abyss Bar/Selection Showcase/1 - Prepare Portraits And Animations")]
    public static void Prepare()
    {
        EditMode(); EnsureFolder("Assets/Data/Showcase");
        var seedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "portraits.json");
        if (seedAsset == null) throw new InvalidOperationException("Missing portraits.json.");
        var seed = JsonUtility.FromJson<PortraitSeed>(seedAsset.text);
        if (seed?.portraits == null || seed.portraits.Length == 0) throw new InvalidOperationException("Empty portrait seed.");
        if (seed.portraits.Select(r => r.characterId + "/" + r.skinId).Distinct(StringComparer.Ordinal).Count() != seed.portraits.Length)
            throw new InvalidOperationException("Duplicate portrait bindings.");
        var characters = AssetDatabase.FindAssets("t:CharacterData").Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray();
        var bindings = seed.portraits.Select(r => {
            var matches = characters.Where(c => c.CharacterId == r.characterId).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one character: " + r.characterId);
            var skins = matches[0].Skins.Where(s => s != null && s.SkinId == r.skinId).ToArray();
            if (skins.Length != 1) throw new InvalidOperationException("Missing/duplicate skin " + r.skinId);
            if (r.file != Path.GetFileName(r.file) || AssetImporter.GetAtPath(Art + r.file) == null) throw new InvalidOperationException("Missing portrait PNG.");
            return new { row = r, character = matches[0], skin = skins[0] };
        }).ToArray();
        string backup = null;
        foreach (var item in bindings)
        {
            string path = Art + item.row.file; var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; importer.maxTextureSize = 2048; importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Portrait import failed: " + path);
            if (item.skin.Portrait == null)
            { Backup(item.skin, ref backup); var so = new SerializedObject(item.skin); so.FindProperty("portrait").objectReferenceValue = sprite; so.ApplyModifiedProperties(); }
            if (item.skin.UnlockedByDefault && item.character.Portrait == null)
            { Backup(item.character, ref backup); var so = new SerializedObject(item.character); so.FindProperty("portrait").objectReferenceValue = item.skin.Portrait ?? sprite; so.ApplyModifiedProperties(); }
        }
        var data = AssetDatabase.LoadAssetAtPath<ShowcasePresentationData>(Data);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ShowcasePresentationData>();
            data.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
            data.enemy = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>("Assets/Data/EnemyAnimations/WandererAnimations.asset");
            if (data.material == null || data.enemy == null || !data.enemy.IsValid) throw new InvalidOperationException("Install existing enemy animations/material first.");
            AssetDatabase.CreateAsset(data, Data);
        }
        if (data.avatars == null || data.avatars.Length == 0 || data.avatars.Any(r => r == null || !r.IsValid))
        {
            if (data.material == null) data.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
            if (data.enemy == null) data.enemy = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>("Assets/Data/EnemyAnimations/WandererAnimations.asset");
            var clips = ImportAvatar(data);
            data.avatars = bindings.Select(b => b.character).Distinct().Select(c => new ShowcaseAvatarRig {
                character = c, sourceSprite = c.Skins.FirstOrDefault(s => s != null && s.UnlockedByDefault && s.GameplaySprite != null)?.GameplaySprite ?? c.GameplaySprite,
                idle = clips[0], attack = clips[1], fan = clips[2], radial = clips[3], shock = clips[4]
            }).ToArray(); EditorUtility.SetDirty(data);
        }
        if (data.avatars == null || data.avatars.Any(r => r == null || !r.IsValid)) throw new InvalidOperationException("Invalid showcase avatar rigs.");
        AssetDatabase.SaveAssets(); Selection.activeObject = data;
        if (backup != null) Debug.Log("Original cosmetic/character data backed up outside Assets: " + backup);
        Debug.Log("Showcase ready: six portraits and30 preview-only animation frames. Gameplay sprites, stats, IDs, ownership and equipped saves unchanged.");
    }
    private static EnemySpriteClip[] ImportAvatar(ShowcasePresentationData owner)
    {
        var metadataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "avatar-slices.json");
        if (metadataFile == null) throw new InvalidOperationException("Missing avatar-slices.json.");
        var atlas = JsonUtility.FromJson<Atlas>(metadataFile.text); string path = Art + atlas.file;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing showcase avatar PNG.");
        importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture.width != atlas.width || texture.height != atlas.height || atlas.frames.Length != 30) throw new InvalidOperationException("Showcase atlas size differs from metadata.");
        var clips = new EnemySpriteClip[5]; string[] names = { "idle", "attack", "fan", "radial", "shock" };
        for (int row = 0; row < 5; row++)
        {
            var frames = new Sprite[6];
            for (int col = 0; col < 6; col++)
            {
                var f = atlas.frames.Single(s => s.row == row && s.col == col);
                if (f.x < 0 || f.y < 0 || f.width <= 0 || f.height <= 0 || f.x + f.width > texture.width || f.y + f.height > texture.height
                    || f.pivotX < 0 || f.pivotX > 1 || f.pivotY < 0 || f.pivotY > 1) throw new InvalidOperationException("Invalid showcase slice.");
                string spriteName = "showcase_" + names[row] + "_" + col;
                var sprite = AssetDatabase.LoadAllAssetsAtPath(Data).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
                if (sprite == null)
                {
                    sprite = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(f.pivotX, f.pivotY), f.height / 1.8f, 0, SpriteMeshType.FullRect);
                    sprite.name = spriteName; AssetDatabase.AddObjectToAsset(sprite, owner);
                }
                frames[col] = sprite;
            }
            clips[row] = new EnemySpriteClip { frames = frames, fps = row == 0 ? 6 : 10, impactFrame = 2 };
        }
        return clips;
    }
    [MenuItem("Abyss Bar/Selection Showcase/2 - Install In BarHub")]
    public static void Install()
    {
        EditMode(); var scene = SceneManager.GetActiveScene(); if (scene.name != "BarHub") throw new InvalidOperationException("Open BarHub first.");
        var roots = scene.GetRootGameObjects();
        var hub = roots.SelectMany(g => g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();
        var follow = roots.SelectMany(g => g.GetComponentsInChildren<HubCameraFollow>(true)).Single();
        var so = new SerializedObject(hub);
        var panel = so.FindProperty("selectionPanel").objectReferenceValue as GameObject;
        var name = so.FindProperty("selectedName").objectReferenceValue as TMP_Text;
        var detail = so.FindProperty("profileDetails").objectReferenceValue as TMP_Text;
        var confirm = so.FindProperty("confirmButton").objectReferenceValue as Button;
        var talk = so.FindProperty("talkButton").objectReferenceValue as GameObject;
        var dialogue = so.FindProperty("dialogueText").objectReferenceValue as TMP_Text;
        var close = panel != null ? panel.GetComponentsInChildren<Button>(true).SingleOrDefault(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "ClosePreview")) : null;
        if (panel == null || name == null || name.font == null || confirm == null || talk == null || dialogue == null || close == null) throw new InvalidOperationException("Check existing selection panel/font/buttons first.");
        if (roots.SelectMany(g => g.GetComponentsInChildren<Transform>(true)).Any(t => t.gameObject.layer == 31)) throw new InvalidOperationException("Layer31 is already used in BarHub; reserve an unused preview layer before installing.");
        Prepare(); EnsureFolder("Assets/Scenes/Backups");
        string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/BarHub_BeforeSelectionShowcase.unity");
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed.");
        if (so.FindProperty("showcase").objectReferenceValue != null)
        { Debug.Log("Showcase already installed; existing UI customization preserved. Use Validate and save BarHub."); return; }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install selection showcase");
        ShowcaseUIBuilder.Box((RectTransform)panel.transform, Vector2.zero, Vector2.one);
        var background = panel.GetComponent<Image>(); if (background != null) { Undo.RecordObject(background, "Clear showcase center"); background.color = new Color(0, 0, 0, .08f); }
        Undo.RecordObject(name.gameObject, "Hide legacy profile text"); name.gameObject.SetActive(false);
        if (detail != null) { Undo.RecordObject(detail.gameObject, "Hide legacy details"); detail.gameObject.SetActive(false); }
        var view = ShowcaseUIBuilder.Build(panel, hub, follow, name.font, AssetDatabase.LoadAssetAtPath<ShowcasePresentationData>(Data));
        so.FindProperty("showcase").objectReferenceValue = view; so.ApplyModifiedProperties();
        ShowcaseUIBuilder.Box((RectTransform)confirm.transform, new Vector2(.385f, .03f), new Vector2(.615f, .095f));
        ShowcaseUIBuilder.Box((RectTransform)talk.transform, new Vector2(.385f, .105f), new Vector2(.615f, .17f));
        ShowcaseUIBuilder.Box((RectTransform)close.transform, new Vector2(.435f, .93f), new Vector2(.565f, .98f));
        ShowcaseUIBuilder.Box(dialogue.rectTransform, new Vector2(.3f, .81f), new Vector2(.65f, .91f));
        Undo.RecordObject(dialogue, "Showcase dialogue style"); dialogue.enableAutoSizing = true; dialogue.fontSizeMin = 15; dialogue.fontSizeMax = 21; dialogue.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(hub); Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Selection showcase installed. Scene backup: " + backup + ". Save BarHub with Ctrl+S; test native rendering and camera focus.");
    }
    [MenuItem("Abyss Bar/Selection Showcase/3 - Validate BarHub")]
    public static void Validate()
    {
        EditMode(); var scene = SceneManager.GetActiveScene(); if (scene.name != "BarHub") throw new InvalidOperationException("Open BarHub first.");
        var hub = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();
        var view = new SerializedObject(hub).FindProperty("showcase").objectReferenceValue as HubSelectionShowcase;
        if (view == null || !view.IsConfigured) throw new InvalidOperationException("Run Selection Showcase / 1 and2; check all presentation references.");
        var raw = view.GetComponentInChildren<RawImage>(true); if (raw == null || raw.GetComponent<ShowcaseBattlePreview>() == null) throw new InvalidOperationException("Missing preview output.");
        Debug.Log("Showcase references valid. Unlocked/locked browsing, camera restore, RT and cleanup still require Play Mode and Windows verification.");
    }
    private static void Backup(UnityEngine.Object asset, ref string directory)
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        if (directory == null) { directory = Path.Combine(project, "Backups/SelectionShowcase", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")); Directory.CreateDirectory(directory); }
        string path = AssetDatabase.GetAssetPath(asset), destination = Path.Combine(directory, AssetDatabase.AssetPathToGUID(path) + "_" + Path.GetFileName(path));
        if (!File.Exists(destination)) { File.Copy(Path.Combine(project, path), destination); if (File.Exists(Path.Combine(project, path + ".meta"))) File.Copy(Path.Combine(project, path + ".meta"), destination + ".meta"); }
    }
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash)); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1)); }
    private static void EditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
}
#endif
