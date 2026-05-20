using UnityEngine;

/// <summary>
/// Provides visual feedback for player actions like landing, crashing, and collecting items.
/// Includes snow spray, impact effects, and speed lines.
/// </summary>
public class PlayerEffectsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    
    [Header("Snow Spray")]
    [SerializeField] private ParticleSystem snowSprayParticles;
    [SerializeField] private TrailRenderer snowTrail;
    [SerializeField] private float trailMinSpeed = 5f;
    
    [Header("Landing Impact")]
    [SerializeField] private ParticleSystem landingImpactParticles;
    [SerializeField] private float minLandingSpeed = 8f;
    
    [Header("Speed Lines")]
    [SerializeField] private LineRenderer[] speedLines;
    [SerializeField] private float speedLinesMinSpeed = 15f;
    [SerializeField] private float speedLinesMaxSpeed = 30f;
    
    [Header("Crash Effects")]
    [SerializeField] private ParticleSystem crashParticles;
    [SerializeField] private float crashDuration = 1f;
    [SerializeField] private AudioClip crashSound;
    
    [Header("Collection Effects")]
    [SerializeField] private ParticleSystem collectionParticles;
    
    // Cached state
    private bool wasAirborne;
    private float lastLandingSpeed;
    
    private void Awake()
    {
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }
    }
    
    private void Start()
    {
        SubscribeToPlayerEvents();
        InitializeEffects();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromPlayerEvents();
    }
    
    private void SubscribeToPlayerEvents()
    {
        if (player != null)
        {
            player.OnJump += HandleJump;
            player.OnLand += HandleLand;
            player.OnCrash += HandleCrash;
            player.OnTrickPerformed += HandleTrick;
            player.OnSpeedChanged += HandleSpeedChange;
        }
    }
    
    private void UnsubscribeFromPlayerEvents()
    {
        if (player != null)
        {
            player.OnJump -= HandleJump;
            player.OnLand -= HandleLand;
            player.OnCrash -= HandleCrash;
            player.OnTrickPerformed -= HandleTrick;
            player.OnSpeedChanged -= HandleSpeedChange;
        }
    }
    
    private void InitializeEffects()
    {
        SetTrailActive(false);
        SetSpeedLinesActive(false);
    }
    
    private void Update()
    {
        UpdateSpeedLines();
        UpdateTrail();
    }
    
    private void UpdateTrail()
    {
        if (player == null) return;
        
        bool shouldShowTrail = player.IsGrounded && player.CurrentSpeed >= trailMinSpeed;
        SetTrailActive(shouldShowTrail);
    }
    
    private void UpdateSpeedLines()
    {
        if (speedLines == null || speedLines.Length == 0) return;
        if (player == null) return;
        
        float speed = player.CurrentSpeed;
        float t = Mathf.InverseLerp(speedLinesMinSpeed, speedLinesMaxSpeed, speed);
        
        foreach (var line in speedLines)
        {
            if (line != null)
            {
                line.enabled = speed >= speedLinesMinSpeed;
                Color lineColor = line.startColor;
                lineColor.a = t * 0.5f;
                line.startColor = lineColor;
                line.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
            }
        }
    }
    
    #region Event Handlers
    
    private void HandleJump()
    {
        wasAirborne = true;
        SetTrailActive(false);
    }
    
    private void HandleLand()
    {
        if (wasAirborne)
        {
            lastLandingSpeed = player.CurrentSpeed;
            
            if (lastLandingSpeed >= minLandingSpeed)
            {
                PlayLandingImpact();
            }
            
            wasAirborne = false;
        }
    }
    
    private void HandleCrash()
    {
        PlayCrashEffect();
    }
    
    private void HandleTrick(TrickType trick)
    {
        PlayTrickEffect(trick);
    }
    
    private void HandleSpeedChange(float speedPercent)
    {
        // Visual feedback for speed changes
    }
    
    #endregion
    
    #region Effect Methods
    
    private void SetTrailActive(bool active)
    {
        if (snowTrail != null)
        {
            snowTrail.emitting = active;
        }
    }
    
    private void SetSpeedLinesActive(bool active)
    {
        if (speedLines != null)
        {
            foreach (var line in speedLines)
            {
                if (line != null)
                {
                    line.enabled = active;
                }
            }
        }
    }
    
    private void PlayLandingImpact()
    {
        if (landingImpactParticles != null)
        {
            ParticleSystem impact = Instantiate(landingImpactParticles, transform.position, Quaternion.identity);
            impact.Play();
            Destroy(impact.gameObject, impact.main.duration + 2f);
        }
    }
    
    private void PlayCrashEffect()
    {
        if (crashParticles != null)
        {
            ParticleSystem crash = Instantiate(crashParticles, transform.position, Quaternion.identity);
            crash.Play();
            Destroy(crash.gameObject, crashDuration);
        }
        
        if (crashSound != null)
        {
            AudioSource.PlayClipAtPoint(crashSound, transform.position);
        }
        
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayCrashSound();
        }
        
        StartCoroutine(CrashFlashCoroutine());
    }
    
    private System.Collections.IEnumerator CrashFlashCoroutine()
    {
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            Color original = sprite.color;
            for (int i = 0; i < 3; i++)
            {
                sprite.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                sprite.color = original;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    private void PlayTrickEffect(TrickType trick)
    {
        if (collectionParticles != null)
        {
            ParticleSystem trickVFX = Instantiate(collectionParticles, transform.position, Quaternion.identity);
            trickVFX.Play();
            Destroy(trickVFX.gameObject, 2f);
        }
    }
    
    public void PlayCollectionEffect(Vector3 position)
    {
        if (collectionParticles != null)
        {
            ParticleSystem collect = Instantiate(collectionParticles, position, Quaternion.identity);
            collect.Play();
            Destroy(collect.gameObject, 2f);
        }
    }
    
    public void TriggerSpeedBoostVFX()
    {
        if (snowTrail != null)
        {
            snowTrail.emitting = true;
            Invoke(nameof(StopTrail), 0.5f);
        }
    }
    
    private void StopTrail()
    {
        if (snowTrail != null)
        {
            snowTrail.emitting = false;
        }
    }
    
    #endregion
}
