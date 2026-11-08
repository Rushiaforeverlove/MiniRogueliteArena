#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AbyssBar.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RunUpgradeInstaller
{
    private const string Folder = "Assets/Data/RunUpgrades";
    [Serializable] private sealed class Seed { public SeedCard[] cards; }
    [Serializable] private sealed class SeedCard
    {
        public string id, title, description, ownerRoleId, effectKey, artBrief;
        public int category, quality, maxRank;
        public float drawWeight;
        public bool requiresQ, requiresNodeFive;
        public UpgradeNumber[] numbers;
    }
    [MenuItem("Abyss Bar/Run Upgrades/1 - Prepare Data Assets")]
    public static void PrepareAssets()
    {
        var seedFile = Resources.Load<TextAsset>("RunUpgradeSeed");
        if (seedFile == null) throw new InvalidOperationException("Copy Assets/Resources/RunUpgradeSeed.json first.");
        var seed = JsonUtility.FromJson<Seed>(seedFile.text);
        if (seed?.cards == null || seed.cards.Length == 0) throw new InvalidOperationException("Empty upgrade seed.");
        EnsureFolder(Folder); EnsureFolder(Folder + "/Cards"); EnsureFolder("Assets/Resources");
        var cards = new List<UpgradeCardData>();
        foreach (var s in seed.cards)
        {
            string path = Folder + "/Cards/" + s.id + ".asset";
            var card = AssetDatabase.LoadAssetAtPath<UpgradeCardData>(path);
            if (card == null)
            {
                card = ScriptableObject.CreateInstance<UpgradeCardData>();
                card.id = s.id; card.title = s.title; card.description = s.description;
                card.ownerRoleId = s.ownerRoleId; card.effectKey = s.effectKey; card.artBrief = s.artBrief;
                card.category = (UpgradeCategory)s.category; card.quality = (UpgradeQuality)s.quality;
                card.maxRank = s.maxRank; card.drawWeight = s.drawWeight;
                card.requiresQ = s.requiresQ; card.requiresNodeFive = s.requiresNodeFive; card.numbers = s.numbers;
                AssetDatabase.CreateAsset(card, path);
            }
            cards.Add(card);
        }
        string dbPath = "Assets/Resources/RunUpgradeDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<UpgradeDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<UpgradeDatabase>(); db.cards = cards.ToArray();
            db.roster = AssetDatabase.FindAssets("t:CharacterData").Select(g => AssetDatabase.LoadAssetAtPath<CharacterData>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(c => c.CharacterId, StringComparer.Ordinal).Select(c => new UpgradeRosterEntry { character = c, unlockedByDefault = true }).ToArray();
            AssetDatabase.CreateAsset(db, dbPath);
        }
        else
        {
            // Preserve edits to existing cards/weights/roster. Only append missing seed IDs.
            Undo.RecordObject(db, "Append upgrade cards");
            db.cards = db.cards.Concat(cards.Where(c => !db.cards.Any(old => old != null && old.id == c.id))).ToArray(); EditorUtility.SetDirty(db);
        }
        if (Resources.Load<CombatBalance>("CombatBalance") == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<CombatBalance>(), "Assets/Resources/CombatBalance.asset");
        AssetDatabase.SaveAssets(); Validate(); Selection.activeObject = db;
        Debug.Log("Run upgrades: prepared data. Existing card tuning, art and definitions were preserved.");
    }
    [MenuItem("Abyss Bar/Run Upgrades/2 - Install In Open Main Scene")]
    public static void InstallScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var player = UnityEngine.Object.FindAnyObjectByType<PlayerCharacter>();
        var manager = UnityEngine.Object.FindAnyObjectByType<UpgradeManager>();
        var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (player == null || manager == null || canvas == null) throw new InvalidOperationException("Open the Main combat scene with PlayerCharacter, UpgradeManager and Canvas.");
        PrepareAssets();
        Undo.IncrementCurrentGroup(); int undoGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install run upgrades");
        if (player.GetComponent<RunUpgradeRuntime>() == null) Undo.AddComponent<RunUpgradeRuntime>(player.gameObject);
        var serialized = new SerializedObject(manager);
        serialized.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
        var font = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(f => f != null && (f.name.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0 || f.name.IndexOf("CJK", StringComparison.OrdinalIgnoreCase) >= 0));
        if (font != null) serialized.FindProperty("chineseFont").objectReferenceValue = font;
        var ui = serialized.FindProperty("cardUI").objectReferenceValue as UpgradeChoiceUI;
        if (ui == null)
        { ui = UpgradeChoiceUI.Build(canvas, font); Undo.RegisterCreatedObjectUndo(ui.gameObject, "Create four-choice UI"); serialized.FindProperty("cardUI").objectReferenceValue = ui; }
        serialized.ApplyModifiedProperties();
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Debug.Log("Installed four-choice UI and battle runtime. Save Main with Ctrl+S; check Chinese Font on UpgradeManager.");
    }
    [MenuItem("Abyss Bar/Run Upgrades/3 - Validate Database")]
    public static void Validate()
    {
        var db = Resources.Load<UpgradeDatabase>("RunUpgradeDatabase");
        if (db == null) { Debug.LogError("No RunUpgradeDatabase; prepare data first."); return; }
        var errors = db.Validate(); foreach (var e in errors) Debug.LogError(e);
        if (errors.Count == 0) Debug.Log($"Validated {db.cards.Length} cards, supported effects, IDs and parameters.");
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/'); string parent = path.Substring(0, last); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(last + 1));
    }
}
#endif
