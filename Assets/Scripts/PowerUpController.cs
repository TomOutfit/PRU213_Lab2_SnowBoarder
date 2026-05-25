using UnityEngine;

public class PowerUpController : MonoBehaviour
{
    public enum PowerUpType { SpeedBoost, Invincibility }

    [Header("Power-Up Settings")]
    [SerializeField] private PowerUpType powerUpType;
    [SerializeField] private float speedBoostAmount = 5f;
    [SerializeField] private float speedBoostDuration = 3f;
    [SerializeField] private float invincibilityDuration = 5f;

    [Header("Motion")]
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float rotationSpeed = 30f;

    [Header("Effects")]
    [SerializeField] private GameObject collectionParticles;
    [SerializeField] private AudioClip collectSound;
    [SerializeField] private float pointsOnCollect = 25f;

    private Vector3 startPosition;
    private float floatTimer;
    private bool isActive = true;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!isActive) return;

        FloatEffect();
        RotateEffect();
    }

    private void FloatEffect()
    {
        floatTimer += Time.deltaTime * floatSpeed;
        float yOffset = Mathf.Sin(floatTimer) * floatAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;
    }

    private void RotateEffect()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        CollectPowerUp(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        CollectPowerUp(collision.gameObject);
    }

    private void CollectPowerUp(GameObject playerObj)
    {
        if (!isActive) return;
        isActive = false;

        PlayerController player = playerObj.GetComponent<PlayerController>();
        if (player == null) return;

        switch (powerUpType)
        {
            case PowerUpType.SpeedBoost:
                player.ApplySpeedBoost(speedBoostAmount, speedBoostDuration);
                break;
            case PowerUpType.Invincibility:
                player.ApplyInvincibility();
                break;
        }

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddDestructionScore(Mathf.RoundToInt(pointsOnCollect));

        if (collectionParticles != null)
            Instantiate(collectionParticles, transform.position, Quaternion.identity);

        if (collectSound != null)
            AudioManager.Instance?.PlaySFX(collectSound);

        gameObject.SetActive(false);
    }

    private void OnBecameInvisible()
    {
        if (gameObject.scene.isLoaded)
            gameObject.SetActive(false);
    }
}
