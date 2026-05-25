using UnityEngine;

/// <summary>
/// Controller for collectible items like snowflakes, power-ups, and bonuses.
/// Handles collection mechanics, visual feedback, and scoring.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CollectibleController : MonoBehaviour
{
    [Header("Collectible Settings")]
    [SerializeField] protected CollectibleType collectibleType = CollectibleType.Snowflake;
    
    [Tooltip("Value of this collectible")]
    [SerializeField] protected int value = 1;
    
    [Tooltip("Points awarded when collected")]
    [SerializeField] protected int points = 100;
    
    [Header("Collection Settings")]
    [Tooltip("Auto-collect radius")]
    [SerializeField] protected float collectionRadius = 1f;
    
    [Tooltip("Time before collectible respawns (0 = no respawn)")]
    [SerializeField] protected float respawnTime = 0f;
    
    [Tooltip("Can be collected only once")]
    [SerializeField] protected bool oneTimeCollection = true;
    
    [Header("Visual")]
    [SerializeField] protected SpriteRenderer visualRenderer;
    [SerializeField] protected Sprite[] visualSprites;
    
    [Header("Animation")]
    [SerializeField] protected bool enableBobAnimation = true;
    [SerializeField] protected float bobSpeed = 2f;
    [SerializeField] protected float bobHeight = 0.2f;
    [SerializeField] protected bool enableRotation = true;
    [SerializeField] protected float rotationSpeed = 30f;
    
    [Header("Effects")]
    [SerializeField] protected ParticleSystem collectParticles;
    [SerializeField] protected AudioClip collectSound;
    [SerializeField] protected GameObject collectVFX;
    
    // State
    protected bool isCollected;
    protected Vector3 startPosition;
    protected Collider2D triggerCollider;
    protected SpriteRenderer spriteRenderer;
    protected float collectTimer;
    
    // Cached components
    protected Transform cachedTransform;
    
    public CollectibleType Type => collectibleType;
    public virtual void SetCollectibleType(CollectibleType type) { collectibleType = type; }
    public bool IsCollected => isCollected;
    
    protected virtual void Awake()
    {
        cachedTransform = transform;
        startPosition = cachedTransform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }
    
    protected virtual void Start()
    {
        SetupVisuals();
    }
    
    protected virtual void Update()
    {
        if (!isCollected && enableBobAnimation)
        {
            AnimateBob();
        }
        
        if (enableRotation)
        {
            AnimateRotation();
        }
        
        UpdateRespawn();
    }
    
    protected virtual void SetupVisuals()
    {
        if (visualSprites != null && visualSprites.Length > 0)
        {
            int spriteIndex = (int)collectibleType % visualSprites.Length;
            if (spriteRenderer != null && visualSprites[spriteIndex] != null)
            {
                spriteRenderer.sprite = visualSprites[spriteIndex];
            }
        }
        
        if (visualRenderer != null && spriteRenderer == null)
        {
            spriteRenderer = visualRenderer;
        }
    }
    
    protected virtual void AnimateBob()
    {
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        cachedTransform.position = startPosition + Vector3.up * yOffset;
    }
    
    protected virtual void AnimateRotation()
    {
        cachedTransform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return;
        
        if (other.CompareTag("Player"))
        {
            Collect(other.gameObject);
        }
    }
    
    public virtual void Collect(GameObject collector)
    {
        if (isCollected && oneTimeCollection) return;
        
        isCollected = true;
        
        PlayCollectionEffects();
        AwardPoints();
        NotifyGameManager();
        
        if (oneTimeCollection)
        {
            DisableCollectible();
        }
        else
        {
            if (respawnTime > 0)
            {
                StartRespawnTimer();
            }
        }
    }
    
    protected virtual void PlayCollectionEffects()
    {
        PlaySound(collectSound);
        PlayParticleEffects();
        PlayVFX();
        AnimateCollection();
    }
    
    protected virtual void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, cachedTransform.position);
        }
    }
    
    protected virtual void PlayParticleEffects()
    {
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, cachedTransform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + 1f);
        }
    }
    
    protected virtual void PlayVFX()
    {
        if (collectVFX != null)
        {
            GameObject vfx = Instantiate(collectVFX, cachedTransform.position, Quaternion.identity);
            Destroy(vfx, 1f);
        }
    }
    
    protected virtual void AnimateCollection()
    {
        if (spriteRenderer != null)
        {
            StartCoroutine(CollectionAnimationCoroutine());
        }
        else
        {
            DisableCollectible();
        }
    }
    
    protected System.Collections.IEnumerator CollectionAnimationCoroutine()
    {
        Vector3 startScale = cachedTransform.localScale;
        Vector3 endScale = Vector3.zero;
        float duration = 0.2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            cachedTransform.localScale = Vector3.Lerp(startScale, endScale, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f - t;
                spriteRenderer.color = c;
            }
            
            yield return null;
        }
        
        DisableCollectible();
    }
    
    protected virtual void DisableCollectible()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }
    }
    
    protected virtual void AwardPoints()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }
    }
    
    protected virtual void NotifyGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectItem(collectibleType, value);
        }
    }
    
    protected virtual void StartRespawnTimer()
    {
        collectTimer = respawnTime;
        DisableCollectible();
    }
    
    protected virtual void UpdateRespawn()
    {
        if (!isCollected || respawnTime <= 0 || oneTimeCollection) return;
        
        if (collectTimer > 0)
        {
            collectTimer -= Time.deltaTime;
            
            if (collectTimer <= 0)
            {
                Respawn();
            }
        }
    }
    
    protected virtual void Respawn()
    {
        isCollected = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
            cachedTransform.localScale = Vector3.one;
        }
        
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
    }
    
    public virtual void ForceCollect()
    {
        Collect(gameObject);
    }
    
    public void SetValue(int newValue)
    {
        value = newValue;
        points = value * 100;
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
    
    protected virtual Color GetGizmoColor()
    {
        return collectibleType switch
        {
            CollectibleType.Snowflake => Color.cyan,
            CollectibleType.ExtraLife => Color.red,
            CollectibleType.SpeedBoost => Color.yellow,
            CollectibleType.Invincibility => Color.magenta,
            CollectibleType.Shield => Color.blue,
            _ => Color.white
        };
    }
}
