using UnityEngine;

public class SnowflakeCollectible : MonoBehaviour
{
    [Header("Collectible Settings")]
    [SerializeField] private int pointsPerCollect = 10;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float bobAmplitude = 0.2f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Effects")]
    [SerializeField] private GameObject collectParticles;
    [SerializeField] private AudioClip collectSound;

    private Vector3 startPosition;
    private float bobTimer;
    private bool isActive = true;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!isActive) return;

        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        bobTimer += Time.deltaTime * bobSpeed;
        float yOffset = Mathf.Sin(bobTimer) * bobAmplitude;
        transform.position = startPosition + Vector3.up * yOffset;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Collect(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        Collect(collision.gameObject);
    }

    private void Collect(GameObject playerObj)
    {
        if (!isActive) return;
        isActive = false;

        ScoreManager sm = ScoreManager.Instance;
        if (sm != null)
            sm.AddItemScore(pointsPerCollect);

        if (collectParticles != null)
            Instantiate(collectParticles, transform.position, Quaternion.identity);

        if (collectSound != null)
            AudioManager.Instance?.PlaySFX(collectSound);

        UIManager ui = UIManager.Instance;
        if (ui != null)
            ui.ShowNotification($"+{pointsPerCollect} Snowflakes!");

        gameObject.SetActive(false);
    }

    private void OnBecameInvisible()
    {
        if (gameObject.scene.isLoaded)
            gameObject.SetActive(false);
    }
}
