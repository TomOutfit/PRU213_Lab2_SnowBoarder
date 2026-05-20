using UnityEngine;

/// <summary>
/// Extra life power-up that grants additional player lives.
/// </summary>
public class ExtraLifePowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    [SerializeField] private int livesToAdd = 1;
    [SerializeField] private int pointBonus = 75;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Sprite extraLifeSprite;
    [SerializeField] private Color heartColor = Color.red;
    
    [Header("Animation")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.15f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private AudioClip heartBeatSound;
    
    private CollectibleController collectible;
    private SpriteRenderer spriteRenderer;
    private Vector3 startPosition;
    private float rotationAngle;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collectible = GetComponent<CollectibleController>();
        
        if (collectible != null)
        {
            collectible.SetCollectibleType(CollectibleType.ExtraLife);
        }
        
        startPosition = transform.position;
    }
    
    private void Start()
    {
        SetupVisuals();
    }
    
    private void SetupVisuals()
    {
        if (extraLifeSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = extraLifeSprite;
            spriteRenderer.color = heartColor;
        }
    }
    
    private void Update()
    {
        AnimateBob();
        AnimatePulse();
    }
    
    private void AnimateBob()
    {
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = startPosition + Vector3.up * yOffset;
    }
    
    private void AnimatePulse()
    {
        rotationAngle += Time.deltaTime * 30f;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 2f) * 5f);
    }
    
    public void ApplyPowerUp(PlayerController player)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CollectItem(CollectibleType.ExtraLife, livesToAdd);
            GameManager.Instance.AddScore(pointBonus);
        }
        
        PlayEffects();
        Destroy(gameObject);
    }
    
    private void PlayEffects()
    {
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        if (heartBeatSound != null)
        {
            Invoke(nameof(PlayHeartBeat), 0.2f);
        }
    }
    
    private void PlayHeartBeat()
    {
        AudioSource.PlayClipAtPoint(heartBeatSound, transform.position);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyPowerUp(other.GetComponent<PlayerController>());
        }
    }
}
