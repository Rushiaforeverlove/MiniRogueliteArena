#if UNITY_EDITOR
using System;
using System.Linq;
using AbyssBar.Enemies;
using AbyssBar.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RiftmawInstaller
{
    private const string Data = "Assets/Data/Enemies/";
    private const string Art = "Assets/Art/Enemies/Riftmaw/riftmaw-idle-v1.png";
    private const string PrefabPath = "Assets/Prefabs/Enemies/RiftmawShooter.prefab";
    private const string PoolPath = Data + "StationEnemyPool.asset";

    [MenuItem("Abyss Bar/Riftmaw/1 - Prepare Enemy And Pool")]
    public static void Prepare()
    {
        RequireEditMode();
        var normal = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy.prefab");
        var fast = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/FastEnemy.prefab");
        if (normal == null || fast == null)
            throw new InvalidOperationException("Expected Assets/Prefabs/Enemies/Enemy.prefab and FastEnemy.prefab.");
        EnsureFolder(Data.TrimEnd('/')); EnsureFolder("Assets/Prefabs/Enemies");
        ImportSprite();
        var settings = AssetDatabase.LoadAssetAtPath<RangedEnemySettings>(Data + "RiftmawAttack.asset");
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<RangedEnemySettings>();
            settings.effectMaterial = PrepareMaterial();
            AssetDatabase.CreateAsset(settings, Data + "RiftmawAttack.asset");
        }
        if (!settings.IsValid) throw new InvalidOperationException("RiftmawAttack has missing/invalid settings or material.");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            var contents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(normal));
            try
            {
                contents.name = "RiftmawShooter";
                contents.transform.position = Vector3.zero; contents.transform.rotation = Quaternion.identity;
                contents.transform.localScale = Vector3.one;
                var sr = contents.GetComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art); sr.color = Color.white;
                sr.sharedMaterial = settings.effectMaterial; sr.spriteSortPoint = SpriteSortPoint.Pivot;
                var flash = new SerializedObject(contents.GetComponent<DamageFlash>());
                flash.FindProperty("flashColor").colorValue = new Color(1, .2f, .2f, 1); flash.ApplyModifiedPropertiesWithoutUndo();
                SetNumber(contents.GetComponent<EnemyHealth>(), "maxHealth", 2);
                SetNumber(contents.GetComponent<EnemyController>(), "moveSpeed", 1.8f);
                var body = contents.GetComponent<Rigidbody2D>(); body.gravityScale = 0;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                var collider = contents.GetComponent<BoxCollider2D>();
                collider.size = new Vector2(.7f, .45f); collider.offset = new Vector2(0, .23f);
                var ranged = contents.AddComponent<RangedEnemyAttack>();
                var serialized = new SerializedObject(ranged);
                serialized.FindProperty("settings").objectReferenceValue = settings;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                prefab = PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        var a = Definition("Wanderer", "abyss_wanderer", "深淵遊蕩者", normal);
        var b = Definition("Runner", "abyss_runner", "疾行爪獸", fast);
        var c = Definition("Riftmaw", "abyss_riftmaw", "裂口射手", prefab);
        if (prefab.GetComponent<RangedEnemyAttack>() == null)
            throw new InvalidOperationException("Existing RiftmawShooter prefab is missing RangedEnemyAttack; repair it rather than overwriting custom work.");
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<EnemySpawnPool>();
            pool.rows = new EnemyPoolRow[10];
            int[] caps = { 0, 0, 2, 3, 4, 5, 6, 6, 7, 8 };
            for (int i = 0; i < 10; i++)
                pool.rows[i] = new EnemyPoolRow { firstWave = i + 1,
                    entries = i == 0 ? new[] { Entry(a, 100, 90) }
                        : i == 1 ? new[] { Entry(a, 75, 90), Entry(b, 25, 90) }
                        : new[] { Entry(a, 65, 90), Entry(b, 25, 90), Entry(c, 10, caps[i], 1) } };
            AssetDatabase.CreateAsset(pool, PoolPath);
        }
        if (!pool.TrySnapshot(1, out _, out string error)) throw new InvalidOperationException(error);
        AssetDatabase.SaveAssets(); Selection.activeObject = pool;
        Debug.Log("Riftmaw prepared. Existing enemy prefabs, settings and pool tuning are preserved. New defaults use raw HP 2 and raw shot damage 1, scaled at runtime by CombatBalance.");
    }

    [MenuItem("Abyss Bar/Riftmaw/2 - Install In Main")]
    public static void Install()
    {
        RequireEditMode();
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "Main" || !scene.isLoaded) throw new InvalidOperationException("Open Main first.");
        TenWaveInstaller.Validate();
        var spawners = Find<EnemySpawner>(scene);
        if (spawners.Length != 1) throw new InvalidOperationException("Main needs exactly one EnemySpawner.");
        Prepare();
        var pool = AssetDatabase.LoadAssetAtPath<EnemySpawnPool>(PoolPath);
        var data = new SerializedObject(spawners[0]); var field = data.FindProperty("enemySpawnPool");
        if (field.objectReferenceValue != pool)
        {
            EnsureFolder("Assets/Scenes/Backups");
            string backup = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/Backups/Main_BeforeRiftmaw.unity");
            if (!EditorSceneManager.SaveScene(scene, backup, true))
                throw new InvalidOperationException("Scene backup failed; no pool assigned.");
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Install Riftmaw enemy pool");
            field.objectReferenceValue = pool; data.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene); Debug.Log("Scene backup: " + backup);
        }
        Debug.Log("Riftmaw pool assigned. Wave quotas and map bounds are unchanged. Save Main with Ctrl+S.");
    }

    [MenuItem("Abyss Bar/Riftmaw/3 - Validate Main")]
    public static void Validate()
    {
        RequireEditMode(); TenWaveInstaller.Validate();
        Scene scene = SceneManager.GetActiveScene(); var spawners = Find<EnemySpawner>(scene);
        if (spawners.Length != 1) throw new InvalidOperationException("Main needs exactly one EnemySpawner.");
        var pool = new SerializedObject(spawners[0]).FindProperty("enemySpawnPool").objectReferenceValue as EnemySpawnPool;
        if (pool == null) throw new InvalidOperationException("Enemy pool not assigned. Run Riftmaw / 2.");
        var manager = Find<WaveManager>(scene).Single();
        var plan = new SerializedObject(manager).FindProperty("wavePlan").objectReferenceValue as BattleWavePlan;
        for (int wave = 1; wave <= plan.waves.Length; wave++)
        {
            if (!pool.TrySnapshot(wave, out var entries, out string error)) throw new InvalidOperationException(error);
            if (entries.Sum(e => (long)e.minimumSpawns) > plan.waves[wave - 1].enemyCount)
                throw new InvalidOperationException("Wave " + wave + " has more minimum introductions than its total quota.");
        }
        var cameraRef = new SerializedObject(spawners[0]).FindProperty("gameplayCamera").objectReferenceValue as Camera;
        if (cameraRef == null && Camera.main == null)
            throw new InvalidOperationException("Assign EnemySpawner Gameplay Camera or tag the gameplay camera MainCamera.");
        Debug.Log("Enemy pool valid. Riftmaw introduced at wave 3; default caps: 2,3,4,5,6,6,7,8. Run a Play Mode/Windows test for physics and visuals.");
    }

    private static EnemyPoolEntry Entry(EnemyDefinition enemy, float weight, int cap, int minimum = 0)
        => new EnemyPoolEntry { enemy = enemy, weight = weight, maximumAlive = cap, minimumSpawns = minimum };
    private static EnemyDefinition Definition(string file, string id, string name, GameObject prefab)
    {
        string path = Data + file + ".asset";
        var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            definition.enemyId = id; definition.displayName = name; definition.prefab = prefab;
            AssetDatabase.CreateAsset(definition, path);
        }
        if (!definition.Validate(out string error)) throw new InvalidOperationException(error);
        return definition;
    }
    private static Material PrepareMaterial()
    {
        string path = Data + "EnemyUnlit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
        if (shader == null) throw new InvalidOperationException("No compatible sprite shader.");
        material = new Material(shader); AssetDatabase.CreateAsset(material, path); return material;
    }
    private static void ImportSprite()
    {
        var importer = AssetImporter.GetAtPath(Art) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing Riftmaw PNG: " + Art);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 800; importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = 2048; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.493f, .045f);
        importer.SetTextureSettings(settings); importer.SaveAndReimport();
    }
    private static void SetNumber(Component component, string field, float value)
    {
        var data = new SerializedObject(component); var property = data.FindProperty(field);
        if (property.propertyType == SerializedPropertyType.Integer) property.intValue = (int)value;
        else property.floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static T[] Find<T>(Scene scene) where T : Component
        => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); string parent = path.Substring(0, slash);
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
    private static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
    }
}
#endif
