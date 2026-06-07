using UnityEngine;
using UnityEngine.InputSystem;

public class TrickDetector : MonoBehaviour
{
    public static TrickDetector Instance { get; private set; }

    [Header("Key Bindings")]
    public Key jumpKey = Key.Space;
    public Key indyGrabKey = Key.Q;
    public Key methodGrabKey = Key.E;

    [Header("Trick Settings")]
    public float trickCooldown = 0.5f;
    public float inputBufferTime = 0.2f;
    public float grabInterval = 0.4f;

    PlayerController playerController;
    float startRotationZ;
    float totalRotation;
    int currentCombo = 1;
    float lastTrickTime = -999f;
    float airTime = 0f;
    bool wasInAir = false;
    int pendingScore = 0;

    // Grab & Spin Tracking
    Vector3 originalSpriteScale = Vector3.one;
    bool hasOriginalScale = false;
    float grabTimer = 0f;
    int flipCount = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController != null && playerController.playerSprite != null)
        {
            originalSpriteScale = playerController.playerSprite.transform.localScale;
            hasOriginalScale = true;
        }
    }

    void Update()
    {
        if (playerController == null) return;

        // Lazy load original scale if it wasn't available at Start
        if (!hasOriginalScale && playerController.playerSprite != null)
        {
            originalSpriteScale = playerController.playerSprite.transform.localScale;
            hasOriginalScale = true;
        }

        if (!playerController.IsGrounded())
        {
            // --- MID-AIR LOGIC ---
            float deltaAngle = Mathf.DeltaAngle(startRotationZ, transform.rotation.eulerAngles.z);
            totalRotation += deltaAngle;
            startRotationZ = transform.rotation.eulerAngles.z;

            airTime += Time.deltaTime;

            // 1. Board Grabs Detection
            bool isGrabbingThisFrame = false;
            string grabName = "";
            Vector3 targetScale = originalSpriteScale;

            if (Keyboard.current != null)
            {
                if (Keyboard.current[indyGrabKey].isPressed)
                {
                    isGrabbingThisFrame = true;
                    grabName = "INDY GRAB";
                    targetScale = Vector3.Scale(originalSpriteScale, new Vector3(0.8f, 0.8f, 1f)); // Squish/crouch
                }
                else if (Keyboard.current[methodGrabKey].isPressed)
                {
                    isGrabbingThisFrame = true;
                    grabName = "METHOD GRAB";
                    targetScale = Vector3.Scale(originalSpriteScale, new Vector3(1.2f, 0.7f, 1f)); // Tweak/stretch
                }
            }

            if (isGrabbingThisFrame)
            {
                grabTimer += Time.deltaTime;
                if (grabTimer >= grabInterval)
                {
                    grabTimer = 0f;
                    int multiplier = Mathf.Min(currentCombo, 10);
                    int grabPoints = 150 * multiplier;
                    pendingScore += grabPoints;

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"{grabName}! +{grabPoints} (Pending)", transform.position);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound();
                }
            }
            else
            {
                grabTimer = 0f;
            }

            // Apply visual warping smoothly
            if (playerController.playerSprite != null)
            {
                playerController.playerSprite.transform.localScale = Vector3.Lerp(playerController.playerSprite.transform.localScale, targetScale, Time.deltaTime * 10f);
            }

            // 2. Directional Flips Detection
            if (Mathf.Abs(totalRotation) >= 360f)
            {
                flipCount++;
                bool isBackflip = totalRotation > 0f;
                totalRotation = totalRotation > 0f ? totalRotation - 360f : totalRotation + 360f;

                if (Time.time - lastTrickTime > trickCooldown)
                {
                    lastTrickTime = Time.time;
                    int multiplier = Mathf.Min(currentCombo, 10);

                    int flipPoints = 300;
                    if (flipCount == 2) flipPoints = 400;
                    else if (flipCount == 3) flipPoints = 500;
                    else if (flipCount > 3) flipPoints = 600;

                    pendingScore += flipPoints * multiplier;

                    string flipText = "";
                    if (flipCount == 1) flipText = isBackflip ? "BACKFLIP!" : "FRONTFLIP!";
                    else if (flipCount == 2) flipText = isBackflip ? "DOUBLE BACKFLIP!" : "DOUBLE FRONTFLIP!";
                    else if (flipCount == 3) flipText = isBackflip ? "TRIPLE BACKFLIP!" : "TRIPLE FRONTFLIP!";
                    else flipText = $"{flipCount}x {(isBackflip ? "BACKFLIP!" : "FRONTFLIP!")}";

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"{flipText} x{multiplier} (Pending)", transform.position);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound();

                    currentCombo++;
                }
            }
        }
        else
        {
            // --- GROUNDED LOGIC ---
            // Reset sprite scale smoothly
            if (hasOriginalScale && playerController.playerSprite != null)
            {
                playerController.playerSprite.transform.localScale = Vector3.Lerp(playerController.playerSprite.transform.localScale, originalSpriteScale, Time.deltaTime * 10f);
            }

            if (wasInAir) // Player just landed
            {
                // Landed tricks score
                if (pendingScore > 0)
                {
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddTrickScore(pendingScore, 1);

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"+{pendingScore} LANDED!", transform.position);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound();

                    pendingScore = 0;
                }

                // Check Perfect Landing alignment
                float groundAngle = GetGroundAngle();
                float playerAngle = transform.rotation.eulerAngles.z;
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(playerAngle, groundAngle));

                if (angleDiff <= 15f)
                {
                    int perfectLandingBonus = 200;
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddTrickScore(perfectLandingBonus, 1);

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText("PERFECT LANDING! +200", transform.position + Vector3.up * 1.5f);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound();
                }

                // Check Big Air duration
                if (airTime > 1.5f)
                {
                    int bigAirBonus = Mathf.RoundToInt((airTime - 1.0f) * 250f);
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddTrickScore(bigAirBonus, 1);

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"BIG AIR! +{bigAirBonus}", transform.position + Vector3.up * 3f);
                }

                if (airTime > 0.5f)
                {
                    currentCombo = 1;
                }
            }

            startRotationZ = transform.rotation.eulerAngles.z;
            totalRotation = 0f;
            airTime = 0f;
            flipCount = 0;
            grabTimer = 0f;
        }

        wasInAir = !playerController.IsGrounded();
    }

    float GetGroundAngle()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 5f);
        if (hit.collider != null && (hit.collider.CompareTag("Ground") || hit.collider.gameObject.name.Contains("Level") || hit.collider.gameObject.name.Contains("Slope")))
        {
            Vector2 normal = hit.normal;
            return Mathf.Atan2(-normal.x, normal.y) * Mathf.Rad2Deg;
        }
        return 0f;
    }

    public void ResetCombo()
    {
        currentCombo = 1;
        totalRotation = 0f;
        airTime = 0f;
        pendingScore = 0;
        flipCount = 0;
        grabTimer = 0f;
        if (hasOriginalScale && playerController != null && playerController.playerSprite != null)
        {
            playerController.playerSprite.transform.localScale = originalSpriteScale;
        }
    }

    public void ResetAll()
    {
        ResetCombo();
        lastTrickTime = -999f;
        wasInAir = false;
    }
}
