using UnityEngine;

/// <summary>
/// Controls environmental elements like wind, weather changes, and ambient effects.
/// </summary>
public class EnvironmentController : MonoBehaviour
{
    [Header("Wind Settings")]
    [SerializeField] private bool enableWind = true;
    [SerializeField] private float baseWindStrength = 1f;
    [SerializeField] private float windVariation = 0.5f;
    [SerializeField] private float windChangeInterval = 10f;
    
    [Header("Weather")]
    [SerializeField] private bool dynamicWeather = true;
    [SerializeField] private WeatherType[] weatherPatterns;
    [SerializeField] private float weatherChangeInterval = 60f;
    
    [Header("References")]
    [SerializeField] private SnowfallEffect snowfallEffect;
    [SerializeField] private WindZone windZone;
    
    // State
    private float currentWindStrength;
    private float targetWindStrength;
    private float windDirection = 1f;
    private WeatherType currentWeather;
    private float weatherTimer;
    private float windChangeTimer;
    
    public float CurrentWindStrength => currentWindStrength;
    public WeatherType CurrentWeather => currentWeather;
    
    private void Awake()
    {
        currentWindStrength = baseWindStrength;
        currentWeather = WeatherType.Clear;
    }
    
    private void Start()
    {
        if (weatherPatterns == null || weatherPatterns.Length == 0)
        {
            weatherPatterns = new WeatherType[] { WeatherType.Clear, WeatherType.LightSnow, WeatherType.HeavySnow };
        }
    }
    
    private void Update()
    {
        UpdateWind();
        UpdateWeather();
    }
    
    #region Wind System
    
    private void UpdateWind()
    {
        if (!enableWind) return;
        
        windChangeTimer += Time.deltaTime;
        
        if (windChangeTimer >= windChangeInterval)
        {
            windChangeTimer = 0f;
            targetWindStrength = baseWindStrength + Random.Range(-windVariation, windVariation);
            windDirection = Random.Range(-1f, 1f) > 0 ? 1f : -1f;
        }
        
        currentWindStrength = Mathf.Lerp(currentWindStrength, targetWindStrength, Time.deltaTime * 0.5f);
        
        if (windZone != null)
        {
            windZone.windMain = currentWindStrength;
            windZone.transform.rotation = Quaternion.Euler(0, windDirection > 0 ? 0 : 180, 0);
        }
        
        if (snowfallEffect != null)
        {
            snowfallEffect.SetWindStrength(currentWindStrength);
        }
    }
    
    public void SetWindStrength(float strength)
    {
        targetWindStrength = Mathf.Max(0f, strength);
    }
    
    public void TriggerGust(float strength, float duration)
    {
        StartCoroutine(GustCoroutine(strength, duration));
    }
    
    private System.Collections.IEnumerator GustCoroutine(float gustStrength, float duration)
    {
        float originalStrength = targetWindStrength;
        targetWindStrength = gustStrength;
        
        yield return new WaitForSeconds(duration);
        
        targetWindStrength = originalStrength;
    }
    
    #endregion
    
    #region Weather System
    
    private void UpdateWeather()
    {
        if (!dynamicWeather || weatherPatterns == null || weatherPatterns.Length == 0) return;
        
        weatherTimer += Time.deltaTime;
        
        if (weatherTimer >= weatherChangeInterval)
        {
            weatherTimer = 0f;
            ChangeWeather();
        }
    }
    
    private void ChangeWeather()
    {
        WeatherType newWeather;
        
        do
        {
            int index = Random.Range(0, weatherPatterns.Length);
            newWeather = weatherPatterns[index];
        }
        while (newWeather == currentWeather && weatherPatterns.Length > 1);
        
        SetWeather(newWeather);
    }
    
    public void SetWeather(WeatherType weather)
    {
        currentWeather = weather;
        
        if (snowfallEffect != null)
        {
            switch (weather)
            {
                case WeatherType.Clear:
                    snowfallEffect.SetIntensity(0f);
                    break;
                case WeatherType.LightSnow:
                    snowfallEffect.SetIntensity(0.5f);
                    break;
                case WeatherType.HeavySnow:
                    snowfallEffect.SetIntensity(1.5f);
                    snowfallEffect.SetWindStrength(currentWindStrength * 1.5f);
                    break;
                case WeatherType.Blizzard:
                    snowfallEffect.SetIntensity(2f);
                    snowfallEffect.SetWindStrength(currentWindStrength * 2f);
                    break;
            }
        }
    }
    
    #endregion
    
    public float GetWindForce(Vector3 position)
    {
        if (!enableWind) return 0f;
        
        float heightFactor = 1f - (position.y / 100f);
        heightFactor = Mathf.Clamp01(heightFactor);
        
        return currentWindStrength * windDirection * heightFactor;
    }
}

public enum WeatherType
{
    Clear,
    LightSnow,
    HeavySnow,
    Blizzard
}
