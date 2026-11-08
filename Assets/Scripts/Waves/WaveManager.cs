using System;
using System.Collections.Generic;
using AbyssBar.Waves;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private GameResultUI gameResultUI;
    [SerializeField] private BattleWavePlan wavePlan;
    [SerializeField, Range(1, 32)] private int maximumSpawnsPerFrame = 8;
    private BattleWaveDefinition[] definitions;
    private WaveBattleProgress progress;
    private PlayerHealth playerHealth;
    private float spawnTimer;
    private readonly Dictionary<int, TrackedEnemy> tracked = new Dictionary<int, TrackedEnemy>();
    private readonly List<TrackedEnemy> checkBuffer = new List<TrackedEnemy>();
    private bool resultHandled;
    private int shownWave = -1, shownAlive = -1, shownPending = -1, shownRest = -1;
    private WaveBattlePhase shownPhase;
    public WaveBattleProgress Progress => progress;
    private sealed class TrackedEnemy
    { public int token, wave; public EnemyHealth health; public Action<EnemyHealth> handler; }
    private void Awake()
    {
        if (gameResultUI == null) gameResultUI = FindAnyObjectByType<GameResultUI>();
        if (enemySpawner == null) enemySpawner = FindAnyObjectByType<EnemySpawner>();
        if (gameHUD == null) gameHUD = FindAnyObjectByType<GameHUD>();
        if (gameResultUI != null) gameResultUI.BattleFinished += OnBattleFinished;
    }
    private void Start()
    {
        if (resultHandled || gameResultUI != null && gameResultUI.HasSettled) return;
        if (wavePlan == null || enemySpawner == null || gameHUD == null || gameResultUI == null)
        { Fail("Assign the wave plan, spawner, HUD and result UI using the Ten Waves installer."); return; }
        if (!wavePlan.TrySnapshot(out definitions, out string error)) { Fail(error); return; }
        if (!enemySpawner.ValidateConfiguration(out error)) { Fail(error); return; }
        playerHealth = enemySpawner.Player != null ? enemySpawner.Player.GetComponent<PlayerHealth>() : null;
        if (playerHealth == null) { Fail("Spawner Player must have PlayerHealth."); return; }
        int[] quotas = new int[definitions.Length];
        for (int i = 0; i < quotas.Length; i++) quotas[i] = definitions[i].enemyCount;
        progress = new WaveBattleProgress(quotas, wavePlan.intermissionSeconds);
        progress.Start(); BeginCurrentWave(); RefreshHUD();
    }
    private void BeginCurrentWave()
    {
        spawnTimer = 0;
        enemySpawner.SetWave(progress.CurrentWave);
        enemySpawner.SetSpawnInterval(definitions[progress.CurrentWave - 1].spawnInterval);
    }
    private void Update()
    {
        if (progress == null || resultHandled || gameResultUI == null || gameResultUI.HasSettled) return;
        if (playerHealth == null || !playerHealth.IsAlive) { StopBattle(); return; }
        if (Time.timeScale <= 0) return;
        if (!CheckTracked()) return;
        if (progress.Phase == WaveBattlePhase.Intermission)
        {
            if (progress.TickIntermission(Time.deltaTime)) BeginCurrentWave();
            RefreshHUD(); return;
        }
        if (!progress.IsWaveActive) return;
        var definition = definitions[progress.CurrentWave - 1];
        spawnTimer = Mathf.Min(spawnTimer + Time.deltaTime, definition.spawnInterval * maximumSpawnsPerFrame);
        int thisFrame = 0;
        while (progress.Phase == WaveBattlePhase.Spawning && spawnTimer >= definition.spawnInterval
            && progress.Alive < definition.maximumAlive && thisFrame++ < maximumSpawnsPerFrame)
        {
            EnemySpawnResult result = enemySpawner.TrySpawnOne(out EnemyHealth enemy);
            if (result == EnemySpawnResult.InvalidConfiguration) { Fail("Enemy spawner lost a required reference or valid prefab."); return; }
            if (result != EnemySpawnResult.Spawned) { spawnTimer = 0; break; }
            spawnTimer -= definition.spawnInterval;
            var entry = new TrackedEnemy { token = enemy.RuntimeId, wave = progress.CurrentWave, health = enemy };
            if (!progress.RegisterSpawn(entry.token, entry.wave))
            { Destroy(enemy.gameObject); Fail("Duplicate/invalid enemy spawn token."); return; }
            entry.handler = defeated => OnEnemyDefeated(entry, defeated);
            tracked.Add(entry.token, entry); enemy.Defeated += entry.handler;
            if (!enemy.IsAlive) OnEnemyDefeated(entry, enemy);
        }
        if (progress.Alive >= definition.maximumAlive) spawnTimer = Mathf.Min(spawnTimer, definition.spawnInterval);
        RefreshHUD();
    }
    private bool CheckTracked()
    {
        checkBuffer.Clear(); checkBuffer.AddRange(tracked.Values);
        foreach (var entry in checkBuffer)
        {
            if (entry.health == null || !entry.health.gameObject.activeInHierarchy && entry.health.IsAlive)
            { Fail("A wave enemy was removed/disabled without a defeat event. Restart the battle; it is not counted as a kill."); return false; }
            if (!entry.health.IsAlive) OnEnemyDefeated(entry, entry.health);
        }
        return true;
    }
    private void OnEnemyDefeated(TrackedEnemy entry, EnemyHealth enemy)
    {
        if (progress == null || resultHandled || entry.wave != progress.CurrentWave || !ReferenceEquals(enemy, entry.health)
            || !tracked.TryGetValue(entry.token, out var current) || !ReferenceEquals(entry, current)) return;
        enemy.Defeated -= entry.handler; tracked.Remove(entry.token);
        progress.RegisterDefeat(entry.token, entry.wave); RefreshHUD();
        // Present victory in LateUpdate, after the hit, gem drop and other kill callbacks finish.
    }
    private void LateUpdate()
    {
        if (resultHandled || progress == null || progress.Phase != WaveBattlePhase.AwaitingVictory) return;
        if (gameResultUI == null || gameResultUI.HasSettled || playerHealth == null || !playerHealth.IsAlive)
        { StopBattle(); return; }
        if (!gameResultUI.IsConfigured) { Fail("GameResultUI needs its panel/title/reward references."); return; }
        gameResultUI.ShowVictory();
    }
    private void OnBattleFinished(bool victory)
    {
        if (victory && progress != null && progress.Phase == WaveBattlePhase.AwaitingVictory) progress.ConfirmVictory();
        else progress?.Stop();
        resultHandled = true; UnsubscribeEnemies(); RefreshHUD();
    }
    public void StopBattle() { resultHandled = true; progress?.Stop(); UnsubscribeEnemies(); RefreshHUD(); }
    private void Fail(string message)
    {
        progress?.Fault(message); resultHandled = true; UnsubscribeEnemies();
        Debug.LogError("WaveManager: " + message, this); RefreshHUD();
    }
    private void RefreshHUD()
    {
        if (gameHUD == null || progress == null) return;
        int rest = (int)Math.Ceiling(progress.RestRemaining);
        if (shownWave == progress.CurrentWave && shownAlive == progress.Alive && shownPending == progress.Pending
            && shownRest == rest && shownPhase == progress.Phase) return;
        shownWave = progress.CurrentWave; shownAlive = progress.Alive; shownPending = progress.Pending;
        shownRest = rest; shownPhase = progress.Phase; gameHUD.UpdateWaveProgress(progress);
    }
    private void UnsubscribeEnemies()
    {
        foreach (var entry in tracked.Values) if (entry.health != null) entry.health.Defeated -= entry.handler;
        tracked.Clear();
    }
    private void OnDisable()
    { if (gameResultUI != null) gameResultUI.BattleFinished -= OnBattleFinished; StopBattle(); }
}
