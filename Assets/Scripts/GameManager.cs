using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete }
    public GameState State { get; private set; }

    [Header("Game Settings")]
    public int startingLives = 1;
    public string level1Scene = "Level1";
    public string level2Scene = "Level2";
    public string level3Scene = "Level3";
    public string endScreenScene = "Winner";
    public float restartDelay = 2f;

    public int Lives { get; private set; }

    CanvasGroup fadeGroup;

    void Awake()
    {
        endScreenScene = "Winner";

        // Thiết lập đồng bộ khung hình VSync và giới hạn FPS để tốc độ game luôn mượt mà và ổn định
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;

        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();

            if (AudioManager.Instance == null)
            {
                GameObject audioManagerGO = new GameObject("AudioManager");
                audioManagerGO.AddComponent<AudioManager>();
            }
        }
        else
        {
            // Relink Menu buttons to the persistent Instance before destroying the duplicate
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Menu")
            {
                Instance.BindMenuButtons();
            }
            Destroy(gameObject);
            return;
        }
    }

    public void BindMenuButtons()
    {
        UnityEngine.UI.Button[] btns = Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include);
        foreach (var btn in btns)
        {
            if (btn.name.ToLower().Contains("play") || btn.name.ToLower().Contains("start"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => StartGame());
            }
            else if (btn.name.ToLower().Contains("quit") || btn.name.ToLower().Contains("exit"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => QuitGame());
            }
            else if (btn.name.ToLower().Contains("guide"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowGuide(true));
            }
            else if (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("close"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ShowGuide(false));
            }
        }
    }

    public void ShowGuide(bool show)
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        Canvas mainCanvas = null;

        foreach (var c in canvases)
        {
            if (c.name.ToLower().Contains("menu") || c.name.ToLower() == "canvas")
            {
                mainCanvas = c;
                break;
            }
        }

        if (mainCanvas != null)
        {
            for (int i = 0; i < mainCanvas.transform.childCount; i++)
            {
                Transform child = mainCanvas.transform.GetChild(i);
                string childName = child.name.ToLower();

                if (childName == "guidecanva" || (childName.Contains("guide") && !childName.Contains("button")))
                {
                    child.gameObject.SetActive(show);
                }
                else if (childName.Contains("button") || childName.Contains("title"))
                {
                    child.gameObject.SetActive(!show);
                }
            }
        }
    }

    void Start()
    {
        Lives = startingLives;
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            State = GameState.Menu;
            BindMenuButtons();
            ShowGuide(false);
        }
        else
        {
            State = GameState.Playing;
        }
    }

    void CreateFadeCanvas()
    {
        GameObject fadeObj = new GameObject("FadeCanvas");
        fadeObj.transform.SetParent(this.transform);
        
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        fadeObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        fadeObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        
        UnityEngine.UI.Image img = imgObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        fadeGroup = fadeObj.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeAndLoad(sceneName, -1));
    }

    public void LoadSceneWithFade(int buildIndex)
    {
        StartCoroutine(FadeAndLoad(string.Empty, buildIndex));
    }

    private System.Collections.IEnumerator FadeAndLoad(string sceneName, int buildIndex)
    {
        Time.timeScale = 1f; // Ensure time scale is reset
        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            yield return null; // Chờ 1 frame để scene mới khởi tạo xong
        }
        else
        {
            SceneManager.LoadScene(buildIndex);
            yield return null;
        }

        if (fadeGroup != null)
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false; // Bỏ chặn click để người chơi tương tác
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    Rigidbody2D playerRb;
    float startXPos;
    PlayerController playerController;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Menu")
        {
            State = GameState.Menu;
            BindMenuButtons();
            ShowGuide(false);
        }
        else if (scene.name == level1Scene || scene.name == level2Scene || scene.name == "Level3" || scene.name == "Level1")
        {
            State = GameState.Playing;
            SpawnItemsProcedurally();
            SpawnWeatherManager();
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerRb = player.GetComponent<Rigidbody2D>();
                playerController = player.GetComponent<PlayerController>();
                startXPos = player.transform.position.x;
                
                // Determine target distance
                float targetDistance = 0f;
                if (scene.name == "Level1" || scene.name == level1Scene) targetDistance = 1000f;
                else if (scene.name == "Level2" || scene.name == level2Scene) targetDistance = 2000f;
                else if (scene.name == "Level3") targetDistance = 4000f;

                // Move Finish Line to the exact target distance
                if (targetDistance > 0)
                {
                    FinishLine finishLine = Object.FindAnyObjectByType<FinishLine>();
                    if (finishLine != null)
                    {
                        float targetX = startXPos + targetDistance;
                        // Raycast down from high up to find the ground
                        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(targetX, 500f), Vector2.down, 1000f);
                        foreach (var hit in hits)
                        {
                            if (hit.collider != null && hit.collider.CompareTag("Ground"))
                            {
                                finishLine.transform.position = new Vector3(targetX, hit.point.y + 4f, 0f);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    void SpawnItemsProcedurally()
    {
        // 1. Tải Prefab từ Resources/Items
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Items");
        if (prefabs == null || prefabs.Length == 0) return;

        // Xóa các item cũ nếu có
        GameObject itemsParent = GameObject.Find("--- ITEMS ---");
        if (itemsParent == null)
        {
            itemsParent = new GameObject("--- ITEMS ---");
        }
        else
        {
            foreach (Transform child in itemsParent.transform)
            {
                Destroy(child.gameObject);
            }
        }

        EdgeCollider2D groundEdge = Object.FindAnyObjectByType<EdgeCollider2D>();
        if (groundEdge != null)
        {
            Vector2[] points = groundEdge.points;
            Transform groundTransform = groundEdge.transform;

            int spawnInterval = 15;
            for (int i = 5; i < points.Length - 5; i += spawnInterval)
            {
                i += Random.Range(-3, 4);
                if (i < 0 || i >= points.Length) continue;

                Vector2 worldPos = groundTransform.TransformPoint(points[i]);
                worldPos.y += Random.Range(1.5f, 4.0f);

                GameObject randomPrefab = prefabs[Random.Range(0, prefabs.Length)];
                GameObject spawnedItem = Instantiate(randomPrefab, worldPos, Quaternion.identity);
                spawnedItem.transform.SetParent(itemsParent.transform);
                spawnedItem.AddComponent<DestroyBehindPlayer>();
            }
        }
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            if (playerRb == null || playerController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerRb = player.GetComponent<Rigidbody2D>();
                    playerController = player.GetComponent<PlayerController>();
                    startXPos = player.transform.position.x;
                }
            }

            if (playerRb != null)
            {
                float distance = playerRb.position.x - startXPos;
                string sceneName = SceneManager.GetActiveScene().name;
                float targetDistance = 0f;

                if (sceneName == "Level1" || sceneName == level1Scene) targetDistance = 1000f;
                else if (sceneName == "Level2" || sceneName == level2Scene) targetDistance = 2000f;
                else if (sceneName == "Level3") targetDistance = 4000f;

                if (targetDistance > 0 && distance >= targetDistance)
                {
                    if (playerController != null) playerController.SetFinished();
                    
                    // Add score bonus when finishing via distance
                    if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(1000);

                    LevelComplete(true); // Phát nhạc hoàn thành vì cán đích bằng khoảng cách
                }
            }
        }

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }
    }

    public void PlayerCrashed()
    {
        if (State == GameState.LevelComplete || State == GameState.GameOver) return;
        Lives = 0;
        State = GameState.GameOver;
        if (UIManager.Instance != null) UIManager.Instance.ShowGameOverPanel();
    }

    void ReloadCurrentScene()
    {
        LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    public void LevelComplete(bool playSFX = true)
    {
        if (State == GameState.GameOver || State == GameState.LevelComplete) return;
        State = GameState.LevelComplete;
        if (playSFX && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFinishSound();
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowLevelCompletePanel();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Time.timeScale = 0f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu();
        }
        else if (State == GameState.Paused)
        {
            State = GameState.Playing;
            Time.timeScale = 1f;
            if (UIManager.Instance != null) UIManager.Instance.HidePauseMenu();
        }
    }

    public void StartGame()
    {
        Lives = startingLives;
        Time.timeScale = 1f;
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        if (TrickDetector.Instance != null) TrickDetector.Instance.ResetAll();
        if (!string.IsNullOrEmpty(level1Scene))
        {
            LoadSceneWithFade(level1Scene);
        }
        else
        {
            LoadSceneWithFade(1);
        }
    }

    public void RestartGame()
    {
        Lives = startingLives;
        Time.timeScale = 1f;

        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        if (TrickDetector.Instance != null) TrickDetector.Instance.ResetAll();

        LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Level1" || currentScene == level1Scene)
        {
            LoadSceneWithFade(!string.IsNullOrEmpty(level2Scene) ? level2Scene : "Level2");
        }
        else if (currentScene == "Level2" || currentScene == level2Scene)
        {
            LoadSceneWithFade("Level3");
        }
        else if (currentScene == "Level3")
        {
            LoadEndScreen();
        }
        else
        {
            LoadSceneWithFade(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void LoadEndScreen()
    {
        Time.timeScale = 1f;
        if (endScreenScene == "EndScreen")
        {
            endScreenScene = "Winner";
        }
        
        if (!string.IsNullOrEmpty(endScreenScene))
        {
            LoadSceneWithFade(endScreenScene);
        }
        else
        {
            LoadSceneWithFade("Menu");
        }
    }

    void SpawnWeatherManager()
    {
        GameObject weatherObj = GameObject.Find("WeatherManager");
        if (weatherObj == null)
        {
            weatherObj = new GameObject("WeatherManager");
            weatherObj.AddComponent<WeatherManager>();
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
        Debug.Log("Quit Game");
    }
}
