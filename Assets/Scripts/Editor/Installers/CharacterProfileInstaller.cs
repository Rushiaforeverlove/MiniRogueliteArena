#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using AbyssBar.Characters;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CharacterProfileInstaller
{
    private const string Folder = "Assets/Data/Characters/Profiles/";
    private const string SeedPath = "Assets/Data/Characters/ProfileSeeds/character-profiles.json";
    [Serializable] private sealed class Seed { public Row[] characters; }
    [Serializable] private sealed class Row { public string characterId, profileAsset; public Fields profile; }
    [Serializable] private sealed class Fields
    {
        public string characterName, roleTitle, combatRole, summary, strength, caution;
        public string[] specialties;
        public string baseEHint, enhancedEHint, baseQHint, enhancedQHint;
    }
    [MenuItem("Abyss Bar/Character Profiles/1 - Prepare Names And Profiles")]
    public static void Prepare()
    {
        RequireEditMode(); var seed = ReadSeed(); var roster = Resolve(seed);
        EnsureFolder(Folder.TrimEnd('/'));
        string backupDirectory = null;
        for (int i = 0; i < seed.characters.Length; i++)
        {
            var row = seed.characters[i]; var character = roster[i];
            string path = Folder + row.profileAsset + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<CharacterProfileData>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<CharacterProfileData>();
                var f = row.profile;
                profile.characterName = f.characterName; profile.roleTitle = f.roleTitle; profile.combatRole = f.combatRole;
                profile.summary = f.summary; profile.specialties = f.specialties; profile.strength = f.strength; profile.caution = f.caution;
                profile.baseEHint = f.baseEHint; profile.enhancedEHint = f.enhancedEHint;
                profile.baseQHint = f.baseQHint; profile.enhancedQHint = f.enhancedQHint;
                if (!profile.IsValid) { UnityEngine.Object.DestroyImmediate(profile); throw new InvalidOperationException("Incomplete profile seed: " + row.characterId); }
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.IsValid) throw new InvalidOperationException("Complete profile fields on " + path);
            // An intentional custom profile reference is never silently replaced.
            if (character.IdentityProfile != null) continue;
            if (backupDirectory == null)
            {
                backupDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    "Backups/CharacterProfiles", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
                Directory.CreateDirectory(backupDirectory);
            }
            string original = AssetDatabase.GetAssetPath(character);
            string originalFile = Path.Combine(Directory.GetParent(Application.dataPath).FullName, original);
            string backupFile = Path.Combine(backupDirectory, AssetDatabase.AssetPathToGUID(original) + "_" + Path.GetFileName(original));
            // Keep backups outside Assets so duplicate CharacterIds never enter
            // FindAssets, upgrade rosters, or the shipped build.
            File.Copy(originalFile, backupFile, false);
            if (File.Exists(originalFile + ".meta")) File.Copy(originalFile + ".meta", backupFile + ".meta", false);
            var so = new SerializedObject(character); so.FindProperty("identityProfile").objectReferenceValue = profile;
            so.ApplyModifiedProperties(); EditorUtility.SetDirty(character);
        }
        AssetDatabase.SaveAssets();
        if (backupDirectory != null) Debug.Log("Character data backup (outside Assets): " + backupDirectory);
        Debug.Log("Character profiles prepared. Stable IDs, stats, weapons, skills, skins and save keys retained; existing profile edits preserved.");
    }
    [MenuItem("Abyss Bar/Character Profiles/2 - Install In BarHub")]
    public static void Install()
    {
        RequireEditMode(); var scene = SceneManager.GetActiveScene();
        if (scene.name != "BarHub") throw new InvalidOperationException("Open BarHub first.");
        var hub = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();
        var so = new SerializedObject(hub);
        var panel = so.FindProperty("selectionPanel").objectReferenceValue as GameObject;
        var overview = so.FindProperty("selectedName").objectReferenceValue as TMP_Text;
        var dialogue = so.FindProperty("dialogueText").objectReferenceValue as TMP_Text;
        var confirm = so.FindProperty("confirmButton").objectReferenceValue as Button;
        var talk = so.FindProperty("talkButton").objectReferenceValue as GameObject;
        var close = panel != null ? panel.GetComponentsInChildren<Button>(true).SingleOrDefault(b =>
            Enumerable.Range(0, b.onClick.GetPersistentEventCount()).Any(i => b.onClick.GetPersistentMethodName(i) == "ClosePreview")) : null;
        if (panel == null || overview == null || dialogue == null || confirm == null || talk == null || close == null
            || overview.font == null || !overview.transform.IsChildOf(panel.transform) || !dialogue.transform.IsChildOf(panel.transform))
            throw new InvalidOperationException("Check selection panel, Chinese font, dialogue, confirmation/talk and ClosePreview button references first.");
        Prepare(); EnsureFolder("Assets/Scenes/Backups");
        string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/BarHub_BeforeCharacterProfiles.unity");
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed; layout not changed.");
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install character profiles");
        var details = so.FindProperty("profileDetails").objectReferenceValue as TMP_Text;
        if (details != null && !details.transform.IsChildOf(panel.transform))
            throw new InvalidOperationException("Profile Details must be a child of SelectionPanel.");
        if (details == null)
        {
            var existing = panel.transform.Find("ProfileDetails");
            if (existing != null) details = existing.GetComponent<TMP_Text>();
            if (details == null)
            {
                var go = new GameObject("ProfileDetails", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.layer = panel.layer; go.transform.SetParent(panel.transform, false); details = go.GetComponent<TMP_Text>();
                Undo.RegisterCreatedObjectUndo(go, "Create profile details");
            }
        }
        so.FindProperty("profileDetails").objectReferenceValue = details; so.ApplyModifiedProperties();
        Box((RectTransform)panel.transform, new Vector2(.06f, .04f), new Vector2(.94f, .96f));
        var background = panel.GetComponent<Image>();
        if (background != null) { Undo.RecordObject(background, "Profile background"); background.color = new Color(.055f, .07f, .1f, .98f); }
        Style(overview, overview.font, 22, 18); Box(overview.rectTransform, new Vector2(.04f, .25f), new Vector2(.475f, .945f));
        Style(details, overview.font, 21, 17); Box(details.rectTransform, new Vector2(.525f, .25f), new Vector2(.96f, .945f));
        Style(dialogue, overview.font, 21, 17); Box(dialogue.rectTransform, new Vector2(.04f, .135f), new Vector2(.96f, .225f));
        Box((RectTransform)close.transform, new Vector2(.04f, .035f), new Vector2(.31f, .115f));
        Box((RectTransform)talk.transform, new Vector2(.365f, .035f), new Vector2(.635f, .115f));
        Box((RectTransform)confirm.transform, new Vector2(.69f, .035f), new Vector2(.96f, .115f));
        foreach (var button in new[] { close, talk.GetComponent<Button>(), confirm })
        {
            if (button == null) continue;
            if (button.image != null)
            {
                Undo.RecordObject(button.image, "Profile button contrast");
                button.image.color = button == confirm ? new Color(.12f, .4f, .42f) : new Color(.2f, .24f, .3f);
            }
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { Style(label, overview.font, 23, 17); label.alignment = TextAlignmentOptions.Center; }
        }
        var first = Resolve(ReadSeed())[0]; overview.text = CharacterProfileText.Overview(first); details.text = CharacterProfileText.Details(first);
        EditorUtility.SetDirty(hub); EditorSceneManager.MarkSceneDirty(scene); Undo.CollapseUndoOperations(undo);
        Debug.Log("Profile layout installed; OnClick bindings retained. Scene backup: " + backup + ". Save BarHub with Ctrl+S.");
    }
    [MenuItem("Abyss Bar/Character Profiles/3 - Validate Profiles And BarHub")]
    public static void Validate()
    {
        RequireEditMode(); var roster = Resolve(ReadSeed());
        foreach (var character in roster)
        {
            if (character.IdentityProfile == null || !character.IdentityProfile.IsValid) throw new InvalidOperationException("Missing/incomplete profile for " + character.CharacterId);
            Debug.Log(character.CharacterId + " -> " + character.DisplayName + " / " + character.IdentityProfile.roleTitle);
        }
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "BarHub") throw new InvalidOperationException("Open BarHub to validate installed UI.");
        var hub = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HubCharacterSelection>(true)).Single();
        var so = new SerializedObject(hub);
        var overview = so.FindProperty("selectedName").objectReferenceValue as TMP_Text;
        var details = so.FindProperty("profileDetails").objectReferenceValue as TMP_Text;
        if (overview == null || details == null || overview == details || overview.font == null || details.font == null)
            throw new InvalidOperationException("Run Character Profiles / 2 to install two distinct Chinese text columns.");
        string names = string.Concat(roster.Select(c => c.DisplayName));
        if (!overview.font.HasCharacters(names)) Debug.LogWarning("Selected Chinese font may lack new name glyphs. Check 朝霧 凜／御影 千歲 and the font fallback/dynamic atlas in Play Mode.");
        Debug.Log("Profiles validate. UI display and save retention still require native Play Mode/Windows verification.");
    }
    private static Seed ReadSeed()
    {
        var file = AssetDatabase.LoadAssetAtPath<TextAsset>(SeedPath);
        if (file == null) throw new InvalidOperationException("Missing character-profiles.json.");
        var seed = JsonUtility.FromJson<Seed>(file.text);
        if (seed?.characters == null || seed.characters.Length == 0 || seed.characters.Any(r => r == null || r.profile == null
            || string.IsNullOrWhiteSpace(r.characterId) || string.IsNullOrWhiteSpace(r.profileAsset)
            || r.profileAsset.Any(c => !char.IsLetterOrDigit(c) && c != '_')))
            throw new InvalidOperationException("Invalid character profile seed.");
        if (seed.characters.Select(r => r.characterId).Distinct(StringComparer.Ordinal).Count() != seed.characters.Length
            || seed.characters.Select(r => r.profileAsset).Distinct(StringComparer.OrdinalIgnoreCase).Count() != seed.characters.Length)
            throw new InvalidOperationException("Duplicate seed IDs or profile filenames.");
        return seed;
    }
    private static CharacterData[] Resolve(Seed seed)
    {
        var all = AssetDatabase.FindAssets("t:CharacterData").Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray();
        return seed.characters.Select(row => {
            var matches = all.Where(c => c.CharacterId == row.characterId).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException("Expected one CharacterData for " + row.characterId + "; found " + matches.Length);
            return matches[0];
        }).ToArray();
    }
    private static void Style(TMP_Text text, TMP_FontAsset font, float size, float min)
    {
        Undo.RecordObject(text, "Profile text style"); text.font = font; text.richText = true; text.fontSize = size;
        text.enableAutoSizing = true; text.fontSizeMin = min; text.fontSizeMax = size;
        text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate; text.color = new Color(.92f, .94f, .96f); text.raycastTarget = false;
    }
    private static void Box(RectTransform rect, Vector2 min, Vector2 max)
    {
        Undo.RecordObject(rect, "Profile layout"); rect.anchorMin = min; rect.anchorMax = max;
        rect.pivot = new Vector2(.5f, .5f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, last)); AssetDatabase.CreateFolder(path.Substring(0, last), path.Substring(last + 1));
    }
    private static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
}
#endif
