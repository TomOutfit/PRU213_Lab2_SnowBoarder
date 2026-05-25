using UnityEngine;
using System;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Scoring Settings")]
    [SerializeField] private float pointsPerSecond = 10f;
    [SerializeField] private float speedBonusThreshold = 15f;
    [SerializeField] private float speedBonusMultiplier = 2f;

    [Header("Combo Settings")]
    [SerializeField] private float comboResetTime = 3f;
    [SerializeField] private float maxComboTimer = 3f;
    [SerializeField] private int maxCombo = 10;
    [SerializeField] private float comboMultiplierGrowth = 0.5f;

    public int TotalScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public float ComboMultiplier { get; private set; } = 1f;
    public float ComboTimer => comboTimer;

    private float comboTimer;
    private float sessionTime;
    private int snowflakesCollected;
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        sessionTime += Time.deltaTime;
        AddSpeedScore();
        UpdateComboTimer();
    }

    private void AddSpeedScore()
    {
        float speed = PlayerController.Instance?.CurrentSpeed ?? 0f;
        float multiplier = speed >= speedBonusThreshold ? speedBonusMultiplier : 1f;
        float earned = pointsPerSecond * multiplier * Time.deltaTime;
        TotalScore += Mathf.RoundToInt(earned);
        OnScoreChanged?.Invoke(TotalScore);
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
        float finalScore = Mathf.RoundToInt(baseScore * ComboMultiplier);
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

    public void AddSnowflakeScore(int points = 10)
    {
        int earned = Mathf.RoundToInt(points * ComboMultiplier);
        TotalScore += earned;
        snowflakesCollected++;
        ResetComboTimer();
        OnScoreChanged?.Invoke(TotalScore);
    }

    public void AddDestructionScore(int points = 50)
    {
        int earned = Mathf.RoundToInt(points * ComboMultiplier);
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
               $"Snowflakes: {snowflakesCollected}\n" +
               $"Tricks: {tricksPerformed}\n" +
               $"Destroyed: {obstaclesDestroyed}";
    }

    public void Reset()
    {
        TotalScore = 0;
        CurrentCombo = 0;
        ComboMultiplier = 1f;
        comboTimer = 0f;
        sessionTime = 0f;
        snowflakesCollected = 0;
        obstaclesDestroyed = 0;
        tricksPerformed = 0;
        trickHistory.Clear();
    }
}
