using UnityEngine;

public class SnowflakeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float spawnInterval = 1.5f;
    public float spawnRangeX = 8f;
    public float spawnYOffset = 15f;

    [Header("Item Prefabs")]
    public GameObject pSmallSnowflake;
    public GameObject pIceCoin;
    public GameObject pGoldenSnowflake;
    public GameObject pTrophy;
    public GameObject pIceDiamond;
    public GameObject pEnergyDrink;
    public GameObject pIceShield;
    public GameObject pMultiplierStar;

    float timer;
    GameObject player;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) player = go;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameManager.GameState.Playing)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnItem();
        }
    }

    void SpawnItem()
    {
        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float spawnY = (player != null ? player.transform.position.y : transform.position.y) + spawnYOffset;
        Vector3 spawnPos = new Vector3(x, spawnY, 0f);

        float roll = Random.value;
        GameObject prefab = null;

        if (roll < 0.45f)
            prefab = pSmallSnowflake;
        else if (roll < 0.70f)
            prefab = pIceCoin;
        else if (roll < 0.82f)
            prefab = pGoldenSnowflake;
        else if (roll < 0.91f)
            prefab = pTrophy;
        else if (roll < 0.97f)
            prefab = pIceDiamond;
        else if (roll < 0.995f)
            prefab = pEnergyDrink;
        else if (roll < 0.999f)
            prefab = pIceShield;
        else
            prefab = pMultiplierStar;

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
            obj.AddComponent<DestroyBehindPlayer>();
        }
    }
}
