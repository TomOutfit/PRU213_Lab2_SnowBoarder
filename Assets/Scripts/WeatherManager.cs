using UnityEngine;
using System.Collections;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance { get; private set; }

    public enum WeatherType { None, LightSnow, WindySnow, Blizzard }

    [Header("Weather Status")]
    public WeatherType activeWeather = WeatherType.None;

    [Header("Wind Physics Settings")]
    public float baseWindForce = 0f;
    public float currentWindForce = 0f;
    private float targetWindForce = 0f;

    /// <summary>True khi đang trong cơn gió giật (Blizzard Gust)</summary>
    public bool IsGustActive { get; private set; } = false;

    /// <summary>Tỉ lệ cường độ gió giật so với base (0 = bình thường, 1 = đỉnh gust)</summary>
    public float GustIntensityRatio => baseWindForce > 0f
        ? Mathf.Clamp01((currentWindForce - baseWindForce) / (baseWindForce * 1.3f))
        : 0f;

    [Header("Camera Shake Settings")]
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0f;
    private Vector3 cameraInitialLocalPos;

    private Camera mainCamera;
    private Rigidbody2D playerRb;
    private WindAudioSynth windAudio;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find player and camera
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = Object.FindAnyObjectByType<Camera>();
        }

        // Configure weather type based on the scene name
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "Level1")
        {
            activeWeather = WeatherType.LightSnow;
            baseWindForce = 0f;
        }
        else if (sceneName == "Level2")
        {
            activeWeather = WeatherType.WindySnow;
            baseWindForce = 6f; // Moderate constant wind
        }
        else if (sceneName == "Level3")
        {
            activeWeather = WeatherType.Blizzard;
            baseWindForce = 12f; // Strong base wind
        }
        else
        {
            activeWeather = WeatherType.None;
            baseWindForce = 0f;
        }

        currentWindForce = baseWindForce;
        targetWindForce = baseWindForce;

        // Initialize Audio Synthesizer for Wind
        if (activeWeather != WeatherType.None)
        {
            GameObject audioObj = new GameObject("WindAudioSynth");
            audioObj.transform.SetParent(this.transform);
            windAudio = audioObj.AddComponent<WindAudioSynth>();

            SetupParticles();
            SetupAtmosphere();

            if (activeWeather == WeatherType.Blizzard)
            {
                StartCoroutine(BlizzardGustLoop());
            }
        }
    }

    void Update()
    {
        // Smoothly interpolate wind force (especially useful during gusts)
        currentWindForce = Mathf.MoveTowards(currentWindForce, targetWindForce, Time.deltaTime * 15f);
    }

    void FixedUpdate()
    {
        // Apply wind force pushing to the left (against the snowboarder)
        if (playerRb != null && GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Playing)
        {
            playerRb.AddForce(Vector2.left * currentWindForce);
        }
        else if (playerRb == null)
        {
            // Try to find the player again if spawned dynamically
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerRb = player.GetComponent<Rigidbody2D>();
            }
        }
    }

    void LateUpdate()
    {
        // Handle screen shake
        if (shakeDuration > 0f && mainCamera != null)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0f; // Keep the camera in 2D plane
            mainCamera.transform.position += shakeOffset;
            shakeDuration -= Time.deltaTime;
        }
    }

    void SetupParticles()
    {
        GameObject psObj = new GameObject("WeatherParticles");
        psObj.transform.SetParent(this.transform);
        psObj.transform.localPosition = new Vector3(0, 12, 0);

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();
        
        // Stop the system before editing its main properties to prevent any playing state warnings
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 1. Configure Main settings
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 4.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.35f);
        main.gravityModifier = 0.05f;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Snow stays in place
        main.maxParticles = 2500;

        // 2. Configure Emission settings
        var emission = ps.emission;
        emission.enabled = true;

        // 3. Configure Shape settings (Wide horizontal bar above screen)
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(45f, 1f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f); // Face down

        // 4. Configure Velocity over Lifetime (horizontal drift/wind)
        // Note: All active axes in VelocityOverLifetime must use the same MinMaxCurve mode (TwoConstants)
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;

        if (activeWeather == WeatherType.LightSnow)
        {
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(60f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
            velocity.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            velocity.y = new ParticleSystem.MinMaxCurve(-3f, -5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }
        else if (activeWeather == WeatherType.WindySnow)
        {
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(220f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
            velocity.x = new ParticleSystem.MinMaxCurve(-7f, -11f); // Angle left
            velocity.y = new ParticleSystem.MinMaxCurve(-6f, -10f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
        }
        else if (activeWeather == WeatherType.Blizzard)
        {
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(550f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 22f);
            velocity.x = new ParticleSystem.MinMaxCurve(-18f, -26f); // Heavy blow left
            velocity.y = new ParticleSystem.MinMaxCurve(-7f, -14f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.45f);
        }

        // Apply dynamic soft snowflake texture
        var psr = psObj.GetComponent<ParticleSystemRenderer>();
        psr.material = CreateSnowMaterial();

        // Start emitter positioning coroutine to follow camera
        StartCoroutine(FollowCameraViewport(psObj.transform));

        ps.Play();
    }

    Material CreateSnowMaterial()
    {
        // Generate soft round pixel map dynamically
        Texture2D tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f;
                float dy = y - 15.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - (dist / 15.5f));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
        }
        tex.Apply();

        // Create standard sprites material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        if (mat == null || mat.shader == null)
        {
            // Fallback for standard or basic shaders
            mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        }
        
        if (mat != null)
        {
            mat.mainTexture = tex;
        }
        return mat;
    }

    IEnumerator FollowCameraViewport(Transform t)
    {
        while (true)
        {
            if (mainCamera != null)
            {
                Vector3 camPos = mainCamera.transform.position;
                // Add lead offset ahead of player movement (+X)
                float lead = (activeWeather == WeatherType.Blizzard) ? 14f : 8f;
                t.position = new Vector3(camPos.x + lead, camPos.y + 11f, 0f);
            }
            yield return null;
        }
    }

    void SetupAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        Color fogColor;
        float fogDensity;
        Color camClearColor;

        switch (activeWeather)
        {
            case WeatherType.LightSnow:
                fogColor = new Color(0.85f, 0.9f, 0.95f);
                fogDensity = 0.012f;
                camClearColor = new Color(0.72f, 0.8f, 0.88f);
                if (windAudio != null) windAudio.targetVolume = 0.04f;
                break;

            case WeatherType.WindySnow:
                fogColor = new Color(0.76f, 0.81f, 0.86f);
                fogDensity = 0.026f;
                camClearColor = new Color(0.58f, 0.66f, 0.74f);
                if (windAudio != null)
                {
                    windAudio.targetVolume = 0.26f;
                    windAudio.targetPitch = 1.0f;
                }
                break;

            case WeatherType.Blizzard:
                fogColor = new Color(0.66f, 0.7f, 0.74f);
                fogDensity = 0.046f;
                camClearColor = new Color(0.46f, 0.52f, 0.6f);
                if (windAudio != null)
                {
                    windAudio.targetVolume = 0.46f;
                    windAudio.targetPitch = 1.08f;
                }
                break;

            default:
                RenderSettings.fog = false;
                return;
        }

        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = camClearColor;
        }
    }

    IEnumerator BlizzardGustLoop()
    {
        while (activeWeather == WeatherType.Blizzard)
        {
            yield return new WaitForSeconds(Random.Range(7f, 12f));

            if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Playing)
            {
                yield return StartCoroutine(TriggerGust());
            }
        }
    }

    IEnumerator TriggerGust()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowNotification("WARNING: GIÓ GIẬT CỰC MẠNH! (BLIZZARD GUST!)");
        }

        IsGustActive = true;
        float originalWindForce = baseWindForce;
        targetWindForce = originalWindForce * 2.3f; // Significant force increase

        if (windAudio != null)
        {
            windAudio.targetVolume = 0.82f;
            windAudio.targetPitch = 1.75f; // Whistle howling pitch
        }

        // Screen shake parameters
        shakeDuration = 1.8f;
        shakeMagnitude = 0.28f;

        yield return new WaitForSeconds(1.8f);

        // Reset to normal Blizzard conditions
        IsGustActive = false;
        targetWindForce = originalWindForce;
        if (windAudio != null)
        {
            windAudio.targetVolume = 0.46f;
            windAudio.targetPitch = 1.08f;
        }
    }

    void OnDisable()
    {
        // Clean up global render settings
        RenderSettings.fog = false;
    }
}

