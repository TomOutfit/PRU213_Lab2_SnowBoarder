using UnityEngine;
using System;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Physics & Movement")]
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float torqueAmount = 25f;
    [SerializeField] private float moveForce = 15f; // Lực di chuyển tới lui
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float crashRotationThreshold = 80f; // Góc lệch tối đa so với mặt đất

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
    private SurfaceEffector2D currentEffector;

    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float RunTime { get; private set; } // Bộ đếm thời gian
    public bool IsGrounded { get; private set; }
    public bool IsInvincible { get; private set; }
    public bool IsPerformingTrick { get; private set; }
    public int CurrentCombo { get; private set; }

    private Vector2 moveInput;
    private float currentTrickCooldown;
    private float invincibilityTimer;
    private float blinkTimer;
    private bool spriteVisible = true;

    // Events
    public event Action OnSpeedChanged;
    public event Action<int> OnComboChanged;
    public event Action OnTrickPerformed;
    public event Action OnCrash;

    private void Awake()
    {
        Instance = this;
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (playerSprite == null) playerSprite = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Cần đảm bảo Rigidbody2D không bị khoá xoay (freeze rotation) để nhào lộn
        if (rb != null) rb.freezeRotation = false; 
    }

    public void OnMoveInput(Vector2 input)
    {
        moveInput = input;
    }

    public void OnJumpInput()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        
        // Nhảy nếu đang ở trên mặt đất
        if (IsGrounded && !IsInvincible && rb != null)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Xử lý Input Nhảy (Jump)
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            OnJumpInput();
        }

        // Cập nhật bộ đếm thời gian
        RunTime += Time.deltaTime;

        HandleTrickCooldown();
        HandleInvincibility();
        
        // Kích hoạt event cập nhật UI tốc độ và thời gian
        OnSpeedChanged?.Invoke();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            float moveX = 0f;
            if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.aKey.isPressed)
                moveX = -1f;
            if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.dKey.isPressed)
                moveX = 1f;
            moveInput = new Vector2(moveX, 0f);
        }

        ApplyRotationTorque();
        ClampSpeed();
    }

    private void ApplyRotationTorque()
    {
        if (rb == null) return;

        // Xử lý phím mũi tên (Trái / Phải)
        if (moveInput.x != 0)
        {
            // Nếu đang chạm đất, phím mũi tên tạo lực đẩy tiến tới / lùi lại
            if (IsGrounded)
            {
                rb.AddForce(new Vector2(moveInput.x * moveForce, 0f), ForceMode2D.Force);
            }
            
            // Bơm lực xoay để nhào lộn (kể cả trên không hay dưới đất)
            rb.AddTorque(-moveInput.x * torqueAmount);
        }
    }

    private void ClampSpeed()
    {
        if (rb == null) return;

        // Giới hạn tốc độ không để ván trượt bay quá nhanh
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
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
            if (playerSprite != null) playerSprite.enabled = spriteVisible;
            blinkTimer = invincibilityBlinkInterval;
        }

        if (invincibilityTimer <= 0f)
        {
            IsInvincible = false;
            if (playerSprite != null) playerSprite.enabled = true;
        }
    }

    public void PerformTrick(string trickName)
    {
        // Trick chỉ được thực hiện khi đang bay trên không
        if (currentTrickCooldown > 0 || IsGrounded || CurrentCombo >= maxComboTricks) return;

        currentTrickCooldown = trickCooldown;
        IsPerformingTrick = true;
        CurrentCombo++;

        float trickScore = CalculateTrickScore(trickName);
        if (ScoreManager.Instance != null)
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

        if (GameManager.Instance != null)
            GameManager.Instance.PlayerDied();
            
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCrashSFX();
            
        OnCrash?.Invoke();
        ApplyInvincibility();
        ResetComboOnCrash();
        
        // Xoá vận tốc khi ngã
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void ApplySpeedBoost(float boostAmount, float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(boostAmount, duration));
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine(float boostAmount, float duration)
    {
        float originalMaxSpeed = maxSpeed;
        maxSpeed += boostAmount;
        
        // Nâng tốc độ của cái băng chuyền mặt đất nếu đang chạm đất
        if (currentEffector != null)
        {
            currentEffector.speed += boostAmount;
        }
        
        yield return new WaitForSeconds(duration);
        
        maxSpeed = originalMaxSpeed;
        if (currentEffector != null)
        {
            currentEffector.speed -= boostAmount;
        }
    }

    public void ApplyInvincibility()
    {
        IsInvincible = true;
        invincibilityTimer = invincibilityDuration;
        blinkTimer = invincibilityBlinkInterval;
        if (playerSprite != null) playerSprite.enabled = true;
    }

    public void ResetComboOnCrash()
    {
        CurrentCombo = 0;
        OnComboChanged?.Invoke(0);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGrounded = true;
            CheckLandingSafety(collision);
            
            currentEffector = collision.gameObject.GetComponent<SurfaceEffector2D>();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGrounded = false;
            currentEffector = null;
        }
    }

    private void CheckLandingSafety(Collision2D collision)
    {
        // Lấy trung bình các điểm va chạm để tìm pháp tuyến mặt đất (Ground Normal)
        Vector2 averageNormal = Vector2.zero;
        foreach (ContactPoint2D contact in collision.contacts)
        {
            averageNormal += contact.normal;
        }
        averageNormal.Normalize();

        // Tính góc chênh lệch giữa thân nhân vật và mặt đất
        float angleDifference = Vector2.Angle(transform.up, averageNormal);

        // Nếu lệch quá ngưỡng (ví dụ cắm đầu hoặc ngửa bụng xuống tuyết) thì bị tính là Crash
        if (angleDifference > crashRotationThreshold)
        {
            TakeDamage(1);
        }
    }
}
