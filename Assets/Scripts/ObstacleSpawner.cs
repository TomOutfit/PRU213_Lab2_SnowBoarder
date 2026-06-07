using UnityEngine;
using System.Collections.Generic;

public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnInterval = 5f;
    public float minSpawnDistance = 18f;
    public float maxSpawnDistance = 28f;

    [Header("Difficulty override per level")]
    public bool useLevelDifficulty = true;

    [Header("Obstacle Prefabs (Assigned in Inspector)")]
    public GameObject[] obstaclePrefabs;

    private List<GameObject> loadedObstacles = new List<GameObject>();
    private float timer;
    private GameObject player;
    private string activeSceneName;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go;

        activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadObstaclesForLevel();
    }

    void LoadObstaclesForLevel()
    {
        loadedObstacles.Clear();

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;

        // Phân loại các prefab từ mảng Inspector
        GameObject mud = null;
        GameObject rock1 = null;
        GameObject rock2 = null;
        GameObject tree1 = null;
        GameObject tree2 = null;

        foreach (var prefab in obstaclePrefabs)
        {
            if (prefab == null) continue;
            string name = prefab.name.ToLower();
            if (name.Contains("muddy")) mud = prefab;
            else if (name.Contains("rock1") || (name.Contains("rock") && !name.Contains("2"))) rock1 = prefab;
            else if (name.Contains("rock2")) rock2 = prefab;
            else if (name.Contains("tree1") || (name.Contains("tree") && !name.Contains("2"))) tree1 = prefab;
            else if (name.Contains("tree2")) tree2 = prefab;
        }

        if (useLevelDifficulty)
        {
            if (activeSceneName == "Level1")
            {
                // Level 1: Dễ - Tần suất spawn thưa, chủ yếu là bùn trơn trượt và đá nhỏ
                spawnInterval = 6.5f;
                if (mud != null) loadedObstacles.Add(mud);
                if (mud != null) loadedObstacles.Add(mud); // tăng tỷ lệ mud
                if (rock1 != null) loadedObstacles.Add(rock1);
            }
            else if (activeSceneName == "Level2")
            {
                // Level 2: Trung bình - Tần suất vừa, kết hợp đá nhỏ, đá to và bùn
                spawnInterval = 4.0f;
                if (mud != null) loadedObstacles.Add(mud);
                if (rock1 != null) loadedObstacles.Add(rock1);
                if (rock2 != null) loadedObstacles.Add(rock2);
            }
            else if (activeSceneName == "Level3")
            {
                // Level 3: Khó - Tần suất rất cao, có thêm cây cối lớn chặn đường khó lách qua
                spawnInterval = 2.5f;
                if (mud != null) loadedObstacles.Add(mud);
                if (rock1 != null) loadedObstacles.Add(rock1);
                if (rock2 != null) loadedObstacles.Add(rock2);
                if (tree1 != null) loadedObstacles.Add(tree1);
                if (tree2 != null) loadedObstacles.Add(tree2);
            }
            else
            {
                spawnInterval = 5.0f;
                foreach (var p in obstaclePrefabs) if (p != null) loadedObstacles.Add(p);
            }
        }
        else
        {
            foreach (var p in obstaclePrefabs) if (p != null) loadedObstacles.Add(p);
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnObstacle();
        }
    }

    void SpawnObstacle()
    {
        if (loadedObstacles.Count == 0) return;

        float playerX = player != null ? player.transform.position.x : transform.position.x;
        float playerY = player != null ? player.transform.position.y : transform.position.y;

        // Xác định X ngẫu nhiên phía trước người chơi
        float spawnX = playerX + Random.Range(minSpawnDistance, maxSpawnDistance);
        
        // Raycast từ trên trời xuống để đặt chướng ngại vật chính xác trên mặt đất
        Vector3 spawnPos = new Vector3(spawnX, playerY - 5f, 0f); // default fallback
        
        Vector2 rayStart = new Vector2(spawnX, playerY + 20f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 45f, LayerMask.GetMask("Ground"));
        if (hit.collider == null)
        {
            hit = Physics2D.Raycast(rayStart, Vector2.down, 45f);
        }

        if (hit.collider != null)
        {
            spawnPos = new Vector3(spawnX, hit.point.y, 0f);
        }

        // Chọn chướng ngại vật ngẫu nhiên trong danh sách hợp lệ của Level
        int index = Random.Range(0, loadedObstacles.Count);
        GameObject prefab = loadedObstacles[index];

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);

            // Gán Tag tương ứng để kích hoạt hiệu ứng va chạm chính xác
            string nameLower = prefab.name.ToLower();
            if (nameLower.Contains("mud"))
            {
                obj.tag = "SlowDown";
            }
            else if (nameLower.Contains("rock"))
            {
                obj.tag = "Penalty";
            }
            else if (nameLower.Contains("tree"))
            {
                obj.tag = "Obstacle";
            }

            if (obj.GetComponent<DestroyBehindPlayer>() == null)
            {
                obj.AddComponent<DestroyBehindPlayer>();
            }
        }
    }

    public void TriggerSpawn()
    {
        SpawnObstacle();
    }
}
