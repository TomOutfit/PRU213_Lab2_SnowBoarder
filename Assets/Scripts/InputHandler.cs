using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class InputHandler : MonoBehaviour
{
    private PlayerController player;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
    }

    private void OnMove(InputValue value)
    {
        player.OnMoveInput(value.Get<Vector2>());
    }

    private void OnJump()
    {
        player.OnJumpInput();
    }
}
