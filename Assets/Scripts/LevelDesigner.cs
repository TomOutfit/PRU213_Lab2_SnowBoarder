using System;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Level Designer component for creating snowboarding levels in the Unity Editor.
/// Provides methods for terrain generation, obstacle placement, and level organization.
/// </summary>
[ExecuteInEditMode]
public class LevelDesigner : MonoBehaviour
{
    [Header("Level Information")]
    [SerializeField] private string levelName = "New Level";
    [SerializeField] private float levelLength = 500f;
    [SerializeField] [Range(0f, 1f)] private float levelDifficulty = 0.5f;
    
    [Header("Terrain")]
    [SerializeField] private UnityEngine.U2D.SpriteShapeController terrainSpline;
    [SerializeField] private Transform obstacleContainer;
    [SerializeField] private Transform collectibleContainer;
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private Color terrainColor = Color.white;
    
    [Header("Points")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform finishPoint;
    
    [Header("Object Pools")]
    [SerializeField] private GameObject[] availableObstacles;
    [SerializeField] private GameObject[] availableCollectibles;
    [SerializeField] private GameObject[] availablePowerUps;
    
    [Header("Grid Settings")]
    [SerializeField] private bool snapToGrid = true;
    [SerializeField] private float gridSize = 1f;
    
    // Properties
    public string LevelName { get => levelName; set => levelName = value; }
    public float LevelDifficulty { get => levelDifficulty; set => levelDifficulty = value; }
    public bool SnapToGrid { get => snapToGrid; set => snapToGrid = value; }
    public float GridSize { get => gridSize; set => gridSize = value; }
    public Transform ObstacleContainer => obstacleContainer;
    public GameObject[] AvailableObstacles => availableObstacles;
    public GameObject[] AvailableCollectibles => availableCollectibles;
    public GameObject[] AvailablePowerUps => availablePowerUps;
    
    private void Awake()
    {
        CreateContainers();
    }
    
    private void CreateContainers()
    {
        if (obstacleContainer == null)
        {
            GameObject obstacles = new GameObject("Obstacles");
            obstacles.transform.parent = transform;
            obstacleContainer = obstacles.transform;
        }
        
        if (collectibleContainer == null)
        {
            GameObject collectibles = new GameObject("Collectibles");
            collectibles.transform.parent = transform;
            collectibleContainer = collectibles.transform;
        }
    }
    
    #region Level Generation
    
    /// <summary>
    /// Generates a preview of the level with default obstacles and collectibles.
    /// </summary>
    public void GenerateLevelPreview()
    {
        if (availableObstacles == null || availableObstacles.Length == 0)
        {
            Debug.LogWarning("No obstacles assigned to LevelDesigner.");
            return;
        }
        
        ClearLevel();
        
        float spacing = 20f;
        float startX = 50f;
        
        for (float x = startX; x < levelLength; x += spacing)
        {
            float y = GetTerrainHeight(x);
            
            if (UnityEngine.Random.value < 0.3f)
            {
                PlaceObstacleAt(x, y + 1f);
            }
            
            if (UnityEngine.Random.value < 0.5f)
            {
                PlaceCollectibleAt(x, y + 3f);
            }
        }
        
        Debug.Log($"Level preview generated: {levelLength}m, {spacing}m spacing");
    }
    
    /// <summary>
    /// Gets the approximate terrain height at a given X position.
    /// </summary>
    private float GetTerrainHeight(float x)
    {
        if (terrainSpline != null && terrainSpline.spline != null)
        {
            float normalizedX = x / levelLength;
            int pointCount = terrainSpline.spline.GetPointCount();
            
            if (pointCount > 1)
            {
                int index = Mathf.FloorToInt(normalizedX * (pointCount - 1));
                index = Mathf.Clamp(index, 0, pointCount - 2);
                
                var point0 = terrainSpline.spline.GetPosition(index);
                var point1 = terrainSpline.spline.GetPosition(index + 1);
                
                float t = (normalizedX * (pointCount - 1)) - index;
                return Mathf.Lerp(point0.y, point1.y, t);
            }
        }
        
        float sinHeight = Mathf.Sin(x * 0.02f) * 5f;
        float noiseHeight = Mathf.PerlinNoise(x * 0.1f, 0f) * 3f;
        return -10f + sinHeight + noiseHeight;
    }
    
    /// <summary>
    /// Clears all placed objects from the level.
    /// </summary>
    public void ClearLevel()
    {
        ClearContainer(obstacleContainer);
        ClearContainer(collectibleContainer);
    }
    
    private void ClearContainer(Transform container)
    {
        if (container == null) return;
        
        while (container.childCount > 0)
        {
            Transform child = container.GetChild(0);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
    
    #endregion
    
    #region Object Placement
    
    /// <summary>
    /// Places an obstacle at the specified position.
    /// </summary>
    public void PlaceObstacleAt(float x, float y)
    {
        if (availableObstacles == null || availableObstacles.Length == 0) return;
        
        GameObject obstaclePrefab = availableObstacles[UnityEngine.Random.Range(0, availableObstacles.Length)];
        Vector3 position = new Vector3(x, y, 0f);
        
        if (snapToGrid)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
        }
        
        PlaceObject(obstaclePrefab, position, obstacleContainer);
    }
    
    /// <summary>
    /// Places a collectible at the specified position.
    /// </summary>
    public void PlaceCollectibleAt(float x, float y)
    {
        if (availableCollectibles == null || availableCollectibles.Length == 0) return;
        
        GameObject collectiblePrefab = availableCollectibles[UnityEngine.Random.Range(0, availableCollectibles.Length)];
        Vector3 position = new Vector3(x, y, 0f);
        
        if (snapToGrid)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
        }
        
        PlaceObject(collectiblePrefab, position, collectibleContainer);
    }
    
    /// <summary>
    /// Places a power-up at the specified position.
    /// </summary>
    public void PlacePowerUpAt(float x, float y)
    {
        if (availablePowerUps == null || availablePowerUps.Length == 0) return;
        
        GameObject powerUpPrefab = availablePowerUps[UnityEngine.Random.Range(0, availablePowerUps.Length)];
        Vector3 position = new Vector3(x, y, 0f);
        
        if (snapToGrid)
        {
            position.x = Mathf.Round(position.x / gridSize) * gridSize;
            position.y = Mathf.Round(position.y / gridSize) * gridSize;
        }
        
        PlaceObject(powerUpPrefab, position, collectibleContainer);
    }
    
    private void PlaceObject(GameObject prefab, Vector3 position, Transform container)
    {
        if (prefab == null) return;
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject newObject = PrefabUtility.InstantiatePrefab(prefab, container) as GameObject;
            newObject.transform.position = position;
            Undo.RegisterCreatedObjectUndo(newObject, "Place Object");
        }
        else
#endif
        {
            GameObject newObject = Instantiate(prefab, position, Quaternion.identity, container);
        }
    }
    
