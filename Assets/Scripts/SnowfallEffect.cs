using UnityEngine;
using System.Collections.Generic;

public class SnowfallEffect : MonoBehaviour
{
    [Header("Snowflake Settings")]
    [SerializeField] private int snowflakeCount = 100;
    [SerializeField] private GameObject snowflakePrefab;
    [SerializeField] private float spawnAreaWidth = 20f;
    [SerializeField] private float spawnAreaHeight = 15f;
    [SerializeField] private float fallSpeedMin = 1f;
    [SerializeField] private float fallSpeedMax = 4f;
    [SerializeField] private float driftSpeedMin = -0.5f;
    [SerializeField] private float driftSpeedMax = 0.5f;
    [SerializeField] private float windStrength = 0.5f;
    [SerializeField] private float windChangeInterval = 5f;

    [Header("Scaling")]
    [SerializeField] private float scaleMin = 0.3f;
    [SerializeField] private float scaleMax = 1f;

    [Header("Visibility Zone")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float followPlayerX = 0f;
    [SerializeField] private float followPlayerY = 0f;

    [Header("Weather Intensity")]
    [Range(0f, 1f)]
    [SerializeField] private float weatherIntensity = 0.7f;
    [SerializeField] private float intensityTransitionSpeed = 0.5f;

    private List<Snowflake> snowflakes = new();
    private Vector3 spawnOrigin;
    private float windDirection = 1f;
    private float windTimer;
    private float currentIntensity;
    private Camera mainCamera;

    private class Snowflake
    {
        public GameObject gameObject;
        public float fallSpeed;
        public float driftSpeed;
        public float scale;
        public float rotationSpeed;
        public float opacity;
        public float x;
        public float y;
    }

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        currentIntensity = weatherIntensity;
        spawnOrigin = transform.position;
        InitializeSnowflakes();
    }

    private void Update()
    {
        UpdateWind();
        UpdateSnowfall();
        FollowPlayer();
    }

    private void InitializeSnowflakes()
    {
        for (int i = 0; i < snowflakeCount; i++)
        {
            SpawnSnowflake(Random.Range(-spawnAreaWidth / 2f, spawnAreaWidth / 2f),
                           Random.Range(-spawnAreaHeight / 2f, spawnAreaHeight / 2f));
        }
    }

    private void SpawnSnowflake(float x, float y)
    {
        GameObject obj;
        if (snowflakePrefab != null)
        {
            obj = Instantiate(snowflakePrefab, spawnOrigin + new Vector3(x, y, 0), Quaternion.identity, transform);
        }
        else
        {
            obj = new GameObject("Snowflake");
            obj.transform.parent = transform;
            obj.transform.position = spawnOrigin + new Vector3(x, y, 0);
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0.8f);
        }

        float scale = Random.Range(scaleMin, scaleMax);
        obj.transform.localScale = Vector3.one * scale;

        snowflakes.Add(new Snowflake
        {
            gameObject = obj,
            fallSpeed = Random.Range(fallSpeedMin, fallSpeedMax),
            driftSpeed = Random.Range(driftSpeedMin, driftSpeedMax),
            scale = scale,
            rotationSpeed = Random.Range(-180f, 180f),
            opacity = Random.Range(0.4f, 0.9f),
            x = x,
            y = y
        });
    }

    private void UpdateWind()
    {
        windTimer += Time.deltaTime;
        if (windTimer >= windChangeInterval)
        {
            windTimer = 0f;
            windDirection = Random.Range(-1f, 1f);
        }

        currentIntensity = Mathf.Lerp(currentIntensity, weatherIntensity, intensityTransitionSpeed * Time.deltaTime);
    }

    private void UpdateSnowfall()
    {
        float halfWidth = spawnAreaWidth / 2f;
        float halfHeight = spawnAreaHeight / 2f;

        foreach (Snowflake sf in snowflakes)
        {
            if (sf.gameObject == null) continue;

            float adjustedFallSpeed = sf.fallSpeed * currentIntensity;
            float adjustedWind = (windDirection * windStrength) + (sf.driftSpeed * currentIntensity);

            sf.y -= adjustedFallSpeed * Time.deltaTime;
            sf.x += adjustedWind * Time.deltaTime;

            sf.gameObject.transform.position = spawnOrigin + new Vector3(sf.x, sf.y, 0);
            sf.gameObject.transform.Rotate(0f, 0f, sf.rotationSpeed * Time.deltaTime);

            if (sf.y < -halfHeight)
            {
                sf.y = halfHeight;
                sf.x = Random.Range(-halfWidth, halfWidth);
            }

            if (sf.x < -halfWidth)
                sf.x = halfWidth;
            else if (sf.x > halfWidth)
                sf.x = -halfWidth;
        }
    }

    private void FollowPlayer()
    {
        if (playerTransform != null)
        {
            spawnOrigin.x = playerTransform.position.x + followPlayerX;
            spawnOrigin.y = playerTransform.position.y + followPlayerY;
        }
    }

    public void SetWeatherIntensity(float intensity)
    {
        weatherIntensity = Mathf.Clamp01(intensity);
    }

    public void SetWindStrength(float strength)
    {
        windStrength = Mathf.Clamp01(strength);
    }

    public void EnableBlizzard(float duration)
    {
        StartCoroutine(BlizzardCoroutine(duration));
    }

    private System.Collections.IEnumerator BlizzardCoroutine(float duration)
    {
        float originalIntensity = weatherIntensity;
        float originalWind = windStrength;

        weatherIntensity = 1f;
        windStrength = 1.5f;

        yield return new WaitForSeconds(duration);

        weatherIntensity = originalIntensity;
        windStrength = originalWind;
    }
}
