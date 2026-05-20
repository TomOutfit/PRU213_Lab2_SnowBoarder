using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager handling game state, scoring, lives, and game flow.
/// Implements singleton pattern for global access.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [Tooltip("Starting number of lives")]
    [SerializeField] private int startingLives = 3;
    
    [Tooltip("Invincibility time after taking damage")]
    [SerializeField] private float invincibilityTime = 2f;
    
    [Tooltip("Delay before respawning after death")]
    [SerializeField] private float respawnDelay = 2f;
    
    [Header("Scoring")]
    [Tooltip("Points awarded per second while moving")]
    [SerializeField] private int pointsPerSecond = 10;
    
    [Tooltip("Points awarded per snowflake collected")]
    [SerializeField] private int pointsPerSnowflake = 100;
    
    [Tooltip("Points awarded per trick")]
    [SerializeField] private int baseTrickPoints = 200;
    
    [Tooltip("Maximum combo multiplier")]
    [SerializeField] private float maxComboMultiplier = 5f;
    
    [Tooltip("Combo decay time in seconds")]
    [SerializeField] private float comboDecayTime = 3f;
    
    [Header("Difficulty")]
    [Tooltip("Level difficulty for scaling")]
    [SerializeField] private float levelDifficulty = 0.5f;
    
    [Header("References")]
    [Tooltip("Player spawn point")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("Player prefab to respawn")]
    [SerializeField] private GameObject playerPrefab;
    
    [Tooltip("Current player instance")]
    [SerializeField] private PlayerController currentPlayer;
    
    // Game State
    private GameState currentState;
    private int currentScore;
    private int highScore;
    private int currentLives;
    private int snowflakesCollected;
    private int tricksPerformed;
    private float distanceTraveled;
    private float gameTime;
    
    // Combo System
    private float currentComboMultiplier = 1f;
    private float comboTimer;
    private int comboCount;
    
    // Events
    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;
    public event Action<int> OnLivesChanged;
    public event Action OnGameStarted;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action OnGameOver;
    public event Action OnPlayerDied;
    public event Action OnLevelComplete;
    public event Action<float> OnComboChanged;
    public event Action<int> OnSpeedMultiplierChanged;
    public event Action<CollectibleType, int> OnCollectibleCollected;
    
    // Properties
    public int Score => currentScore;
    public int HighScore => highScore;
    public int Lives => currentLives;
    public float ComboMultiplier => currentComboMultiplier;
    public int ComboCount => comboCount;
    public int SnowflakesCollected => snowflakesCollected;
    public int TricksPerformed => tricksPerformed;
    public float DistanceTraveled => distanceTraveled;
    public float GameTime => gameTime;
    public GameState CurrentState => currentState;
    public PlayerController CurrentPlayer => currentPlayer;
    public float LevelDifficulty => levelDifficulty;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadHighScore();
    }
    
    private void Start()
    {
        InitializeGame();
    }
    
    private void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateGameplay();
        }
        
        UpdateComboSystem();
    }
    
    private void InitializeGame()
    {
        currentState = GameState.MainMenu;
        currentScore = 0;
        currentLives = startingLives;
        currentComboMultiplier = 1f;
        comboTimer = 0f;
        comboCount = 0;
        snowflakesCollected = 0;
        tricksPerformed = 0;
        distanceTraveled = 0f;
        gameTime = 0f;
    }
    
    private void UpdateGameplay()
    {
        gameTime += Time.deltaTime;
        
        if (currentPlayer != null)
        {
            float speed = currentPlayer.CurrentSpeed;
            currentScore += Mathf.RoundToInt(pointsPerSecond * speed / 10f * Time.deltaTime);
            
            OnScoreChanged?.Invoke(currentScore);
        }
    }
    
    private void UpdateComboSystem()
    {
        if (comboTimer > 0f && currentState == GameState.Playing)
        {
            comboTimer -= Time.deltaTime;
            
            if (comboTimer <= 0f)
            {
                ResetCombo();
            }
        }
    }
    
    #region Game Flow
    
    public void StartGame()
    {
        if (currentState == GameState.MainMenu || currentState == GameState.GameOver)
        {
            currentScore = 0;
            currentLives = startingLives;
            snowflakesCollected = 0;
            tricksPerformed = 0;
            distanceTraveled = 0f;
            gameTime = 0f;
            ResetCombo();
            
            // Load Level1 scene
            SceneManager.LoadScene("Level1");
            return;
        }
    }
    
    public void StartLevel(string levelName)
    {
        currentState = GameState.Playing;
        currentScore = 0;
        currentLives = startingLives;
        snowflakesCollected = 0;
        tricksPerformed = 0;
        distanceTraveled = 0f;
        gameTime = 0f;
        ResetCombo();
        
        OnLivesChanged?.Invoke(currentLives);
        OnScoreChanged?.Invoke(currentScore);
        OnGameStarted?.Invoke();
    }
    
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
    }
    
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
    }
    
    public void GameOver()
    {
        currentState = GameState.GameOver;
        Time.timeScale = 0f;
        
        if (currentScore > highScore)
        {
            highScore = currentScore;
            SaveHighScore();
            OnHighScoreChanged?.Invoke(highScore);
        }
        
        OnGameOver?.Invoke();
    }
    
    public void ReturnToMainMenu()
    {
        currentState = GameState.MainMenu;
        Time.timeScale = 1f;
        
        if (currentPlayer != null)
        {
            currentPlayer.gameObject.SetActive(false);
        }
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void CompleteLevel()
    {
        currentState = GameState.GameOver;
        
        int completionBonus = 1000;
        currentScore += completionBonus;
        OnScoreChanged?.Invoke(currentScore);
        
        OnLevelComplete?.Invoke();
    }

    public void LoadNextLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    #endregion
    
    #region Player Management
    
    private void SpawnPlayer()
    {
        if (spawnPoint != null)
        {
            GameObject playerObj = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            currentPlayer = playerObj.GetComponent<PlayerController>();
            
            SetupPlayerEvents();
        }
    }
    
    private void SetupPlayerEvents()
    {
        if (currentPlayer != null)
        {
            currentPlayer.OnCrash += HandlePlayerCrash;
            currentPlayer.OnLand += HandlePlayerLand;
            currentPlayer.OnTrickPerformed += HandleTrickPerformed;
            currentPlayer.OnSpeedChanged += HandleSpeedChanged;
        }
    }
    
    private void HandlePlayerCrash()
    {
        if (currentPlayer != null && !currentPlayer.IsInvincible)
        {
            LoseLife();
        }
    }
    
    private void HandlePlayerLand()
    {
        // Add points for landing
        AddScore(pointsPerSecond);
    }
    
    private void HandleTrickPerformed(TrickType trick)
    {
        tricksPerformed++;
        
        int trickPoints = CalculateTrickPoints(trick);
        AddScore(trickPoints);
        
        IncreaseCombo();
    }
    
    private void HandleSpeedChanged(float speedMultiplier)
    {
        OnSpeedMultiplierChanged?.Invoke(Mathf.RoundToInt(speedMultiplier * 100));
    }
    
    public void PlayerDied()
    {
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);
        OnPlayerDied?.Invoke();
        
        if (currentLives <= 0)
        {
            GameOver();
        }
        else
        {
            StartCoroutine(RespawnPlayerCoroutine());
        }
    }
    
    private System.Collections.IEnumerator RespawnPlayerCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        
        if (currentState == GameState.Playing && currentLives > 0)
        {
            currentPlayer.gameObject.SetActive(true);
            currentPlayer.transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            currentPlayer.SetInvincible(invincibilityTime);
        }
    }
    
    private void LoseLife()
    {
        PlayerDied();
    }
    
    #endregion
    
    #region Scoring
    
    public void AddScore(int points)
    {
        int multipliedPoints = Mathf.RoundToInt(points * currentComboMultiplier);
        currentScore += multipliedPoints;
        OnScoreChanged?.Invoke(currentScore);
    }
    
    public void AddSnowflakeScore(int count = 1)
    {
        snowflakesCollected += count;
        int points = pointsPerSnowflake * count;
        AddScore(points);
        OnCollectibleCollected?.Invoke(CollectibleType.Snowflake, count);
    }
    
    private int CalculateTrickPoints(TrickType trick)
    {
        int basePoints = baseTrickPoints;
        
        switch (trick)
        {
            case TrickType.Backflip:
            case TrickType.Frontflip:
                basePoints *= 2;
                break;
            case TrickType.Grab:
                basePoints = Mathf.RoundToInt(basePoints * 1.5f);
                break;
            case TrickType.Spin180:
                basePoints *= 1;
                break;
        }
        
        if (currentPlayer != null)
        {
            float speedBonus = 1f + (currentPlayer.CurrentSpeed / 50f);
            basePoints = Mathf.RoundToInt(basePoints * speedBonus);
        }
        
        return basePoints;
    }
    
    public void IncreaseCombo()
    {
        comboCount++;
        comboTimer = comboDecayTime;
        currentComboMultiplier = Mathf.Min(1f + (comboCount * 0.5f), maxComboMultiplier);
        OnComboChanged?.Invoke(currentComboMultiplier);
    }
    
    private void ResetCombo()
    {
        comboCount = 0;
        currentComboMultiplier = 1f;
        OnComboChanged?.Invoke(currentComboMultiplier);
    }
    
    #endregion
    
    #region Persistence
    
    private void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("SnowBoarder_HighScore", 0);
    }
    
    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("SnowBoarder_HighScore", highScore);
        PlayerPrefs.Save();
    }
    
    public void ResetHighScore()
    {
        highScore = 0;
        PlayerPrefs.DeleteKey("SnowBoarder_HighScore");
        OnHighScoreChanged?.Invoke(highScore);
    }
    
    #endregion
    
    #region Collectibles & Powerups
    
    public void CollectItem(CollectibleType type, int value = 1)
    {
        switch (type)
        {
            case CollectibleType.Snowflake:
                AddSnowflakeScore(value);
                break;
            case CollectibleType.ExtraLife:
                currentLives = Mathf.Min(currentLives + value, 5);
                OnLivesChanged?.Invoke(currentLives);
                break;
            case CollectibleType.SpeedBoost:
                if (currentPlayer != null)
                {
                    currentPlayer.ApplySpeedBoost(2f, 5f);
                }
                break;
            case CollectibleType.Invincibility:
                if (currentPlayer != null)
                {
                    currentPlayer.SetInvincible(10f);
                }
                break;
            case CollectibleType.Shield:
                if (currentPlayer != null)
                {
                    currentPlayer.SetInvincible(5f);
                }
                break;
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        
        if (currentPlayer != null)
        {
            currentPlayer.OnCrash -= HandlePlayerCrash;
            currentPlayer.OnLand -= HandlePlayerLand;
            currentPlayer.OnTrickPerformed -= HandleTrickPerformed;
        }
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

public enum CollectibleType
{
    Snowflake,
    ExtraLife,
    SpeedBoost,
    Invincibility,
    Shield
}
