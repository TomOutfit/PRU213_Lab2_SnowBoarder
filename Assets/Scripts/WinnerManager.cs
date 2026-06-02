using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class WinnerManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI finalScoreText;
    public Button menuButton;
    public Button playAgainButton;
    public Button quitButton;

    void Start()
    {
        // Hiển thị điểm số cuối cùng
        if (finalScoreText != null && ScoreManager.Instance != null)
        {
            finalScoreText.text = $"Final Score: {ScoreManager.Instance.CurrentScore}";
        }

        // Gắn sự kiện cho các nút
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(() => 
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
                SceneManager.LoadScene("MainMenu");
            });
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(() => 
            {
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                else SceneManager.LoadScene("Level1");
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() => 
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
                else Application.Quit();
            });
        }
    }
}
