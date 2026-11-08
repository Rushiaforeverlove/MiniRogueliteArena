#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class HordeWaveInstaller
{
    private const string PlanPath = "Assets/Data/Waves/StationHordeTenWaves.asset";

    [MenuItem("Abyss Bar/Horde Waves/1 - Prepare Horde Plan")]
    public static void Prepare()
    {
        RequireEditMode();
        EnsurePlan();
    }

    [MenuItem("Abyss Bar/Horde Waves/2 - Apply To Main")]
    public static void ApplyToMain()
    {
        RequireEditMode();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main" || !scene.isLoaded)
            throw new InvalidOperationException("Open the Main battle scene first.");

        // Reuse the previous installer's complete reference/configuration check.
        // This update expects the already tested ten-wave system to be installed.
        TenWaveInstaller.Validate();
        var managers = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<WaveManager>(true)).ToArray();
        if (managers.Length != 1)
            throw new InvalidOperationException("Main must contain exactly one WaveManager.");

        var plan = EnsurePlan();
        var data = new SerializedObject(managers[0]);
        var assignedPlan = data.FindProperty("wavePlan");
        if (assignedPlan.objectReferenceValue != plan)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath(
                "Assets/Scenes/Backups/Main_BeforeHordeWaves.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new InvalidOperationException("Scene backup failed; wave plan was not switched.");
            Debug.Log("Scene backup: " + backup);

            Undo.SetCurrentGroupName("Apply station horde wave plan");
            assignedPlan.objectReferenceValue = plan;
            data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
        }
        Selection.activeObject = plan;
        LogSummary(plan);
        Debug.Log("Horde plan assigned. Save Main with Ctrl+S. Existing plan assets and manual tuning are preserved.");
    }

    [MenuItem("Abyss Bar/Horde Waves/3 - Validate Main And Show Counts")]
    public static void ValidateMain()
    {
        RequireEditMode();
        TenWaveInstaller.Validate();
        Scene scene = SceneManager.GetActiveScene();
        var managers = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<WaveManager>(true)).ToArray();
        if (managers.Length != 1)
            throw new InvalidOperationException("Main must contain exactly one WaveManager.");
        var plan = new SerializedObject(managers[0]).FindProperty("wavePlan")
            .objectReferenceValue as BattleWavePlan;
        LogSummary(plan);
        if (AssetDatabase.GetAssetPath(plan) != PlanPath)
            Debug.LogWarning("Main uses another plan. Run Horde Waves / 2 to select the horde plan.");
    }

    private static BattleWavePlan EnsurePlan()
    {
        EnsureFolder("Assets/Data/Waves");
        var plan = AssetDatabase.LoadAssetAtPath<BattleWavePlan>(PlanPath);
        if (plan == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(PlanPath) != null)
                throw new InvalidOperationException("Horde plan path is occupied by another asset type.");
            plan = ScriptableObject.CreateInstance<BattleWavePlan>();
            plan.planId = StationHordeWavePreset.PlanId;
            plan.intermissionSeconds = StationHordeWavePreset.IntermissionSeconds;
            plan.waves = StationHordeWavePreset.CreateWaves();
            AssetDatabase.CreateAsset(plan, PlanPath);
            AssetDatabase.SaveAssets();
        }
        if (!plan.TrySnapshot(out _, out string error))
            throw new InvalidOperationException("Invalid horde plan: " + error);
        Selection.activeObject = plan;
        return plan;
    }

    private static void LogSummary(BattleWavePlan plan)
    {
        if (plan == null || !plan.TrySnapshot(out _, out _))
            throw new InvalidOperationException("Assign a valid wave plan first.");
        long total = plan.waves.Sum(w => (long)w.enemyCount);
        string rows = string.Join("\n", plan.waves.Select((w, i) =>
            $"Wave {i + 1}: total {w.enemyCount}, max alive {w.maximumAlive}, interval {w.spawnInterval:0.00}s"));
        Debug.Log($"{plan.name}: {plan.waves.Length} waves / {total} enemies / rest {plan.intermissionSeconds}s\n{rows}");
    }

    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before changing wave plans.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
}
#endif
