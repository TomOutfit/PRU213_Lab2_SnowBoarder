using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    public enum ObstacleType { Rock, Tree }
    public enum DestructionMode { Indestructible, Destructible }

    [Header("Obstacle Properties")]
    [SerializeField] private ObstacleType obstacleType;
    [SerializeField] private DestructionMode destructionMode = DestructionMode.Indestructible;
    [SerializeField] private int damage = 1;
    [SerializeField] private float speedPenalty = 0.3f;
    [SerializeField] private int health = 1;
    [SerializeField] private int destructionPoints = 50;

    [Header("Motion")]
    [SerializeField] private float scrollSpeed = 0f;
    [SerializeField] private float rotationSpeed = 0f;
    [SerializeField] private float verticalSpeed = 0f;

    [Header("Effects")]
    [SerializeField] private GameObject destructionParticles;
    [SerializeField] private AudioClip destructionSound;

    private int currentHealth;
    private Vector3 startPosition;
    private bool isActive = true;

    public ObstacleType Type => obstacleType;

    private void Awake()
    {
        startPosition = transform.position;
        currentHealth = health;
    }

    private void Update()
    {
        if (!isActive) return;
        ApplyMotion();
    }

    private void ApplyMotion()
    {
        if (scrollSpeed != 0f)
            transform.position += Vector3.left * (scrollSpeed * Time.deltaTime);

        if (rotationSpeed != 0f)
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        if (verticalSpeed != 0f)
            transform.position += Vector3.up * (verticalSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        if (player.IsInvincible) return;

        HandleCollision(player);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null) return;

        if (player.IsInvincible) return;

        HandleCollision(player);
    }

    private void HandleCollision(PlayerController player)
    {
        player.TakeDamage(damage);

        if (speedPenalty > 0f)
            player.ApplySpeedBoost(-speedPenalty, 1.5f);

        if (destructionMode == DestructionMode.Destructible)
            TakeDamage(1);

        AudioManager audio = AudioManager.Instance;
        if (audio != null && destructionSound != null)
            audio.PlaySFX(destructionSound);
    }

    public void TakeDamage(int amount)
    {
        if (destructionMode == DestructionMode.Indestructible) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
            DestroyObstacle();
        else
            ShakeEffect();
    }

    private void ShakeEffect()
    {
        StartCoroutine(ShakeCoroutine());
    }

    private System.Collections.IEnumerator ShakeCoroutine()
    {
        Vector3 original = transform.localPosition;
        float shakeMagnitude = 0.1f;
        float shakeDuration = 0.2f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-shakeMagnitude, shakeMagnitude);
            float y = Random.Range(-shakeMagnitude, shakeMagnitude);
            transform.localPosition = original + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = original;
    }

    private void DestroyObstacle()
    {
        isActive = false;

        if (destructionParticles != null)
            Instantiate(destructionParticles, transform.position, Quaternion.identity);

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddDestructionScore(destructionPoints);

        gameObject.SetActive(false);
    }

    private void OnBecameInvisible()
    {
        if (gameObject.scene.isLoaded)
            gameObject.SetActive(false);
    }
}
