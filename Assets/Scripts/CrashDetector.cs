using UnityEngine;
using UnityEngine.SceneManagement;

public class CrashDetector : MonoBehaviour
{
    [Header("Crash Effect")]
    [SerializeField] ParticleSystem crashEffect;
    [SerializeField] float loadDelay = 1f;

    bool hasCrashed = false;
    PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Ground") || other.gameObject.CompareTag("Obstacle"))
        {
            HandleCrash();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetType() == typeof(CircleCollider2D))
        {
            if (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Obstacle"))
            {
                HandleCrash();
            }
        }
    }

    void HandleCrash()
    {
        if (!hasCrashed && playerController != null && !playerController.isInvincible)
        {
            hasCrashed = true;
            playerController.DisableControls();

            if (crashEffect != null)
            {
                crashEffect.Play();
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayCrashSound();

            if (TrickDetector.Instance != null) TrickDetector.Instance.ResetCombo();

            Invoke(nameof(NotifyGameManager), loadDelay);
        }
    }

    void NotifyGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerCrashed();
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    void OnEnable()
    {
        hasCrashed = false;
    }
}
