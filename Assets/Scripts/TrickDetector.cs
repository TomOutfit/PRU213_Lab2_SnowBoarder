using UnityEngine;
using UnityEngine.InputSystem;

public class TrickDetector : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private Key jumpKey = Key.Space;
    [SerializeField] private Key spinKey = Key.Q;
    [SerializeField] private Key flipKey = Key.E;

    [Header("Cooldown")]
    [SerializeField] private float trickCooldown = 0.5f;

    private float lastTrickTime = float.MinValue;

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (Time.time - lastTrickTime < trickCooldown)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current[jumpKey].wasPressedThisFrame)
        {
            AttemptTrick("Jump");
        }
        else if (Keyboard.current[spinKey].wasPressedThisFrame)
        {
            AttemptTrick("Spin");
        }
        else if (Keyboard.current[flipKey].wasPressedThisFrame)
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
