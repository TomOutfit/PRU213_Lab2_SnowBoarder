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
                PlayClickSFX();
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
                if (GameManager.Instance != null) GameManager.Instance.LoadSceneWithFade("Menu");
                else SceneManager.LoadScene("Menu");
            });
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(() => 
            {
                PlayClickSFX();
                if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
                if (GameManager.Instance != null) GameManager.Instance.StartGame();
                else SceneManager.LoadScene("Level1");
            });
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(() => 
            {
                PlayClickSFX();
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
                else Application.Quit();
            });
        }
    }

    private void PlayClickSFX()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuSelectSound();
        }
    }
}
