using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the combo system for chaining tricks and maintaining score multipliers.
/// Tracks combo chains, timing, and bonus calculations.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }
    
    [Header("Combo Settings")]
    [SerializeField] private float comboWindowTime = 3f;
    [SerializeField] private float comboDecayRate = 1f;
    [SerializeField] private float minSpeedForCombo = 5f;
    
    [Header("Multiplier Settings")]
    [SerializeField] private float baseMultiplier = 1f;
    [SerializeField] private float multiplierPerCombo = 0.5f;
    [SerializeField] private float maxMultiplier = 5f;
    
    [Header("Trick Bonuses")]
    [SerializeField] private int baseTrickBonus = 200;
    [SerializeField] private int frontFlipBonus = 300;
    [SerializeField] private int backFlipBonus = 350;
    [SerializeField] private int grabBonus = 150;
    [SerializeField] private int spin180Bonus = 200;
    
    [Header("UI Feedback")]
    [SerializeField] private bool showComboUI = true;
    [SerializeField] private AnimationCurve comboPopupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    [SerializeField] private AudioClip comboStartSound;
    [SerializeField] private AudioClip comboEndSound;
    [SerializeField] private AudioClip trickSound;
    
    // State
    private int currentComboCount;
    private float currentMultiplier;
    private float comboTimer;
    private bool isComboActive;
    private List<TrickType> performedTricks;
    private Queue<ComboEvent> pendingCombos;
    
    // Events
    public event Action<int> OnComboChanged;
    public event Action<float> OnMultiplierChanged;
    public event Action OnComboBroken;
    public event Action OnComboMaxed;
    public event Action OnComboStarted;
    
    // Properties
    public int CurrentComboCount => currentComboCount;
    public float CurrentMultiplier => currentMultiplier;
    public bool IsComboActive => isComboActive;
    public float ComboTimer => comboTimer;
    
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
        
        performedTricks = new List<TrickType>();
        pendingCombos = new Queue<ComboEvent>();
        ResetCombo();
    }
    
    private void Update()
    {
        UpdateComboTimer();
        ProcessPendingCombos();
    }
    
    private void UpdateComboTimer()
    {
        if (!isComboActive || currentComboCount == 0) return;
        
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            float speed = GameManager.Instance.CurrentPlayer.CurrentSpeed;
            if (speed < minSpeedForCombo)
            {
                comboTimer -= Time.deltaTime * 2f;
            }
            else
            {
                comboTimer -= Time.deltaTime * comboDecayRate;
            }
        }
        else
        {
            comboTimer -= Time.deltaTime * comboDecayRate;
        }
        
        if (comboTimer <= 0f)
        {
            BreakCombo();
        }
        
        UpdateComboUI();
    }
    
    private void ProcessPendingCombos()
    {
        while (pendingCombos.Count > 0)
        {
            ComboEvent comboEvent = pendingCombos.Peek();
            
            if (Time.time >= comboEvent.executeTime)
            {
                pendingCombos.Dequeue();
                ProcessComboEvent(comboEvent);
            }
            else
            {
                break;
            }
        }
    }
    
    private void ProcessComboEvent(ComboEvent comboEvent)
    {
        switch (comboEvent.type)
        {
            case ComboEventType.Trick:
                AddTrick(comboEvent.trickType);
                break;
            case ComboEventType.Landing:
                CompleteCombo();
                break;
            case ComboEventType.Break:
                BreakCombo();
                break;
        }
    }
    
    #region Public Methods
    
    public void RegisterTrick(TrickType trick)
    {
        ComboEvent trickEvent = new ComboEvent
        {
            type = ComboEventType.Trick,
            trickType = trick,
            executeTime = Time.time
        };
        
        pendingCombos.Enqueue(trickEvent);
    }
    
    public void RegisterLanding()
    {
        ComboEvent landingEvent = new ComboEvent
        {
            type = ComboEventType.Landing,
            executeTime = Time.time
        };
        
        pendingCombos.Enqueue(landingEvent);
    }
    
    public void RegisterCrash()
    {
        ComboEvent crashEvent = new ComboEvent
        {
            type = ComboEventType.Break,
            executeTime = Time.time
        };
        
        pendingCombos.Enqueue(crashEvent);
    }
    
    private void AddTrick(TrickType trick)
    {
        performedTricks.Add(trick);
        currentComboCount++;
        
        float previousMultiplier = currentMultiplier;
        currentMultiplier = Mathf.Min(baseMultiplier + (currentComboCount * multiplierPerCombo), maxMultiplier);
        
        if (currentComboCount == 1)
        {
            StartCombo();
        }
        else if (previousMultiplier != currentMultiplier)
        {
            OnMultiplierChanged?.Invoke(currentMultiplier);
            
            if (Mathf.Approximately(currentMultiplier, maxMultiplier))
            {
                OnComboMaxed?.Invoke();
            }
        }
        
        comboTimer = comboWindowTime;
        OnComboChanged?.Invoke(currentComboCount);
        
        PlayTrickSound(trick);
    }
    
    private void StartCombo()
    {
        isComboActive = true;
        comboTimer = comboWindowTime;
        
        OnComboStarted?.Invoke();
        PlayComboStartSound();
        ShowComboStartEffect();
    }
    
    private void CompleteCombo()
    {
        if (currentComboCount == 0) return;
        
        int totalBonus = CalculateComboBonus();
        
        if (GameManager.Instance != null)
        {
            for (int i = 0; i < currentComboCount; i++)
            {
                GameManager.Instance.IncreaseCombo();
            }
        }
        
        ShowComboCompleteEffect(currentComboCount, totalBonus);
        
        ResetCombo();
    }
    
    public void BreakCombo()
    {
        if (currentComboCount > 0)
        {
            OnComboBroken?.Invoke();
            ShowComboBreakEffect();
            PlayComboBreakSound();
        }
        
        ResetCombo();
    }
    
    private void ResetCombo()
    {
        currentComboCount = 0;
        currentMultiplier = baseMultiplier;
        comboTimer = 0f;
        isComboActive = false;
        performedTricks.Clear();
    }
    
    private int CalculateComboBonus()
    {
        int bonus = 0;
        
        foreach (TrickType trick in performedTricks)
        {
            bonus += GetTrickBonus(trick);
        }
        
        int comboBonus = currentComboCount * 50;
        bonus += comboBonus;
        
        int multiplierBonus = Mathf.RoundToInt(bonus * (currentMultiplier - 1f));
        bonus += multiplierBonus;
        
        return bonus;
    }
    
    private int GetTrickBonus(TrickType trick)
    {
        return trick switch
        {
            TrickType.Frontflip => frontFlipBonus,
            TrickType.Backflip => backFlipBonus,
            TrickType.Grab => grabBonus,
            TrickType.Spin180 => spin180Bonus,
            _ => baseTrickBonus
        };
    }
    
    #endregion
    
    #region UI & Effects
    
    private void UpdateComboUI()
    {
        if (!showComboUI || UIManager.Instance == null) return;
        
        if (isComboActive && currentComboCount > 0)
        {
            string comboText = $"{currentComboCount}x COMBO!";
            float timerPercent = comboTimer / comboWindowTime;
            
            if (timerPercent < 0.3f)
            {
                comboText += " !!";
            }
            
            // UIManager could show this
        }
    }
    
    private void ShowComboStartEffect()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScorePopup("COMBO!");
        }
    }
    
    private void ShowComboCompleteEffect(int count, int bonus)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScorePopup($"+{bonus}");
        }
    }
    
    private void ShowComboBreakEffect()
    {
        // Visual effect for combo break
    }
    
    #endregion
    
    #region Audio
    
    private void PlayComboStartSound()
    {
        if (comboStartSound != null)
        {
            AudioSource.PlayClipAtPoint(comboStartSound, Vector3.zero);
        }
    }
    
    private void PlayComboBreakSound()
    {
        if (comboEndSound != null)
        {
            AudioSource.PlayClipAtPoint(comboEndSound, Vector3.zero);
        }
    }
    
    private void PlayTrickSound(TrickType trick)
    {
        if (trickSound != null)
        {
            AudioSource.PlayClipAtPoint(trickSound, Vector3.zero);
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

public class ComboEvent
{
    public ComboEventType type;
    public TrickType trickType;
    public float executeTime;
}

public enum ComboEventType
{
    Trick,
    Landing,
    Break
}
