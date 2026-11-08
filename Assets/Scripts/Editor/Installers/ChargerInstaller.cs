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

public static class ChargerInstaller
{
    private const string Data = "Assets/Data/Enemies/Charger/";
    private const string Art = "Assets/Art/Enemies/Charger/";
    private const string CommonPath = Data + "ChargerCommonAnimations.asset";
    private const string SetPath = Data + "ChargerAnimations.asset";
    private const string PrefabPath = "Assets/Prefabs/Enemies/ChargerHound.prefab";
    private const string PoolPath = Data + "StationChargerPool.asset";
    [Serializable] private sealed class Metadata { public Atlas[] atlases; }
    [Serializable] private sealed class Atlas { public string file; public int width, height, rows; public Slice[] frames; }
    [Serializable] private sealed class Slice { public int row, col, x, y, width, height; public float pivotY; }

    [MenuItem("Abyss Bar/Charger/1 - Prepare Enemy And Animations")]
    public static void Prepare()
    {
        RequireEditMode(); EnsureFolder(Data.TrimEnd('/')); EnsureFolder("Assets/Prefabs/Enemies");
        var set = PrepareAnimations();
        var settings = AssetDatabase.LoadAssetAtPath<ChargeAttackSettings>(Data + "ChargerSettings.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ChargeAttackSettings>();
            settings.warningMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Data/Enemies/EnemyUnlit.mat");
            if (settings.warningMaterial == null) throw new InvalidOperationException("Prepare the Riftmaw material first.");
            AssetDatabase.CreateAsset(settings, Data + "ChargerSettings.asset");
        }
        if (!settings.IsValid) throw new InvalidOperationException("Invalid charger settings/material.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            const string sourcePath = "Assets/Prefabs/Enemies/FastEnemy.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null) throw new InvalidOperationException("Missing FastEnemy prefab.");
            var root = PrefabUtility.LoadPrefabContents(sourcePath);
            try
            {
                root.name = "ChargerHound"; root.transform.position = Vector3.zero;
                root.transform.rotation = Quaternion.identity; root.transform.localScale = Vector3.one;
                var contact = root.GetComponent<EnemyDamage>(); if (contact != null) UnityEngine.Object.DestroyImmediate(contact);
                var ranged = root.GetComponent<RangedEnemyAttack>(); if (ranged != null) UnityEngine.Object.DestroyImmediate(ranged);
                SetNumber(root.GetComponent<EnemyHealth>(), "maxHealth", 5);
                SetNumber(root.GetComponent<EnemyController>(), "moveSpeed", 2.2f);
                var box = root.GetComponent<BoxCollider2D>(); box.size = new Vector2(.95f, .5f); box.offset = new Vector2(0, .25f);
                var rb = root.GetComponent<Rigidbody2D>(); rb.gravityScale = 0; rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var renderer = root.GetComponent<SpriteRenderer>(); renderer.sprite = set.common.idle.frames[0];
                renderer.color = Color.white; renderer.sharedMaterial = settings.warningMaterial; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
                var presenter = root.GetComponent<EnemyAnimationPresenter>() ?? root.AddComponent<EnemyAnimationPresenter>();
                SetReference(presenter, "animations", set.common);
                var charge = root.AddComponent<EnemyChargeAttack>();
                SetReference(charge, "settings", settings); SetReference(charge, "animations", set);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Charger.asset");
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>(); definition.enemyId = "abyss_charger_hound";
            definition.displayName = "衝鋒獵犬"; definition.prefab = prefab; AssetDatabase.CreateAsset(definition, Data + "Charger.asset");
        }
        if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        if (pool == null)
        {
            var source = CurrentPool();
            if (source == null) source = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>("Assets/Data/Enemies/StationEnemyPool.asset");
            pool = ScriptableObject.CreateInstance<EnemySpawnPool>(); pool.rows = ChargePoolPreset.Build(source, definition);
            AssetDatabase.CreateAsset(pool, PoolPath);
        }
        if (!pool.TrySnapshot(4, out _, out error)) throw new InvalidOperationException(error);
        AssetDatabase.SaveAssets(); Selection.activeObject = definition;
        Debug.Log("Charger ready: 54 frames, raw HP5 / rush damage2, wave4 introduction. Existing charger assets/tuning and original pool are preserved.");
    }

