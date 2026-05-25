using UnityEngine;

public class TrickDetector : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode spinKey = KeyCode.Q;
    [SerializeField] private KeyCode flipKey = KeyCode.E;

    [Header("Cooldown")]
    [SerializeField] private float trickCooldown = 0.5f;
    [SerializeField] private float inputBufferTime = 0.2f;

    private float lastTrickTime = float.MinValue;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (Time.time - lastTrickTime < trickCooldown)
            return;

        if (Input.GetKeyDown(jumpKey))
        {
            AttemptTrick("Jump");
        }
        else if (Input.GetKeyDown(spinKey))
        {
            AttemptTrick("Spin");
        }
        else if (Input.GetKeyDown(flipKey))
        {
            AttemptTrick("Flip");
        }
    }

    private void AttemptTrick(string trickName)
    {
        PlayerController player = PlayerController.Instance;
        if (player == null) return;

        player.PerformTrick(trickName);
        lastTrickTime = Time.time;

        UIManager ui = UIManager.Instance;
        if (ui != null)
            ui.ShowNotification($"{trickName}!");
    }
}
