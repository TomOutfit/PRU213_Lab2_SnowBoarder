using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBtnHandler : MonoBehaviour
{
    [SerializeField] private GameObject guideCanvas;

    public void OnPlayClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartGame();
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level1");
        }
    }

    public void OnGuideClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGuide();
        }
        else if (guideCanvas != null)
        {
            guideCanvas.SetActive(true);
        }
    }

    public void OnCloseGuideClicked()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGuide();
        }
        else if (guideCanvas != null)
        {
            guideCanvas.SetActive(false);
        }
    }

    public void OnQuitClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
