using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bridges Unity Input System callbacks to PlayerController.
/// Uses SendMessages to receive input events.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class PlayerInputCallbacks : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    
    private PlayerInput playerInput;
    
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
    }
    
    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
    }
    
    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
    }
    
    // ═══════════════════════════════════════════════════════════
    // Input System Callbacks (called via SendMessage from PlayerInput)
    // ═══════════════════════════════════════════════════════════
    
    private void OnMove(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnMove(context);
        }
    }
    
    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && playerController != null)
        {
            playerController.HandleJumpInput(context);
        }
    }
    
    private void OnTrickLeft(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnTrickLeft(context);
        }
    }
    
    private void OnTrickRight(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnTrickRight(context);
        }
    }
    
    private void OnSpeedUp(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnSpeedUp(context);
        }
    }
    
    private void OnSpeedDown(InputAction.CallbackContext context)
    {
        if (playerController != null)
        {
            playerController.OnSpeedDown(context);
        }
    }
    
    private void OnNavigate(InputAction.CallbackContext context)
    {
        // UI handled by EventSystem
    }
    
    private void OnSubmit(InputAction.CallbackContext context)
    {
        // UI handled by EventSystem
    }
    
    private void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
            {
                uiManager.OnPauseClicked();
            }
        }
    }
}
