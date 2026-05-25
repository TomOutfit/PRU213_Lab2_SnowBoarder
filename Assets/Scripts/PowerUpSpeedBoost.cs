using UnityEngine;

/// <summary>
/// Speed boost power-up that temporarily increases player movement speed.
/// </summary>
public class SpeedBoostPowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private int pointBonus = 50;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Sprite speedBoostSprite;
    [SerializeField] private Color boostColor = Color.yellow;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private AudioClip collectSound;
    
    private CollectibleController collectible;
    private SpriteRenderer spriteRenderer;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collectible = GetComponent<CollectibleController>();
        
        if (collectible != null)
        {
            collectible.SetCollectibleType(CollectibleType.SpeedBoost);
        }
    }
    
    private void Start()
    {
        SetupVisuals();
    }
    
    private void SetupVisuals()
    {
        if (speedBoostSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = speedBoostSprite;
            spriteRenderer.color = boostColor;
        }
    }
    
    public void ApplyPowerUp(PlayerController player)
    {
        if (player != null)
        {
            player.ApplySpeedBoost(speedMultiplier, duration);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(pointBonus);
            }
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
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyPowerUp(other.GetComponent<PlayerController>());
        }
    }
}
