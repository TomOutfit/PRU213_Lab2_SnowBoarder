using UnityEngine;

public class ItemCollectible : MonoBehaviour
{
    public enum ItemType
    {
        SmallSnowflake,
        IceCoin,
        GoldenSnowflake,
        Trophy,
        IceDiamond,
        EnergyDrink,
        IceShield,
        MultiplierStar
    }

    [Header("Item Configuration")]
    public ItemType type = ItemType.SmallSnowflake;
    public int points = 10;

    private bool isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;
            CollectItem();
        }
    }

    private void CollectItem()
    {
        // Add Points
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddItemScore(points, true);
        }

        // Apply Effects and Sound based on type
        bool isSpecial = false;
        
        switch (type)
        {
            case ItemType.EnergyDrink:
                if (PlayerController.Instance != null)
                    PlayerController.Instance.ApplySpeedBoost(10f, 5f);
                isSpecial = true;
                break;
                
            case ItemType.IceShield:
                if (PlayerController.Instance != null)
                    PlayerController.Instance.ApplyInvincibility();
                isSpecial = true;
                break;
                
            case ItemType.MultiplierStar:
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.ApplyMultiplierBoost(5f);
                isSpecial = true;
                break;
                
            case ItemType.IceDiamond:
                isSpecial = true;
                break;
        }

        if (AudioManager.Instance != null)
        {
            if (isSpecial)
                AudioManager.Instance.PlayPowerUpSFX();
            else
                AudioManager.Instance.PlayCollectSFX();
        }

        // Spawn Collection Effect
        // For now, just destroy the object
        Destroy(gameObject);
    }
}
