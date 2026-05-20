using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple UIManager for Snow Boarder game.
/// Handles all UI transitions and button clicks.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // UI References
    private Text scoreText;
    private Text livesText;
    private Text highScoreText;
    private Text comboText;

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
        SetupAllButtons();
        SetupUITexts();
        ShowMainMenu();
    }

    private void SetupUITexts()
    {
        // Find UI texts by name
        Text[] allTexts = FindObjectsOfType<Text>();
        foreach (Text t in allTexts)
        {
            if (t.gameObject.name == "ScoreText") scoreText = t;
            else if (t.gameObject.name == "LivesText") livesText = t;
            else if (t.gameObject.name == "HighScoreText") highScoreText = t;
            else if (t.gameObject.name == "ComboText") comboText = t;
        }
    }

    private void SetupAllButtons()
    {
        Button[] allButtons = FindObjectsOfType<Button>();
        foreach (Button btn in allButtons)
        {
            string btnName = btn.gameObject.name;
            
            // Remove existing listeners
            btn.onClick.RemoveAllListeners();
            
            // Main Menu buttons
            if (btnName == "StartButton")
                btn.onClick.AddListener(OnStartClicked);
            else if (btnName == "HowToButton")
                btn.onClick.AddListener(OnShowHowToPlay);
            else if (btnName == "CloseButton")
                btn.onClick.AddListener(OnCloseHowToPlay);
            else if (btnName == "QuitButton")
                btn.onClick.AddListener(OnQuitClicked);
            
            // Pause Menu buttons
            else if (btnName == "PauseButton")
                btn.onClick.AddListener(OnPauseClicked);
            else if (btnName == "ResumeButton")
                btn.onClick.AddListener(OnResumeClicked);
            else if (btnName == "RestartButton")
                btn.onClick.AddListener(OnRestartClicked);
            
            // Game Over buttons
            else if (btnName == "PlayAgainButton")
                btn.onClick.AddListener(OnRestartClicked);
            
            // Level Complete buttons
            else if (btnName == "NextLevelButton")
                btn.onClick.AddListener(OnNextLevelClicked);
            else if (btnName == "RetryButton")
                btn.onClick.AddListener(OnRetryClicked);
            
            // Menu buttons (appear in multiple screens)
            else if (btnName == "MainMenuButton")
                btn.onClick.AddListener(OnMainMenuClicked);
            else if (btnName == "ReturnMenuButton")
                btn.onClick.AddListener(OnMainMenuClicked);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // MAIN MENU
    // ═══════════════════════════════════════════════════════════
    
    public void OnStartClicked()
    {
        Debug.Log("[UIManager] Start Game clicked");
        SceneManager.LoadScene("Level1");
    }

    public void OnShowHowToPlay()
    {
        Debug.Log("[UIManager] Show How To Play");
        HideAllCanvases();
        ShowCanvasByName("HowToPlayPanel");
    }

    public void OnCloseHowToPlay()
    {
        Debug.Log("[UIManager] Close How To Play");
        HideAllCanvases();
        ShowCanvasByName("MainMenuCanvas");
    }

    public void OnQuitClicked()
    {
        Debug.Log("[UIManager] Quit clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ═══════════════════════════════════════════════════════════
    // IN-GAME UI
    // ═══════════════════════════════════════════════════════════

    public void OnPauseClicked()
    {
        Debug.Log("[UIManager] Pause clicked");
        Time.timeScale = 0f;
        HideCanvasByName("HUDCanvas");
        ShowCanvasByName("PauseMenuCanvas");
    }

    public void OnResumeClicked()
    {
        Debug.Log("[UIManager] Resume clicked");
        Time.timeScale = 1f;
        HideCanvasByName("PauseMenuCanvas");
        ShowCanvasByName("HUDCanvas");
    }

    public void OnRestartClicked()
    {
        Debug.Log("[UIManager] Restart clicked");
        Time.timeScale = 1f;
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("[UIManager] Main Menu clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScreen");
    }

    // ═══════════════════════════════════════════════════════════
    // LEVEL TRANSITIONS
    // ═══════════════════════════════════════════════════════════

    public void OnNextLevelClicked()
    {
        Debug.Log("[UIManager] Next Level clicked");
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (currentScene == "Level1")
        {
            SceneManager.LoadScene("Level2");
        }
        else
        {
            // Completed Level 2 - Victory!
            SceneManager.LoadScene("MainMenuScreen");
        }
    }

    public void OnRetryClicked()
    {
        Debug.Log("[UIManager] Retry clicked");
        Time.timeScale = 1f;
        string sceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneName);
    }

    public void OnShowLevelComplete()
    {
        Debug.Log("[UIManager] Show Level Complete");
        HideAllCanvases();
        ShowCanvasByName("LevelCompleteCanvas");
    }

    // ═══════════════════════════════════════════════════════════
    // UI UPDATE METHODS (for other scripts to call)
    // ═══════════════════════════════════════════════════════════

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}";
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }

    public void UpdateHighScore(int highScore)
    {
        if (highScoreText != null)
            highScoreText.text = $"HI: {highScore}";
    }

    public void UpdateCombo(float comboMultiplier)
    {
        if (comboText != null)
        {
            if (comboMultiplier > 1)
            {
                comboText.gameObject.SetActive(true);
                comboText.text = $"x{comboMultiplier:F1}";
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }

    public void ShowComboText(string text)
    {
        if (comboText != null)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = text;
        }
    }

    public void HideComboText()
    {
        if (comboText != null)
        {
            comboText.gameObject.SetActive(false);
        }
    }

    public void PlayCrashSound()
    {
        // Audio handled by AudioManager
    }

    public void ShowScorePopup(int score, Vector3 worldPosition)
    {
        Debug.Log($"[UIManager] Score Popup: +{score}");
    }

    public void ShowScorePopup(string text)
    {
        Debug.Log($"[UIManager] Score Popup: {text}");
        if (comboText != null)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = text;
        }
    }

    public void ShowTrickPopup(string trickName, int bonus)
    {
        Debug.Log($"[UIManager] Trick: {trickName} +{bonus}");
        if (comboText != null)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{trickName} +{bonus}";
        }
    }

    public void ShowShieldIndicator(bool active)
    {
        Debug.Log($"[UIManager] Shield: {(active ? "ON" : "OFF")}");
    }

    public void ShowPowerUpText(string text)
    {
        Debug.Log($"[UIManager] PowerUp: {text}");
    }

    // ═══════════════════════════════════════════════════════════
    // HELPER METHODS
    // ═══════════════════════════════════════════════════════════

    private void ShowMainMenu()
    {
        HideAllCanvases();
        ShowCanvasByName("MainMenuCanvas");
    }

    private void HideAllCanvases()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            c.gameObject.SetActive(false);
        }
    }

    private void ShowCanvasByName(string name)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == name)
            {
                c.gameObject.SetActive(true);
                return;
            }
        }
        
        // Also check parent names
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name.Contains(name) || name.Contains(c.gameObject.name))
            {
                c.gameObject.SetActive(true);
                return;
            }
        }
    }

    private void HideCanvasByName(string name)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.gameObject.name == name)
            {
                c.gameObject.SetActive(false);
                return;
            }
        }
    }
}
