using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvas References")]
    [SerializeField] private Canvas mainMenuCanvas;
    [SerializeField] private Canvas gameHUDCanvas;
    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Canvas gameOverCanvas;
    [SerializeField] private Canvas levelCompleteCanvas;
    [SerializeField] private Canvas optionsCanvas;
    [SerializeField] private Canvas guideCanvas;
    [SerializeField] private Button guideBackButton;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button guideButton;
    [SerializeField] private Button quitButton;

    [Header("HUD References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI speedText;
    [SerializeField] private TextMeshProUGUI timerText; // Bổ sung Text để hiển thị RunTime
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Image livesIcon;
    [SerializeField] private GameObject comboDisplay;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    [Header("Level Complete")]
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI timeBonusText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button completeMenuButton;

    [Header("Pause Menu")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseMenuButton;

    [Header("Options")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Button optionsBackButton;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 2f;

    [Header("Transitions")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    private List<GameObject> activeNotifications = new();
    private float notificationTimer;
    private bool isFading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (sceneName.StartsWith("Level"))
        {
            HideAllCanvases();
            AssignHUDReferences();

            if (gameHUDCanvas != null)
                gameHUDCanvas.gameObject.SetActive(true);
        }
    }

    private void AssignHUDReferences()
    {
        var texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var t in texts)
        {
            string n = t.name.ToLower().Replace(" ", "");
            
            // Bỏ qua các text ở menu hoặc game over
            if (n.Contains("gameover") || n.Contains("final") || n.Contains("bonus")) 
                continue;

            if (n.Contains("score")) scoreText = t;
            else if (n.Contains("speed")) speedText = t;
            else if (n.Contains("timer") || n.Contains("time")) timerText = t;
            else if (n.Contains("lives")) livesText = t;
            else if (n.Contains("combo") && !n.Contains("display")) comboText = t;
            else if (n.Contains("multiplier")) multiplierText = t;
        }

        if (scoreText != null)
        {
            gameHUDCanvas = scoreText.GetComponentInParent<Canvas>();
        }
    }

    private void Start()
    {
        SetupButtonListeners();
        LoadPlayerPrefs();
        
        if (SceneManager.GetActiveScene().name.StartsWith("Level"))
        {
            AssignHUDReferences();
            HideAllCanvases();
            if (gameHUDCanvas != null)
                gameHUDCanvas.gameObject.SetActive(true);
        }
        else
        {
            HideAllCanvases();
            if (mainMenuCanvas != null)
                mainMenuCanvas.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            UpdateHUD();
        }
        UpdateNotifications();
    }

    private void SetupButtonListeners()
    {
        if (startButton != null)
            startButton.onClick.AddListener(() => StartGame());

        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => ShowOptions());

        if (guideButton != null)
            guideButton.onClick.AddListener(() => ShowGuide());

        if (quitButton != null)
            quitButton.onClick.AddListener(() => GameManager.Instance.QuitGame());

        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => GameManager.Instance.ResumeGame());

        if (pauseRestartButton != null)
            pauseRestartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());

        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());

        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());

        if (menuButton != null)
            menuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(() => GameManager.Instance.GoToNextLevel());

        if (replayButton != null)
            replayButton.onClick.AddListener(() => GameManager.Instance.RestartLevel());

        if (completeMenuButton != null)
            completeMenuButton.onClick.AddListener(() => GameManager.Instance.ReturnToMenu());

        if (optionsBackButton != null)
            optionsBackButton.onClick.AddListener(() => HideOptions());

        if (guideBackButton != null)
            guideBackButton.onClick.AddListener(() => HideGuide());

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
    }

    private void LoadPlayerPrefs()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f);
        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
    }

    private void HideAllCanvases()
    {
        void SetActive(Canvas c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        SetActive(mainMenuCanvas, false);
        SetActive(gameHUDCanvas, false);
        SetActive(pauseMenuCanvas, false);
        SetActive(gameOverCanvas, false);
        SetActive(levelCompleteCanvas, false);
        SetActive(optionsCanvas, false);
        SetActive(guideCanvas, false);
    }

    public void ShowGuide()
    {
        if (guideCanvas != null)
            guideCanvas.gameObject.SetActive(true);
    }

    public void HideGuide()
    {
        if (guideCanvas != null)
            guideCanvas.gameObject.SetActive(false);
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame();
        HideAllCanvases();
        if (gameHUDCanvas != null)
            gameHUDCanvas.gameObject.SetActive(true);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.Reset();
    }

    public void ShowPauseMenu()
    {
        HideAllCanvases();
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            if (gameHUDCanvas != null)
                gameHUDCanvas.gameObject.SetActive(true);
    }

    public void ShowGameOver(int finalScore)
    {
        HideAllCanvases();
        if (gameOverCanvas != null)
        {
            gameOverCanvas.gameObject.SetActive(true);
            if (gameOverScoreText != null)
                gameOverScoreText.text = $"Final Score: {finalScore:N0}";
        }
    }

    public void ShowLevelComplete(int finalScore)
    {
        HideAllCanvases();
        if (levelCompleteCanvas != null)
        {
            levelCompleteCanvas.gameObject.SetActive(true);
            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore:N0}";
        }
    }

    public void ShowOptions()
    {
        HideAllCanvases();
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(true);
    }

    public void HideOptions()
    {
        if (optionsCanvas != null)
            optionsCanvas.gameObject.SetActive(false);
        if (mainMenuCanvas != null)
            mainMenuCanvas.gameObject.SetActive(true);
    }

    private void UpdateHUD()
    {
        PlayerController player = PlayerController.Instance;
        ScoreManager score = ScoreManager.Instance;

        if (scoreText != null && score != null)
            scoreText.text = $"Score: {score.TotalScore:N0}";

        if (speedText != null && player != null)
            speedText.text = $"Speed: {player.CurrentSpeed:F1}";

        if (timerText != null && player != null)
        {
            float t = player.RunTime;
            int minutes = Mathf.FloorToInt(t / 60F);
            int seconds = Mathf.FloorToInt(t - minutes * 60);
            timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
        }

        if (livesText != null && GameManager.Instance != null)
            livesText.text = $"Lives: {GameManager.Instance.CurrentLives}";

        if (comboText != null && score != null)
        {
            comboText.text = score.CurrentCombo > 0 ? $"Combo: {score.CurrentCombo}" : "";
            if (comboDisplay != null)
                comboDisplay.SetActive(score.CurrentCombo > 0);
        }

        if (multiplierText != null && score != null)
            multiplierText.text = score.CurrentCombo > 0 ? $"x{score.ComboMultiplier:F1}" : "";
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null)
            livesText.text = $"Lives: {lives}";
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationTimer = notificationDuration;
        }
    }

    private void UpdateNotifications()
    {
        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationText != null)
            {
                Color c = notificationText.color;
                c.a = Mathf.Clamp01(notificationTimer / notificationDuration);
                notificationText.color = c;
            }
        }
    }

    public void ShowCombo(int combo, float multiplier)
    {
        if (comboText != null)
            comboText.text = $"COMBO x{combo}!";
        if (multiplierText != null)
            multiplierText.text = $"x{multiplier:F1}";
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void OnFullscreenToggled(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    private void OnQualityChanged(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
    }

    public void ShowHUD()
    {
        HideAllCanvases();
        if (gameHUDCanvas != null)
            gameHUDCanvas.gameObject.SetActive(true);
    }
}
