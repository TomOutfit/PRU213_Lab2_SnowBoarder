using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private float baseSpeed = 8f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 3f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private float laneWidth = 3f;

    [Header("Slope Physics")]
    [SerializeField] private float gravityMultiplier = 1.5f;
    [SerializeField] private float slopeSpeedBoost = 0.5f;
    [SerializeField] private float friction = 0.1f;
    [SerializeField] private float slopeAngle = 15f;

    [Header("Trick System")]
    [SerializeField] private float trickCooldown = 0.5f;
    [SerializeField] private int maxComboTricks = 5;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 3f;
    [SerializeField] private float invincibilityBlinkInterval = 0.15f;

    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer playerSprite;
    [SerializeField] private AudioSource audioSource;

    public float CurrentSpeed { get; private set; }
    public bool IsGrounded { get; private set; } = true;
    public bool IsInvincible { get; private set; }
    public bool IsPerformingTrick { get; private set; }
    public int CurrentCombo { get; private set; }

    private Vector2 moveInput;
    private Vector2 smoothInput;
    private float currentTrickCooldown;
    private float invincibilityTimer;
    private float blinkTimer;
    private bool spriteVisible = true;
    private float xPosition;

    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    public event Action OnSpeedChanged;
    public event Action<int> OnComboChanged;
    public event Action OnTrickPerformed;
    public event Action OnCrash;

    public void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void OnJumpInput()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (currentTrickCooldown > 0 || !IsGrounded || CurrentCombo >= maxComboTricks) return;
        PerformTrick("Jump");
    }

    private void Awake()
    {
        Instance = this;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerSprite == null) playerSprite = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        ReadInput();
        HandleTrickCooldown();
        HandleInvincibility();
        UpdateSpritePosition();
        ApplySlopePhysics();
        ApplyMovement();
        ClampSpeed();
    }

    private void ReadInput()
    {
        smoothInput = Vector2.Lerp(smoothInput, moveInput, Time.deltaTime * 10f);
    }

    private void HandleTrickCooldown()
    {
        if (currentTrickCooldown > 0)
            currentTrickCooldown -= Time.deltaTime;
    }

    private void HandleInvincibility()
    {
        if (!IsInvincible) return;

        invincibilityTimer -= Time.deltaTime;
        blinkTimer -= Time.deltaTime;

        if (blinkTimer <= 0f)
        {
            spriteVisible = !spriteVisible;
            playerSprite.enabled = spriteVisible;
            blinkTimer = invincibilityBlinkInterval;
        }

        if (invincibilityTimer <= 0f)
        {
            IsInvincible = false;
            playerSprite.enabled = true;
        }
    }

    private void UpdateSpritePosition()
    {
        xPosition += smoothInput.x * turnSpeed * Time.deltaTime;
        xPosition = Mathf.Clamp(xPosition, -laneWidth, laneWidth);
        transform.position = new Vector3(xPosition, transform.position.y, 0f);
    }

    private void ApplySlopePhysics()
    {
        float slopeFactor = Mathf.Sin(Mathf.Deg2Rad * slopeAngle) * gravityMultiplier * slopeSpeedBoost;
        float slopeBonus = baseSpeed * slopeFactor;

        float targetSpeed = baseSpeed + slopeBonus + (smoothInput.y < 0 ? acceleration : 0f);
        targetSpeed = Mathf.Clamp(targetSpeed, 0f, maxSpeed);

        float speedDelta = smoothInput.y < 0 ? acceleration : deceleration;
        CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, targetSpeed, speedDelta * Time.deltaTime);

        float frictionEffect = friction * Time.deltaTime;
        CurrentSpeed = Mathf.Max(0f, CurrentSpeed - frictionEffect);

        OnSpeedChanged?.Invoke();
    }

    private void ApplyMovement()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.y = -CurrentSpeed;
        velocity.x = smoothInput.x * turnSpeed * 0.5f;
        rb.linearVelocity = velocity;
    }

    private void ClampSpeed()
    {
        if (CurrentSpeed > maxSpeed)
            CurrentSpeed = maxSpeed;
    }

    public void PerformTrick(string trickName)
    {
        if (currentTrickCooldown > 0 || !IsGrounded || CurrentCombo >= maxComboTricks) return;

        currentTrickCooldown = trickCooldown;
        IsPerformingTrick = true;
        CurrentCombo++;

        float trickScore = CalculateTrickScore(trickName);
        ScoreManager.Instance.AddTrickScore(trickName, CurrentCombo, trickScore);

        OnTrickPerformed?.Invoke();
        OnComboChanged?.Invoke(CurrentCombo);

        Invoke(nameof(EndTrick), 0.3f);
    }

    private float CalculateTrickScore(string trickName)
    {
        float baseScore = 100f;
        float comboMultiplier = 1f + (CurrentCombo - 1) * 0.5f;

        return trickName switch
        {
            "Jump" => baseScore * 1f * comboMultiplier,
            "Spin" => baseScore * 1.5f * comboMultiplier,
            "Flip" => baseScore * 2f * comboMultiplier,
            _ => baseScore * comboMultiplier
        };
    }

    private void EndTrick()
    {
        IsPerformingTrick = false;
    }

    public void BreakCombo()
    {
        if (!IsPerformingTrick)
            CurrentCombo = 0;
        OnComboChanged?.Invoke(CurrentCombo);
    }

    public void TakeDamage(int damageAmount)
    {
        if (IsInvincible) return;

        if (GameManager.Instance == null) return;
        GameManager.Instance.PlayerDied();
        AudioManager.Instance?.PlayCrashSFX();
        OnCrash?.Invoke();
        ApplyInvincibility();
    }

    public void ApplySpeedBoost(float boostAmount, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(boostAmount, duration));
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine(float boostAmount, float duration)
    {
        float originalMaxSpeed = maxSpeed;
        maxSpeed += boostAmount;
        yield return new WaitForSeconds(duration);
        maxSpeed = originalMaxSpeed;
    }

    public void ApplyInvincibility()
    {
        IsInvincible = true;
        invincibilityTimer = invincibilityDuration;
        blinkTimer = invincibilityBlinkInterval;
        playerSprite.enabled = true;
    }

    public void ResetComboOnCrash()
    {
        CurrentCombo = 0;
        OnComboChanged?.Invoke(0);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            IsGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            IsGrounded = false;
    }
}
