using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndScreenManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private GameObject buttonsPanel;

    [Header("Button References")]
    [SerializeField] private UnityEngine.UI.Button playAgainButton;
    [SerializeField] private UnityEngine.UI.Button menuButton;

    private void Start()
    {
        SetupButtonListeners();
        DisplayResults();
    }

    private void SetupButtonListeners()
    {
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(() => PlayAgain());

        if (menuButton != null)
            menuButton.onClick.AddListener(() => ReturnToMenu());
    }

    private void DisplayResults()
    {
        if (scoreText != null)
        {
            int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
            scoreText.text = $"Final Score: {finalScore:N0}";
        }

        if (statsText != null && ScoreManager.Instance != null)
        {
            statsText.text = ScoreManager.Instance.GetScoreSummary();
        }
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.Reset();
        SceneManager.LoadScene("MainMenu");
    }
}
