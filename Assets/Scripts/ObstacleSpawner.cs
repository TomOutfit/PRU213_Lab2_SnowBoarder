using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns obstacles dynamically based on player progress and difficulty settings.
/// Object pooling for performance optimization.
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private Transform spawnAreaStart;
    [SerializeField] private Transform spawnAreaEnd;
    
    [Header("Spawn Rules")]
    [SerializeField] private float baseSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float spawnDistanceAhead = 50f;
    [SerializeField] private float despawnDistanceBehind = 30f;
    
    [Header("Difficulty Scaling")]
    [SerializeField] private bool scaleWithDifficulty = true;
    [SerializeField] private float difficultyIncreaseRate = 0.1f;
    
    [Header("Pooling")]
    [SerializeField] private int poolSize = 20;
    [SerializeField] private bool useObjectPooling = true;
    
    [Header("Spawn Patterns")]
    [SerializeField] private SpawnPattern[] spawnPatterns;
    
    private List<PooledObject> objectPool;
    private List<GameObject> activeObjects;
    private Transform playerTransform;
    private float spawnTimer;
    private float currentDifficulty;
    private int lastSpawnIndex = -1;
    
    private void Awake()
    {
        objectPool = new List<PooledObject>();
        activeObjects = new List<GameObject>();
        
        if (useObjectPooling)
        {
            InitializeObjectPool();
        }
    }
    
    private void Start()
    {
        FindPlayer();
        currentDifficulty = GameManager.Instance != null ? GameManager.Instance.LevelDifficulty : 0.5f;
    }
    
    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
        {
            return;
        }
        
        UpdateSpawning();
        UpdateDespawning();
        UpdateDifficulty();
    }
    
    private void FindPlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            playerTransform = GameManager.Instance.CurrentPlayer.transform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    
    #region Object Pooling
    
    private void InitializeObjectPool()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
        
        for (int i = 0; i < poolSize; i++)
        {
            int prefabIndex = i % obstaclePrefabs.Length;
            GameObject prefab = obstaclePrefabs[prefabIndex];
            
            if (prefab == null) continue;
            
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            
            PooledObject pooled = new PooledObject
            {
                GameObject = obj,
                PrefabIndex = prefabIndex,
                IsActive = false
            };
            
            objectPool.Add(pooled);
        }
    }
    
    private PooledObject GetPooledObject()
    {
        foreach (PooledObject pooled in objectPool)
        {
            if (!pooled.IsActive)
            {
                pooled.IsActive = true;
                pooled.GameObject.SetActive(true);
                return pooled;
            }
        }
        
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            int prefabIndex = UnityEngine.Random.Range(0, obstaclePrefabs.Length);
            GameObject newObj = Instantiate(obstaclePrefabs[prefabIndex], transform);
            
            PooledObject pooled = new PooledObject
            {
                GameObject = newObj,
                PrefabIndex = prefabIndex,
                IsActive = true
            };
            
            objectPool.Add(pooled);
            return pooled;
        }
        
        return null;
    }
    
    private void ReturnToPool(PooledObject pooled)
    {
        pooled.IsActive = false;
        pooled.GameObject.SetActive(false);
        activeObjects.Remove(pooled.GameObject);
    }
    
    #endregion
    
    #region Spawning
    
    private void UpdateSpawning()
    {
        float spawnInterval = CalculateSpawnInterval();
        
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnObstacle();
        }
    }
    
    private float CalculateSpawnInterval()
    {
        float interval = baseSpawnInterval;
        
        if (scaleWithDifficulty)
        {
            interval -= currentDifficulty * (baseSpawnInterval - minSpawnInterval);
        }
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            float speed = GameManager.Instance.CurrentPlayer.CurrentSpeed;
            interval -= speed * 0.02f;
        }
        
        return Mathf.Max(interval, minSpawnInterval);
    }
    
    private void TrySpawnObstacle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
        if (playerTransform == null) return;
        
        float playerX = playerTransform.position.x;
        float spawnX = playerX + spawnDistanceAhead;
        
        if (spawnAreaStart != null && spawnX < spawnAreaStart.position.x)
        {
            return;
        }
        
        if (spawnAreaEnd != null && spawnX > spawnAreaEnd.position.x)
        {
            return;
        }
        
        int prefabIndex = SelectObstacle();
        
        Vector3 spawnPos = CalculateSpawnPosition(spawnX);
        
        SpawnObstacle(prefabIndex, spawnPos);
    }
    
    private int SelectObstacle()
    {
        if (spawnPatterns != null && spawnPatterns.Length > 0)
        {
            SpawnPattern pattern = GetActivePattern();
            if (pattern != null && pattern.obstacles != null && pattern.obstacles.Length > 0)
            {
                return pattern.obstacles[UnityEngine.Random.Range(0, pattern.obstacles.Length)];
            }
        }
        
        int index;
        do
        {
            index = UnityEngine.Random.Range(0, obstaclePrefabs.Length);
        } while (index == lastSpawnIndex && obstaclePrefabs.Length > 1);
        
        lastSpawnIndex = index;
        return index;
    }
    
    private SpawnPattern GetActivePattern()
    {
        if (spawnPatterns == null || spawnPatterns.Length == 0) return null;
        
        foreach (var pattern in spawnPatterns)
        {
            if (pattern.minDifficulty <= currentDifficulty && currentDifficulty <= pattern.maxDifficulty)
            {
                return pattern;
            }
        }
        
        return null;
    }
    
    private Vector3 CalculateSpawnPosition(float spawnX)
    {
        float y;
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            y = GameManager.Instance.CurrentPlayer.transform.position.y + UnityEngine.Random.Range(-5f, 5f);
        }
        else
        {
            y = UnityEngine.Random.Range(-5f, 10f);
        }
        
        return new Vector3(spawnX, y, 0f);
    }
    
    private void SpawnObstacle(int prefabIndex, Vector3 position)
    {
        if (prefabIndex < 0 || prefabIndex >= obstaclePrefabs.Length) return;
        
        GameObject obstacle;
        
        if (useObjectPooling)
        {
            PooledObject pooled = GetPooledObject();
            if (pooled == null) return;
            
            pooled.GameObject.transform.position = position;
            pooled.GameObject.transform.rotation = Quaternion.identity;
            obstacle = pooled.GameObject;
        }
        else
        {
            obstacle = Instantiate(obstaclePrefabs[prefabIndex], position, Quaternion.identity, transform);
        }
        
        activeObjects.Add(obstacle);
    }
    
    #endregion
    
    #region Despawning
    
    private void UpdateDespawning()
    {
        if (playerTransform == null) return;
        
        float playerX = playerTransform.position.x;
        float despawnX = playerX - despawnDistanceBehind;
        
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            
            if (obj == null)
            {
                activeObjects.RemoveAt(i);
                continue;
            }
            
            if (obj.transform.position.x < despawnX)
            {
                if (useObjectPooling)
                {
                    PooledObject pooled = FindPooledObject(obj);
                    if (pooled != null)
                    {
                        ReturnToPool(pooled);
                    }
                }
                else
                {
                    Destroy(obj);
                    activeObjects.RemoveAt(i);
                }
            }
        }
    }
    
    private PooledObject FindPooledObject(GameObject obj)
    {
        foreach (PooledObject pooled in objectPool)
        {
            if (pooled.GameObject == obj)
            {
                return pooled;
            }
        }
        return null;
    }
    
    #endregion
    
    #region Difficulty
    
    private void UpdateDifficulty()
    {
        if (!scaleWithDifficulty) return;
        
        currentDifficulty += difficultyIncreaseRate * Time.deltaTime;
        currentDifficulty = Mathf.Clamp01(currentDifficulty);
    }
    
    public void SetDifficulty(float difficulty)
    {
        currentDifficulty = Mathf.Clamp01(difficulty);
    }
    
    #endregion
    
    #region Public Methods
    
    public void AddObstaclePrefab(GameObject prefab)
    {
        if (prefab == null) return;
        
        System.Array.Resize(ref obstaclePrefabs, obstaclePrefabs.Length + 1);
        obstaclePrefabs[obstaclePrefabs.Length - 1] = prefab;
    }
    
    public void ClearAllObstacles()
    {
        foreach (var obj in activeObjects)
        {
            if (obj != null)
            {
                if (useObjectPooling)
                {
                    PooledObject pooled = FindPooledObject(obj);
                    if (pooled != null)
                    {
                        ReturnToPool(pooled);
                    }
                }
                else
                {
                    Destroy(obj);
                }
            }
        }
        
        activeObjects.Clear();
    }
    
    public void ResetSpawner()
    {
        ClearAllObstacles();
        spawnTimer = 0f;
        currentDifficulty = 0f;
        lastSpawnIndex = -1;
    }
    
    #endregion
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        
        if (spawnAreaStart != null && spawnAreaEnd != null)
        {
            Vector3 start = spawnAreaStart.position;
            Vector3 end = spawnAreaEnd.position;
            
            Gizmos.DrawWireSphere(start, 1f);
            Gizmos.DrawWireSphere(end, 1f);
            Gizmos.DrawLine(start, end);
        }
        
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 spawnPos = new Vector3(playerTransform.position.x + spawnDistanceAhead, playerTransform.position.y, 0);
            Gizmos.DrawWireSphere(spawnPos, 2f);
            
            Gizmos.color = Color.gray;
            Vector3 despawnPos = new Vector3(playerTransform.position.x - despawnDistanceBehind, playerTransform.position.y, 0);
            Gizmos.DrawWireSphere(despawnPos, 2f);
        }
    }
}

[System.Serializable]
public class PooledObject
{
    public GameObject GameObject;
    public int PrefabIndex;
    public bool IsActive;
}

[System.Serializable]
public class SpawnPattern
{
    public string name = "New Pattern";
    public float minDifficulty = 0f;
    public float maxDifficulty = 1f;
    public int[] obstacles;
}
