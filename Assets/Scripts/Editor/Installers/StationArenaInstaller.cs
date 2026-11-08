#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StationArenaInstaller
{
    private const string Art = "Assets/Art/Maps/StationArena/";
    private const string Data = "Assets/Data/Maps/StationArena/";
    private const string RootName = "StationArena";
    [Serializable] private sealed class SliceFile { public int imageWidth, imageHeight; public Slice[] sprites; }
    [Serializable] private sealed class Slice { public string name; public PixelRect rect; public Vector2 pivot; }
    [Serializable] private sealed class PixelRect { public float x, y, width, height; }

    [MenuItem("Abyss Bar/Station Arena/1 - Prepare Artwork And Map Data")]
    public static void Prepare()
    {
        EnsureFolder(Data.TrimEnd('/'));
        ImportTexture(Art + "station-plaza-background-v1.png", true);
        ImportTexture(Art + "station-props-atlas-v1.png", false);
        var library = PrepareProps();
        string materialPath = Data + "StationArtwork.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("A compatible Sprite shader could not be found.");
            material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
        }
        string path = Data + "StationArena.asset";
        var map = AssetDatabase.LoadAssetAtPath<ArenaMapData>(path);
        if (map == null)
        {
            map = ScriptableObject.CreateInstance<ArenaMapData>();
            map.background = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "station-plaza-background-v1.png");
            map.spriteMaterial = material;
            map.decorations = new[] {
                Prop("west_supply", library.crates, new Vector2(-26, -2), 3, new Vector2(2, .5f)),
                Prop("east_barrels", library.barrels, new Vector2(26, -5), 2.5f, new Vector2(1.6f, .5f)),
                Prop("south_barricade", library.barricade, new Vector2(-12, -16.5f), 4, new Vector2(3, .5f)),
                Prop("south_lamp", library.lamp, new Vector2(18, -16.5f), 1.3f, new Vector2(.4f, .4f)) };
            AssetDatabase.CreateAsset(map, path);
        }
        if (!map.Validate(out string error)) throw new InvalidOperationException(error);
        AssetDatabase.SaveAssets(); Selection.activeObject = map;
        Debug.Log("Station artwork and map data ready. Existing map layout edits are preserved.");
    }
    private static ArenaDecoration Prop(string id, Sprite sprite, Vector2 position, float width, Vector2 colliderSize)
        => new ArenaDecoration { id = id, sprite = sprite, position = position, width = width, collisionSize = colliderSize };

    private static void ImportTexture(string path, bool singleSprite)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing PNG: " + path);
        importer.textureType = singleSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 4096; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = !singleSprite;
        importer.sRGBTexture = true; importer.isReadable = false;
        if (singleSprite)
        {
            importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 32;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }
        importer.SaveAndReimport();
    }
    private static StationPropLibrary PrepareProps()
    {
        string path = Data + "StationProps.asset";
        var existing = AssetDatabase.LoadAssetAtPath<StationPropLibrary>(path);
        if (existing != null && existing.crates != null && existing.barrels != null && existing.barricade != null && existing.lamp != null) return existing;
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "station-props-atlas-v1.png");
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "station-props-slices.json");
        if (atlas == null || json == null) throw new InvalidOperationException("Missing prop atlas/slice metadata.");
        var slices = JsonUtility.FromJson<SliceFile>(json.text);
        if (slices == null || slices.sprites == null || slices.sprites.Length != 4 || atlas.width != slices.imageWidth || atlas.height != slices.imageHeight)
            throw new InvalidOperationException("Atlas dimensions or slicing metadata do not match.");
        var library = existing != null ? existing : ScriptableObject.CreateInstance<StationPropLibrary>();
        if (existing == null) AssetDatabase.CreateAsset(library, path);
        foreach (var entry in slices.sprites)
        {
            var r = entry.rect;
            if (r.x < 0 || r.y < 0 || r.x + r.width > atlas.width || r.y + r.height > atlas.height)
                throw new InvalidOperationException("Out of bounds sprite: " + entry.name);
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == entry.name);
            if (sprite == null)
            {
                sprite = Sprite.Create(atlas, new Rect(r.x, r.y, r.width, r.height), entry.pivot, 32, 0, SpriteMeshType.FullRect);
                sprite.name = entry.name; AssetDatabase.AddObjectToAsset(sprite, library);
            }
            switch (entry.name)
            {
                case "supply-crates": library.crates = sprite; break;
                case "rusted-barrels": library.barrels = sprite; break;
                case "rail-barricade": library.barricade = sprite; break;
                case "station-lamp": library.lamp = sprite; break;
            }
        }
        EditorUtility.SetDirty(library); AssetDatabase.SaveAssets(); return library;
    }

    [MenuItem("Abyss Bar/Station Arena/2 - Install Or Rebuild In Main")]
    public static void Install()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before installing the map.");
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded || scene.name != "Main") throw new InvalidOperationException("Open the Main battle scene first.");
        var player = FindInScene<PlayerCharacter>(scene);
        var camera = FindInScene<CameraFollow>(scene);
        var spawner = FindInScene<EnemySpawner>(scene);
        if (player == null || camera == null || spawner == null) throw new InvalidOperationException("Main needs PlayerCharacter, CameraFollow and EnemySpawner.");
        Prepare();
        var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
        var oldFloor = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true))
            .FirstOrDefault(r => r.gameObject.name == "ArenaFloor");
        if (root == null)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeStation.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Could not back up Main. No map changes applied.");
            Debug.Log("Scene backup saved: " + backup);
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Install station arena");
        if (root == null)
        {
            root = new GameObject(RootName); SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = oldFloor != null ? new Vector3(oldFloor.bounds.center.x, oldFloor.bounds.center.y, 0) : Vector3.zero;
            Undo.RegisterCreatedObjectUndo(root, "Create station arena");
        }
        if (root.transform.lossyScale != Vector3.one || Quaternion.Angle(root.transform.rotation, Quaternion.identity) > .01f)
            throw new InvalidOperationException("StationArena must use scale 1 and zero rotation. Change the map asset dimensions instead.");
        var bounds = root.GetComponent<ArenaMapBounds>() ?? Undo.AddComponent<ArenaMapBounds>(root);
        var map = bounds.Map != null ? bounds.Map : AssetDatabase.LoadAssetAtPath<ArenaMapData>(Data + "StationArena.asset");
        if (!map.Validate(out string error)) throw new InvalidOperationException(error);
        foreach (string name in new[] { "Generated_Visuals", "Generated_Collision", "Generated_Decorations" })
        { var old = root.transform.Find(name); if (old != null) Undo.DestroyObjectImmediate(old.gameObject); }
        var visuals = Child("Generated_Visuals", root.transform);
        var floor = Child("StationBackground", visuals.transform).AddComponent<SpriteRenderer>();
        floor.sprite = map.background; floor.color = Color.white; floor.sharedMaterial = map.spriteMaterial;
        floor.sortingOrder = -10000; floor.spriteSortPoint = SpriteSortPoint.Center;
        floor.transform.localPosition = new Vector3(map.ArtworkLocalCenter.x, map.ArtworkLocalCenter.y, 1);
        Vector2 artSize = map.ArtworkWorldSize;
        floor.transform.localScale = new Vector3(artSize.x / floor.sprite.bounds.size.x, artSize.y / floor.sprite.bounds.size.y, 1);
        var walls = Child("Generated_Collision", root.transform);
        Vector2 half = map.playableSize * .5f; float t = map.wallThickness;
        Wall(walls.transform, "Wall_Left", new Vector2(-half.x - t * .5f, 0), new Vector2(t, map.playableSize.y + 2 * t));
        Wall(walls.transform, "Wall_Right", new Vector2(half.x + t * .5f, 0), new Vector2(t, map.playableSize.y + 2 * t));
        Wall(walls.transform, "Wall_Top", new Vector2(0, half.y + t * .5f), new Vector2(map.playableSize.x + 2 * t, t));
        Wall(walls.transform, "Wall_Bottom", new Vector2(0, -half.y - t * .5f), new Vector2(map.playableSize.x + 2 * t, t));
        var decor = Child("Generated_Decorations", root.transform);
        foreach (var entry in map.decorations)
        {
            var go = Child(entry.id, decor.transform); go.transform.localPosition = entry.position;
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = entry.sprite; renderer.sharedMaterial = map.spriteMaterial;
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            float scale = entry.width / entry.sprite.bounds.size.x; go.transform.localScale = new Vector3(scale, scale, 1);
            go.AddComponent<SpriteDepthSort>();
            if (entry.blocksMovement)
            {
                // Collider uses map units and does not inherit the sprite's art scale.
                var body = Child(entry.id + "_Body", walls.transform); body.transform.localPosition = entry.position + entry.collisionOffset;
                body.tag = "Wall"; body.AddComponent<BoxCollider2D>().size = entry.collisionSize;
            }
        }
        if (root.transform.Find("Custom_Decorations") == null) Child("Custom_Decorations", root.transform);
        Undo.RecordObject(bounds, "Configure map bounds"); bounds.Configure(map, floor);
        var cameraData = new SerializedObject(camera); cameraData.FindProperty("arenaFloor").objectReferenceValue = floor; cameraData.ApplyModifiedProperties();
        var spawnData = new SerializedObject(spawner); spawnData.FindProperty("arenaBounds").objectReferenceValue = bounds;
        spawnData.FindProperty("gameplayCamera").objectReferenceValue = camera.GetComponent<Camera>(); spawnData.ApplyModifiedProperties();
        if (oldFloor != null) { Undo.RecordObject(oldFloor.gameObject, "Hide legacy floor"); oldFloor.gameObject.SetActive(false); }
        var oldWalls = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Walls");
        if (oldWalls != null) { Undo.RecordObject(oldWalls, "Disable legacy walls"); oldWalls.SetActive(false); }
        if (!ArenaSpawnSampler.ContainsInclusive(bounds.PlayableRect, player.transform.position))
        { Undo.RecordObject(player.transform, "Move player into map"); player.transform.position = root.transform.position; }
        Undo.CollapseUndoOperations(group); EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Debug.Log("Station arena installed. Playable area is 47 x 27 (4x old area). Camera zoom is preserved. Save Main with Ctrl+S.");
    }
    private static T FindInScene<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    private static GameObject Child(string name, Transform parent)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); Undo.RegisterCreatedObjectUndo(go, "Create station map object"); return go;
    }
    private static void Wall(Transform parent, string name, Vector2 at, Vector2 size)
    { var go = Child(name, parent); go.transform.localPosition = at; go.tag = "Wall"; go.isStatic = true; go.AddComponent<BoxCollider2D>().size = size; }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int last = path.LastIndexOf('/'); string parent = path.Substring(0, last); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(last + 1));
    }
}
#endif
