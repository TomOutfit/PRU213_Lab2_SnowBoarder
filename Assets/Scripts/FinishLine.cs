using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLine : MonoBehaviour
{
    [Header("Finish Settings")]
    [SerializeField] private int finishPointsBonus = 500;
    [SerializeField] private float triggerWidth = 1f;
    [SerializeField] private bool isLastLevel = true;

    [Header("Effects")]
    [SerializeField] private GameObject finishParticles;
    [SerializeField] private AudioClip finishSound;
    [SerializeField] private Color triggerColor = new Color(1f, 1f, 1f, 0.3f);

    [Header("Scene Transition")]
    [SerializeField] private string nextLevelScene = "";
    [SerializeField] private float finishDelay = 3f;

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
            ScoreManager.Instance.AddDestructionScore(finishPointsBonus);
        }

        UIManager ui = UIManager.Instance;
        if (ui != null)
            ui.ShowNotification("LEVEL COMPLETE!");

        if (GameManager.Instance != null)
            GameManager.Instance.LevelComplete();

        if (!isLastLevel && !string.IsNullOrEmpty(nextLevelScene))
            Invoke(nameof(LoadNextLevel), finishDelay);
    }

    private void LoadNextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelScene))
            SceneManager.LoadScene(nextLevelScene);
    }
}
