using UnityEngine;
using System.Collections.Generic;

public class SnowflakeSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float spawnRangeX = 8f;
    [SerializeField] private float spawnYOffset = 15f;

    [Header("Item Prefabs")]
    public GameObject pSmallSnowflake;
    public GameObject pIceCoin;
    public GameObject pGoldenSnowflake;
    public GameObject pTrophy;
    public GameObject pIceDiamond;
    public GameObject pEnergyDrink;
    public GameObject pIceShield;
    public GameObject pMultiplierStar;

    private float spawnTimer;

    private void Start()
    {
        // Try to load prefabs if not assigned
        if (pSmallSnowflake == null) pSmallSnowflake = Resources.Load<GameObject>("Prefabs/Items/SmallSnowflake");
        if (pIceCoin == null) pIceCoin = Resources.Load<GameObject>("Prefabs/Items/IceCoin");
        if (pGoldenSnowflake == null) pGoldenSnowflake = Resources.Load<GameObject>("Prefabs/Items/GoldenSnowflake");
        if (pTrophy == null) pTrophy = Resources.Load<GameObject>("Prefabs/Items/Trophy");
        if (pIceDiamond == null) pIceDiamond = Resources.Load<GameObject>("Prefabs/Items/IceDiamond");
        if (pEnergyDrink == null) pEnergyDrink = Resources.Load<GameObject>("Prefabs/Items/EnergyDrink");
        if (pIceShield == null) pIceShield = Resources.Load<GameObject>("Prefabs/Items/IceShield");
        if (pMultiplierStar == null) pMultiplierStar = Resources.Load<GameObject>("Prefabs/Items/MultiplierStar");
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnRandomItem();
            // Chain spawn logic (sometimes spawn multiple in a row)
            if (Random.value < 0.3f)
                spawnTimer = spawnInterval * 0.2f;
            else
                spawnTimer = spawnInterval;
        }
    }

    private void SpawnRandomItem()
    {
        float roll = Random.Range(0f, 100f);
        GameObject selectedPrefab = null;

        // Weights:
        // Basic: Small Snowflake (55%), Ice Coin (30%)
        // Rare: Golden Snowflake (8%), Trophy (4%)
        // Special: Energy Drink (1%), Ice Shield (1%), Multiplier Star (0.8%), Ice Diamond (0.2%)

        if (roll < 55f) selectedPrefab = pSmallSnowflake;
        else if (roll < 85f) selectedPrefab = pIceCoin;
        else if (roll < 93f) selectedPrefab = pGoldenSnowflake;
        else if (roll < 97f) selectedPrefab = pTrophy;
        else if (roll < 98f) selectedPrefab = pEnergyDrink;
        else if (roll < 99f) selectedPrefab = pIceShield;
        else if (roll < 99.8f) selectedPrefab = pMultiplierStar;
        else selectedPrefab = pIceDiamond;

        // Fallback to SmallSnowflake if something is missing
        if (selectedPrefab == null)
            selectedPrefab = pSmallSnowflake;
            
        if (selectedPrefab == null) return; // Still null, skip

        float x = Random.Range(-spawnRangeX, spawnRangeX);
        float baseY = PlayerController.Instance != null
            ? PlayerController.Instance.transform.position.y
            : transform.position.y;
        Vector3 spawnPos = new Vector3(x, baseY - Mathf.Abs(spawnYOffset), 0f);

        Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
    }
}
