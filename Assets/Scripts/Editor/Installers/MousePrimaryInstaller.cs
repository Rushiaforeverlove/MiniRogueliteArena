#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using AbyssBar.Combat;
using AbyssBar.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class MousePrimaryInstaller
{
    private const string OldRangeText = "每級普攻索敵範圍與 E 索敵距離各 +1，最多三級。";
    private const string NewRangeText = "每級普攻子彈射程與 E 索敵距離各 +1，最多三級。";
    [MenuItem("Abyss Bar/Mouse Primary/1 - Install In Main")]
    public static void Install()
    {
        RequireMain(); var scene = SceneManager.GetActiveScene();
        var player = Find<PlayerCharacter>().Single(); var attack = player.GetComponent<PlayerAutoAttack>();
        if (attack == null) throw new InvalidOperationException("Player needs the updated PlayerAutoAttack.");
        var cameras = Find<Camera>(); var camera = cameras.FirstOrDefault(c => c.CompareTag("MainCamera"));
        if (camera == null) camera = cameras.Single();
        if (Find<EventSystem>().Length != 1) throw new InvalidOperationException("Main needs one EventSystem for interactive UI blocking.");
        var upgrades = Find<UpgradeManager>().Single(); var result = Find<GameResultUI>().Single();
        EnsureFolder("Assets/Scenes/Backups");
        string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeMousePrimary.unity");
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install mouse primary attack");
        var so = new SerializedObject(attack); so.FindProperty("aimCamera").objectReferenceValue = camera;
        if (so.FindProperty("reticleMaterial").objectReferenceValue == null)
            so.FindProperty("reticleMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
        so.ApplyModifiedProperties();
        var cards = new SerializedObject(upgrades).FindProperty("cardUI").objectReferenceValue as Component;
        if (cards != null) Mark(cards.gameObject);
        var resultPanel = new SerializedObject(result).FindProperty("resultPanel").objectReferenceValue as GameObject;
        if (resultPanel != null) Mark(resultPanel);
        UpdateRangeDescription();
        Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Mouse primary installed. No UI layout or weapon tuning changed. Backup: " + backup + ". Save Main with Ctrl+S.");
    }
    [MenuItem("Abyss Bar/Mouse Primary/2 - Validate Main")]
    public static void Validate()
    {
        RequireMain(); var attack = Find<PlayerCharacter>().Single().GetComponent<PlayerAutoAttack>();
        if (attack == null || new SerializedObject(attack).FindProperty("aimCamera").objectReferenceValue == null)
            throw new InvalidOperationException("Install mouse primary in Main first.");
        if (Find<EventSystem>().Length != 1) throw new InvalidOperationException("Main needs one EventSystem.");
        foreach (var weapon in AssetDatabase.FindAssets("t:WeaponData").Select(g => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))))
        {
            if (weapon.ProjectilePrefab == null || weapon.ProjectilePrefab.GetComponent<Projectile>() == null
                || !Positive(weapon.AttackRange) || !Positive(weapon.AttackInterval)) throw new InvalidOperationException("Invalid weapon: " + weapon.name);
            Debug.Log(weapon.DisplayName + " / base interval " + weapon.AttackInterval + "s / normal projectile range " + weapon.AttackRange);
        }
        Debug.Log("Mouse primary references validate. Native pointer projection, UI/alt-tab safety, range collision and Windows play still need testing.");
    }
    private static void Mark(GameObject root)
    { if (root.GetComponent<PrimaryAttackUiBlocker>() == null) Undo.AddComponent<PrimaryAttackUiBlocker>(root); }
    private static void UpdateRangeDescription()
    {
        var card = AssetDatabase.FindAssets("t:UpgradeCardData").Select(g => AssetDatabase.LoadAssetAtPath<UpgradeCardData>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(c => c != null && c.id == "SCT-W06");
        string seedPath = "Assets/Resources/RunUpgradeSeed.json";
        var seed = AssetDatabase.LoadAssetAtPath<TextAsset>(seedPath);
        bool updateCard = card != null && card.description == OldRangeText;
        bool updateSeed = seed != null && seed.text.Contains(OldRangeText);
        if (!updateCard && !updateSeed) return;
        string project = Directory.GetParent(Application.dataPath).FullName;
        string folder = Path.Combine(project, "Backups/MousePrimary", DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff")); Directory.CreateDirectory(folder);
        Action<string> backup = path => {
            string target = Path.Combine(folder, AssetDatabase.AssetPathToGUID(path) + "_" + Path.GetFileName(path));
            File.Copy(Path.Combine(project, path), target);
            if (File.Exists(Path.Combine(project, path + ".meta"))) File.Copy(Path.Combine(project, path + ".meta"), target + ".meta");
        };
        if (updateCard) { backup(AssetDatabase.GetAssetPath(card)); Undo.RecordObject(card, "Range lens description"); card.description = NewRangeText; EditorUtility.SetDirty(card); }
        if (updateSeed) { backup(seedPath); File.WriteAllText(Path.Combine(project, seedPath), seed.text.Replace(OldRangeText, NewRangeText), new UTF8Encoding(false)); AssetDatabase.ImportAsset(seedPath); }
        AssetDatabase.SaveAssets(); Debug.Log("Updated default range-lens wording; tuning unchanged. Asset backup: " + folder);
    }
    private static bool Positive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    private static T[] Find<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    private static void RequireMain()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().name != "Main") throw new InvalidOperationException("Open Main first.");
    }
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, i)); AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1)); }
}
#endif
