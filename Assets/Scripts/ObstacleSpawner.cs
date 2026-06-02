using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnInterval = 5f;
    public float spawnRangeX = 8f;
    public float spawnYOffset = 15f;

    [Header("Obstacle Prefabs")]
    public GameObject[] obstaclePrefabs;

    [Header("Difficulty")]
    public bool autoSpawn = true;
    public float difficultyScale = 1f;
    public float difficultyIncreaseRate = 0f;

    float timer;
    float currentDifficulty;
    GameObject player;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        if (!autoSpawn) return;

        if (difficultyIncreaseRate > 0f)
        {
            currentDifficulty += difficultyIncreaseRate * Time.deltaTime;
        }

        timer += Time.deltaTime;
        float adjustedInterval = spawnInterval / (difficultyScale + currentDifficulty);

        if (timer >= adjustedInterval)
        {
            timer = 0f;
            SpawnObstacle();
        }
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float spawnY = (player != null ? player.transform.position.y : transform.position.y) + spawnYOffset;
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        int index = Random.Range(0, obstaclePrefabs.Length);
        if (obstaclePrefabs[index] != null)
        {
            GameObject obj = Instantiate(obstaclePrefabs[index], spawnPos, Quaternion.identity);
            obj.AddComponent<DestroyBehindPlayer>();
        }
    }

    public void TriggerSpawn()
    {
        SpawnObstacle();
    }
}
