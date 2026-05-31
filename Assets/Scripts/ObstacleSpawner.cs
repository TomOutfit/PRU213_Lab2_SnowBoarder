using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float spawnRangeX = 8f;
    [SerializeField] private float spawnYOffset = 15f;

    [Header("Obstacle Prefabs")]
    [SerializeField] private GameObject[] obstaclePrefabs;

    [Header("Spawner Behavior")]
    [SerializeField] private bool autoSpawn = true;
    [SerializeField] private float difficultyScale = 1f;
    [SerializeField] private float difficultyIncreaseRate = 0.01f;

    private float spawnTimer;
    private float currentDifficulty;
    private float sessionTime;

    private void Update()
    {
        if (!autoSpawn) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        sessionTime += Time.deltaTime;
        currentDifficulty = 1f + (sessionTime * difficultyIncreaseRate);

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnObstacle();
            float scaledInterval = spawnInterval / (currentDifficulty * difficultyScale);
            spawnTimer = Mathf.Max(0.5f, scaledInterval);
        }
    }

    private void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            GameObject rock = Resources.Load<GameObject>("Prefabs/RockPrefab1");
            if (rock != null)
                Instantiate(rock, GetSpawnPosition(), Quaternion.identity);
            return;
        }

        int index = Random.Range(0, obstaclePrefabs.Length);
        GameObject prefab = obstaclePrefabs[index];
        if (prefab != null)
            Instantiate(prefab, GetSpawnPosition(), Quaternion.identity);
    }

    private Vector3 GetSpawnPosition()
    {
        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float baseY = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position.y
            : transform.position.y;
        return new Vector3(x, baseY - Mathf.Abs(spawnYOffset), 0f);
    }
}
