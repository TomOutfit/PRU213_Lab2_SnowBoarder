using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Menu, Playing, Paused, GameOver, LevelComplete }
    public GameState State { get; private set; }

    [Header("Game Settings")]
    public int startingLives = 3;
    public string firstLevelScene = "Level1";
    public string level2Scene = "Level2";
    public string endScreenScene = "Winner";
    public float restartDelay = 2f;

    public int Lives { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Lives = startingLives;
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            State = GameState.Menu;
        }
        else
        {
            State = GameState.Playing;
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
        if (scene.name == firstLevelScene || scene.name == level2Scene || scene.name == "Level3" || scene.name == "Level1")
        {
            SpawnItemsProcedurally();
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerRb = player.GetComponent<Rigidbody2D>();
                playerController = player.GetComponent<PlayerController>();
                startXPos = player.transform.position.x;
                
                // Determine target distance
                float targetDistance = 0f;
                if (scene.name == "Level1" || scene.name == firstLevelScene) targetDistance = 1000f;
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
        if (State == GameState.Playing && playerRb != null)
        {
            float distance = playerRb.position.x - startXPos;
            string sceneName = SceneManager.GetActiveScene().name;
            float targetDistance = 0f;

            if (sceneName == "Level1" || sceneName == firstLevelScene) targetDistance = 1000f;
            else if (sceneName == "Level2" || sceneName == level2Scene) targetDistance = 2000f;
            else if (sceneName == "Level3") targetDistance = 4000f;

            if (targetDistance > 0 && distance >= targetDistance)
            {
                if (playerController != null) playerController.SetFinished();
                
                // Add score bonus when finishing via distance
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(1000);
                if (AudioManager.Instance != null && AudioManager.Instance.sfxSource != null)
                {
                    // Optionally play a finish sound if we have one, otherwise just complete
                }

                LevelComplete();
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
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LevelComplete()
    {
        if (State == GameState.GameOver || State == GameState.LevelComplete) return;
        State = GameState.LevelComplete;
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
        State = GameState.Playing;
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(firstLevelScene))
        {
            SceneManager.LoadScene(firstLevelScene);
        }
        else
        {
            SceneManager.LoadScene(1);
        }
    }

    public void RestartGame()
    {
        Lives = startingLives;
        State = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Level1" || currentScene == firstLevelScene)
        {
            SceneManager.LoadScene(!string.IsNullOrEmpty(level2Scene) ? level2Scene : "Level2");
        }
        else if (currentScene == "Level2" || currentScene == level2Scene)
        {
            SceneManager.LoadScene("Level3");
        }
        else if (currentScene == "Level3")
        {
            LoadEndScreen();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
    }

    public void LoadEndScreen()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(endScreenScene))
        {
            SceneManager.LoadScene(endScreenScene);
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}
