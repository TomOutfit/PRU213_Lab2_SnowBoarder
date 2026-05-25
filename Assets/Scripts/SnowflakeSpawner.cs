using UnityEngine;

public class SnowflakeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private float spawnRangeX = 8f;
    [SerializeField] private float spawnYOffset = 15f;

    [Header("Appearance")]
    [SerializeField] private GameObject snowflakeVisualPrefab;
    [SerializeField] private SpriteRenderer previewSprite;
    [SerializeField] private Color snowflakeColor = new Color(0.85f, 0.95f, 1f, 0.9f);

    private float spawnTimer;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnSnowflake();
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnSnowflake()
    {
        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float baseY = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position.y
            : transform.position.y;
        Vector3 spawnPos = new Vector3(x, baseY + spawnYOffset, 0f);

        GameObject obj = new GameObject("Snowflake");
        obj.transform.position = spawnPos;
        obj.layer = LayerMask.NameToLayer("Default");

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.color = snowflakeColor;
        if (snowflakeVisualPrefab != null)
        {
            var prefab = Instantiate(snowflakeVisualPrefab, spawnPos, Quaternion.identity);
            Destroy(obj);
            obj = prefab;
            obj.transform.position = spawnPos;
        }
        else
        {
            obj.AddComponent<CircleCollider2D>();
            obj.AddComponent<SnowflakeCollectible>();
        }

        var collider = obj.GetComponent<Collider2D>();
        if (collider != null && !collider.isTrigger)
            collider.isTrigger = true;
    }
}
