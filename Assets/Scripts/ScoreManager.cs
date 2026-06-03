using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int CurrentScore { get; private set; }
    public Action<int> OnScoreUpdated;

    Rigidbody2D playerRb;

    [Header("Level1 Settings")]
    [SerializeField] TextMeshProUGUI scoreTextHUD;

    [Header("MainMenu Settings")]
    public float pointsPerSecond = 10f;
    public float speedBonusThreshold = 15f;
    public float speedBonusMultiplier = 2f;
    public float maxComboTimer = 3f;
    public float comboResetTime = 3f;
    public int maxCombo = 10;
    public float comboMultiplierGrowth = 0.5f;
    public TextMeshProUGUI scoreTextMenu;
    public TextMeshProUGUI comboTextMenu;
    public TextMeshProUGUI multiplierTextMenu;

    int currentCombo = 1;
    float comboTimer = 0f;
    float speedBonusAccumulator = 0f;

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
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Playing)
        {
            if (playerRb == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerRb = player.GetComponent<Rigidbody2D>();
                }
            }

            // Giảm combo timer theo thời gian thực
            if (comboTimer > 0f)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    currentCombo = 1;
                    OnScoreUpdated?.Invoke(CurrentScore);
                }
            }

            float pointsThisFrame = pointsPerSecond * Time.deltaTime;
            if (playerRb != null && playerRb.linearVelocity.magnitude > speedBonusThreshold)
            {
                pointsThisFrame *= speedBonusMultiplier;
            }

            // Nhân thêm hệ số combo hiện tại vào điểm thời gian thực
            pointsThisFrame *= currentCombo;
            speedBonusAccumulator += pointsThisFrame;

            if (speedBonusAccumulator >= 1f)
            {
                int toAdd = Mathf.FloorToInt(speedBonusAccumulator);
                CurrentScore += toAdd;
                speedBonusAccumulator -= toAdd;
                OnScoreUpdated?.Invoke(CurrentScore);
            }
        }
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        OnScoreUpdated?.Invoke(CurrentScore);
    }

    public void IncrementComboMultiplier()
    {
        currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
        comboTimer = maxComboTimer;
        OnScoreUpdated?.Invoke(CurrentScore);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowFloatingText($"x{currentCombo} COMBO!", Vector3.zero);
        }
    }

    public void AddTrickScore(int basePoints, int multiplier)
    {
        int totalTrickScore = basePoints * multiplier;
        CurrentScore += totalTrickScore;
        currentCombo = Mathf.Min(currentCombo + 1, maxCombo);
        comboTimer = maxComboTimer;
        
        OnScoreUpdated?.Invoke(CurrentScore);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowFloatingText($"x{currentCombo} COMBO!", Vector3.zero);
        }
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        currentCombo = 1;
        comboTimer = 0f;
        speedBonusAccumulator = 0f;
        OnScoreUpdated?.Invoke(CurrentScore);
    }

    void OnEnable()
    {
        CurrentScore = 0;
    }
}
