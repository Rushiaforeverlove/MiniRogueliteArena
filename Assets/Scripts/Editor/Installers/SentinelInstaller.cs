#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.Enemies;
using AbyssBar.EnemyAnimations;
using AbyssBar.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SentinelInstaller
{
    private const string Data = "Assets/Data/Enemies/Sentinel/";
    private const string Art = "Assets/Art/Enemies/Sentinel/";
    private const string CommonPath = Data + "SentinelCommonAnimations.asset";
    private const string SetPath = Data + "SentinelAnimations.asset";
    private const string PrefabPath = "Assets/Prefabs/Enemies/ArmoredSentinel.prefab";
    private const string PoolPath = Data + "StationSentinelPool.asset";
    [Serializable] private sealed class Metadata { public Atlas[] atlases; }
    [Serializable] private sealed class Atlas { public string file; public int width, height, rows; public Slice[] frames; }
    [Serializable] private sealed class Slice { public int row, col, x, y, width, height; public float pivotY; }

    [MenuItem("Abyss Bar/Sentinel/1 - Prepare Enemy And Animations")]
    public static void Prepare()
    {
        RequireEditMode(); EnsureFolder(Data.TrimEnd('/')); EnsureFolder("Assets/Prefabs/Enemies");
        var set = PrepareAnimations();
        var settings = AssetDatabase.LoadAssetAtPath<SentinelAttackSettings>(Data + "SentinelSettings.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<SentinelAttackSettings>();
            settings.warningMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
            if (settings.warningMaterial == null) throw new InvalidOperationException("Missing existing enemy material.");
            AssetDatabase.CreateAsset(settings, Data + "SentinelSettings.asset");
        }
        if (!settings.IsValid) throw new InvalidOperationException("Invalid sentinel settings/material.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            const string source = "Assets/Prefabs/Enemies/Enemy.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(source) == null) throw new InvalidOperationException("Missing normal Enemy prefab.");
            var root = PrefabUtility.LoadPrefabContents(source);
            try
            {
                root.name = "ArmoredSentinel"; root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
                SetNumber(root.GetComponent<EnemyHealth>(), "maxHealth", 12);
                SetNumber(root.GetComponent<EnemyController>(), "moveSpeed", 1.1f);
                var contact = root.GetComponent<EnemyDamage>(); if (contact != null) UnityEngine.Object.DestroyImmediate(contact);
                var box = root.GetComponent<BoxCollider2D>(); box.size = new Vector2(1.05f, .6f); box.offset = new Vector2(0, .3f);
                var rb = root.GetComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = set.common.idle.frames[0];
                renderer.color = Color.white; renderer.sharedMaterial = settings.warningMaterial; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var presenter = root.GetComponent<EnemyAnimationPresenter>() ?? root.AddComponent<EnemyAnimationPresenter>();
                SetReference(presenter, "animations", set.common);
                var attack = root.AddComponent<EnemyArmoredSentinel>();
                SetReference(attack, "settings", settings); SetReference(attack, "animations", set);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Sentinel.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>(); definition.enemyId = "abyss_armored_sentinel";
            definition.displayName = "重甲守衛"; definition.prefab = prefab; AssetDatabase.CreateAsset(definition, Data + "Sentinel.asset");
        }
        if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        if (pool == null)
        {
            var source = CurrentPool();
            if (source == null) source = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>("Assets/Data/Enemies/Corrosive/StationCorrosivePool.asset");
            pool = ScriptableObject.CreateInstance<EnemySpawnPool>(); pool.rows = SentinelPoolPreset.Build(source, definition);
            AssetDatabase.CreateAsset(pool, PoolPath);
        }
        if (!pool.TrySnapshot(8, out _, out error)) throw new InvalidOperationException(error);
        AssetDatabase.SaveAssets(); Selection.activeObject = definition;
        Debug.Log("Sentinel ready: 54 frames / wave8 / armor40% / one-second locked slam / vulnerable recovery. Existing tuning preserved.");
    }
    [MenuItem("Abyss Bar/Sentinel/2 - Install In Main")]
    public static void Install()
    {
        RequireEditMode(); Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main") throw new InvalidOperationException("Open Main first.");
        TenWaveInstaller.Validate(); Prepare();
        var spawner = Find<EnemySpawner>(scene).Single(); var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        var so = new SerializedObject(spawner); var field = so.FindProperty("enemySpawnPool");
        if (field.objectReferenceValue != pool)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeSentinel.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed; pool not switched.");
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Install sentinel enemy pool");
            field.objectReferenceValue = pool; so.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(scene); Debug.Log("Scene backup: " + backup);
        }
        LogPlan(); Debug.Log("Sentinel pool assigned. Save Main with Ctrl+S and begin a new battle.");
    }
    [MenuItem("Abyss Bar/Sentinel/3 - Validate Main")]
    public static void Validate()
    {
        RequireEditMode(); TenWaveInstaller.Validate(); var pool = CurrentPool();
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Sentinel.asset");
        if (pool == null || definition == null || !definition.Validate(out _)) throw new InvalidOperationException("Prepare and install Sentinel first.");
        var action = definition.prefab.GetComponent<EnemyArmoredSentinel>();
        var view = definition.prefab.GetComponent<EnemyAnimationPresenter>();
        if (action == null || !action.IsConfigured || view == null || view.Data == null || !view.Data.IsValid
            || definition.prefab.GetComponent<EnemyDamage>() != null)
            throw new InvalidOperationException("Sentinel needs complete animations/action and no ordinary contact attack.");
        var plan = Plan();
        for (int wave = 1; wave <= plan.waves.Length; wave++)
        {
            if (!pool.TrySnapshot(wave, out var entries, out string error)) throw new InvalidOperationException(error);
            bool present = entries.Any(e => e.enemy.enemyId == definition.enemyId);
            if (wave < 8 && present || wave >= 8 && !present) throw new InvalidOperationException("Sentinel must be introduced at wave8.");
            if (entries.Sum(e => (long)e.minimumSpawns) > plan.waves[wave - 1].enemyCount)
                throw new InvalidOperationException("Test wave quota below guaranteed introductions. Default waves8+ need at least4.");
        }
        LogPlan(); Debug.Log("Sentinel setup validates. Native animation, physics and Windows build still need testing.");
    }
    private static SentinelAnimationSet PrepareAnimations()
    {
        var common = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(CommonPath);
        var set = AssetDatabase.LoadAssetAtPath<SentinelAnimationSet>(SetPath);
        if (common != null && common.IsValid && set != null && set.IsValid) return set;
        if (common == null) { common = ScriptableObject.CreateInstance<EnemyAnimationData>(); AssetDatabase.CreateAsset(common, CommonPath); }
        if (set == null) { set = ScriptableObject.CreateInstance<SentinelAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "sentinel-slices.json");
        if (json == null) throw new InvalidOperationException("Missing sentinel-slices.json.");
        var metadata = JsonUtility.FromJson<Metadata>(json.text);
        if (metadata == null || metadata.atlases == null || metadata.atlases.Length != 2) throw new InvalidOperationException("Invalid sentinel atlas metadata.");
        var clips = new EnemySpriteClip[9]; int index = 0;
        string[] names = { "idle", "walk", "hurt", "stun", "death", "block", "windup", "slam", "recovery" };
        foreach (var atlas in metadata.atlases)
        {
            string path = Art + atlas.file; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing PNG: " + path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096; importer.mipmapEnabled = false; importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true; importer.isReadable = false; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture.width != atlas.width || texture.height != atlas.height || atlas.frames.Length != atlas.rows * 6) throw new InvalidOperationException("Atlas dimensions/frame count differ from metadata.");
            for (int row = 0; row < atlas.rows; row++)
            {
                var frames = new Sprite[6];
                for (int col = 0; col < 6; col++)
                {
                    var f = atlas.frames.Single(s => s.row == row && s.col == col);
                    if (f.width <= 0 || f.height <= 0 || f.x < 0 || f.y < 0 || f.x + f.width > texture.width || f.y + f.height > texture.height || f.pivotY < 0 || f.pivotY > 1)
                        throw new InvalidOperationException("Invalid sentinel slice.");
                    string name = "sentinel_" + names[index] + "_" + col;
                    var sprite = AssetDatabase.LoadAllAssetsAtPath(CommonPath).OfType<Sprite>().FirstOrDefault(s => s.name == name);
                    if (sprite == null)
                    {
                        sprite = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(.5f, f.pivotY), f.height / 2.2f, 0, SpriteMeshType.FullRect);
                        sprite.name = name; AssetDatabase.AddObjectToAsset(sprite, common);
                    }
                    frames[col] = sprite;
                }
                clips[index] = new EnemySpriteClip { frames = frames, impactFrame = index == 7 ? 0 : 4,
                    fps = index == 0 || index == 3 ? 6 : index == 1 ? 8 : index == 2 || index == 5 ? 18 : 10 }; index++;
            }
        }
        if (index != 9) throw new InvalidOperationException("Expected nine sentinel motions.");
        common.idle = clips[0]; common.walk = clips[1]; common.hurt = clips[2]; common.stun = clips[3]; common.death = clips[4]; common.attack = clips[6];
        set.common = common; set.block = clips[5]; set.windup = clips[6]; set.slam = clips[7]; set.recovery = clips[8];
        EditorUtility.SetDirty(common); EditorUtility.SetDirty(set); AssetDatabase.SaveAssets(); return set;
    }
    private static EnemySpawnPool CurrentPool()
    {
        var scene = SceneManager.GetActiveScene(); if (scene.name != "Main") return null;
        var spawner = Find<EnemySpawner>(scene).FirstOrDefault();
        return spawner != null ? new SerializedObject(spawner).FindProperty("enemySpawnPool").objectReferenceValue as EnemySpawnPool : null;
    }
    private static BattleWavePlan Plan()
    {
        var manager = Find<WaveManager>(SceneManager.GetActiveScene()).FirstOrDefault();
        return manager != null ? new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan : null;
    }
    private static void LogPlan()
    { var plan = Plan(); if (plan != null) Debug.Log("Wave plan retained: " + plan.name + " / " + plan.waves.Sum(w => (long)w.enemyCount) + " enemies."); }
    private static void SetReference(Component c, string field, UnityEngine.Object value)
    { var so = new SerializedObject(c); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetNumber(Component c, string field, float value)
    { var so = new SerializedObject(c); var p = so.FindProperty(field); if (p.propertyType == SerializedPropertyType.Integer) p.intValue = (int)value; else p.floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); string parent = path.Substring(0, i); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(i + 1)); }
    private static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
}
#endif
