using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Header("Finish Settings")]
    public int finishBonus = 1000;
    public ParticleSystem finishParticles;
    public AudioClip finishSound;
    public Color triggerColor = new Color(1f, 1f, 1f, 0.3f);

    bool hasFinished = false;
    SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && triggerColor.a > 0f)
        {
            spriteRenderer.color = triggerColor;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasFinished)
        {
            hasFinished = true;
            
            PlayerController pc = other.GetComponent<PlayerController>();
            if (pc != null) pc.SetFinished();

            if (finishParticles != null)
                finishParticles.Play();

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource != null && finishSound != null)
            {
                audioSource.PlayOneShot(finishSound);
            }
            else if (AudioManager.Instance != null && finishSound != null)
            {
                AudioManager.Instance.sfxSource.PlayOneShot(finishSound);
            }

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(finishBonus);

            Invoke(nameof(TriggerFinish), 1f);
        }
    }

    void TriggerFinish()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LevelComplete();
    }

    void OnEnable()
    {
        hasFinished = false;
    }
}
