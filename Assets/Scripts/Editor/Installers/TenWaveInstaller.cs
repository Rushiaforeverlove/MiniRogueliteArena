#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.Waves;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TenWaveInstaller
{
    private const string PlanPath = "Assets/Data/Waves/StationTenWaves.asset";
    [MenuItem("Abyss Bar/Ten Waves/1 - Prepare Wave Plan")]
    public static void Prepare()
    {
        EnsureFolder("Assets/Data/Waves");
        var plan = AssetDatabase.LoadAssetAtPath<BattleWavePlan>(PlanPath);
        if (plan == null)
        {
            plan = ScriptableObject.CreateInstance<BattleWavePlan>();
            AssetDatabase.CreateAsset(plan, PlanPath); AssetDatabase.SaveAssets();
        }
        if (!plan.TrySnapshot(out var waves, out string error)) throw new InvalidOperationException(error);
        Selection.activeObject = plan;
        Debug.Log($"Wave plan ready: {waves.Length} waves. Existing tuning is preserved.");
    }
    [MenuItem("Abyss Bar/Ten Waves/2 - Install In Main")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing waves.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main" || !scene.isLoaded) throw new InvalidOperationException("Open the Main battle scene first.");
        var manager = Find<WaveManager>(scene); var spawner = Find<EnemySpawner>(scene);
        var hud = Find<GameHUD>(scene); var result = Find<GameResultUI>(scene); var timer = Find<GameTimer>(scene);
        if (manager == null || spawner == null || hud == null || result == null || timer == null)
            throw new InvalidOperationException("Main needs WaveManager, EnemySpawner, GameHUD, GameResultUI and GameTimer.");
        if (!spawner.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        ValidateResult(result);
        Prepare();
        var managerData = new SerializedObject(manager);
        var plan = managerData.FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        if (plan == null)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeTenWaves.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Could not back up Main; no wave scene changes applied.");
            Debug.Log("Scene backup: " + backup);
            plan = AssetDatabase.LoadAssetAtPath<BattleWavePlan>(PlanPath);
        }
        if (!plan.TrySnapshot(out _, out error)) throw new InvalidOperationException(error);
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install clear-to-advance waves");
        managerData.FindProperty("enemySpawner").objectReferenceValue = spawner;
        managerData.FindProperty("gameHUD").objectReferenceValue = hud;
        managerData.FindProperty("gameResultUI").objectReferenceValue = result;
        managerData.FindProperty("wavePlan").objectReferenceValue = plan;
        managerData.ApplyModifiedProperties();
        var timerData = new SerializedObject(timer);
        timerData.FindProperty("gameHUD").objectReferenceValue = hud;
        timerData.FindProperty("gameResultUI").objectReferenceValue = result;
        timerData.ApplyModifiedProperties();
        var hudData = new SerializedObject(hud);
        var details = hudData.FindProperty("waveDetailText").objectReferenceValue as TMP_Text;
        if (details == null)
        {
            var canvas = hud.GetComponentInParent<Canvas>() ?? Find<Canvas>(scene);
            if (canvas == null) throw new InvalidOperationException("HUD requires a Canvas.");
            var go = new GameObject("WaveStatusText", typeof(RectTransform)); go.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(go, "Create wave status text");
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -24); rect.sizeDelta = new Vector2(720, 105);
            details = Undo.AddComponent<TextMeshProUGUI>(go); details.fontSize = 26;
            details.alignment = TextAlignmentOptions.Top; details.color = Color.white; details.raycastTarget = false;
            details.enableAutoSizing = true; details.fontSizeMin = 20; details.fontSizeMax = 26;
            // Under full-screen upgrade/result overlays, while visible in normal combat.
            go.transform.SetSiblingIndex(0);
            hudData.FindProperty("waveDetailText").objectReferenceValue = details;
            hudData.ApplyModifiedProperties();
        }
        var font = AssetDatabase.FindAssets("t:TMP_FontAsset").Select(g => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault(f => f != null && (f.name.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0 || f.name.IndexOf("CJK", StringComparison.OrdinalIgnoreCase) >= 0));
        Undo.RecordObject(details, "Set wave status font"); if (font != null) details.font = font;
        details.text = $"{plan.waves.Length} 波清場挑戰\n清除全部敵人後換波";
        Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("Ten-wave clearing rules installed. Map/spawn bounds retained. Save Main with Ctrl+S.");
    }
    [MenuItem("Abyss Bar/Ten Waves/3 - Validate Main Setup")]
    public static void Validate()
    {
        var scene = SceneManager.GetActiveScene(); var manager = Find<WaveManager>(scene); var spawner = Find<EnemySpawner>(scene);
        var result = Find<GameResultUI>(scene);
        if (scene.name != "Main" || manager == null || spawner == null || result == null) throw new InvalidOperationException("Open Main and install first.");
        var plan = new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        if (plan == null || !plan.TrySnapshot(out _, out _)) throw new InvalidOperationException("Missing/invalid wave plan.");
        if (!spawner.ValidateConfiguration(out string error)) throw new InvalidOperationException(error);
        ValidateResult(result);
        Debug.Log($"Wave setup valid: {plan.waves.Length} waves, rest {plan.intermissionSeconds} s. Timer is statistics only.");
        if (plan.waves.Length != 10) Debug.LogWarning("This plan has a custom wave count. The agreed initial battle uses 10 waves.");
    }
    private static void ValidateResult(GameResultUI ui)
    {
        var data = new SerializedObject(ui);
        foreach (string field in new[] { "resultPanel", "resultTitleText", "rewardText" })
            if (data.FindProperty(field).objectReferenceValue == null) throw new InvalidOperationException("GameResultUI missing " + field);
    }
    private static T Find<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/'); string parent = path.Substring(0, last); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(last + 1));
    }
}
#endif