    #endregion
    
    #region Terrain Manipulation
    
    /// <summary>
    /// Smooths the terrain spline for a more natural look.
    /// </summary>
    public void SmoothTerrain()
    {
        if (terrainSpline == null || terrainSpline.spline == null) return;
        
#if UNITY_EDITOR
        Undo.RecordObject(terrainSpline, "Smooth Terrain");
#endif
        
        int pointCount = terrainSpline.spline.GetPointCount();
        
        for (int i = 1; i < pointCount - 1; i++)
        {
            var prev = terrainSpline.spline.GetPosition(i - 1);
            var curr = terrainSpline.spline.GetPosition(i);
            var next = terrainSpline.spline.GetPosition(i + 1);
            
            Vector3 smoothed = (prev + curr * 2f + next) / 4f;
            
            terrainSpline.spline.SetPosition(i, smoothed);
        }
        
        EditorUtility.SetDirty(terrainSpline);
    }
    
    /// <summary>
    /// Adds terrain variations like slopes, valleys, and jumps.
    /// </summary>
    /// <param name="variationType">0=Slope, 1=Valley, 2=Jump</param>
    public void AddTerrainVariation(int variationType)
    {
        if (terrainSpline == null || terrainSpline.spline == null) return;
        
#if UNITY_EDITOR
        Undo.RecordObject(terrainSpline, "Add Terrain Variation");
#endif
        
        int pointCount = terrainSpline.spline.GetPointCount();
        if (pointCount < 3) return;
        
        int insertIndex = pointCount - 2;
        Vector3 insertPosition = terrainSpline.spline.GetPosition(insertIndex);
        
        switch (variationType)
        {
            case 0: // Slope
                insertPosition.y -= 5f;
                break;
            case 1: // Valley
                insertPosition.y += 3f;
                break;
            case 2: // Jump
                insertPosition.y -= 3f;
                break;
        }
        
        terrainSpline.spline.InsertPointAt(insertIndex + 1, insertPosition);
        EditorUtility.SetDirty(terrainSpline);
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Erases all objects in a specific layer/category.
    /// </summary>
    public void EraseObjectsInLayer(string layerName)
    {
        Transform targetContainer = null;
        
        switch (layerName)
        {
            case "Obstacles":
                targetContainer = obstacleContainer;
                break;
            case "Collectibles":
                targetContainer = collectibleContainer;
                break;
        }
        
        if (targetContainer != null)
        {
            ClearContainer(targetContainer);
        }
    }
    
    /// <summary>
    /// Validates the level configuration.
    /// </summary>
    public bool ValidateLevel()
    {
        if (terrainSpline == null)
        {
            Debug.LogError("Terrain spline not assigned!");
            return false;
        }
        
        if (spawnPoint == null)
        {
            Debug.LogError("Spawn point not assigned!");
            return false;
        }
        
        if (finishPoint == null)
        {
            Debug.LogError("Finish point not assigned!");
            return false;
        }
        
        if (availableObstacles == null || availableObstacles.Length == 0)
        {
            Debug.LogWarning("No obstacles assigned. Level may be too easy.");
        }
        
        return true;
    }
    
    /// <summary>
    /// Creates a checkpoint at the specified position.
    /// </summary>
    public void CreateCheckpoint(float x, float y)
    {
        GameObject checkpoint = new GameObject($"Checkpoint_{x}");
        checkpoint.transform.parent = transform;
        checkpoint.transform.position = new Vector3(x, y, 0f);
        
        var collider = checkpoint.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2f, 5f);
        
        checkpoint.AddComponent<CheckpointController>();
        
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(checkpoint, "Create Checkpoint");
#endif
    }
    
    #endregion
    
    private void OnDrawGizmos()
    {
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 2f);
            Gizmos.DrawIcon(spawnPoint.position, "Assets/Sprites/blue-fill.png", true);
        }
        
        if (finishPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(finishPoint.position, Vector3.one * 2f);
        }
        
        if (terrainSpline != null)
        {
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireCube(new Vector3(levelLength / 2f, 0, 0), new Vector3(levelLength, 20f, 1f));
        }
    }
}

/// <summary>
/// Checkpoint controller for tracking player progress through the level.
/// </summary>
public class CheckpointController : MonoBehaviour
{
    [SerializeField] private int checkpointNumber = 1;
    [SerializeField] private bool isActivated;
    
    public event Action<int> OnCheckpointActivated;
    
    public int CheckpointNumber => checkpointNumber;
    public bool IsActivated => isActivated;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            Activate();
        }
    }
    
    public void Activate()
    {
        isActivated = true;
        OnCheckpointActivated?.Invoke(checkpointNumber);
        
        GetComponent<SpriteRenderer>().color = Color.green;
        
        Debug.Log($"Checkpoint {checkpointNumber} activated!");
    }
}
