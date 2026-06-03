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
    System.Collections.Generic.List<SurfaceEffector2D> surfaceEffectors = new System.Collections.Generic.List<SurfaceEffector2D>();
    float rotationInput = 0f;
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

        surfaceEffectors.Clear();
        SurfaceEffector2D[] effectors = FindObjectsByType<SurfaceEffector2D>(FindObjectsInactive.Exclude);
        foreach (var effector in effectors)
        {
            if (effector.gameObject.name.Contains("Level") || effector.gameObject.name.Contains("Ground") || effector.gameObject.name.Contains("Slope") || effector.gameObject.CompareTag("Ground"))
            {
                surfaceEffectors.Add(effector);
                if (surfaceEffectorRB == null)
                {
                    surfaceEffectorRB = effector.GetComponent<Rigidbody2D>();
                }
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
            ReadRotationInput();
            RespondToJump();
        }

        if (isInvincible && !isBlinking)
        {
            StartCoroutine(InvincibilityBlink());
        }
    }

    void ReadRotationInput()
    {
        rotationInput = 0f;
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        if (UnityEngine.InputSystem.Keyboard.current.leftArrowKey.isPressed ||
            UnityEngine.InputSystem.Keyboard.current.aKey.isPressed)
        {
            rotationInput = 1f;
        }
        else if (UnityEngine.InputSystem.Keyboard.current.rightArrowKey.isPressed ||
                 UnityEngine.InputSystem.Keyboard.current.dKey.isPressed)
        {
            rotationInput = -1f;
        }
    }

    void FixedUpdate()
    {
        if (hasFinished || !canMove) return;

        // Tính toán vật lý dốc, ma sát, trọng lực và đà trượt
        UpdateSlopePhysics();

        // Xoay nhân vật dựa trên rotationInput (Chạy ở FixedUpdate để đồng bộ vật lý, không phụ thuộc FPS)
        if (rotationInput != 0f)
        {
            float currentTorque = isGrounded ? torqueAmount : torqueAmount * 2.5f;
            rb.AddTorque(rotationInput * currentTorque);
        }

        // Áp dụng tốc độ/lực đẩy cho Surface Effectors
        RespondToBoost();

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
        else if (collision.gameObject.CompareTag("SlowDown"))
        {
            if (!isInvincible)
            {
                ApplySlowDown(3f);
            }
        }
        else if (collision.gameObject.CompareTag("Obstacle") && isInvincible)
        {
            // Phá huỷ cây cối cản đường nếu đang bất tử để tránh bị kẹt
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCrashSound();
            Destroy(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("SlowDown"))
        {
            if (!isInvincible)
            {
                ApplySlowDown(3f);
            }
        }
        else if (collision.CompareTag("Penalty"))
        {
            if (!isInvincible)
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(-300);
                if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("-300", transform.position);
                if (AudioManager.Instance != null) AudioManager.Instance.PlayCrashSound();
                StartCoroutine(FlashRedCoroutine(0.5f));
            }
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("Obstacle") && isInvincible)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayCrashSound();
            Destroy(collision.gameObject);
        }
    }

    public void ApplySlowDown(float duration)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySlowDownSound();
        StartCoroutine(SlowDownCoroutine(duration));
    }

    private System.Collections.IEnumerator SlowDownCoroutine(float duration)
    {
        float originalBase = baseSpeed;
        float originalBoost = boostSpeed;
        
        baseSpeed *= 0.5f;
        boostSpeed *= 0.5f;
        
        foreach (var effector in surfaceEffectors)
        {
            if (effector != null) effector.speed = baseSpeed;
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("SLOWED!", transform.position);
        
        // Hiệu ứng màu bùn đất lên nhân vật
        if (playerSprite != null) playerSprite.color = new Color(0.6f, 0.4f, 0.2f); 

        yield return new WaitForSeconds(duration);

        baseSpeed = originalBase;
        boostSpeed = originalBoost;
        foreach (var effector in surfaceEffectors)
        {
            if (effector != null) effector.speed = baseSpeed;
        }
        
        // Trả lại màu gốc
        if (playerSprite != null) playerSprite.color = isInvincible ? Color.cyan : Color.white; 
    }

    private System.Collections.IEnumerator FlashRedCoroutine(float duration)
    {
        if (playerSprite != null)
        {
            playerSprite.color = Color.red;
            yield return new WaitForSeconds(duration);
            playerSprite.color = isInvincible ? Color.cyan : Color.white;
        }
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

    void RespondToBoost()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        bool isBoosting = UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed ||
                          UnityEngine.InputSystem.Keyboard.current.wKey.isPressed;

        if (surfaceEffectors.Count > 0)
        {
            float targetSpeed = isBoosting ? boostSpeed : baseSpeed;
            targetSpeed += slopeSpeedBoost; // Cộng dynamic boost từ độ dốc
            targetSpeed = Mathf.Max(targetSpeed, baseSpeed * 0.5f); // Tốc độ không giảm quá 50% baseSpeed

            foreach (var effector in surfaceEffectors)
            {
                if (effector != null) effector.speed = targetSpeed;
            }
        }
        else if (surfaceEffectorRB != null)
        {
            float targetForce = isBoosting ? moveForce : moveForce * 0.5f;
            if (slopeSpeedBoost > 0) targetForce += slopeSpeedBoost * 2f;
            surfaceEffectorRB.AddForce(Vector2.right * targetForce);
        }
    }

    void UpdateSlopePhysics()
    {
        // 1. Raycast từ nhân vật xuống dưới theo hướng đứng để tìm mặt đất
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 3.5f);
        bool groundFound = false;

        if (hit.collider != null)
        {
            // Kiểm tra xem collider có phải là Ground hay Level không
            if (hit.collider.CompareTag("Ground") || hit.collider.gameObject.name.Contains("Level") || hit.collider.gameObject.name.Contains("Slope"))
            {
                groundFound = true;
            }
        }

        if (groundFound)
        {
            Vector2 normal = hit.normal;
            
            // Tính góc dốc (độ) giữa Vector pháp tuyến bề mặt và Vector hướng lên thẳng đứng
            slopeAngle = Vector2.Angle(normal, Vector2.up);

            // Xác định hệ số trọng lực dựa trên độ dốc (dốc càng cao, tác dụng trọng lực dọc dốc càng mạnh)
            gravityMultiplier = 1f + (slopeAngle / 45f) * 0.5f;

            // slopeDirection: normal.x > 0 là dốc xuống bên phải (thuận chiều trượt); normal.x < 0 là dốc lên
            if (normal.x > 0.02f)
            {
                // Dốc xuống: Tăng tốc độ trượt tỉ lệ với góc dốc, ma sát giảm
                slopeSpeedBoost = slopeAngle * 0.4f;
                friction = 0.01f;
            }
            else if (normal.x < -0.02f)
            {
                // Dốc lên: Giảm tốc độ trượt (slopeSpeedBoost âm), ma sát tăng
                slopeSpeedBoost = -slopeAngle * 0.6f;
                friction = 0.15f;
            }
            else
            {
                // Đường bằng phẳng
                slopeSpeedBoost = 0f;
                friction = 0.05f;
            }

            // 2. Tính toán và áp dụng lực kéo dọc theo sườn dốc (đà trượt / momentum)
            // Vector tiếp tuyến sườn dốc hướng xuống/về phía trước (+X)
            Vector2 slopeTangent = new Vector2(normal.y, -normal.x).normalized;
            
            // Trọng lượng kéo dọc dốc: F = mass * g * sin(slopeAngle)
            float g = Physics2D.gravity.magnitude;
            float gravityForceComponent = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * g * gravityMultiplier;

            // Áp dụng lực gia tốc dọc dốc lên Rigidbody2D
            rb.AddForce(slopeTangent * gravityForceComponent * rb.mass * 0.4f);

            // 3. Áp dụng lực ma sát động cản trở chuyển động dựa trên hệ số ma sát
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                rb.AddForce(-rb.linearVelocity.normalized * friction * rb.mass * 6f);
            }
        }
        else
        {
            // Khi đang bay trên không (In Air)
            slopeAngle = 0f;
            slopeSpeedBoost = 0f;
            gravityMultiplier = 1f;
            friction = 0.02f; // Sức cản không khí nhỏ
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
        
        foreach (var effector in surfaceEffectors)
        {
            if (effector != null) effector.speed = baseSpeed;
        }
        if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("SPEED UP!", transform.position);
        if (playerSprite != null) playerSprite.color = Color.cyan;
        
        yield return new WaitForSeconds(duration);
        
        baseSpeed = originalBase;
        boostSpeed = originalBoost;
        foreach (var effector in surfaceEffectors)
        {
            if (effector != null) effector.speed = baseSpeed;
        }
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

    public void ResetAll()
    {
        canMove = true;
        isGrounded = false;
        hasFinished = false;
        isInvincible = false;
        isBlinking = false;
    }
}
