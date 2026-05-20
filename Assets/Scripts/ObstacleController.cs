using UnityEngine;

/// <summary>
/// Base class for all obstacles in the game.
/// Handles collision detection and damage application to the player.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ObstacleController : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Type of obstacle for variety")]
    [SerializeField] protected ObstacleType obstacleType = ObstacleType.Rock;
    
    [Tooltip("Damage dealt to player on collision")]
    [SerializeField] protected int damage = 1;
    
    [Tooltip("Speed penalty when hitting this obstacle")]
    [SerializeField] protected float speedPenalty = 0.3f;
    
    [Tooltip("Can this obstacle be destroyed?")]
    [SerializeField] protected bool indestructible = true;
    
    [Tooltip("Health for destructible obstacles")]
    [SerializeField] protected int health = 1;
    
    [Tooltip("Points awarded for destroying")]
    [SerializeField] protected int destructionPoints = 50;
    
    [Header("Visual")]
    [SerializeField] protected SpriteRenderer visualRenderer;
    
    [Header("Effects")]
    [SerializeField] protected ParticleSystem destructionParticles;
    [SerializeField] protected AudioClip hitSound;
    [SerializeField] protected AudioClip destructionSound;
    
    // State
    protected int currentHealth;
    protected bool isDestroyed;
    protected AudioSource audioSource;
    
    public ObstacleType Type => obstacleType;
    public bool IsDestroyed => isDestroyed;
    
    protected virtual void Awake()
    {
        currentHealth = health;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (hitSound != null || destructionSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    protected virtual void Start()
    {
        SetupColliders();
    }
    
    protected virtual void SetupColliders()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = false;
        }
    }
    
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDestroyed) return;
        
        if (collision.gameObject.CompareTag("Player"))
        {
            HandlePlayerCollision(collision);
        }
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isDestroyed) return;
        
        if (other.CompareTag("Player"))
        {
            HandlePlayerTrigger(other);
        }
        else if (other.CompareTag("Projectile"))
        {
            HandleProjectileHit(other);
        }
    }
    
    protected virtual void HandlePlayerCollision(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && !player.IsInvincible)
        {
            ApplyDamage(player);
            ApplySpeedPenalty(player);
            PlaySound(hitSound);
        }
    }
    
    protected virtual void HandlePlayerTrigger(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && !player.IsInvincible)
        {
            ApplyDamage(player);
            ApplySpeedPenalty(player);
            PlaySound(hitSound);
        }
    }
    
    protected virtual void ApplyDamage(PlayerController player)
    {
        if (!indestructible)
        {
            currentHealth--;
            if (currentHealth <= 0)
            {
                DestroyObstacle();
            }
            else
            {
                FlashOnHit();
            }
        }
        
        player.TriggerCrash();
    }
    
    protected virtual void ApplySpeedPenalty(PlayerController player)
    {
        player.ApplySpeedBoost(speedPenalty, 2f);
    }
    
    protected virtual void HandleProjectileHit(Collider2D projectile)
    {
        if (!indestructible)
        {
            currentHealth--;
            Destroy(projectile.gameObject);
            
            if (currentHealth <= 0)
            {
                DestroyObstacle();
            }
            else
            {
                FlashOnHit();
            }
        }
    }
    
    protected virtual void FlashOnHit()
    {
        if (visualRenderer != null)
        {
            StartCoroutine(FlashCoroutine());
        }
    }
    
    protected System.Collections.IEnumerator FlashCoroutine()
    {
        Color originalColor = visualRenderer.color;
        visualRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        visualRenderer.color = originalColor;
    }
    
    protected virtual void DestroyObstacle()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(destructionPoints);
        }
        
        PlaySound(destructionSound);
        PlayDestructionEffects();
        
        gameObject.SetActive(false);
        Destroy(gameObject, 0.5f);
    }
    
    protected virtual void PlayDestructionEffects()
    {
        if (destructionParticles != null)
        {
            ParticleSystem particles = Instantiate(destructionParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + 2f);
        }
    }
    
    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (active)
        {
            isDestroyed = false;
            currentHealth = health;
        }
    }
}

public enum ObstacleType
{
    Rock,
    Tree,
    IceBlock,
    Log,
    Ramp,
    Jump,
    Wall
}
