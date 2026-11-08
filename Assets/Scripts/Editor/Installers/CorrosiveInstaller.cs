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

public static class CorrosiveInstaller
{
    private const string Data = "Assets/Data/Enemies/Corrosive/";
    private const string Art = "Assets/Art/Enemies/Corrosive/";
    private const string CommonPath = Data + "CorrosiveCommonAnimations.asset";
    private const string SetPath = Data + "CorrosiveAnimations.asset";
    private const string PrefabPath = "Assets/Prefabs/Enemies/CorrosiveSac.prefab";
    private const string PoolPath = Data + "StationCorrosivePool.asset";
    [Serializable] private sealed class Metadata { public Atlas[] atlases; }
    [Serializable] private sealed class Atlas { public string file; public int width, height, rows; public bool ground; public Slice[] frames; }
    [Serializable] private sealed class Slice { public int row, col, x, y, width, height; public float pivotY; }

    [MenuItem("Abyss Bar/Corrosive/1 - Prepare Enemy And Effects")]
    public static void Prepare()
    {
        RequireEditMode(); EnsureFolder(Data.TrimEnd('/')); EnsureFolder("Assets/Prefabs/Enemies");
        var set = PrepareAnimations();
        var settings = AssetDatabase.LoadAssetAtPath<CorrosiveEnemySettings>(Data + "CorrosiveSettings.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<CorrosiveEnemySettings>();
            settings.material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
            if (settings.material == null) throw new InvalidOperationException("Prepare the existing enemy material first.");
            AssetDatabase.CreateAsset(settings, Data + "CorrosiveSettings.asset");
        }
        if (!settings.IsValid) throw new InvalidOperationException("Invalid corrosive settings/material.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            const string sourcePath = "Assets/Prefabs/Enemies/Enemy.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null) throw new InvalidOperationException("Missing normal enemy prefab.");
            var root = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                root.name = "CorrosiveSac"; root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
                SetNumber(root.GetComponent<EnemyHealth>(), "maxHealth", 3);
                SetNumber(root.GetComponent<EnemyController>(), "moveSpeed", 1.4f);
                var contact = root.GetComponent<EnemyDamage>();
                if (contact == null) throw new InvalidOperationException("Normal enemy needs EnemyDamage.");
                SetNumber(contact, "damage", 1); SetNumber(contact, "damageCooldown", 1.2f);
                var box = root.GetComponent<BoxCollider2D>(); box.size = new Vector2(.8f, .5f); box.offset = new Vector2(0, .25f);
                var rb = root.GetComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = set.common.idle.frames[0];
                renderer.color = Color.white; renderer.sharedMaterial = settings.material; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var presenter = root.GetComponent<EnemyAnimationPresenter>() ?? root.AddComponent<EnemyAnimationPresenter>();
                SetReference(presenter, "animations", set.common);
                var death = root.AddComponent<EnemyCorrosiveDeath>();
                SetReference(death, "settings", settings); SetReference(death, "animations", set);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Corrosive.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>(); definition.enemyId = "abyss_corrosive_sac";
            definition.displayName = "腐液囊獸"; definition.prefab = prefab; AssetDatabase.CreateAsset(definition, Data + "Corrosive.asset");
        }
        if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        if (pool == null)
        {
            var source = CurrentPool();
            if (source == null) source = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>("Assets/Data/Enemies/Charger/StationChargerPool.asset");
            pool = ScriptableObject.CreateInstance<EnemySpawnPool>(); pool.rows = CorrosivePoolPreset.Build(source, definition);
            AssetDatabase.CreateAsset(pool, PoolPath);
        }
        if (!pool.TrySnapshot(6, out _, out error)) throw new InvalidOperationException(error);
        AssetDatabase.SaveAssets(); Selection.activeObject = definition;
        Debug.Log("Corrosive sac ready: 60 unique frames, wave6, warning .6s / active4s / radius1.5 / cap6. Existing tuning preserved.");
    }
    [MenuItem("Abyss Bar/Corrosive/2 - Install In Main")]
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
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeCorrosive.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed; pool not switched.");
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Install corrosive enemy pool");
            field.objectReferenceValue = pool; so.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(scene); Debug.Log("Scene backup: " + backup);
        }
        LogPlan(); Debug.Log("Corrosive pool assigned. Save Main with Ctrl+S and start a fresh battle.");
    }
    [MenuItem("Abyss Bar/Corrosive/3 - Validate Main")]
    public static void Validate()
    {
        RequireEditMode(); TenWaveInstaller.Validate(); var pool = CurrentPool();
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Corrosive.asset");
        if (pool == null || definition == null || !definition.Validate(out _)) throw new InvalidOperationException("Prepare and install Corrosive first.");
        var death = definition.prefab.GetComponent<EnemyCorrosiveDeath>();
        var view = definition.prefab.GetComponent<EnemyAnimationPresenter>();
        if (death == null || !death.IsConfigured || view == null || view.Data == null || !view.Data.IsValid)
            throw new InvalidOperationException("Corrosive prefab needs complete death/animation setup.");
        var plan = Plan();
        for (int wave = 1; wave <= plan.waves.Length; wave++)
        {
            if (!pool.TrySnapshot(wave, out var entries, out string error)) throw new InvalidOperationException(error);
            bool present = entries.Any(e => e.enemy.enemyId == definition.enemyId);
            if (wave < 6 && present || wave >= 6 && !present) throw new InvalidOperationException("Corrosive enemy must be introduced at wave6.");
            if (entries.Sum(e => (long)e.minimumSpawns) > plan.waves[wave - 1].enemyCount)
                throw new InvalidOperationException("Test wave quota is smaller than guaranteed introductions. Waves6+ need at least3 with the default pool.");
        }
        LogPlan(); Debug.Log("Corrosive setup validates. Native visuals, player collision and Windows build still need testing.");
    }
    private static CorrosiveAnimationSet PrepareAnimations()
    {
        var common = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(CommonPath);
        var set = AssetDatabase.LoadAssetAtPath<CorrosiveAnimationSet>(SetPath);
        if (common != null && common.IsValid && set != null && set.IsValid) return set;
        if (common == null) { common = ScriptableObject.CreateInstance<EnemyAnimationData>(); AssetDatabase.CreateAsset(common, CommonPath); }
        if (set == null) { set = ScriptableObject.CreateInstance<CorrosiveAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "corrosive-slices.json");
        if (json == null) throw new InvalidOperationException("Missing corrosive-slices.json.");
        var metadata = JsonUtility.FromJson<Metadata>(json.text);
        if (metadata == null || metadata.atlases == null || metadata.atlases.Length != 3) throw new InvalidOperationException("Invalid corrosive atlas metadata.");
        var clips = new EnemySpriteClip[10]; int index = 0;
        string[] names = { "idle", "walk", "attack", "hurt", "stun", "inflate", "burst", "formation", "bubbling", "dissolve" };
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
                        throw new InvalidOperationException("Invalid corrosive sprite slice.");
                    string spriteName = "corrosive_" + names[index] + "_" + col;
                    var sprite = AssetDatabase.LoadAllAssetsAtPath(CommonPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
                    if (sprite == null)
                    {
                        sprite = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(.5f, f.pivotY),
                            atlas.ground ? f.width : f.height / 1.7f, 0, SpriteMeshType.FullRect);
                        sprite.name = spriteName; AssetDatabase.AddObjectToAsset(sprite, common);
                    }
                    frames[col] = sprite;
                }
                clips[index] = new EnemySpriteClip { frames = frames, impactFrame = 4,
                    fps = index == 0 || index == 4 ? 6 : index == 1 || index == 8 ? 8 : index == 3 ? 18 : index == 6 || index == 7 || index == 9 ? 12 : 10 }; index++;
            }
        }
        if (index != 10) throw new InvalidOperationException("Expected ten corrosive/effect motions.");
        common.idle = clips[0]; common.walk = clips[1]; common.attack = clips[2]; common.hurt = clips[3]; common.stun = clips[4];
        common.death = new EnemySpriteClip { frames = clips[5].frames.Concat(clips[6].frames).ToArray(), fps = 10, impactFrame = 0 };
        set.common = common; set.inflate = clips[5]; set.burst = clips[6]; set.formation = clips[7]; set.bubbling = clips[8]; set.dissolve = clips[9];
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
