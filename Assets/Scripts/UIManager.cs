using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Canvases")]
    public Canvas mainMenuCanvas;
    public Canvas gameHUDCanvas;
    public Canvas pauseMenuCanvas;
    public Canvas gameOverCanvas;
    public Canvas levelCompleteCanvas;
    public Canvas optionsCanvas;
    public Canvas guideCanvas;

    [Header("Buttons")]
    public Button guideBackButton;
    public Button startButton;
    public Button optionsButton;
    public Button guideButton;
    public Button quitButton;
    public Button restartButton;
    public Button menuButton;
    public Button nextLevelButton;
    public Button replayButton;
    public Button completeMenuButton;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;
    public Button optionsBackButton;

    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverTimeText;
    public TextMeshProUGUI gameOverDistanceText;
    public TextMeshProUGUI gameOverSpeedText;
    public TextMeshProUGUI completeScoreText;
    public TextMeshProUGUI completeTimeText;
    public TextMeshProUGUI completeDistanceText;
    public TextMeshProUGUI completeSpeedText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI timeBonusText;
    public TextMeshProUGUI notificationText;
    public TextMeshProUGUI distanceText;

    [Header("HUD Images")]
    public Image livesIcon;
    public GameObject comboDisplay;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject pausePanel;
    public GameObject levelCompletePanel;

    [Header("Options")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown qualityDropdown;

    [Header("Floating Text Prefab")]
    public GameObject floatingTextPrefab;

    [Header("Notification")]
    public float notificationDuration = 2f;

    [Header("Fade")]
    public CanvasGroup fadeCanvasGroup;

    Rigidbody2D playerRb;
    float notificationTimer;
    float startXPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (timerText == null || speedText == null || distanceText == null || scoreText == null)
        {
            TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
            foreach (var t in texts)
            {
                if (t.name.Contains("Time") && t.name.Contains("Text")) timerText = t;
                else if (t.name.Contains("Speed") && t.name.Contains("Text")) speedText = t;
                else if (t.name.Contains("Distance") && t.name.Contains("Text")) distanceText = t;
                else if (t.name.Contains("Score") && t.name.Contains("Text") && !t.name.Contains("gameOver")) scoreText = t;
            }
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreUpdated += UpdateScoreText;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            startXPos = player.transform.position.x;
        }

        UpdateLivesText();
        UpdateScoreText(0);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }

        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        
        if (gameOverCanvas == null)
        {
            foreach (var c in allCanvases)
                if (c.name.ToLower().Contains("gameover") || c.name.ToLower().Contains("game over"))
                { gameOverCanvas = c; gameOverPanel = c.gameObject; break; }
        }

        if (levelCompleteCanvas == null)
        {
            foreach (var c in allCanvases)
                if (c.name.ToLower().Contains("complete"))
                { levelCompleteCanvas = c; levelCompletePanel = c.gameObject; break; }
        }

        if (gameOverCanvas != null) gameOverCanvas.gameObject.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (levelCompleteCanvas != null) levelCompleteCanvas.gameObject.SetActive(false);
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        // Cài đặt nút bấm cho GameOver Canvas
        if (gameOverCanvas != null)
        {
            Button[] btns = gameOverCanvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in btns)
            {
                if (btn.name.Contains("Restart"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.RestartGame(); });
                }
                else if (btn.name.Contains("Menu"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); });
                }
            }
        }

        // Cài đặt nút bấm cho Level Complete Canvas
        if (levelCompleteCanvas != null)
        {
            Button[] btns = levelCompleteCanvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in btns)
            {
                if (btn.name.Contains("Restart"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.RestartGame(); });
                }
                else if (btn.name.Contains("Menu"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); });
                }
                else if (btn.name.Contains("Next") || btn.name.Contains("Level"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { 
                        if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
                        else UnityEngine.SceneManagement.SceneManager.LoadScene("Level2"); 
                    });
                }
            }
        }

        // Cài đặt fallback nếu được gán ở Inspector
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.RestartGame(); });
        }
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(() => { UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); });
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.StartGame(); });
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.QuitGame(); });
        }

        if (guideButton != null)
        {
            guideButton.onClick.RemoveAllListeners();
            guideButton.onClick.AddListener(() => { 
                if (guideCanvas != null) guideCanvas.gameObject.SetActive(true);
                if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(false);
            });
        }

        if (guideBackButton != null)
        {
            guideBackButton.onClick.RemoveAllListeners();
            guideBackButton.onClick.AddListener(() => { 
                if (guideCanvas != null) guideCanvas.gameObject.SetActive(false);
                if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(true);
            });
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveAllListeners();
            optionsButton.onClick.AddListener(() => { 
                if (optionsCanvas != null) optionsCanvas.gameObject.SetActive(true);
                if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(false);
            });
        }

        if (optionsBackButton != null)
        {
            optionsBackButton.onClick.RemoveAllListeners();
            optionsBackButton.onClick.AddListener(() => { 
                if (optionsCanvas != null) optionsCanvas.gameObject.SetActive(false);
                if (mainMenuCanvas != null) mainMenuCanvas.gameObject.SetActive(true);
            });
        }
    }

    void Update()
    {
        bool isPlaying = (GameManager.Instance == null) || (GameManager.Instance.State == GameManager.GameState.Playing);

        if (isPlaying)
        {
            if (timerText != null)
            {
                float time = Time.timeSinceLevelLoad;
                int minutes = Mathf.FloorToInt(time / 60F);
                int seconds = Mathf.FloorToInt(time - minutes * 60);
                timerText.text = string.Format("Time: {0:00}:{1:00}", minutes, seconds);
            }

            if (playerRb != null)
            {
                if (speedText != null)
                {
                    float speedKmh = playerRb.linearVelocity.magnitude * 2.5f;
                    speedText.text = $"Speed: {Mathf.RoundToInt(speedKmh)} km/h";
                }

                if (distanceText != null)
                {
                    float distance = playerRb.position.x - startXPos;
                    if (distance < 0) distance = 0;
                    distanceText.text = $"Dist: {Mathf.RoundToInt(distance)}m";
                }
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerRb = player.GetComponent<Rigidbody2D>();
                    startXPos = player.transform.position.x;
                }
            }
        }

        if (notificationTimer > 0f)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f && notificationText != null)
            {
                notificationText.gameObject.SetActive(false);
            }
        }
    }

    public void UpdateScoreText(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore}";
        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {newScore}";
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score: {newScore}";
    }

    public void UpdateLivesText()
    {
        if (livesText != null && GameManager.Instance != null)
        {
            livesText.text = $"Lives: {GameManager.Instance.Lives}";
        }
    }

    public void ShowGameOverPanel()
    {
        // 1. Dọn dẹp sạch sẽ Scene: Ẩn mặt đất, vật cản, vật phẩm, nhân vật
        GameObject[] grounds = GameObject.FindGameObjectsWithTag("Ground");
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        foreach (var g in grounds) g.SetActive(false);
        foreach (var o in obstacles) o.SetActive(false);
        if (player != null) player.SetActive(false);

        // Ẩn các thư mục nếu có dùng SceneOptimizerTool
        GameObject env = GameObject.Find("--- ENVIRONMENT ---");
        GameObject obs = GameObject.Find("--- OBSTACLES ---");
        GameObject items = GameObject.Find("--- ITEMS ---");
        if (env != null) env.SetActive(false);
        if (obs != null) obs.SetActive(false);
        if (items != null) items.SetActive(false);

        // Ẩn HUD hiện tại
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach(var c in canvases) {
            if(c.name == "HUD Canvas") c.gameObject.SetActive(false);
        }

        // 2. Cập nhật Text Game Over (đã được tạo sẵn trong Editor)
        if (gameOverTimeText != null && timerText != null) gameOverTimeText.text = timerText.text;
        if (gameOverDistanceText != null && distanceText != null) gameOverDistanceText.text = distanceText.text;
        if (gameOverSpeedText != null && speedText != null) gameOverSpeedText.text = speedText.text;
        if (gameOverScoreText != null && scoreText != null) gameOverScoreText.text = scoreText.text;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverCanvas != null) gameOverCanvas.gameObject.SetActive(true);
    }

    public void ShowLevelCompletePanel()
    {
        // Ẩn HUD hiện tại
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach(var c in canvases) {
            if(c.name == "HUD Canvas") c.gameObject.SetActive(false);
        }

        // Tự động tìm liên kết Text nếu chưa có trong Inspector
        if (levelCompleteCanvas != null && (completeScoreText == null || completeTimeText == null))
        {
            TextMeshProUGUI[] texts = levelCompleteCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts)
            {
                if (t.name.Contains("Score")) completeScoreText = t;
                else if (t.name.Contains("Time")) completeTimeText = t;
                else if (t.name.Contains("Distance") || t.name.Contains("Dist")) completeDistanceText = t;
                else if (t.name.Contains("Speed")) completeSpeedText = t;
            }
        }

        // Cập nhật Text
        if (completeTimeText != null && timerText != null) completeTimeText.text = timerText.text;
        if (completeDistanceText != null && distanceText != null) completeDistanceText.text = distanceText.text;
        if (completeSpeedText != null && speedText != null) completeSpeedText.text = speedText.text;
        if (completeScoreText != null && scoreText != null) completeScoreText.text = scoreText.text;

        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);
        if (levelCompleteCanvas != null) 
        {
            levelCompleteCanvas.gameObject.SetActive(true);
            
            Button[] btns = levelCompleteCanvas.GetComponentsInChildren<Button>(true);
            foreach (var btn in btns)
            {
                if (btn.name.Contains("Next") || btn.name.Contains("Level"))
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (btnText != null)
                    {
                        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level3")
                            btnText.text = "Finish";
                        else
                            btnText.text = "Next Level";
                    }
                }
            }
        }
    }

    public void ShowPauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(true);
        if (pauseMenuCanvas != null) pauseMenuCanvas.gameObject.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (pauseMenuCanvas != null) pauseMenuCanvas.gameObject.SetActive(false);
    }

    public void ShowFloatingText(string message, Vector3 position)
    {
        if (floatingTextPrefab != null)
        {
            GameObject floatText = Instantiate(floatingTextPrefab, position + Vector3.up * 2, Quaternion.identity);
            TextMeshPro textMesh = floatText.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = message;
            }
            Destroy(floatText, 1.5f);
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            notificationTimer = notificationDuration;
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreUpdated -= UpdateScoreText;
        }
    }
}
