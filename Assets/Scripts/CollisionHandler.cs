using UnityEngine;

/// <summary>
/// Handles collision detection and response between the player and various game objects.
/// Manages ground detection, obstacle collision, and trigger-based interactions.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CollisionHandler : MonoBehaviour
{
    [Header("Collision Layers")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask collectibleLayer;
    [SerializeField] private LayerMask powerUpLayer;
    [SerializeField] private LayerMask rampLayer;
    [SerializeField] private LayerMask triggerLayer;
    
    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundCheckRadius = 0.5f;
    [SerializeField] private bool useCirclecast = true;
    
    [Header("Collision Response")]
    [SerializeField] private bool applyKnockback = true;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float crashRecoveryTime = 0.5f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float minImpactSpeed = 5f;
    
    // References
    private PlayerController player;
    private Collider2D bodyCollider;
    private Rigidbody2D rb;
    private bool isInCrashRecovery;
    
    // Cached contacts
    private ContactPoint2D[] contacts = new ContactPoint2D[4];
    
    public bool IsGrounded { get; private set; }
    public bool IsInCrashRecovery => isInCrashRecovery;
    
    private void Awake()
    {
        player = GetComponent<PlayerController>();
        bodyCollider = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Start()
    {
        SetupCollisionLayers();
    }
    
    private void SetupCollisionLayers()
    {
        gameObject.layer = LayerMask.NameToLayer("Player");
    }
    
    private void Update()
    {
        PerformGroundCheck();
    }
    
    private void FixedUpdate()
    {
        // Physics-based collision processing handled in OnCollisionEnter2D
    }
    
    private void PerformGroundCheck()
    {
        Vector2 origin = bodyCollider.bounds.center;
        Vector2 direction = Vector2.down;
        
        if (useCirclecast)
        {
            RaycastHit2D hit = Physics2D.CircleCast(origin, groundCheckRadius, direction, groundCheckDistance, groundLayer);
            IsGrounded = hit.collider != null;
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, groundCheckDistance, groundLayer);
            IsGrounded = hit.collider != null;
        }
    }
    
    #region Collision Handling
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isInCrashRecovery) return;
        
        if ((groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            HandleGroundCollision(collision);
        }
        else if ((obstacleLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            HandleObstacleCollision(collision);
        }
        else if ((rampLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            HandleRampCollision(collision);
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            IsGrounded = true;
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if ((groundLayer.value & (1 << collision.gameObject.layer)) != 0)
        {
            PerformGroundCheck();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isInCrashRecovery) return;
        
        if ((collectibleLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            HandleCollectibleTrigger(other);
        }
        else if ((powerUpLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            HandlePowerUpTrigger(other);
        }
        else if ((triggerLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            HandleGenericTrigger(other);
        }
    }
    
    #endregion
    
    #region Collision Response
    
    private void HandleGroundCollision(Collision2D collision)
    {
        IsGrounded = true;
        
        collision.GetContacts(contacts);
        for (int i = 0; i < contacts.Length; i++)
        {
            if (Vector2.Dot(contacts[i].normal, Vector2.up) > 0.5f)
            {
                if (rb != null)
                {
                    Vector2 newVelocity = rb.linearVelocity;
                    newVelocity.y = 0;
                    rb.linearVelocity = newVelocity;
                }
                break;
            }
        }
    }
    
    private void HandleObstacleCollision(Collision2D collision)
    {
        if (player != null && player.IsInvincible)
        {
            return;
        }
        
        collision.GetContacts(contacts);
        if (contacts.Length > 0)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            
            if (impactSpeed > minImpactSpeed)
            {
                Vector2 impactDirection = contacts[0].normal;
                
                PlayImpactEffects(contacts[0].point, impactSpeed);
                
                if (applyKnockback)
                {
                    ApplyKnockback(impactDirection, impactSpeed);
                }
                
                StartCrashRecovery();
                
                if (player != null)
                {
                    player.TriggerCrash();
                }
            }
        }
    }
    
    private void HandleRampCollision(Collision2D collision)
    {
        RampController ramp = collision.gameObject.GetComponent<RampController>();
        if (ramp != null)
        {
            Vector2 tangent = collision.GetContact(0).normal;
            tangent = Vector2.Perpendicular(tangent);
            
            if (rb != null)
            {
                float rampBoost = ramp.RampAngle / 45f;
                rb.AddForce(tangent * rampBoost * 10f, ForceMode2D.Impulse);
            }
        }
    }
    
    private void HandleCollectibleTrigger(Collider2D other)
    {
        CollectibleController collectible = other.GetComponent<CollectibleController>();
        if (collectible != null && !collectible.IsCollected)
        {
            collectible.Collect(gameObject);
        }
    }
    
    private void HandlePowerUpTrigger(Collider2D other)
    {
        SpeedBoostPowerUp speedBoost = other.GetComponent<SpeedBoostPowerUp>();
        if (speedBoost != null)
        {
            speedBoost.ApplyPowerUp(player);
            return;
        }
        
        InvincibilityPowerUp invincibility = other.GetComponent<InvincibilityPowerUp>();
        if (invincibility != null)
        {
            invincibility.ApplyPowerUp(player);
            return;
        }
        
        ExtraLifePowerUp extraLife = other.GetComponent<ExtraLifePowerUp>();
        if (extraLife != null)
        {
            extraLife.ApplyPowerUp(player);
        }
    }
    
    private void HandleGenericTrigger(Collider2D other)
    {
        ITriggerHandler handler = other.GetComponent<ITriggerHandler>();
        handler?.OnTriggered(gameObject);
    }
    
    #endregion
    
    #region Effects & Recovery
    
    private void ApplyKnockback(Vector2 direction, float speed)
    {
        if (rb == null || !applyKnockback) return;
        
        Vector2 knockback = direction * knockbackForce * (speed / 10f);
        knockback.y = Mathf.Abs(knockback.y);
        
        rb.AddForce(knockback, ForceMode2D.Impulse);
    }
    
    private void PlayImpactEffects(Vector2 position, float impactSpeed)
    {
        if (impactParticles != null)
        {
            float intensity = Mathf.Clamp01((impactSpeed - minImpactSpeed) / 20f);
            ParticleSystem particles = Instantiate(impactParticles, position, Quaternion.identity);
            var main = particles.main;
            main.startSpeed = Mathf.Lerp(2f, 8f, intensity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }
        
        if (impactSound != null && impactSpeed > minImpactSpeed * 1.5f)
        {
            AudioSource.PlayClipAtPoint(impactSound, position, Mathf.Clamp01(impactSpeed / 30f));
        }
    }
    
    private void StartCrashRecovery()
    {
        if (isInCrashRecovery) return;
        
        isInCrashRecovery = true;
        Invoke(nameof(EndCrashRecovery), crashRecoveryTime);
    }
    
    private void EndCrashRecovery()
    {
        isInCrashRecovery = false;
    }
    
    #endregion
    
    #region Public Methods
    
    public void ForceGroundCheck()
    {
        PerformGroundCheck();
    }
    
    public void SetCollisionLayers(LayerMask ground, LayerMask obstacle, LayerMask collectible)
    {
        groundLayer = ground;
        obstacleLayer = obstacle;
        collectibleLayer = collectible;
    }
    
    public void IgnoreCollisionWith(GameObject obj, bool ignore)
    {
        Collider2D[] colliders = obj.GetComponents<Collider2D>();
        foreach (var col in colliders)
        {
            if (col != null)
            {
                Physics2D.IgnoreCollision(bodyCollider, col, ignore);
            }
        }
    }
    
    public Vector2 GetGroundNormal()
    {
        Vector2 origin = bodyCollider.bounds.center;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance * 2f, groundLayer);
        
        if (hit.collider != null)
        {
            return hit.normal;
        }
        
        return Vector2.up;
    }
    
    public float GetGroundAngle()
    {
        Vector2 normal = GetGroundNormal();
        return Vector2.Angle(normal, Vector2.up);
    }
    
    #endregion
    
    private void OnDrawGizmosSelected()
    {
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider2D>();
        }
        
        if (bodyCollider != null)
        {
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Vector3 center = bodyCollider.bounds.center;
            Vector3 size = new Vector3(groundCheckRadius * 2f, groundCheckDistance + groundCheckRadius * 2f, 0.1f);
            Gizmos.DrawWireCube(center + Vector3.down * (groundCheckDistance / 2f + groundCheckRadius), size);
        }
    }
}

public interface ITriggerHandler
{
    void OnTriggered(GameObject triggeringObject);
}
