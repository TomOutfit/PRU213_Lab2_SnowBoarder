using UnityEngine;

public class ItemCollectible : MonoBehaviour
{
    public enum ItemType
    {
        Snowflake,
        SpeedBoost,
        Invincibility
    }

    public ItemType itemType;
    public int scoreValue = 100;
    [SerializeField] float effectDuration = 3f;
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCollectSound();

        if (collectEffect != null)
        {
            ParticleSystem effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, 2f); // Xoá hiệu ứng sau 2 giây
        }

        switch (itemType)
        {
            case ItemType.Snowflake:
                if (ScoreManager.Instance != null) ScoreManager.Instance.AddScore(scoreValue);
                if (UIManager.Instance != null) UIManager.Instance.ShowFloatingText("+" + scoreValue, transform.position);
                break;
            case ItemType.SpeedBoost:
                player.ApplySpeedBoost(effectDuration);
                break;
            case ItemType.Invincibility:
                player.ApplyInvincibility(effectDuration);
                break;
        }

        Destroy(gameObject);
    }
}