    [MenuItem("Abyss Bar/Charger/2 - Install In Main")]
    public static void Install()
    {
        RequireEditMode(); Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main") throw new InvalidOperationException("Open Main first.");
        TenWaveInstaller.Validate(); Prepare();
        var spawner = Find<EnemySpawner>(scene).Single();
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        var data = new SerializedObject(spawner); var field = data.FindProperty("enemySpawnPool");
        if (field.objectReferenceValue != pool)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeCharger.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new InvalidOperationException("Scene backup failed; pool not switched.");
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Install charger enemy pool");
            field.objectReferenceValue = pool; data.ApplyModifiedProperties(); EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("Scene backup: " + backup);
        }
        LogPlan(); Debug.Log("Charger pool assigned. Save Main with Ctrl+S, then start a fresh battle.");
    }

    [MenuItem("Abyss Bar/Charger/3 - Validate Main")]
    public static void Validate()
    {
        RequireEditMode(); TenWaveInstaller.Validate();
        var pool = CurrentPool(); if (pool == null) throw new InvalidOperationException("Missing pool.");
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Data + "Charger.asset");
        if (definition == null || !definition.Validate(out _)) throw new InvalidOperationException("Invalid charger definition.");
        var prefab = definition.prefab;
        var action = prefab.GetComponent<EnemyChargeAttack>(); var presenter = prefab.GetComponent<EnemyAnimationPresenter>();
        if (action == null || !action.IsConfigured || presenter == null || presenter.Data == null || !presenter.Data.IsValid
            || prefab.GetComponent<EnemyDamage>() != null) throw new InvalidOperationException("Charger needs complete animations and no normal contact damage component.");
        if (prefab.layer != LayerMask.NameToLayer("Enemy")) throw new InvalidOperationException("Charger prefab must use the Enemy layer.");
        var manager = Find<WaveManager>(SceneManager.GetActiveScene()).Single();
        var plan = new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        for (int wave = 1; wave <= plan.waves.Length; wave++)
        {
            if (!pool.TrySnapshot(wave, out var entries, out string error)) throw new InvalidOperationException(error);
            var charger = entries.FirstOrDefault(e => e.enemy.enemyId == definition.enemyId);
            if (wave < 4 && charger != null) throw new InvalidOperationException("Charger is configured before wave4.");
            if (wave >= 4 && charger == null) throw new InvalidOperationException("Missing charger entry from wave " + wave);
            if (entries.Sum(e => (long)e.minimumSpawns) > plan.waves[wave - 1].enemyCount)
                throw new InvalidOperationException("Wave quota is smaller than guaranteed introductions. Test waves from 4 need at least two enemies with the default pool.");
        }
        LogPlan(); Debug.Log("Charger setup validates: four enemy types, nine animation motions, no extra wave quota. Native physics/visual QA still required.");
    }
    private static ChargeAnimationSet PrepareAnimations()
    {
        var common = AssetDatabase.LoadAssetAtPath<EnemyAnimationData>(CommonPath);
        var set = AssetDatabase.LoadAssetAtPath<ChargeAnimationSet>(SetPath);
        if (common != null && common.IsValid && set != null && set.IsValid) return set;
        if (common == null) { common = ScriptableObject.CreateInstance<EnemyAnimationData>(); AssetDatabase.CreateAsset(common, CommonPath); }
        if (set == null) { set = ScriptableObject.CreateInstance<ChargeAnimationSet>(); AssetDatabase.CreateAsset(set, SetPath); }
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Art + "charger-slices.json");
        if (json == null) throw new InvalidOperationException("Missing charger-slices.json.");
        var metadata = JsonUtility.FromJson<Metadata>(json.text);
        if (metadata == null || metadata.atlases == null || metadata.atlases.Length != 2) throw new InvalidOperationException("Invalid charger atlas metadata.");
        var clips = new EnemySpriteClip[9]; int index = 0;
        string[] names = { "idle", "walk", "hurt", "stun", "death", "windup", "rush", "wallImpact", "recovery" };
        foreach (var atlas in metadata.atlases)
        {
            string path = Art + atlas.file; var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing charger atlas " + path);
            importer.textureType = TextureImporterType.Default; importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true; importer.isReadable = false; importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture.width != atlas.width || texture.height != atlas.height || atlas.frames.Length != atlas.rows * 6) throw new InvalidOperationException("Atlas dimensions or frame count changed.");
            for (int row = 0; row < atlas.rows; row++)
            {
                var frames = new Sprite[6];
                for (int col = 0; col < 6; col++)
                {
                    var f = atlas.frames.Single(s => s.row == row && s.col == col);
                    if (f.width <= 0 || f.height <= 0 || f.x < 0 || f.y < 0 || f.x + f.width > texture.width || f.y + f.height > texture.height || f.pivotY < 0 || f.pivotY > 1)
                        throw new InvalidOperationException("Invalid charger sprite rect/pivot.");
                    string spriteName = "charger_" + names[index] + "_" + col;
                    var sprite = AssetDatabase.LoadAllAssetsAtPath(CommonPath).OfType<Sprite>().FirstOrDefault(s => s.name == spriteName);
                    if (sprite == null)
                    {
                        sprite = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(.5f, f.pivotY), f.height / 1.9f, 0, SpriteMeshType.FullRect);
                        sprite.name = spriteName; AssetDatabase.AddObjectToAsset(sprite, common);
                    }
                    frames[col] = sprite;
                }
                clips[index] = new EnemySpriteClip { frames = frames, impactFrame = 4, fps = index == 0 || index == 3 || index == 8 ? 6 : index == 2 ? 18 : index == 6 ? 12 : index == 7 ? 15 : 10 }; index++;
            }
        }
        if (index != 9) throw new InvalidOperationException("Expected nine charger motions.");
        common.idle = clips[0]; common.walk = clips[1]; common.hurt = clips[2]; common.stun = clips[3]; common.death = clips[4]; common.attack = clips[5];
        set.common = common; set.windup = clips[5]; set.rush = clips[6]; set.wallImpact = clips[7]; set.recovery = clips[8];
        EditorUtility.SetDirty(common); EditorUtility.SetDirty(set); AssetDatabase.SaveAssets(); return set;
    }
    private static EnemySpawnPool CurrentPool()
    {
        Scene scene = SceneManager.GetActiveScene(); if (scene.name != "Main") return null;
        var spawner = Find<EnemySpawner>(scene).FirstOrDefault();
        return spawner != null ? new SerializedObject(spawner).FindProperty("enemySpawnPool").objectReferenceValue as EnemySpawnPool : null;
    }
    private static void LogPlan()
    {
        var manager = Find<WaveManager>(SceneManager.GetActiveScene()).FirstOrDefault(); if (manager == null) return;
        var plan = new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        if (plan != null) Debug.Log("Wave plan retained: " + plan.name + " / " + plan.waves.Sum(w => (long)w.enemyCount) + " enemies.");
    }
    private static void SetReference(Component component, string field, UnityEngine.Object value)
    { var so = new SerializedObject(component); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetNumber(Component component, string field, float value)
    {
        var so = new SerializedObject(component); var property = so.FindProperty(field);
        if (property.propertyType == SerializedPropertyType.Integer) property.intValue = (int)value; else property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static T[] Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    private static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; int i = path.LastIndexOf('/'); string parent = path.Substring(0, i); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(i + 1)); }
    private static void RequireEditMode()
    { if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first."); }
}
#endif
