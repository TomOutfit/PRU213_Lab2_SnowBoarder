using UnityEngine;
using System;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager _instance;
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ScoreManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ScoreManager");
                    _instance = go.AddComponent<ScoreManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Scoring Settings")]
    [SerializeField] private float pointsPerSecond = 10f;
    [SerializeField] private float speedBonusThreshold = 15f;
    [SerializeField] private float speedBonusMultiplier = 2f;

    [Header("Combo Settings")]
    [SerializeField] private float maxComboTimer = 3f;
    [SerializeField] private int maxCombo = 10;
    [SerializeField] private float comboMultiplierGrowth = 0.5f;

    public int TotalScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public float ComboMultiplier { get; private set; } = 1f;
    public float EffectiveMultiplier => ComboMultiplier * (multiplierBoostTimer > 0 ? 2f : 1f);
    public float ComboTimer => comboTimer;

    private float comboTimer;
    private float multiplierBoostTimer;
    private float sessionTime;
    private float accumulatedScore; // Thêm biến để tích lũy điểm lẻ
    private int itemsCollected;
    private int obstaclesDestroyed;
    private int tricksPerformed;
    private List<TrickRecord> trickHistory = new();

    private struct TrickRecord
    {
        public string Name;
        public float Score;
        public float Timestamp;
    }

    public event Action<int> OnScoreChanged;
    public event Action<int, float> OnComboChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        sessionTime += Time.deltaTime;
        
        if (multiplierBoostTimer > 0)
            multiplierBoostTimer -= Time.deltaTime;

        AddSpeedScore();
        UpdateComboTimer();
    }

    private void AddSpeedScore()
    {
        float speed = PlayerController.Instance?.CurrentSpeed ?? 0f;
        float multiplier = speed >= speedBonusThreshold ? speedBonusMultiplier : 1f;
        float earned = pointsPerSecond * multiplier * Time.deltaTime;
        
        accumulatedScore += earned;
        if (accumulatedScore >= 1f)
        {
            int pointsToAdd = Mathf.FloorToInt(accumulatedScore);
            TotalScore += pointsToAdd;
            accumulatedScore -= pointsToAdd;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    private void UpdateComboTimer()
    {
        if (CurrentCombo <= 0) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
            ResetCombo();
    }

    public void AddTrickScore(string trickName, int combo, float baseScore)
    {
        CurrentCombo = Mathf.Min(combo, maxCombo);
        ComboMultiplier = 1f + (CurrentCombo - 1) * comboMultiplierGrowth;
        float finalScore = Mathf.RoundToInt(baseScore * EffectiveMultiplier);
        TotalScore += Mathf.RoundToInt(finalScore);
        comboTimer = maxComboTimer;
        tricksPerformed++;

        trickHistory.Add(new TrickRecord
        {
            Name = trickName,
            Score = finalScore,
            Timestamp = sessionTime
        });

        OnScoreChanged?.Invoke(TotalScore);
        OnComboChanged?.Invoke(CurrentCombo, ComboMultiplier);
    }

    public void AddItemScore(int points, bool resetComboTimer = true)
    {
        int earned = Mathf.RoundToInt(points * EffectiveMultiplier);
        TotalScore += earned;
        itemsCollected++;
        if (resetComboTimer) ResetComboTimer();
        OnScoreChanged?.Invoke(TotalScore);
    }

    public void ApplyMultiplierBoost(float duration)
    {
        multiplierBoostTimer = Mathf.Max(multiplierBoostTimer, duration);
        OnComboChanged?.Invoke(CurrentCombo, EffectiveMultiplier);
    }

    public void AddDestructionScore(int points = 50)
    {
        int earned = Mathf.RoundToInt(points * EffectiveMultiplier);
        TotalScore += earned;
        obstaclesDestroyed++;
        OnScoreChanged?.Invoke(TotalScore);
    }

    public void AddFinishScore()
    {
        float timeBonus = Mathf.Max(0f, 1000f - sessionTime * 10f);
        float comboBonus = CurrentCombo * 100f;
        TotalScore += Mathf.RoundToInt(timeBonus + comboBonus);
        OnScoreChanged?.Invoke(TotalScore);
    }

    private void ResetComboTimer()
    {
        comboTimer = maxComboTimer;
    }

    private void ResetCombo()
    {
        CurrentCombo = 0;
        ComboMultiplier = 1f;
        OnComboChanged?.Invoke(0, 1f);
    }

    public void BreakCombo()
    {
        ResetCombo();
    }

    public string GetScoreSummary()
    {
        return $"Score: {TotalScore}\n" +
               $"Time: {sessionTime:F1}s\n" +
               $"Items: {itemsCollected}\n" +
               $"Tricks: {tricksPerformed}\n" +
               $"Destroyed: {obstaclesDestroyed}";
    }

    public void Reset()
    {
        TotalScore = 0;
        CurrentCombo = 0;
        ComboMultiplier = 1f;
        comboTimer = 0f;
        multiplierBoostTimer = 0f;
        sessionTime = 0f;
        accumulatedScore = 0f;
        itemsCollected = 0;
        obstaclesDestroyed = 0;
        tricksPerformed = 0;
        trickHistory.Clear();
    }
}
