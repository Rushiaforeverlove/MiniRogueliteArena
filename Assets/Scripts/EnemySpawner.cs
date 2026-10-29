using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject fastEnemyPrefab;
    [SerializeField, Range(0f, 1f)] private float fastEnemyChance = 0.25f;
    [SerializeField, Min(1)] private int fastEnemyUnlockWave = 2;
    private int currentWave = 1;
    [SerializeField] private Transform player;
    [SerializeField] private float spawnInterval = 1.5f;

    [Header("Arena Bounds")]
    [SerializeField] private float minX = -10.8f;
    [SerializeField] private float maxX = 10.8f;
    [SerializeField] private float minY = -5.8f;
    [SerializeField] private float maxY = 5.8f;

    private float spawnTimer;

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        Vector2 spawnPosition = GetRandomSpawnPosition();

        GameObject selectedPrefab = enemyPrefab;

        if (currentWave >= fastEnemyUnlockWave
            && fastEnemyPrefab != null
            && Random.value < fastEnemyChance)
        {
            selectedPrefab = fastEnemyPrefab;
        }

        GameObject enemyObject = Instantiate(
            selectedPrefab, spawnPosition, Quaternion.identity);

        EnemyController enemyController =
            enemyObject.GetComponent<EnemyController>();

        enemyController.SetTarget(player);
    }

    private Vector2 GetRandomSpawnPosition()
    {
        int side = Random.Range(0, 4);

        if (side == 0)
        {
            return new Vector2(Random.Range(minX, maxX), maxY);
        }

        if (side == 1)
        {
            return new Vector2(Random.Range(minX, maxX), minY);
        }

        if (side == 2)
        {
            return new Vector2(minX, Random.Range(minY, maxY));
        }

        return new Vector2(maxX, Random.Range(minY, maxY));
    }
    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.05f, newInterval);
    }
    public void SetWave(int wave)
    {
        currentWave = Mathf.Max(1, wave);
    }
}