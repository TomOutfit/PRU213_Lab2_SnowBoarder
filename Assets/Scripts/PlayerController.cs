using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float baseSpeed = 35f;
    public float boostSpeed = 52f; // ~130 km/h
    public float maxSpeed = 65f;
    public float acceleration = 30f;
    public float deceleration = 20f;

    [Header("Physics Settings")]
    public float torqueAmount = 25f;
    public float moveForce = 50f;
    public float turnSpeed = 5f;
    public float gravityMultiplier = 1f;
    public float slopeSpeedBoost = 0f;
    public float friction = 0f;
    public float slopeAngle = 0f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;

    [Header("Crash Settings")]
    public float crashRotationThreshold = 80f;

    [Header("Trick Settings")]
    public float trickCooldown = 0.5f;
    public int maxComboTricks = 5;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 3f;
    public float invincibilityBlinkInterval = 0.15f;

    [Header("References (Auto-populated)")]
    public Rigidbody2D rb;
    public SpriteRenderer playerSprite;
    public AudioSource audioSource;

    Rigidbody2D surfaceEffectorRB;
    SurfaceEffector2D surfaceEffector2D;
    bool canMove = true;
    bool isGrounded = false;
    public bool isInvincible = false;
    bool isBlinking = false;
    float startXPos;
    bool hasFinished = false;

    void Start()
    {
        startXPos = transform.position.x;
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        SurfaceEffector2D[] effectors = FindObjectsByType<SurfaceEffector2D>();
        foreach (var effector in effectors)
        {
            if (effector.gameObject.name.Contains("Level") || effector.gameObject.name.Contains("Ground") || effector.gameObject.name.Contains("Slope"))
            {
                surfaceEffector2D = effector;
                surfaceEffectorRB = effector.GetComponent<Rigidbody2D>();
                break;
            }
        }

        if (playerSprite == null)
        {
            Transform child = transform.Find("Boarder_Top");
            if (child != null) playerSprite = child.GetComponent<SpriteRenderer>();
        }
        if (playerSprite == null) playerSprite = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (hasFinished) return;

        if (canMove)
        {
            RotatePlayer();
            RespondToBoost();
            RespondToJump();
        }

        if (isInvincible && !isBlinking)
        {
            StartCoroutine(InvincibilityBlink());
        }
    }

    void FixedUpdate()
    {
        if (hasFinished || !canMove) return;

        // Giữ tốc độ tối thiểu theo chiều X để người chơi không bao giờ đi quá chậm
        float targetXSpeed = baseSpeed * 0.9f; 
        
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed || 
             UnityEngine.InputSystem.Keyboard.current.wKey.isPressed))
        {
            targetXSpeed = boostSpeed * 0.95f; // Ép tốc độ sát với BoostSpeed nhất có thể
        }

        if (rb.linearVelocity.x < targetXSpeed)
        {
            // Bù lực đẩy cực mạnh về phía trước (đặc biệt khi đang bay trên không hoặc leo dốc)
            rb.AddForce(Vector2.right * moveForce * 4f);
        }

        // Giới hạn tốc độ tối đa để không bay mất kiểm soát
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            if (AudioManager.Instance != null) AudioManager.Instance.ResumeBoardingSound();
        }
        else if (collision.gameObject.CompareTag("SlowDown") && !isInvincible)
        {
            ApplySlowDown(3f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SlowDown") && !isInvincible)
        {
            ApplySlowDown(3f);
        }
        else if (collision.CompareTag("Penalty") && !isInvincible)
        {
            if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(-300);
            if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("-300", transform.position);
            
            // Phá hủy tảng đá khi va chạm
            Destroy(collision.gameObject);
        }
    }

    public void ApplySlowDown(float duration)
    {
        StartCoroutine(SlowDownCoroutine(duration));
    }

    private System.Collections.IEnumerator SlowDownCoroutine(float duration)
    {
        float originalBase = baseSpeed;
        float originalBoost = boostSpeed;
        
        baseSpeed *= 0.5f;
        boostSpeed *= 0.5f;
        
        if (surfaceEffector2D != null) surfaceEffector2D.speed = baseSpeed;
        if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("SLOWED!", transform.position);
        
        // Hiệu ứng màu bùn đất lên nhân vật
        if (playerSprite != null) playerSprite.color = new Color(0.6f, 0.4f, 0.2f); 

        yield return new WaitForSeconds(duration);

        baseSpeed = originalBase;
        boostSpeed = originalBoost;
        if (surfaceEffector2D != null) surfaceEffector2D.speed = baseSpeed;
        
        // Trả lại màu gốc
        if (playerSprite != null) playerSprite.color = Color.white; 
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
            if (AudioManager.Instance != null) AudioManager.Instance.PauseBoardingSound();
        }
    }

    public void DisableControls()
    {
        canMove = false;
    }

    public void SetFinished()
    {
        hasFinished = true;
        DisableControls();
    }

    void RotatePlayer()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        // Tăng lực xoay (torque) lên gấp 2-3 lần khi đang ở trên không để dễ làm trick lộn nhào hơn
        float currentTorque = isGrounded ? torqueAmount : torqueAmount * 2.5f;

        if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed ||
            UnityEngine.InputSystem.Keyboard.current.aKey.isPressed)
        {
            rb.AddTorque(currentTorque);
        }
        else if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed ||
                 UnityEngine.InputSystem.Keyboard.current.dKey.isPressed)
        {
            rb.AddTorque(-currentTorque);
        }
    }

    void RespondToBoost()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        if (surfaceEffector2D != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed ||
                UnityEngine.InputSystem.Keyboard.current.wKey.isPressed)
            {
                surfaceEffector2D.speed = boostSpeed;
            }
            else
            {
                surfaceEffector2D.speed = baseSpeed;
            }
        }
        else if (surfaceEffectorRB != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed ||
                UnityEngine.InputSystem.Keyboard.current.wKey.isPressed)
            {
                surfaceEffectorRB.AddForce(Vector2.right * moveForce);
            }
        }
    }

    void RespondToJump()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJumpSound();
            isGrounded = false;
        }
    }

    public void ApplySpeedBoost(float duration)
    {
        StartCoroutine(SpeedBoostCoroutine(duration));
    }

    private System.Collections.IEnumerator SpeedBoostCoroutine(float duration)
    {
        float originalBase = baseSpeed;
        float originalBoost = boostSpeed;
        baseSpeed *= 1.5f;
        boostSpeed *= 1.5f;
        
        if (surfaceEffector2D != null) surfaceEffector2D.speed = baseSpeed;
        if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("SPEED UP!", transform.position);
        if (playerSprite != null) playerSprite.color = Color.cyan;
        
        yield return new WaitForSeconds(duration);
        
        baseSpeed = originalBase;
        boostSpeed = originalBoost;
        if (surfaceEffector2D != null) surfaceEffector2D.speed = baseSpeed;
        if (playerSprite != null) playerSprite.color = Color.white;
    }

    public void ApplyInvincibility(float duration)
    {
        StartCoroutine(InvincibilityCoroutine(duration));
    }

    private System.Collections.IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("INVINCIBLE!", transform.position);
        yield return new WaitForSeconds(duration);
        isInvincible = false;
        if (playerSprite != null) playerSprite.enabled = true;
    }

    private System.Collections.IEnumerator InvincibilityBlink()
    {
        isBlinking = true;
        while (isInvincible)
        {
            if (playerSprite != null) playerSprite.enabled = false;
            yield return new WaitForSeconds(invincibilityBlinkInterval);
            if (playerSprite != null) playerSprite.enabled = true;
            yield return new WaitForSeconds(invincibilityBlinkInterval);
        }
        isBlinking = false;
    }

    public bool IsGrounded() { return isGrounded; }
}
