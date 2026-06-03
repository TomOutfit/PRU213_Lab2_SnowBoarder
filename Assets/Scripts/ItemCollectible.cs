using UnityEngine;

public class ItemCollectible : MonoBehaviour
{
    public enum ItemType
    {
        SmallSnowflake = 0,
        IceCoin = 1,
        GoldenSnowflake = 2,
        Trophy = 3,
        IceDiamond = 4,
        EnergyDrink = 5,
        IceShield = 6,
        MultiplierStar = 7
    }

    [Header("Item Configuration")]
    public ItemType type;
    public int points;

    [SerializeField] float effectDuration = 4f;
    [SerializeField] ParticleSystem collectEffect;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                CollectItem(player);
            }
        }
    }

    void CollectItem(PlayerController player)
    {
        // 1. Phát âm thanh tương ứng với loại vật phẩm
        if (AudioManager.Instance != null)
        {
            if (type == ItemType.SmallSnowflake || type == ItemType.IceCoin || type == ItemType.GoldenSnowflake)
            {
                AudioManager.Instance.PlayCollectSound(); // Âm thanh nhặt nhỏ
            }
            else
            {
                AudioManager.Instance.PlayTrickSuccessSound(); // Âm thanh power-up / vật phẩm lớn
            }
        }

        // 2. Hiệu ứng hạt (Particle)
        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f);
        }

        // 3. Cộng điểm số
        if (ScoreManager.Instance != null && points > 0)
        {
            ScoreManager.Instance.AddScore(points);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowFloatingText("+" + points, transform.position);
            }
        }

        // 4. Áp dụng hiệu ứng Gameplay
        switch (type)
        {
            case ItemType.EnergyDrink:
                player.ApplySpeedBoost(effectDuration);
                break;
            case ItemType.IceShield:
                player.ApplyInvincibility(effectDuration);
                break;
            case ItemType.MultiplierStar:
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.IncrementComboMultiplier();
                }
                break;
        }

        // 5. Huỷ vật phẩm sau khi nhặt
        Destroy(gameObject);
    }
}
