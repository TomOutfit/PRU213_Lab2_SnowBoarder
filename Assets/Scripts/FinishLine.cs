using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int finishBonus = 1000;

    [Header("Effects")]
    [SerializeField] private GameObject finishParticles;
    [SerializeField] private AudioClip finishSound;
    [SerializeField] private Color triggerColor = new Color(1f, 1f, 1f, 0.3f);

    private bool triggered = false;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && triggerColor != default)
            spriteRenderer.color = triggerColor;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered) return;
        triggered = true;
        OnPlayerCrossedFinish();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        if (triggered) return;
        triggered = true;
        OnPlayerCrossedFinish();
    }

    private void OnPlayerCrossedFinish()
    {
        if (finishParticles != null)
            Instantiate(finishParticles, transform.position, Quaternion.identity);

        if (finishSound != null)
            AudioManager.Instance?.PlayFinishSFX();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddFinishScore();
            ScoreManager.Instance.AddDestructionScore(finishBonus);
        }

        UIManager ui = UIManager.Instance;
        if (ui != null)
            ui.ShowNotification("LEVEL COMPLETE!");

        if (GameManager.Instance != null)
            GameManager.Instance.LevelComplete();
    }
}
