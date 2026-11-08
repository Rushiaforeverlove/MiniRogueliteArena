using AbyssBar.Hub;
using AbyssBar.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScenePreviewMenu
{
    private const string MenuPath = "Abyss Bar/Scene Preview/";

    [MenuItem(MenuPath + "1 - Main Menu")]
    private static void OpenMainMenu() => Open("MainMenu");

    [MenuItem(MenuPath + "2 - Bar Hub")]
    private static void OpenBarHub() => Open("BarHub");

    [MenuItem(MenuPath + "3 - Station Battle")]
    private static void OpenBattle() => Open("Main");

    [MenuItem(MenuPath + "Frame Current Scene")]
    public static void FrameCurrentScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        var bounds = GetFramingBounds(scene);
        if (!bounds.HasValue)
            return;
        view.in2DMode = true;
        view.Frame(bounds.Value, false);
        view.Focus();
    }

    [MenuItem(MenuPath + "Show Camera Preview")]
    private static void ShowCameraPreview() => EditorApplication.ExecuteMenuItem("Window/General/Game");

    private static void Open(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity");
        SceneArtPreview.Rebuild();
        FrameCurrentScene();
    }

    internal static Bounds? GetFramingBounds(Scene scene)
    {
        switch (scene.name)
        {
            case "MainMenu":
                var canvas = FindInScene<Canvas>(scene);
                if (canvas == null)
                    return null;
                var corners = new Vector3[4];
                Canvas.ForceUpdateCanvases();
                ((RectTransform)canvas.transform).GetWorldCorners(corners);
                var bounds = new Bounds(corners[0], Vector3.zero);
                foreach (var corner in corners)
                    bounds.Encapsulate(corner);
                return bounds;
            case "BarHub":
                var layout = HubLayoutData.Load();
                return layout == null ? null : new Bounds(layout.bounds.center, layout.bounds.size);
            case "Main":
                return FindInScene<ArenaMapBounds>(scene)?.BackgroundRenderer?.bounds;
            default:
                return null;
        }
    }

    internal static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var component = root.GetComponentInChildren<T>();
            if (component != null)
                return component;
        }
        return null;
    }
}
