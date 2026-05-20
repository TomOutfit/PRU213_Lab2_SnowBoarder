using UnityEngine;

/// <summary>
/// Creates a dynamic snowfall effect with particle system.
/// Falls relative to camera movement for immersive experience.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class SnowfallEffect : MonoBehaviour
{
    [Header("Snow Settings")]
    [SerializeField] private int snowflakeCount = 200;
    [SerializeField] private float snowflakeSizeMin = 0.05f;
    [SerializeField] private float snowflakeSizeMax = 0.2f;
    [SerializeField] private float fallSpeedMin = 1f;
    [SerializeField] private float fallSpeedMax = 3f;
    [SerializeField] private float driftSpeed = 0.5f;
    
    [Header("Area Settings")]
    [SerializeField] private Vector2 spawnAreaSize = new Vector2(50f, 30f);
    [SerializeField] private float spawnHeightAboveCamera = 10f;
    [SerializeField] private Transform cameraFollowTarget;
    
    [Header("Wind Effect")]
    [SerializeField] private bool enableWind = true;
    [SerializeField] private float windChangeInterval = 5f;
    [SerializeField] private float maxWindStrength = 2f;
    [SerializeField] private float windStrengthVariation = 0.5f;
    
    // Components
    private ParticleSystem particles;
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.EmissionModule emissionModule;
    private ParticleSystem.ShapeModule shapeModule;
    private ParticleSystem.VelocityOverLifetimeModule velocityModule;
    
    // Wind state
    private float currentWindDirection = 1f;
    private float targetWindDirection = 1f;
    private float windChangeTimer;
    
    // Camera tracking
    private Vector3 lastCameraPosition;
    
    private void Awake()
    {
        particles = GetComponent<ParticleSystem>();
        CacheModules();
    }
    
    private void Start()
    {
        SetupParticleSystem();
        FindCamera();
    }
    
    private void CacheModules()
    {
        mainModule = particles.main;
        emissionModule = particles.emission;
        shapeModule = particles.shape;
        velocityModule = particles.velocityOverLifetime;
    }
    
    private void SetupParticleSystem()
    {
        mainModule.maxParticles = snowflakeCount * 2;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
        mainModule.gravityModifier = 1f;
        
        emissionModule.rateOverTime = snowflakeCount * 0.5f;
        
        shapeModule.shapeType = ParticleSystemShapeType.Box;
        shapeModule.scale = new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f);
        
        SetupParticleColor();
        SetupParticleSize();
        SetupParticleMovement();
    }
    
    private void SetupParticleColor()
    {
        ParticleSystem.MinMaxGradient gradient = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 1f, 0.8f),
            new Color(0.9f, 0.95f, 1f, 0.4f)
        );
        mainModule.startColor = gradient;
    }
    
    private void SetupParticleSize()
    {
        ParticleSystem.MinMaxCurve sizeCurve = new ParticleSystem.MinMaxCurve();
        sizeCurve.mode = ParticleSystemCurveMode.TwoConstants;
        sizeCurve.constantMin = snowflakeSizeMin;
        sizeCurve.constantMax = snowflakeSizeMax;
        mainModule.startSize = sizeCurve;
    }
    
    private void SetupParticleMovement()
    {
        velocityModule.space = ParticleSystemSimulationSpace.World;
        
        ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve();
        xVelocity.constant = driftSpeed;
        velocityModule.x = xVelocity;
        
        ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve();
        yVelocity.mode = ParticleSystemCurveMode.TwoConstants;
        yVelocity.constantMin = -fallSpeedMin;
        yVelocity.constantMax = -fallSpeedMax;
        velocityModule.y = yVelocity;
    }
    
    private void FindCamera()
    {
        if (cameraFollowTarget == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cameraFollowTarget = cam.transform;
            }
        }
    }
    
    private void Update()
    {
        UpdateParticlePosition();
        UpdateWind();
    }
    
    private void UpdateParticlePosition()
    {
        if (cameraFollowTarget != null)
        {
            Vector3 cameraPos = cameraFollowTarget.position;
            transform.position = new Vector3(
                cameraPos.x,
                cameraPos.y + spawnHeightAboveCamera,
                0f
            );
            
            lastCameraPosition = cameraPos;
        }
    }
    
    private void UpdateWind()
    {
        if (!enableWind) return;
        
        windChangeTimer += Time.deltaTime;
        
        if (windChangeTimer >= windChangeInterval)
        {
            windChangeTimer = 0f;
            targetWindDirection = Random.Range(-1f, 1f);
        }
        
        currentWindDirection = Mathf.Lerp(currentWindDirection, targetWindDirection, Time.deltaTime * windStrengthVariation);
        
        ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve();
        xVelocity.constant = currentWindDirection * driftSpeed * maxWindStrength;
        velocityModule.x = xVelocity;
    }
    
    public void SetIntensity(float multiplier)
    {
        emissionModule.rateOverTime = snowflakeCount * 0.5f * multiplier;
    }
    
    public void SetSnowflakeSize(float minSize, float maxSize)
    {
        snowflakeSizeMin = minSize;
        snowflakeSizeMax = maxSize;
        SetupParticleSize();
    }
    
    public void SetWindStrength(float strength)
    {
        maxWindStrength = Mathf.Max(0f, strength);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);
        
        Vector3 center = transform.position;
        Vector3 size = new Vector3(spawnAreaSize.x, spawnAreaSize.y, 1f);
        
        Gizmos.DrawWireCube(center, size);
    }
}
