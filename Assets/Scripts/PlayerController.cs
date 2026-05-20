using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main player controller handling snowboarding physics, movement, and tricks.
/// Uses Unity's physics engine with custom slope calculations for realistic snowboarding feel.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum horizontal velocity")]
    [SerializeField] private float maxMoveSpeed = 25f;
    
    [Tooltip("How quickly the player accelerates to target velocity")]
    [SerializeField] private float acceleration = 8f;
    
    [Tooltip("How quickly the player decelerates when not pressing keys")]
    [SerializeField] private float deceleration = 5f;
    
    [Header("Gravity & Slope Physics")]
    [Tooltip("Base gravity strength")]
    [SerializeField] private float gravity = 25f;
    
    [Tooltip("Additional gravity multiplier based on slope angle")]
    [SerializeField] private float slopeGravityMultiplier = 1.5f;
    
    [Tooltip("Friction coefficient on snow")]
    [SerializeField] private float snowFriction = 0.98f;
    
    [Tooltip("Air resistance")]
    [SerializeField] private float airResistance = 0.995f;
    
    [Header("Jump & Tricks")]
    [Tooltip("Jump force when performing a trick")]
    [SerializeField] private float jumpForce = 12f;
    
    [Tooltip("Time window for landing a trick")]
    [SerializeField] private float trickWindowTime = 0.5f;
    
    [Tooltip("Available trick types")]
    [SerializeField] private TrickType[] availableTricks;
    
    [Header("Speed Control")]
    [Tooltip("Minimum speed multiplier")]
    [SerializeField] private float minSpeedMultiplier = 0.5f;
    
    [Tooltip("Maximum speed multiplier from power-ups")]
    [SerializeField] private float maxSpeedMultiplier = 2f;
    
    [Tooltip("Speed change rate when using speed control")]
    [SerializeField] private float speedChangeRate = 0.5f;
    
    [Header("Ground Detection")]
    [Tooltip("Layer mask for ground detection")]
    [SerializeField] private LayerMask groundLayer;
    
    [Tooltip("Distance to check for ground")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    
    [Header("Effects")]
    [Tooltip("Trail renderer for snow spray")]
    [SerializeField] private TrailRenderer snowTrail;
    
    [Tooltip("Particle system for landing impact")]
    [SerializeField] private ParticleSystem landingParticles;
    
    [Header("Animation")]
    [Tooltip("Animator for player sprite")]
    [SerializeField] private Animator playerAnimator;
    
    [Tooltip("Sprite renderer for flipping")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    // Components
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Collider2D playerCollider;
    
    // State
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private float currentSpeedMultiplier = 1f;
    private bool isGrounded;
    private bool isGroundedPrevious;
    private bool isJumping;
    private bool canTrick;
    private float trickTimer;
    private TrickType currentTrick;
    private bool isSpeedBoostActive;
    private bool isInvincible;
    private float invincibleTimer;
    
    // Slope tracking
    private float currentSlopeAngle;
    private Vector2 currentSlopeDirection;
    private float lastGroundedTime;
    
    // Events
    public event Action OnJump;
    public event Action OnLand;
    public event Action<TrickType> OnTrickPerformed;
    public event Action OnCrash;
    public event Action<float> OnSpeedChanged;
    
    // Properties
    public bool IsGrounded => isGrounded;
    public bool IsJumping => isJumping;
    public float CurrentSpeed => rb.linearVelocity.magnitude;
    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public bool IsInvincible => isInvincible;
    public float CurrentSlopeAngle => currentSlopeAngle;
    public Vector2 Velocity => rb.linearVelocity;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerCollider = GetComponent<Collider2D>();
        
        SetupRigidbody();
    }
    
    private void SetupRigidbody()
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    
    private void Update()
    {
        UpdateGroundCheck();
        UpdateTrickWindow();
        UpdateInvincibility();
        UpdateAnimator();
        UpdateEffects();
    }
    
    private void FixedUpdate()
    {
        ApplyCustomGravity();
        HandleHorizontalMovement();
        ApplyFriction();
        ClampVelocity();
    }
    
    private void UpdateGroundCheck()
    {
        isGroundedPrevious = isGrounded;
        
        Vector2 checkOrigin = playerCollider.bounds.center;
        Vector2 checkDirection = Vector2.down;
        
        float checkDistance = groundCheckDistance;
        
        if (Physics2D.Raycast(checkOrigin, checkDirection, checkDistance, groundLayer))
        {
            if (!isGrounded)
            {
                OnLanding();
            }
            isGrounded = true;
            lastGroundedTime = Time.time;
            canTrick = true;
        }
        else
        {
            if (isGrounded && !isGroundedPrevious)
            {
                if (!isJumping)
                {
                    OnCrash?.Invoke();
                }
            }
            isGrounded = false;
        }
    }
    
    private void OnLanding()
    {
        if (isJumping)
        {
            if (trickTimer > 0)
            {
                PerformTrick(currentTrick);
            }
            else
            {
                OnLand?.Invoke();
            }
            
            landingParticles?.Play();
        }
        
        isJumping = false;
        transform.rotation = Quaternion.identity;
    }
    
    private void ApplyCustomGravity()
    {
        Vector2 gravityForce = Vector2.down * gravity;
        
        if (!isGrounded)
        {
            rb.AddForce(gravityForce);
            
            if (Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance * 2f, groundLayer))
            {
                rb.AddForce(gravityForce * slopeGravityMultiplier);
            }
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance * 2f, groundLayer);
            if (hit.collider != null)
            {
                Vector2 surfaceNormal = hit.normal;
                currentSlopeAngle = Vector2.Angle(surfaceNormal, Vector2.up);
                currentSlopeDirection = Vector2.Perpendicular(surfaceNormal).normalized;
                
                float slopeGravity = gravity * Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad);
                rb.AddForce(new Vector2(currentSlopeDirection.x, -1) * slopeGravity);
            }
        }
    }
    
    private void HandleHorizontalMovement()
    {
        float targetVelocityX = moveInput.x * maxMoveSpeed * currentSpeedMultiplier;
        
        if (isGrounded)
        {
            float currentVelocityX = rb.linearVelocity.x;
            float velocityDiff = targetVelocityX - currentVelocityX;
            
            if (moveInput.x != 0f)
            {
                float accelerationForce = acceleration * Mathf.Sign(velocityDiff);
                accelerationForce = Mathf.Clamp(accelerationForce, -Mathf.Abs(velocityDiff) / Time.fixedDeltaTime, Mathf.Abs(velocityDiff) / Time.fixedDeltaTime);
                
                rb.AddForce(Vector2.right * accelerationForce);
            }
            else
            {
                rb.AddForce(Vector2.right * -currentVelocityX * deceleration);
            }
        }
        else
        {
            rb.AddForce(Vector2.right * moveInput.x * acceleration * 0.3f);
        }
    }
    
    private void ApplyFriction()
    {
        if (isGrounded)
        {
            rb.linearVelocity *= snowFriction;
        }
        else
        {
            rb.linearVelocity *= airResistance;
        }
    }
    
    private void ClampVelocity()
    {
        float maxSpeed = maxMoveSpeed * currentSpeedMultiplier;
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
        
        float minSpeed = maxMoveSpeed * minSpeedMultiplier;
        if (isGrounded && rb.linearVelocity.magnitude < minSpeed && moveInput.x == 0)
        {
            // Maintain minimum speed on slopes
        }
    }
    
    private void UpdateTrickWindow()
    {
        if (!isGrounded && !isJumping)
        {
            return;
        }
        
        if (isGrounded && canTrick)
        {
            trickTimer = trickWindowTime;
            canTrick = false;
        }
        
        if (trickTimer > 0f)
        {
            trickTimer -= Time.deltaTime;
        }
    }
    
    private void UpdateInvincibility()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }
    
    private void UpdateAnimator()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            playerAnimator.SetFloat("VerticalVelocity", rb.linearVelocity.y);
            playerAnimator.SetBool("IsGrounded", isGrounded);
            playerAnimator.SetBool("IsJumping", isJumping);
        }
        
        if (spriteRenderer != null && moveInput.x != 0f)
        {
            spriteRenderer.flipX = moveInput.x < 0f;
        }
    }
    
    private void UpdateEffects()
    {
        if (snowTrail != null)
        {
            snowTrail.emitting = isGrounded && Mathf.Abs(rb.linearVelocity.x) > 5f;
        }
    }
    
    #region Input Handling
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void HandleJumpInput(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded && !isJumping)
        {
            Jump();
        }
    }
    
    public void OnTrickLeft(InputAction.CallbackContext context)
    {
        if (context.performed && !isGrounded && canTrick)
        {
            StartTrick(TrickType.Backflip);
        }
    }
    
    public void OnTrickRight(InputAction.CallbackContext context)
    {
        if (context.performed && !isGrounded && canTrick)
        {
            StartTrick(TrickType.Frontflip);
        }
    }
    
    public void OnSpeedUp(InputAction.CallbackContext context)
    {
        if (context.performed && !isSpeedBoostActive)
        {
            isSpeedBoostActive = true;
            currentSpeedMultiplier = Mathf.Clamp(currentSpeedMultiplier + speedChangeRate, minSpeedMultiplier, maxSpeedMultiplier);
            OnSpeedChanged?.Invoke(currentSpeedMultiplier);
        }
        else if (context.canceled)
        {
            isSpeedBoostActive = false;
        }
    }
    
    public void OnSpeedDown(InputAction.CallbackContext context)
    {
        if (context.performed && !isSpeedBoostActive)
        {
            isSpeedBoostActive = true;
            currentSpeedMultiplier = Mathf.Clamp(currentSpeedMultiplier - speedChangeRate, minSpeedMultiplier, maxSpeedMultiplier);
            OnSpeedChanged?.Invoke(currentSpeedMultiplier);
        }
        else if (context.canceled)
        {
            isSpeedBoostActive = false;
        }
    }
    
    #endregion
    
    #region Movement Actions
    
    private void Jump()
    {
        isJumping = true;
        canTrick = true;
        trickTimer = trickWindowTime;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        OnJump?.Invoke();
    }
    
    private void StartTrick(TrickType trick)
    {
        currentTrick = trick;
        canTrick = false;
    }
    
    private void PerformTrick(TrickType trick)
    {
        OnTrickPerformed?.Invoke(trick);
        isJumping = false;
    }
    
    #endregion
    
    #region Public Methods
    
    public void ApplySpeedBoost(float multiplier, float duration)
    {
        currentSpeedMultiplier = Mathf.Clamp(multiplier, minSpeedMultiplier, maxSpeedMultiplier);
        OnSpeedChanged?.Invoke(currentSpeedMultiplier);
        
        Invoke(nameof(ResetSpeedBoost), duration);
    }
    
    private void ResetSpeedBoost()
    {
        currentSpeedMultiplier = 1f;
        OnSpeedChanged?.Invoke(currentSpeedMultiplier);
    }
    
    public void SetInvincible(float duration)
    {
        isInvincible = true;
        invincibleTimer = duration;
    }
    
    public void HandleCollision(Collision2D collision)
    {
        if (isInvincible)
        {
            return;
        }
        
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            float impactForce = collision.relativeVelocity.magnitude;
            
            if (impactForce > 10f)
            {
                OnCrash?.Invoke();
            }
            else
            {
                Vector2 reflectDirection = Vector2.Reflect(rb.linearVelocity, collision.GetContact(0).normal);
                rb.linearVelocity = reflectDirection * 0.5f;
            }
        }
    }
    
    public void TriggerCrash()
    {
        if (!isInvincible)
        {
            OnCrash?.Invoke();
        }
    }
    
    public void AddExternalForce(Vector2 force)
    {
        rb.AddForce(force, ForceMode2D.Impulse);
    }
    
    #endregion
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}

[System.Serializable]
public enum TrickType
{
    None,
    Backflip,
    Frontflip,
    Grab,
    Spin180
}
