using UnityEngine;
using System.Collections.Generic;

public class SnowflakeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnInterval = 2f;
    public float minSpawnDistance = 15f;
    public float maxSpawnDistance = 25f;

    [Header("Difficulty override per level")]
    public bool useLevelDifficulty = true;

    private Dictionary<string, GameObject> itemPrefabs = new Dictionary<string, GameObject>();
    private float timer;
    private GameObject player;
    private string activeSceneName;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go;

        activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        LoadItemPrefabs();
        ConfigureInterval();
    }

    void LoadItemPrefabs()
    {
        itemPrefabs.Clear();
        string[] names = { "SmallSnowflake", "IceCoin", "GoldenSnowflake", "Trophy", "IceDiamond", "EnergyDrink", "IceShield", "MultiplierStar" };
        foreach (var name in names)
        {
            GameObject prefab = Resources.Load<GameObject>("Items/" + name);
            if (prefab != null)
            {
                itemPrefabs.Add(name, prefab);
            }
        }
    }

    void ConfigureInterval()
    {
        if (useLevelDifficulty)
        {
            if (activeSceneName == "Level1")
            {
                spawnInterval = 1.8f; // Rải nhiều vật phẩm ở Level 1 (Dễ)
            }
            else if (activeSceneName == "Level2")
            {
                spawnInterval = 3.0f; // Rải trung bình ở Level 2 (Trung bình)
            }
            else if (activeSceneName == "Level3")
            {
                spawnInterval = 4.5f; // Rải rất ít vật phẩm ở Level 3 (Khó)
            }
            else
            {
                spawnInterval = 2.0f;
            }
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
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        if (itemPrefabs.Count == 0) return;

        float playerX = player != null ? player.transform.position.x : transform.position.x;
        float playerY = player != null ? player.transform.position.y : transform.position.y;

        // Xác định X ngẫu nhiên phía trước người chơi
        float spawnX = playerX + Random.Range(minSpawnDistance, maxSpawnDistance);
        
        // Raycast từ trên trời xuống để lấy vị trí mặt đất chính xác
        float groundY = playerY - 3f;
        Vector2 rayStart = new Vector2(spawnX, playerY + 20f);
        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, 45f, LayerMask.GetMask("Ground"));
        if (hit.collider == null)
        {
            hit = Physics2D.Raycast(rayStart, Vector2.down, 45f);
        }

        if (hit.collider != null)
        {
            groundY = hit.point.y;
        }

        // Vật phẩm bay lơ lửng trên mặt đất một khoảng ngẫu nhiên để người chơi dễ lượn qua ăn được
        float hoverHeight = Random.Range(0.8f, 3.2f);
        Vector3 spawnPos = new Vector3(spawnX, groundY + hoverHeight, 0f);

        // Quyết định vật phẩm nào sẽ xuất hiện dựa trên độ khó của Level
        GameObject prefab = SelectPrefabByDifficulty();

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            if (obj.GetComponent<DestroyBehindPlayer>() == null)
            {
                obj.AddComponent<DestroyBehindPlayer>();
            }
        }
    }

    GameObject GetPrefab(string name)
    {
        GameObject p;
        itemPrefabs.TryGetValue(name, out p);
        return p;
    }

    GameObject SelectPrefabByDifficulty()
    {
        float roll = Random.value;

        if (activeSceneName == "Level1")
        {
            // Level 1: Chủ yếu là SmallSnowflake, IceCoin, GoldenSnowflake và có tỷ lệ cao trúng Trophy / IceShield
            if (roll < 0.35f) return GetPrefab("SmallSnowflake");
            if (roll < 0.65f) return GetPrefab("IceCoin");
            if (roll < 0.80f) return GetPrefab("GoldenSnowflake");
            if (roll < 0.88f) return GetPrefab("Trophy");
            if (roll < 0.93f) return GetPrefab("IceShield");
            if (roll < 0.97f) return GetPrefab("IceDiamond");
            return GetPrefab("MultiplierStar");
        }
        else if (activeSceneName == "Level2")
        {
            // Level 2: Phân bố đều hơn
            if (roll < 0.40f) return GetPrefab("SmallSnowflake");
            if (roll < 0.65f) return GetPrefab("IceCoin");
            if (roll < 0.80f) return GetPrefab("GoldenSnowflake");
            if (roll < 0.86f) return GetPrefab("Trophy");
            if (roll < 0.91f) return GetPrefab("IceDiamond");
            if (roll < 0.95f) return GetPrefab("EnergyDrink");
            if (roll < 0.98f) return GetPrefab("IceShield");
            return GetPrefab("MultiplierStar");
        }
        else if (activeSceneName == "Level3")
        {
            // Level 3: Khó - Đồ xịn hiếm hơn nhiều, đa số chỉ ra SmallSnowflake và IceCoin
            if (roll < 0.50f) return GetPrefab("SmallSnowflake");
            if (roll < 0.80f) return GetPrefab("IceCoin");
            if (roll < 0.90f) return GetPrefab("GoldenSnowflake");
            if (roll < 0.93f) return GetPrefab("Trophy");
            if (roll < 0.95f) return GetPrefab("IceDiamond");
            if (roll < 0.97f) return GetPrefab("EnergyDrink");
            if (roll < 0.99f) return GetPrefab("IceShield");
            return GetPrefab("MultiplierStar");
        }
        else
        {
            if (roll < 0.45f) return GetPrefab("SmallSnowflake");
            return GetPrefab("IceCoin");
        }
    }
}
