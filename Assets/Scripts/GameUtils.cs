using UnityEngine;

/// <summary>
/// Common utility functions used throughout the game.
/// </summary>
public static class GameUtils
{
    /// <summary>
    /// Clamps a value within a range and wraps around if it exceeds the range.
    /// </summary>
    public static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        if (range <= 0f) return min;
        
        while (value < min) value += range;
        while (value > max) value -= range;
        return value;
    }
    
    /// <summary>
    /// Converts a speed value to a percentage for UI display.
    /// </summary>
    public static int SpeedToPercent(float speed, float maxSpeed = 30f)
    {
        return Mathf.RoundToInt(Mathf.Clamp01(speed / maxSpeed) * 100);
    }
    
    /// <summary>
    /// Formats a large number for display with K/M suffixes.
    /// </summary>
    public static string FormatScore(int score)
    {
        if (score >= 1000000)
        {
            return $"{score / 1000000.0:F1}M";
        }
        if (score >= 1000)
        {
            return $"{score / 1000.0:F1}K";
        }
        return score.ToString();
    }
    
    /// <summary>
    /// Calculates a bounce effect value.
    /// </summary>
    public static float Bounce(float time, float frequency = 1f, float amplitude = 1f)
    {
        return Mathf.Abs(Mathf.Sin(time * frequency * Mathf.PI * 2f)) * amplitude;
    }
    
    /// <summary>
    /// Smoothly interpolates between angles in degrees.
    /// </summary>
    public static float LerpAngle(float from, float to, float t)
    {
        float delta = Mathf.Repeat(to - from, 360f);
        if (delta > 180f) delta -= 360f;
        return from + delta * Mathf.Clamp01(t);
    }
    
    /// <summary>
    /// Returns a random direction vector.
    /// </summary>
    public static Vector2 RandomDirection2D()
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
    
    /// <summary>
    /// Maps a value from one range to another.
    /// </summary>
    public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
    {
        return (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
    }
    
    /// <summary>
    /// Checks if a point is within the camera's visible area with optional padding.
    /// </summary>
    public static bool IsVisibleFromCamera(Vector3 position, Camera camera, float padding = 1f)
    {
        if (camera == null) return false;
        
        Vector3 viewportPoint = camera.WorldToViewportPoint(position);
        return viewportPoint.x >= -padding && viewportPoint.x <= 1 + padding &&
               viewportPoint.y >= -padding && viewportPoint.y <= 1 + padding;
    }
    
    /// <summary>
    /// Gets a perpendicular vector to the given direction.
    /// </summary>
    public static Vector2 GetPerpendicular(Vector2 direction)
    {
        return new Vector2(-direction.y, direction.x);
    }
    
    /// <summary>
    /// Shuffles an array in place using Fisher-Yates algorithm.
    /// </summary>
    public static void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}

/// <summary>
/// Extension methods for common types.
/// </summary>
public static class Extensions
{
    public static bool Approximately(this Vector2 self, Vector2 other, float threshold = 0.01f)
    {
        return Vector2.Distance(self, other) < threshold;
    }
    
    public static bool Approximately(this Vector3 self, Vector3 other, float threshold = 0.01f)
    {
        return Vector3.Distance(self, other) < threshold;
    }
    
    public static float SqrMagnitude(this Vector2 self, Vector2 other)
    {
        Vector2 delta = other - self;
        return delta.x * delta.x + delta.y * delta.y;
    }
    
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
    
    public static void Clamp(ref this float value, float min, float max)
    {
        value = Mathf.Clamp(value, min, max);
    }
    
    public static void Clamp01(ref this float value)
    {
        value = Mathf.Clamp01(value);
    }
}

/// <summary>
/// Data container for difficulty settings.
/// </summary>
[System.Serializable]
public class DifficultySettings
{
    [SerializeField] private string name = "Easy";
    [SerializeField] private float obstacleSpawnRate = 1f;
    [SerializeField] private float obstacleSpeed = 1f;
    [SerializeField] private float playerSpeed = 1f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float collectibleRate = 1f;
    
    public string Name => name;
    public float ObstacleSpawnRate => obstacleSpawnRate;
    public float ObstacleSpeed => obstacleSpeed;
    public float PlayerSpeed => playerSpeed;
    public int StartingLives => startingLives;
    public float CollectibleRate => collectibleRate;
    
    public static DifficultySettings[] Presets = new DifficultySettings[]
    {
        new DifficultySettings { name = "Easy", obstacleSpawnRate = 0.5f, obstacleSpeed = 0.7f, playerSpeed = 1.2f, startingLives = 5, collectibleRate = 1.5f },
        new DifficultySettings { name = "Normal", obstacleSpawnRate = 1f, obstacleSpeed = 1f, playerSpeed = 1f, startingLives = 3, collectibleRate = 1f },
        new DifficultySettings { name = "Hard", obstacleSpawnRate = 1.5f, obstacleSpeed = 1.3f, playerSpeed = 0.9f, startingLives = 2, collectibleRate = 0.7f },
        new DifficultySettings { name = "Expert", obstacleSpawnRate = 2f, obstacleSpeed = 1.5f, playerSpeed = 0.8f, startingLives = 1, collectibleRate = 0.5f }
    };
}
