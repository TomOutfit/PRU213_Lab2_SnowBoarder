using UnityEngine;

/// <summary>
/// Trigger that marks the end of a level.
/// When player enters this trigger, the level is complete.
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField] private string nextLevelName = "";
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }
    
    private void CompleteLevel()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.CompleteLevel();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
        
        Debug.Log("Level Complete!");
    }
}
