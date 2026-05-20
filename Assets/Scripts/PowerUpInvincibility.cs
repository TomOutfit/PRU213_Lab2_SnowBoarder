using UnityEngine;

/// <summary>
/// Invincibility power-up that makes the player immune to damage.
/// </summary>
public class InvincibilityPowerUp : MonoBehaviour
{
    [Header("Power-up Settings")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private int pointBonus = 100;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Sprite invincibilitySprite;
    [SerializeField] private Color invincibilityColor = Color.magenta;
    [SerializeField] private float pulseSpeed = 2f;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private ParticleSystem shieldEffect;
    [SerializeField] private AudioClip collectSound;
    
    private CollectibleController collectible;
    private SpriteRenderer spriteRenderer;
    private GameObject activeShield;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        collectible = GetComponent<CollectibleController>();
        
        if (collectible != null)
        {
            collectible.SetCollectibleType(CollectibleType.Invincibility);
        }
    }
    
    private void Start()
    {
        SetupVisuals();
    }
    
    private void SetupVisuals()
    {
        if (invincibilitySprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = invincibilitySprite;
            spriteRenderer.color = invincibilityColor;
        }
    }
    
    private void Update()
    {
        if (spriteRenderer != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.2f + 0.8f;
            spriteRenderer.color = new Color(invincibilityColor.r, invincibilityColor.g, invincibilityColor.b, pulse);
        }
    }
    
    public void ApplyPowerUp(PlayerController player)
    {
        if (player != null)
        {
            player.SetInvincible(duration);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(pointBonus);
            }
            
            ShowShieldIndicator();
        }
        
        PlayEffects();
        Destroy(gameObject);
    }
    
    private void ShowShieldIndicator()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowShieldIndicator(true);
            Invoke(nameof(HideShieldIndicator), duration);
        }
    }
    
    private void HideShieldIndicator()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowShieldIndicator(false);
        }
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
