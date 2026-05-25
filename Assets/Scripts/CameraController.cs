using UnityEngine;

/// <summary>
/// Smooth camera follow system for tracking the player with various effect options.
/// Includes look-ahead, vertical offset, and screen shake capabilities.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);
    
    [Header("Follow Settings")]
    [SerializeField] private float followSpeedX = 5f;
    [SerializeField] private float followSpeedY = 3f;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSpeed = 2f;
    
    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-100f, -100f);
    [SerializeField] private Vector2 maxBounds = new Vector2(100f, 100f);
    
    [Header("Vertical Adjustment")]
    [SerializeField] private bool autoAdjustVertical = true;
    [SerializeField] private float minVerticalOffset = 1f;
    [SerializeField] private float maxVerticalOffset = 5f;
    [SerializeField] private float playerVelocityVerticalScale = 0.1f;
    
    [Header("Screen Shake")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    
    [Header("FOV Effects")]
    [SerializeField] private bool useFOVZoom = true;
    [SerializeField] private float baseFOV = 60f;
    [SerializeField] private float maxFOV = 80f;
    [SerializeField] private float FOVSpeedThreshold = 20f;
    
    // Components
    private Camera cam;
    
    // State
    private Vector3 currentVelocity;
    private Vector3 lookAheadOffset;
    private float targetFOV;
    private bool isShaking;
    private float shakeTimer;
    private Vector3 shakeOffset;
    
    // Cached
    private float baseY;
    
    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetFOV = baseFOV;
        
        if (cam != null)
        {
            cam.orthographic = false;
        }
    }
    
    private void Start()
    {
        FindPlayer();
        SetupInitialPosition();
    }
    
    private void FindPlayer()
    {
        if (target == null)
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
            {
                target = GameManager.Instance.CurrentPlayer.transform;
            }
            else
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }
        }
    }
    
    private void SetupInitialPosition()
    {
        if (target != null)
        {
            Vector3 targetPos = target.position + offset;
            transform.position = targetPos;
            baseY = targetPos.y;
        }
    }
    
    private void LateUpdate()
    {
        if (target == null)
        {
            FindPlayer();
            return;
        }
        
        UpdateFollowPosition();
        UpdateFOV();
        UpdateScreenShake();
        ApplyBounds();
    }
    
    private void UpdateFollowPosition()
    {
        float currentSpeed = 0f;
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            currentSpeed = GameManager.Instance.CurrentPlayer.CurrentSpeed;
        }
        
        lookAheadOffset = Vector3.Lerp(
            lookAheadOffset,
            Vector3.right * lookAheadDistance * Mathf.Sign(target.lossyScale.x),
            Time.deltaTime * lookAheadSpeed
        );
        
        float verticalOffset = offset.y;
        if (autoAdjustVertical && GameManager.Instance?.CurrentPlayer != null)
        {
            float velocityY = GameManager.Instance.CurrentPlayer.Velocity.y;
            verticalOffset = Mathf.Lerp(minVerticalOffset, maxVerticalOffset, 
                Mathf.InverseLerp(-10f, 10f, velocityY));
        }
        
        Vector3 targetPosition = target.position + offset + lookAheadOffset;
        targetPosition.y = baseY + verticalOffset + (currentSpeed * playerVelocityVerticalScale * 0.1f);
        
        float smoothX = Mathf.SmoothDamp(transform.position.x, targetPosition.x, ref currentVelocity.x, 1f / followSpeedX);
        float smoothY = Mathf.SmoothDamp(transform.position.y, targetPosition.y, ref currentVelocity.y, 1f / followSpeedY);
        
        transform.position = new Vector3(smoothX, smoothY, targetPosition.z);
        
        baseY = transform.position.y;
    }
    
    private void UpdateFOV()
    {
        if (!useFOVZoom || cam == null) return;
        
        float currentSpeed = 0f;
        if (GameManager.Instance != null && GameManager.Instance.CurrentPlayer != null)
        {
            currentSpeed = GameManager.Instance.CurrentPlayer.CurrentSpeed;
        }
        
        float speedRatio = Mathf.Clamp01((currentSpeed - FOVSpeedThreshold) / (maxFOV - baseFOV));
        targetFOV = Mathf.Lerp(baseFOV, maxFOV, speedRatio);
        
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 5f);
    }
    
    private void UpdateScreenShake()
    {
        if (isShaking)
        {
            shakeTimer -= Time.deltaTime;
            
            if (shakeTimer <= 0f)
            {
                isShaking = false;
                shakeOffset = Vector3.zero;
            }
            else
            {
                float x = Random.Range(-1f, 1f) * shakeMagnitude;
                float y = Random.Range(-1f, 1f) * shakeMagnitude;
                shakeOffset = new Vector3(x, y, 0f) * (shakeTimer / shakeDuration);
            }
            
            transform.position += shakeOffset;
        }
    }
    
    private void ApplyBounds()
    {
        if (!useBounds) return;
        
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
        pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
        transform.position = pos;
    }
    
    #region Public Methods
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            baseY = target.position.y + offset.y;
        }
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    public void SetFollowSpeed(float speedX, float speedY)
    {
        followSpeedX = speedX;
        followSpeedY = speedY;
    }
    
    public void SetBounds(Vector2 min, Vector2 max)
    {
        minBounds = min;
        maxBounds = max;
        useBounds = true;
    }
    
    public void DisableBounds()
    {
        useBounds = false;
    }
    
    public void TriggerShake(float duration = 0f, float magnitude = 0f)
    {
        if (duration <= 0f) duration = shakeDuration;
        if (magnitude <= 0f) magnitude = shakeMagnitude;
        
        shakeTimer = duration;
        shakeMagnitude = magnitude;
        isShaking = true;
    }
    
    public void StopShake()
    {
        isShaking = false;
        shakeOffset = Vector3.zero;
    }
    
    public void ZoomFOV(float targetFOV, float zoomSpeed)
    {
        StartCoroutine(ZoomFOVCoroutine(targetFOV, zoomSpeed));
    }
    
    private System.Collections.IEnumerator ZoomFOVCoroutine(float target, float speed)
    {
        float startFOV = cam.fieldOfView;
        float elapsed = 0f;
        
        while (Mathf.Abs(cam.fieldOfView - target) > 0.1f)
        {
            elapsed += Time.deltaTime * speed;
            cam.fieldOfView = Mathf.Lerp(startFOV, target, elapsed);
            yield return null;
        }
        
        cam.fieldOfView = target;
    }
    
    public void ResetFOV()
    {
        targetFOV = baseFOV;
    }
    
    public void SetBaseFOV(float fov)
    {
        baseFOV = fov;
        if (cam != null)
        {
            cam.fieldOfView = baseFOV;
        }
    }
    
    public void LookAtPoint(Vector3 point, float duration)
    {
        StartCoroutine(LookAtCoroutine(point, duration));
    }
    
    private System.Collections.IEnumerator LookAtCoroutine(Vector3 point, float duration)
    {
        Quaternion startRot = transform.rotation;
        transform.LookAt(point);
        Quaternion endRot = transform.rotation;
        transform.rotation = startRot;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            yield return null;
        }
        
        transform.rotation = endRot;
    }
    
    #endregion
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 targetPos = Application.isPlaying ? target.position : Vector3.zero;
        Gizmos.DrawWireSphere(targetPos + offset, 0.5f);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPos + offset);
        
        if (useBounds)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0.1f);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