// Programmatic synthesizer for realistic Mountain Wind howling audio
public class WindAudioSynth : MonoBehaviour
{
    public float targetVolume = 0f;
    private float currentVolume = 0f;
    public float targetPitch = 1.0f;
    private float currentPitch = 1.0f;

    private double phase;
    private double sampleRate;
    private System.Random random = new System.Random();

    // Lowpass filter memory variables
    private float lastFilterVal1 = 0f;
    private float lastFilterVal2 = 0f;

    void Awake()
    {
        AudioSource audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D Stereo

        // Create silent source trigger clip
        audioSource.clip = AudioClip.Create("SilenceWindTrigger", 44100, 1, 44100, false);
        audioSource.Play();

        sampleRate = AudioSettings.outputSampleRate;
        if (sampleRate <= 0) sampleRate = 44100;
    }

    void Update()
    {
        // Smooth audio transition logic
        currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, Time.deltaTime * 0.45f);
        currentPitch = Mathf.MoveTowards(currentPitch, targetPitch, Time.deltaTime * 0.45f);
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentVolume <= 0.001f)
        {
            System.Array.Clear(data, 0, data.Length);
            return;
        }

        // Base frequency calculation for resonance
        float cutoff = 380f + currentPitch * 280f;

        float rc = 1.0f / (2.0f * Mathf.PI * cutoff);
        float dt = 1.0f / (float)sampleRate;
        float filterCoefficient = dt / (rc + dt);

        for (int i = 0; i < data.Length; i += channels)
        {
            // Generate standard white noise sample
            float noiseVal = (float)(random.NextDouble() * 2.0 - 1.0);

            // Double pole low-pass filters to form whistling noise
            lastFilterVal1 = lastFilterVal1 + filterCoefficient * (noiseVal - lastFilterVal1);
            lastFilterVal2 = lastFilterVal2 + filterCoefficient * (lastFilterVal1 - lastFilterVal2);

            // High pitch howling/resonance oscillation
            phase += 2.0 * System.Math.PI * (cutoff + 120f * Mathf.Sin((float)phase * 0.005f)) / sampleRate;
            if (phase > 2.0 * System.Math.PI)
            {
                phase -= 2.0 * System.Math.PI;
            }
            float whistleOscillator = (float)System.Math.Sin(phase) * 0.14f;

            // Combine base storm noise and howling whistle
            float windOutput = (lastFilterVal2 + whistleOscillator) * currentVolume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = windOutput;
            }
        }
    }
}
