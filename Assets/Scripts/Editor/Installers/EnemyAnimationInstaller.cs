#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.EnemyAnimations;
using AbyssBar.Enemies;
using AbyssBar.Waves;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyAnimationInstaller
{
    private const string Data = "Assets/Data/EnemyAnimations/";
    private const string Art = "Assets/Art/Enemies/Animations/";
    private static readonly string[] Ids = { "wanderer", "runner", "riftmaw" };
    private static readonly string[] Names = { "Wanderer", "Runner", "Riftmaw" };
    private static readonly string[] Prefabs = { "Assets/Prefabs/Enemies/Enemy.prefab", "Assets/Prefabs/Enemies/FastEnemy.prefab", "Assets/Prefabs/Enemies/RiftmawShooter.prefab" };
    [Serializable] private sealed class SliceSet { public Atlas[] atlases; }
    [Serializable] private sealed class Atlas { public string id; public int width, height; public Slice[] frames; }
    [Serializable] private sealed class Slice { public int row, col, x, y, width, height; public float pivotY; }

    [MenuItem("Abyss Bar/Enemy Animations/1 - Prepare Animation Data")]
    public static void Prepare()
    {
        RequireEditMode(); EnsureFolder(Data.TrimEnd('/'));
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "animation-slices.json");
        if (json == null) throw new InvalidOperationException("Missing animation-slices.json. Import all Assets from the animation package.");
        var set = JsonUtility.FromJson<SliceSet>(json.text);
        if (set == null || set.atlases == null || set.atlases.Length != 3) throw new InvalidOperationException("Invalid atlas metadata.");
        for (int role = 0; role < Ids.Length; role++)
        {
            var atlas = set.atlases.Single(a => a.id == Ids[role]);
            string path = Art + atlas.id + "-sheet-v1.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing PNG: " + path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
            importer.isReadable = false; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture.width != atlas.width || texture.height != atlas.height || atlas.frames == null || atlas.frames.Length != 30)
                throw new InvalidOperationException("Atlas dimensions/frame count differ from metadata: " + path);
            string dataPath = Data + Names[role] + "Animations.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(dataPath);
            if (data != null && data.IsValid) continue; // Preserve hand-tuned clips on subsequent runs.
            if (data == null) { data = ScriptableObject.CreateInstance<EnemyAnimationData>(); AssetDatabase.CreateAsset(data, dataPath); }
            var clips = new EnemySpriteClip[5];
            string[] clipNames = { "idle", "walk", "attack", "hurt", "death" };
            for (int row = 0; row < 5; row++)
            {
                var frames = new Sprite[6];
                for (int col = 0; col < 6; col++)
                {
                    var slice = atlas.frames.Single(f => f.row == row && f.col == col);
                    if (slice.width <= 0 || slice.height <= 0 || slice.x < 0 || slice.y < 0
                        || slice.x + slice.width > texture.width || slice.y + slice.height > texture.height
                        || slice.pivotY < 0 || slice.pivotY > 1) throw new InvalidOperationException("Invalid sprite rect/pivot.");
                    string spriteName = atlas.id + "_" + clipNames[row] + "_" + col;
                    var sprite = AssetDatabase.LoadAllAssetsAtPath(dataPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
                    if (sprite == null)
                    {
                        sprite = Sprite.Create(texture, new Rect(slice.x, slice.y, slice.width, slice.height),
                            new Vector2(.5f, slice.pivotY), slice.height / 1.7f, 0, SpriteMeshType.FullRect);
                        sprite.name = spriteName; AssetDatabase.AddObjectToAsset(sprite, data);
                    }
                    frames[col] = sprite;
                }
                clips[row] = new EnemySpriteClip { frames = frames, fps = row == 0 ? 6 : row == 1 ? (role == 1 ? 12 : 8) : row == 3 ? 18 : 10, impactFrame = 4 };
            }
            data.idle = clips[0]; data.walk = clips[1]; data.attack = clips[2]; data.hurt = clips[3]; data.death = clips[4];
            data.stun = new EnemySpriteClip { frames = new[] { clips[3].frames[1], clips[3].frames[2], clips[3].frames[3], clips[3].frames[2] }, fps = 6, impactFrame = 0 };
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(Data + "RiftmawAnimations.asset");
        Debug.Log("Three enemy animation sets prepared: 90 frames, five authored motions plus a hurt-pose stun loop. Existing valid clip tuning preserved.");
    }

    [MenuItem("Abyss Bar/Enemy Animations/2 - Install All Three Prefabs")]
    public static void Install()
    {
        RequireEditMode(); Prepare();
        foreach (string path in Prefabs)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Missing prefab: " + path);
        EnsureFolder("Assets/Prefabs/Backups/BeforeEnemyAnimations");
        for (int role = 0; role < Prefabs.Length; role++)
        {
            string path = Prefabs[role];
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source.GetComponent<EnemyAnimationPresenter>() == null)
            {
                string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Prefabs/Backups/BeforeEnemyAnimations/" + Names[role] + ".prefab");
                if (!AssetDatabase.CopyAsset(path, backup)) throw new InvalidOperationException("Prefab backup failed: " + path);
                Debug.Log("Prefab backup: " + backup);
            }
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var presentation = root.GetComponent<EnemyAnimationPresenter>();
                bool first = presentation == null;
                if (first) presentation = root.AddComponent<EnemyAnimationPresenter>();
                var serialized = new SerializedObject(presentation);
                var field = serialized.FindProperty("animations");
                var data = field.objectReferenceValue as EnemyAnimationData;
                if (data == null)
                {
                    data = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(Data + Names[role] + "Animations.asset");
                    field.objectReferenceValue = data; serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (!data.IsValid) throw new InvalidOperationException("Invalid animation assignment: " + path);
                var renderer = root.GetComponent<SpriteRenderer>();
                renderer.sprite = data.idle.frames[0]; renderer.color = Color.white;
                renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
                if (material != null) renderer.sharedMaterial = material;
                if (first)
                {
                    // Normalize presentation scale while preserving the existing box's world size.
                    var scale = root.transform.localScale; var box = root.GetComponent<BoxCollider2D>();
                    if (box == null) throw new InvalidOperationException("Expected root BoxCollider2D: " + path);
                    box.size = new Vector2(box.size.x * Mathf.Abs(scale.x), box.size.y * Mathf.Abs(scale.y));
                    box.offset = new Vector2(0, box.size.y * .5f);
                    root.transform.localScale = Vector3.one;
                }
                if (PrefabUtility.SaveAsPrefabAsset(root, path) == null) throw new InvalidOperationException("Could not save " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets(); LogWavePlan();
        Debug.Log("All three enemy prefabs animated. Prefab GUIDs, health, speed, damage, drops and wave references retained. Start a fresh battle to test.");
    }

    [MenuItem("Abyss Bar/Enemy Animations/3 - Validate Prefabs And Main")]
    public static void Validate()
    {
        RequireEditMode();
        foreach (string path in Prefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var presenter = prefab != null ? prefab.GetComponent<EnemyAnimationPresenter>() : null;
            if (presenter == null || presenter.Data == null || !presenter.Data.IsValid)
                throw new InvalidOperationException("Missing/invalid animation data: " + path);
            if (prefab.GetComponent<EnemyDamage>() == null || prefab.GetComponent<EnemyController>() == null
                || prefab.GetComponent<EnemyHealth>() == null) throw new InvalidOperationException("Missing combat component: " + path);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyDefinition"))
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            var view = definition.prefab != null ? definition.prefab.GetComponent<EnemyAnimationPresenter>() : null;
            if (view == null || view.Data == null || !view.Data.IsValid)
                throw new InvalidOperationException("Enemy definition has incomplete animation coverage: " + definition.name);
        }
        if (SceneManager.GetActiveScene().name == "Main") TenWaveInstaller.Validate();
        LogWavePlan(); Debug.Log("Enemy animation assets and prefab assignments validate. Native Play Mode/Windows visual QA is still required.");
    }
    private static void LogWavePlan()
    {
        var scene = SceneManager.GetActiveScene(); if (scene.name != "Main") return;
        var manager = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<WaveManager>(true)).FirstOrDefault();
        if (manager == null) return;
        var plan = new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        if (plan == null) return;
        Debug.Log("Current wave plan (not changed by animation installer): " + plan.name + " / " + plan.waves.Sum(w => (long)w.enemyCount) + " enemies.");
        if (plan.waves.Length == 10 && plan.waves.All(w => w.enemyCount == 20))
            Debug.LogWarning("Main still uses a 20-per-wave preview. Select Horde Waves / 2 if you want the agreed 1250-enemy curve, then save Main.");
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); string parent = path.Substring(0, slash);
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
    private static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
}
#endif
