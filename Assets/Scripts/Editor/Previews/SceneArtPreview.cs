using System;
using System.Collections.Generic;
using System.Linq;
using AbyssBar.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SceneArtPreview
{
    public const string HubRootName = "BarHub editor preview (temporary)";
    private const string HubToggleMenu = "Abyss Bar/Hub/Editor Preview/Show Current Art";
    private const string HubPreference = "AbyssBar.Hub.EditorPreview.Enabled";
    private const HideFlags HubPreviewFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
    private static readonly Dictionary<Scene, Action> cleanup = new Dictionary<Scene, Action>();
    private static bool requested = true;

    static SceneArtPreview()
    {
        EditorApplication.update += Update;
        EditorSceneManager.sceneOpened += (scene, mode) => RequestRefresh();
        EditorSceneManager.sceneClosing += (scene, removing) => Clear(scene);
        EditorSceneManager.sceneSaving += (scene, path) => Clear(scene);
        EditorSceneManager.sceneSaved += scene => RequestRefresh();
        AssemblyReloadEvents.beforeAssemblyReload += ClearAll;
        EditorApplication.quitting += ClearAll;
        Undo.undoRedoPerformed += RequestRefresh;
        Undo.postprocessModifications += changes =>
        {
            RequestRefresh();
            return changes;
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ClearAll();
            if (state == PlayModeStateChange.EnteredEditMode)
                RequestRefresh();
        };
    }

    private static bool ShowHubArt => EditorPrefs.GetBool(HubPreference, true);
    internal static void RequestRefresh() => requested = true;

    private static void Update()
    {
        if (!requested || EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;

        try { Rebuild(); }
        catch (Exception error) { Debug.LogException(error); }
    }

    [MenuItem("Abyss Bar/Scene Preview/Refresh Current Art")]
    [MenuItem("Abyss Bar/Hub/Editor Preview/Refresh Preview")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        requested = false;
        ClearAll();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            if (scene.path == "Assets/Scenes/BarHub.unity" && ShowHubArt)
                BuildHub(scene);
            else if (scene.path == "Assets/Scenes/Main.unity")
            {
                var preview = BattleEditorPreview.Create(scene);
                if (preview != null)
                    cleanup.Add(scene, preview.Clear);
            }
        }
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private static void BuildHub(Scene scene)
    {
        var art = HubArtCatalog.Load();
        var layout = HubLayoutData.Load();
        if (art == null || layout == null)
            return;

        var root = new GameObject(HubRootName) { hideFlags = HubPreviewFlags };
        SceneManager.MoveGameObjectToScene(root, scene);
        var composer = root.AddComponent<HubSceneComposer>();
        void Release()
        {
            if (composer != null)
                composer.ReleaseVisuals();
            if (root != null)
                Object.DestroyImmediate(root);
        }

        try
        {
            composer.Compose(scene, art, layout, true);
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.hideFlags = HubPreviewFlags;
            cleanup.Add(scene, Release);
        }
        catch
        {
            Release();
            throw;
        }
    }

    private static void Clear(Scene scene)
    {
        if (!cleanup.TryGetValue(scene, out var release))
            return;
        cleanup.Remove(scene);
        release();
    }

    private static void ClearAll()
    {
        foreach (var scene in cleanup.Keys.ToArray())
            Clear(scene);
    }

    [MenuItem(HubToggleMenu)]
    private static void ToggleHub()
    {
        EditorPrefs.SetBool(HubPreference, !ShowHubArt);
        Rebuild();
    }

    [MenuItem(HubToggleMenu, true)]
    private static bool ValidateToggle()
    {
        Menu.SetChecked(HubToggleMenu, ShowHubArt);
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Abyss Bar/Hub/Editor Preview/Edit Layout Data")]
    private static void EditLayout()
    {
        Selection.activeObject = HubLayoutData.Load();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
}

internal sealed class ScenePreviewAssetWatcher : AssetPostprocessor
{
    private static readonly string[] PreviewFolders =
    {
        "Assets/Art/", "Assets/Data/Characters/", "Assets/Data/Skins/",
        "Assets/Resources/Hub/", "Assets/Resources/HubMotion/",
        "Assets/Resources/CharacterPresentation/", "Assets/Resources/Mio/"
    };

    private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (imported.Concat(deleted).Concat(moved).Concat(movedFrom)
            .Any(path => PreviewFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal))))
            SceneArtPreview.RequestRefresh();
    }
}
