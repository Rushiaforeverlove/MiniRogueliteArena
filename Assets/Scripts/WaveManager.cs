using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private GameHUD gameHUD;

    [Header("Wave Settings")]
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float startingSpawnInterval = 1.5f;
    [SerializeField] private float spawnIntervalReduction = 0.2f;
    [SerializeField] private float minimumSpawnInterval = 0.35f;

    private int currentWave = 1;
    private float waveTimer;

    private void Start()
    {
        ApplyWaveSettings();
    }

    private void Update()
    {
        waveTimer += Time.deltaTime;

        while (waveTimer >= waveDuration)
        {
            waveTimer -= waveDuration;
            currentWave++;
            ApplyWaveSettings();
        }
    }

    private void ApplyWaveSettings()
    {
        float spawnInterval = Mathf.Max(
            minimumSpawnInterval,
            startingSpawnInterval - (currentWave - 1) * spawnIntervalReduction);

        enemySpawner.SetWave(currentWave);
        enemySpawner.SetSpawnInterval(spawnInterval);
        gameHUD.UpdateWave(currentWave);

        Debug.Log(
            "Wave " + currentWave
            + " | Spawn interval: " + spawnInterval.ToString("F2"));
    }
}