using UnityEngine;

/// <summary>
/// Manages level progression, checkpoints, and level completion.
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Configuration")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelName = "Mountain 1";
    [SerializeField] private float levelLength = 500f;
    [SerializeField] private float levelDifficulty = 0.5f;
    
    [Header("Checkpoints")]
    [SerializeField] private Transform[] checkpoints;
    [SerializeField] private int currentCheckpointIndex = 0;
    [SerializeField] private bool useCheckpoints = true;
    
    [Header("Level Bounds")]
    [SerializeField] private Transform levelStart;
    [SerializeField] private Transform levelEnd;
    
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private ObstacleSpawner obstacleSpawner;
    [SerializeField] private LevelDesigner levelDesigner;
    
    // Level State
    private float startPosition;
    private float distanceTraveled;
    private float progressPercent;
    private int scoreAtStart;
    private bool isLevelComplete;
    
    // Events
    public event System.Action OnLevelStart;
    public event System.Action<float> OnProgressChanged;
    public event System.Action<int> OnCheckpointReached;
    public event System.Action OnLevelComplete;
    public event System.Action<float> OnLevelFailed;
    
    // Properties
    public int LevelNumber => levelNumber;
    public string LevelName => levelName;
    public float LevelLength => levelLength;
    public float LevelDifficulty => levelDifficulty;
    public float ProgressPercent => progressPercent;
    public float DistanceTraveled => distanceTraveled;
    public bool IsLevelComplete => isLevelComplete;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        SetupLevel();
    }
    
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing)
        {
            UpdateProgress();
            CheckCheckpointReached();
            CheckLevelCompletion();
        }
    }
    
    private void SetupLevel()
    {
        if (levelStart != null)
        {
            startPosition = levelStart.position.x;
        }
        
        if (checkpoints == null || checkpoints.Length == 0)
        {
            useCheckpoints = false;
        }
        
        isLevelComplete = false;
        currentCheckpointIndex = 0;
        distanceTraveled = 0f;
        progressPercent = 0f;
    }
    
    #region Level Flow
    
    public void StartLevel()
    {
        if (player == null)
        {
            FindPlayer();
        }
        
        if (GameManager.Instance != null)
        {
            scoreAtStart = GameManager.Instance.Score;
        }
        
        isLevelComplete = false;
        distanceTraveled = 0f;
        progressPercent = 0f;
        currentCheckpointIndex = 0;
        
        OnLevelStart?.Invoke();
    }
    
    public void RestartLevel()
    {
        if (obstacleSpawner != null)
        {
            obstacleSpawner.ResetSpawner();
        }
        
        if (player != null)
        {
            Vector3 respawnPos = GetRespawnPosition();
            player.transform.position = respawnPos;
            player.gameObject.SetActive(true);
        }
        
        isLevelComplete = false;
        distanceTraveled = 0f;
        progressPercent = 0f;
        currentCheckpointIndex = 0;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
    }
    
    public void CompleteLevel()
    {
        if (isLevelComplete) return;
        
        isLevelComplete = true;
        
        float completionBonus = CalculateCompletionBonus();
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(Mathf.RoundToInt(completionBonus));
            GameManager.Instance.CompleteLevel();
        }
        
        OnLevelComplete?.Invoke();
    }
    
    public void FailLevel()
    {
        if (isLevelComplete) return;
        
        float failProgress = progressPercent;
        OnLevelFailed?.Invoke(failProgress);
    }
    
    #endregion
    
    #region Progress Tracking
    
    private void UpdateProgress()
    {
        if (player == null)
        {
            FindPlayer();
            return;
        }
        
        float playerX = player.transform.position.x;
        distanceTraveled = Mathf.Max(0, playerX - startPosition);
        progressPercent = Mathf.Clamp01(distanceTraveled / levelLength);
        
        OnProgressChanged?.Invoke(progressPercent);
    }
    
    private void CheckCheckpointReached()
    {
        if (!useCheckpoints || checkpoints == null || checkpoints.Length == 0) return;
        if (currentCheckpointIndex >= checkpoints.Length) return;
        
        Transform currentCheckpoint = checkpoints[currentCheckpointIndex];
        
        if (currentCheckpoint == null) return;
        
        float playerX = player != null ? player.transform.position.x : 0;
        
        if (playerX >= currentCheckpoint.position.x)
        {
            ReachCheckpoint(currentCheckpointIndex);
        }
    }
    
    private void ReachCheckpoint(int index)
    {
        if (GameManager.Instance != null)
        {
            int checkpointBonus = 500 * (index + 1);
            GameManager.Instance.AddScore(checkpointBonus);
        }
        
        currentCheckpointIndex = index + 1;
        OnCheckpointReached?.Invoke(index);
        
        Debug.Log($"Checkpoint {index + 1} reached!");
    }
    
    private void CheckLevelCompletion()
    {
        if (levelEnd == null) return;
        
        float playerX = player != null ? player.transform.position.x : 0;
        
        if (playerX >= levelEnd.position.x)
        {
            CompleteLevel();
        }
    }
    
    #endregion
    
    #region Respawn System
    
    private Vector3 GetRespawnPosition()
    {
        if (useCheckpoints && checkpoints != null && checkpoints.Length > 0)
        {
            int respawnIndex = Mathf.Min(currentCheckpointIndex, checkpoints.Length - 1);
            
            if (checkpoints[respawnIndex] != null)
            {
                return checkpoints[respawnIndex].position + Vector3.up * 2f;
            }
        }
        
        if (levelStart != null)
        {
            return levelStart.position + Vector3.up * 2f;
        }
        
        return Vector3.up * 2f;
    }
    
    public void RespawnPlayer()
    {
        if (player == null)
        {
            FindPlayer();
        }
        
        if (player != null)
        {
            Vector3 respawnPos = GetRespawnPosition();
            player.transform.position = respawnPos;
            player.SetInvincible(2f);
        }
    }
    
    #endregion
    
    #region Scoring
    
    private float CalculateCompletionBonus()
    {
        float baseBonus = 1000f;
        float progressBonus = progressPercent * 2000f;
        float difficultyBonus = levelDifficulty * 500f;
        float timeBonus = CalculateTimeBonus();
        
        return baseBonus + progressBonus + difficultyBonus + timeBonus;
    }
    
    private float CalculateTimeBonus()
    {
        if (GameManager.Instance == null) return 0f;
        
        float gameTime = GameManager.Instance.GameTime;
        float idealTime = levelLength / 20f;
        
        if (gameTime < idealTime)
        {
            return (idealTime - gameTime) * 50f;
        }
        
        return 0f;
    }
    
    #endregion
    
    #region Public Methods
    
    public void SetLevelData(int number, string name, float length, float difficulty)
    {
        levelNumber = number;
        levelName = name;
        levelLength = length;
        levelDifficulty = difficulty;
    }
    
    public void AddCheckpoint(Transform checkpoint)
    {
        if (checkpoints == null)
        {
            checkpoints = new Transform[1];
            checkpoints[0] = checkpoint;
        }
        else
        {
            System.Array.Resize(ref checkpoints, checkpoints.Length + 1);
            checkpoints[checkpoints.Length - 1] = checkpoint;
        }
    }
    
    private void FindPlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            player = GameManager.Instance.CurrentPlayer;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerController>();
            }
        }
    }
    
    public Transform GetCurrentCheckpoint()
    {
        if (useCheckpoints && checkpoints != null && currentCheckpointIndex < checkpoints.Length)
        {
            return checkpoints[currentCheckpointIndex];
        }
        return levelStart;
    }
    
    public float GetDistanceToNextCheckpoint()
    {
        if (!useCheckpoints || checkpoints == null || checkpoints.Length == 0)
        {
            return levelEnd != null ? levelEnd.position.x - (player != null ? player.transform.position.x : 0) : 0;
        }
        
        int nextIndex = Mathf.Min(currentCheckpointIndex, checkpoints.Length - 1);
        
        if (checkpoints[nextIndex] != null && player != null)
        {
            return checkpoints[nextIndex].position.x - player.transform.position.x;
        }
        
        return 0;
    }
    
    #endregion
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        
        if (levelStart != null)
        {
            Gizmos.DrawWireSphere(levelStart.position, 2f);
            Gizmos.DrawIcon(levelStart.position, "Assets/Sprites/Snow-tile-low-res.png", true);
        }
        
        Gizmos.color = Color.yellow;
        
        if (levelEnd != null)
        {
            Gizmos.DrawWireSphere(levelEnd.position, 2f);
            Gizmos.DrawWireCube(levelEnd.position, Vector3.one * 4f);
        }
        
        if (checkpoints != null)
        {
            Gizmos.color = new Color(0, 0.5f, 1f, 0.5f);
            
            foreach (var checkpoint in checkpoints)
            {
                if (checkpoint != null)
                {
                    Gizmos.DrawWireSphere(checkpoint.position, 1.5f);
                }
            }
        }
        
        Gizmos.color = Color.gray;
        if (levelStart != null && levelEnd != null)
        {
            Vector3 start = levelStart.position;
            Vector3 end = levelEnd.position;
            Gizmos.DrawLine(start, end);
        }
    }
}
