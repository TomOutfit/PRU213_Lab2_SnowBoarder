using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, GameOver, LevelComplete, EndScreen }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("Player Settings")]
    [SerializeField] private int startingLives = 3;
    public int CurrentLives { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private string firstLevelScene = "Level1";
    [SerializeField] private string level2Scene = "Level2";
    [SerializeField] private string endScreenScene = "EndScreen";
    [SerializeField] private float restartDelay = 2f;

    public float RestartDelay => restartDelay;

    private string currentLevelScene = "";
    private string lastPlayedLevel = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Tự động chuyển sang trạng thái Playing nếu đang test trực tiếp từ một Scene Level
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
    }

    public void StartGame()
    {
        CurrentLives = startingLives;
        CurrentState = GameState.Playing;
        currentLevelScene = firstLevelScene;
        lastPlayedLevel = firstLevelScene;
        SceneManager.LoadScene(firstLevelScene);
        Time.timeScale = 1f;
    }

    public void StartLevel2()
    {
        CurrentLives = startingLives;
        CurrentState = GameState.Playing;
        currentLevelScene = level2Scene;
        lastPlayedLevel = level2Scene;
        SceneManager.LoadScene(level2Scene);
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        CurrentState = GameState.Paused;
        Time.timeScale = 0f;
        UIManager.Instance?.ShowPauseMenu();
    }

    public void ResumeGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        UIManager.Instance?.HidePauseMenu();
    }

    public void PlayerDied()
    {
        CurrentLives--;
        if (CurrentLives <= 0)
        {
            CurrentState = GameState.GameOver;
            if (currentLevelScene == level2Scene)
            {
                LoadEndScreen();
            }
            else
            {
                UIManager.Instance?.ShowGameOver(ScoreManager.Instance?.TotalScore ?? 0);
            }
        }
        else
        {
            UIManager.Instance?.UpdateLives(CurrentLives);
        }
    }

    public void LevelComplete()
    {
        CurrentState = GameState.LevelComplete;
        Time.timeScale = 0f;
        GoToNextLevel();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(currentLevelScene))
            SceneManager.LoadScene(currentLevelScene);
        else if (!string.IsNullOrEmpty(lastPlayedLevel))
            SceneManager.LoadScene(lastPlayedLevel);
        else
            SceneManager.LoadScene(firstLevelScene);
        CurrentState = GameState.Playing;
    }

    public void GoToNextLevel()
    {
        CurrentState = GameState.LevelComplete;
        Time.timeScale = 1f;
        if (level2Scene == currentLevelScene)
        {
            SceneManager.LoadScene(endScreenScene);
            CurrentState = GameState.EndScreen;
        }
        else
        {
            StartLevel2();
        }
    }

    public void LoadEndScreen()
    {
        Time.timeScale = 1f;
        CurrentState = GameState.EndScreen;
        SceneManager.LoadScene(endScreenScene);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        CurrentState = GameState.MainMenu;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
