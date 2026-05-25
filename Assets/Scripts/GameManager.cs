using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Playing, Paused, GameOver, LevelComplete }
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    [Header("Player Settings")]
    [SerializeField] private int startingLives = 3;
    public int CurrentLives { get; private set; }

    [Header("Level Settings")]
    [SerializeField] private string gameplaySceneName = "Level1";
    [SerializeField] private float restartDelay = 2f;

    public float RestartDelay => restartDelay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == GameState.Playing)
            PauseGame();
        else if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == GameState.Paused)
            ResumeGame();
    }

    public void StartGame()
    {
        CurrentLives = startingLives;
        CurrentState = GameState.Playing;
        SceneManager.LoadScene(gameplaySceneName);
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
            UIManager.Instance?.ShowGameOver(ScoreManager.Instance?.TotalScore ?? 0);
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
        UIManager.Instance?.ShowLevelComplete(ScoreManager.Instance?.TotalScore ?? 0);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
        CurrentState = GameState.Playing;
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
