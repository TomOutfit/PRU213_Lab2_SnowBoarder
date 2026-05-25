using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all input processing for the snowboarding game.
/// Centralizes input management and provides clean interface for other scripts.
/// </summary>
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }
    
    [Header("Input Settings")]
    
    // Input State
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool sprintPressed;
    private bool pausePressed;
    
    // Events
    public event System.Action OnJumpPressed;
    public event System.Action OnJumpReleased;
    public event System.Action OnPausePressed;
    public event System.Action OnInteractPressed;
    public event System.Action OnTrickLeft;
    public event System.Action OnTrickRight;
    public event System.Action OnSprintPressed;
    
    // Properties
    public Vector2 MoveInput => moveInput;
    public Vector2 LookInput => lookInput;
    public bool JumpPressed => jumpPressed;
    public bool JumpHeld => jumpHeld;
    public bool SprintPressed => sprintPressed;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Update()
    {
        ProcessInputEvents();
    }
    
    #region Input System Callbacks
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpPressed = true;
            jumpHeld = true;
            OnJumpPressed?.Invoke();
        }
        else if (context.performed)
        {
            jumpHeld = true;
        }
        else if (context.canceled)
        {
            jumpPressed = false;
            jumpHeld = false;
            OnJumpReleased?.Invoke();
        }
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            sprintPressed = true;
            OnSprintPressed?.Invoke();
        }
        else if (context.canceled)
        {
            sprintPressed = false;
        }
    }
    
    public void OnTrickLeftAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnTrickLeft?.Invoke();
        }
    }
    
    public void OnTrickRightAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnTrickRight?.Invoke();
        }
    }
    
    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            pausePressed = true;
            OnPausePressed?.Invoke();
        }
        else if (context.canceled)
        {
            pausePressed = false;
        }
    }
    
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            OnInteractPressed?.Invoke();
        }
    }
    
    #endregion
    
    #region Input Processing
    
    private void ProcessInputEvents()
    {
        if (pausePressed)
        {
            pausePressed = false;
            
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.CurrentState == GameState.Playing)
                {
                    GameManager.Instance.PauseGame();
                }
                else if (GameManager.Instance.CurrentState == GameState.Paused)
                {
                    GameManager.Instance.ResumeGame();
                }
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    public void EnableInput(bool enable)
    {
        enabled = enable;
    }
    
    public void ResetInput()
    {
        moveInput = Vector2.zero;
        lookInput = Vector2.zero;
        jumpPressed = false;
        jumpHeld = false;
        sprintPressed = false;
        pausePressed = false;
    }
    
    public bool GetKeyDown(KeyCode key)
    {
        return Input.GetKeyDown(key);
    }
    
    public bool GetKey(KeyCode key)
    {
        return Input.GetKey(key);
    }
    
    public bool GetKeyUp(KeyCode key)
    {
        return Input.GetKeyUp(key);
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
