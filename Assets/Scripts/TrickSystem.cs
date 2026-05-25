using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages trick detection, scoring, and animations.
/// Works with the ComboSystem to track tricks performed in the air.
/// </summary>
public class TrickSystem : MonoBehaviour
{
    public static TrickSystem Instance { get; private set; }
    
    [Header("Trick Detection")]
    [SerializeField] private float trickDetectionTime = 0.5f;
    [SerializeField] private float minTrickAngle = 90f;
    [SerializeField] private float minAirTime = 0.3f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float frontFlipRotation = 360f;
    [SerializeField] private float backFlipRotation = -360f;
    [SerializeField] private float spin180Rotation = 180f;
    
    [Header("Jump Detection")]
    [SerializeField] private float minJumpHeight = 2f;
    
    [Header("Grounded Trick")]
    [SerializeField] private float grabDuration = 0.5f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem trickParticleEffect;
    [SerializeField] private AudioClip trickCompleteSound;
    [SerializeField] private AudioClip trickFailSound;
    
    // State
    private bool isPerformingTrick;
    private TrickType currentTrick;
    private float trickStartTime;
    private float trickTimer;
    private float totalRotation;
    private float previousRotation;
    private Vector3 previousPosition;
    private float airTime;
    private float maxHeightReached;
    private bool canAttemptTrick;
    
    // Events
    public event Action<TrickType, bool> OnTrickAttempt;
    public event Action<TrickType, int> OnTrickCompleted;
    public event Action<TrickType> OnTrickFailed;
    public event Action<float> OnAirTimeChanged;
    
    // Properties
    public bool IsPerformingTrick => isPerformingTrick;
    public TrickType CurrentTrick => currentTrick;
    public float AirTime => airTime;
    
    private PlayerController player;
    
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
        
