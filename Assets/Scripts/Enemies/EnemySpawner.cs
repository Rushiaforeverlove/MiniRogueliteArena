using UnityEngine;
using AbyssBar.Maps;
using AbyssBar.Enemies;
using System;
using System.Collections.Generic;

public enum EnemySpawnResult { Spawned, NoValidPosition, Unavailable, InvalidConfiguration }

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;
    [SerializeField, Range(0f, 1f)] private float fastEnemyChance = .25f;
    [SerializeField, Min(1)] private int fastEnemyUnlockWave = 2;
    [SerializeField] private Transform player;
    [SerializeField] private float spawnInterval = 1.5f;
    [Header("Legacy Arena Bounds - used only without a map")]
    [SerializeField] private float minX = -10.8f, maxX = 10.8f, minY = -5.8f, maxY = 5.8f;
    [Header("Data Driven Arena")]
    [SerializeField] private ArenaMapBounds arenaBounds;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(.1f)] private float enemySpawnRadius = .5f;
    [Header("Optional Data Driven Enemy Pool")]
    [SerializeField] private EnemySpawnPool enemySpawnPool;
    private int currentWave = 1;
    private EnemyPoolSelection poolSelection;
    private EnemyPoolEntry[] poolEntries;
    private EnemySpawnPool poolSource;
    private EnemyCombatWorld combatWorld;
    private sealed class PoolTracked
    { public EnemyHealth health; public string id; public Action<EnemyHealth> handler; }
    private readonly Dictionary<int, PoolTracked> poolTracked = new Dictionary<int, PoolTracked>();
    public Transform Player => player;
    public float SpawnInterval => spawnInterval;
    public bool ValidateConfiguration(out string error)
    {
        error = null;
        if (player == null) error = "Spawner needs Player.";
        else if (enemySpawnPool != null && !enemySpawnPool.TrySnapshot(currentWave, out _, out error)) { }
        else if (enemySpawnPool == null && (enemyPrefab == null || !ValidPrefab(enemyPrefab)
            || fastEnemyPrefab != null && !ValidPrefab(fastEnemyPrefab)))
            error = "Each enemy prefab needs valid EnemyHealth and EnemyController components.";
        else if (arenaBounds != null && arenaBounds.Map != null && !arenaBounds.Map.Validate(out error)) { }
        else if (arenaBounds == null && (minX >= maxX || minY >= maxY)) error = "Invalid legacy spawn bounds.";
        return error == null;
    }
    private static bool ValidPrefab(GameObject prefab)
    {
        if (prefab == null) return false;
        var health = prefab.GetComponent<EnemyHealth>();
        return health != null && health.HasValidStartingHealth && prefab.GetComponent<EnemyController>() != null;
    }
    // WaveManager requests each enemy. There is no autonomous endless-spawn Update.
    public EnemySpawnResult TrySpawnOne(out EnemyHealth enemy)
    {
        enemy = null;
        if (!isActiveAndEnabled || Time.timeScale <= 0) return EnemySpawnResult.Unavailable;
        if (player == null) return EnemySpawnResult.InvalidConfiguration;
        if (enemySpawnPool == null && !ValidateConfiguration(out _)) return EnemySpawnResult.InvalidConfiguration;
        if (enemySpawnPool != null && !EnsurePool()) return EnemySpawnResult.InvalidConfiguration;
        var health = player.GetComponent<PlayerHealth>();
        if (health != null && !health.IsAlive) return EnemySpawnResult.Unavailable;
        int poolIndex = enemySpawnPool != null ? poolSelection.Select(UnityEngine.Random.value) : -1;
        if (enemySpawnPool != null && poolIndex < 0) return EnemySpawnResult.Unavailable;
        if (!TryGetSpawnPosition(out Vector2 at)) return EnemySpawnResult.NoValidPosition;
        var prefab = enemySpawnPool != null ? poolEntries[poolIndex].enemy.prefab
            : currentWave >= fastEnemyUnlockWave && fastEnemyPrefab != null && UnityEngine.Random.value < fastEnemyChance
            ? fastEnemyPrefab : enemyPrefab;
        if (!ValidPrefab(prefab)) return EnemySpawnResult.InvalidConfiguration;
        GameObject instance = Instantiate(prefab, at, Quaternion.identity);
        instance.transform.SetParent(transform, true);
        instance.GetComponent<EnemyController>()?.SetTarget(player);
        enemy = instance.GetComponent<EnemyHealth>();
        if (enemySpawnPool != null)
        {
            if (!poolSelection.RegisterSpawn(poolIndex)) { Destroy(instance); enemy = null; return EnemySpawnResult.InvalidConfiguration; }
            var entry = new PoolTracked { health = enemy, id = poolEntries[poolIndex].enemy.enemyId };
            int token = enemy.RuntimeId;
            entry.handler = defeated => {
                if (!poolTracked.TryGetValue(token, out var current) || !ReferenceEquals(current, entry)) return;
                defeated.Defeated -= entry.handler; poolTracked.Remove(token); poolSelection.RegisterDefeat(entry.id);
            };
            poolTracked.Add(token, entry); enemy.Defeated += entry.handler;
        }
        var combat = instance.GetComponent<IEnemyCombatBinding>();
        if (combat != null) combat.Bind(player, GetCombatWorld());
        return EnemySpawnResult.Spawned;
    }
    private bool EnsurePool()
    {
        if (poolSelection != null && poolSource == enemySpawnPool) return true;
        if (!enemySpawnPool.TrySnapshot(currentWave, out poolEntries, out string error))
        { Debug.LogError(error, this); return false; }
        var rules = new EnemyPoolSelection.Rule[poolEntries.Length];
        for (int i = 0; i < rules.Length; i++)
        {
            var e = poolEntries[i]; rules[i] = new EnemyPoolSelection.Rule {
                id = e.enemy.enemyId, weight = e.weight, cap = e.maximumAlive, minimum = e.minimumSpawns };
        }
        poolSelection = new EnemyPoolSelection(rules); poolSource = enemySpawnPool; return true;
    }
    private EnemyCombatWorld GetCombatWorld()
    {
        if (combatWorld != null) return combatWorld;
        var go = new GameObject("Enemy battle effects"); go.transform.SetParent(transform, false);
        // Keep effect dimensions independent of any spawner transform scale.
        go.transform.SetParent(null, true);
        go.transform.position = Vector3.zero; go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        combatWorld = go.AddComponent<EnemyCombatWorld>();
        combatWorld.Initialize(player.GetComponent<PlayerHealth>(), FindAnyObjectByType<GameResultUI>(),
            gameplayCamera != null ? gameplayCamera : Camera.main);
        return combatWorld;
    }
    private void ClearPoolTracking()
    {
        foreach (var entry in poolTracked.Values) if (entry.health != null) entry.health.Defeated -= entry.handler;
        poolTracked.Clear(); poolSelection = null; poolSource = null; poolEntries = null;
    }
    private bool TryGetSpawnPosition(out Vector2 at)
    {
        if (arenaBounds != null && arenaBounds.Map != null)
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            if (gameplayCamera == null || !gameplayCamera.orthographic) { at = default; return false; }
            var map = arenaBounds.Map;
            return ArenaSpawnSampler.TrySample(arenaBounds.SpawnRect, ArenaMapBounds.CameraRect(gameplayCamera),
                player.position, Mathf.Max(map.spawnViewMargin, enemySpawnRadius * 2 + .1f), map.minimumSpawnDistance,
                () => UnityEngine.Random.value, Blocked, out at);
        }
        at = ArenaSpawnSampler.EdgePoint(Rect.MinMaxRect(minX, minY, maxX, maxY), UnityEngine.Random.value);
        return !Blocked(at);
    }
    private bool Blocked(Vector2 at)
    {
        foreach (var collider in Physics2D.OverlapCircleAll(at, enemySpawnRadius))
        {
            if (collider.isTrigger || collider.GetComponentInParent<EnemyHealth>() != null) continue;
            if (collider.GetComponentInParent<PlayerHealth>() != null) return true;
            return true;
        }
        return false;
    }
    public void SetSpawnInterval(float value) { spawnInterval = Mathf.Max(.05f, value); }
    public void SetWave(int wave) { ClearPoolTracking(); currentWave = Mathf.Max(1, wave); }
    private void OnDisable()
    {
        ClearPoolTracking();
        if (combatWorld != null) Destroy(combatWorld.gameObject);
        combatWorld = null;
    }
}
