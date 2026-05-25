using UnityEngine;

/// <summary>
/// Special obstacle type that acts as a ramp, launching the player into the air.
/// Used for performing tricks and accessing shortcuts.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RampController : MonoBehaviour
{
    [Header("Ramp Settings")]
    [Tooltip("Direction the player will be launched")]
    [SerializeField] private Vector2 launchDirection = new Vector2(1f, 1f);
    
    [Tooltip("Force multiplier for the launch")]
    [SerializeField] private float launchForce = 15f;
    
    [Tooltip("Minimum player speed required to use ramp")]
    [SerializeField] private float minimumSpeed = 5f;
    
    [Tooltip("Angle of the ramp in degrees")]
    [Range(15f, 75f)]
    [SerializeField] private float rampAngle = 30f;
    
    [Header("Trick Zones")]
    [Tooltip("Points awarded for landing a trick after using this ramp")]
    [SerializeField] private int trickBonusPoints = 100;
    
    [Tooltip("Allow trick initiation when using this ramp")]
    [SerializeField] private bool allowTricks = true;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer rampVisual;
    [SerializeField] private Sprite[] rampSprites;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem launchParticles;
    [SerializeField] private TrailRenderer launchTrail;
    [SerializeField] private AudioClip launchSound;
    
    // Components
    private Collider2D rampCollider;
    private SpriteRenderer spriteRenderer;
    private bool wasUsedRecently;
    
    // Cached values
    private Transform cachedTransform;
    
    public float RampAngle => rampAngle;
    public bool AllowTricks => allowTricks;
    public int TrickBonusPoints => trickBonusPoints;
    
    private void Awake()
    {
        cachedTransform = transform;
        rampCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (rampCollider != null)
        {
            rampCollider.isTrigger = true;
        }
        
        CalculateLaunchDirection();
    }
    
    private void Start()
    {
        SetupVisuals();
    }
    
    private void CalculateLaunchDirection()
    {
        float angleRad = rampAngle * Mathf.Deg2Rad;
        launchDirection = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
    }
    
    private void SetupVisuals()
    {
        if (rampSprites != null && rampSprites.Length > 0)
        {
            int spriteIndex = Mathf.Clamp(Mathf.RoundToInt(rampAngle / 15f) - 1, 0, rampSprites.Length - 1);
            if (spriteRenderer != null && rampSprites[spriteIndex] != null)
            {
                spriteRenderer.sprite = rampSprites[spriteIndex];
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                TryLaunch(player);
            }
        }
    }
    
    private void TryLaunch(PlayerController player)
    {
        if (wasUsedRecently) return;
        
        float playerSpeed = player.CurrentSpeed;
        
        if (playerSpeed >= minimumSpeed)
        {
            LaunchPlayer(player);
        }
        else
        {
            Debug.Log($"Ramp requires speed of {minimumSpeed} or more. Current: {playerSpeed}");
        }
    }
    
    private void LaunchPlayer(PlayerController player)
    {
        wasUsedRecently = true;
        
        Vector2 launchVector = launchDirection * launchForce;
        launchVector.x *= Mathf.Sign(cachedTransform.lossyScale.x);
        
        if (player.CurrentSpeed > launchForce * 0.5f)
        {
            launchVector += new Vector2(player.Velocity.x * 0.5f, 0);
        }
        
        player.AddExternalForce(launchVector);
        
        PlayLaunchEffects();
        
        if (allowTricks)
        {
            StartCoroutine(ResetUsedState());
        }
        
        Debug.Log($"Launched player with force: {launchVector}");
    }
    
    private System.Collections.IEnumerator ResetUsedState()
    {
        yield return new WaitForSeconds(0.5f);
        wasUsedRecently = false;
    }
    
    private void PlayLaunchEffects()
    {
        if (launchParticles != null)
        {
            ParticleSystem particles = Instantiate(launchParticles, cachedTransform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration + 1f);
        }
        
        if (launchTrail != null)
        {
            launchTrail.emitting = true;
            Invoke(nameof(StopTrail), 0.5f);
        }
        
        if (launchSound != null)
        {
            AudioSource.PlayClipAtPoint(launchSound, cachedTransform.position);
        }
        
        if (rampVisual != null)
        {
            StartCoroutine(FlashRamp());
        }
    }
    
    private System.Collections.IEnumerator FlashRamp()
    {
        Color original = rampVisual.color;
        rampVisual.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        rampVisual.color = original;
    }
    
    private void StopTrail()
    {
        if (launchTrail != null)
        {
            launchTrail.emitting = false;
        }
    }
    
    public void SetRampAngle(float angle)
    {
        rampAngle = Mathf.Clamp(angle, 15f, 75f);
        CalculateLaunchDirection();
        SetupVisuals();
    }
    
    public void SetLaunchForce(float force)
    {
        launchForce = Mathf.Max(0f, force);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 direction = new Vector3(launchDirection.x, launchDirection.y, 0);
        if (Application.isPlaying)
        {
            direction.x *= Mathf.Sign(cachedTransform.lossyScale.x);
        }
        
        Gizmos.DrawRay(cachedTransform.position, direction * 3f);
        
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawCube(cachedTransform.position, Vector3.one * 0.5f);
    }
}