        player = GetComponent<PlayerController>();
        previousPosition = transform.position;
    }
    
    private void Start()
    {
        SubscribeToPlayerEvents();
        SubscribeToGameInputEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromPlayerEvents();
        UnsubscribeFromGameInputEvents();
    }
    
    private void SubscribeToPlayerEvents()
    {
        if (player != null)
        {
            player.OnJump += HandleJump;
            player.OnLand += HandleLand;
        }
    }
    
    private void UnsubscribeFromPlayerEvents()
    {
        if (player != null)
        {
            player.OnJump -= HandleJump;
            player.OnLand -= HandleLand;
        }
    }
    
    private void SubscribeToGameInputEvents()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnTrickLeft += HandleTrickLeftInput;
            GameInput.Instance.OnTrickRight += HandleTrickRightInput;
        }
    }
    
    private void UnsubscribeFromGameInputEvents()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnTrickLeft -= HandleTrickLeftInput;
            GameInput.Instance.OnTrickRight -= HandleTrickRightInput;
        }
    }
    
    private void HandleTrickLeftInput()
    {
        if (player != null && !player.IsGrounded && canAttemptTrick && airTime >= minAirTime)
        {
            AttemptTrick(TrickType.Backflip);
        }
    }
    
    private void HandleTrickRightInput()
    {
        if (player != null && !player.IsGrounded && canAttemptTrick && airTime >= minAirTime)
        {
            AttemptTrick(TrickType.Frontflip);
        }
    }
    
    private void Update()
    {
        if (player == null) return;
        
        if (isPerformingTrick)
        {
            UpdateTrickProgress();
        }
        else if (!player.IsGrounded)
        {
            UpdateAirTime();
        }
    }
    
    private void UpdateAirTime()
    {
        if (player == null || player.IsGrounded) return;
        
        airTime += Time.deltaTime;
        OnAirTimeChanged?.Invoke(airTime);
        
        float height = transform.position.y;
        if (height > maxHeightReached)
        {
            maxHeightReached = height;
        }
    }
    
    private void UpdateTrickProgress()
    {
        trickTimer -= Time.deltaTime;
        
        float currentRotation = transform.eulerAngles.z;
        float rotationDelta = Mathf.Abs(currentRotation - previousRotation);
        
        if (rotationDelta > 180f)
        {
            rotationDelta = 360f - rotationDelta;
        }
        
        totalRotation += rotationDelta;
        previousRotation = currentRotation;
        
        if (trickTimer <= 0f)
        {
            CompleteTrick();
        }
    }
    
    #region Jump Handling
    
    private void HandleJump()
    {
        airTime = 0f;
        maxHeightReached = transform.position.y;
        totalRotation = 0f;
        previousRotation = transform.eulerAngles.z;
        previousPosition = transform.position;
        canAttemptTrick = true;
    }
    
    private void HandleLand()
    {
        bool trickPerformed = false;
        
        if (airTime >= minAirTime && Mathf.Abs(totalRotation) >= minTrickAngle)
        {
            TrickType completedTrick = DetectCompletedTrick();
            if (completedTrick != TrickType.None)
            {
                CompleteTrickAs(completedTrick);
                trickPerformed = true;
            }
        }
        
        if (!trickPerformed && isPerformingTrick)
        {
            FailTrick();
        }
        
        canAttemptTrick = false;
        ResetTrickState();
    }
    
    #endregion
    
    #region Trick Execution
    
    private void AttemptTrick(TrickType trick)
    {
        if (isPerformingTrick) return;
        
        isPerformingTrick = true;
        currentTrick = trick;
        trickStartTime = Time.time;
        trickTimer = trickDetectionTime;
        totalRotation = 0f;
        canAttemptTrick = false;
        
        OnTrickAttempt?.Invoke(trick, true);
        
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.RegisterTrick(trick);
        }
        
        PlayTrickStartEffect();
    }
    
    public void OnPerformTrick(TrickType trick)
    {
        if (player != null && !player.IsGrounded && canAttemptTrick && airTime >= minAirTime)
        {
            AttemptTrick(trick);
        }
    }
    
    private void CompleteTrick()
    {
        if (!isPerformingTrick) return;
        
        TrickType completedTrick = DetectCompletedTrick();
        if (completedTrick != TrickType.None)
        {
            CompleteTrickAs(completedTrick);
        }
        else
        {
            FailTrick();
        }
        
        ResetTrickState();
    }
    
    private void CompleteTrickAs(TrickType trick)
    {
        int bonus = CalculateTrickBonus(trick);
        
        OnTrickCompleted?.Invoke(trick, bonus);
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(bonus);
        }
        
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.RegisterLanding();
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowTrickPopup(trick.ToString(), bonus);
        }
        
        PlayTrickCompleteEffect(trick);
    }
    
    private void FailTrick()
    {
        OnTrickFailed?.Invoke(currentTrick);
        PlayTrickFailSound();
    }
    
    #endregion
    
    #region Trick Detection
    
    private TrickType DetectCompletedTrick()
    {
        if (totalRotation <= backFlipRotation * 1.2f && totalRotation >= backFlipRotation * 0.8f)
        {
            return TrickType.Backflip;
        }
        
        if (totalRotation >= frontFlipRotation * 0.8f && totalRotation <= frontFlipRotation * 1.2f)
        {
            return TrickType.Frontflip;
        }
        
        if (Mathf.Abs(totalRotation) >= spin180Rotation * 0.8f)
        {
            return TrickType.Spin180;
        }
        
        if (maxHeightReached - previousPosition.y >= minJumpHeight)
        {
            return TrickType.Grab;
        }
        
        return TrickType.None;
    }
    
    private void ResetTrickState()
    {
        isPerformingTrick = false;
        currentTrick = TrickType.None;
        trickTimer = 0f;
        totalRotation = 0f;
        airTime = 0f;
        maxHeightReached = 0f;
    }
    
    #endregion
    
    #region Scoring
    
    private int CalculateTrickBonus(TrickType trick)
    {
        int baseBonus = 200;
        
        switch (trick)
        {
            case TrickType.Frontflip:
                baseBonus = 300;
                break;
            case TrickType.Backflip:
                baseBonus = 350;
                break;
            case TrickType.Spin180:
                baseBonus = 200;
                break;
            case TrickType.Grab:
                baseBonus = 150;
                break;
        }
        
        float heightBonus = 1f + ((maxHeightReached - previousPosition.y) / 10f);
        baseBonus = Mathf.RoundToInt(baseBonus * heightBonus);
        
        float airTimeBonus = 1f + (airTime / 2f);
        baseBonus = Mathf.RoundToInt(baseBonus * airTimeBonus);
        
        return baseBonus;
    }
    
    #endregion
    
    #region Effects
    
    private void PlayTrickStartEffect()
    {
        if (trickParticleEffect != null)
        {
            ParticleSystem particles = Instantiate(trickParticleEffect, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }
    }
    
    private void PlayTrickCompleteEffect(TrickType trick)
    {
        if (trickCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(trickCompleteSound, transform.position);
        }
        
        if (trickParticleEffect != null)
        {
            ParticleSystem particles = Instantiate(trickParticleEffect, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }
    }
    
    private void PlayTrickFailSound()
    {
        if (trickFailSound != null)
        {
            AudioSource.PlayClipAtPoint(trickFailSound, transform.position);
        }
    }
    
    #endregion
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = isPerformingTrick ? Color.yellow : Color.gray;
        Vector3 groundLevel = new Vector3(transform.position.x, previousPosition.y - minJumpHeight, transform.position.z);
        Gizmos.DrawLine(transform.position, groundLevel);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, maxHeightReached, transform.position.z), 0.3f);
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, previousPosition);
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
