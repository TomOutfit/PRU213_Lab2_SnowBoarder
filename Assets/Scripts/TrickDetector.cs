using UnityEngine;
using UnityEngine.InputSystem;

public class TrickDetector : MonoBehaviour
{
    public static TrickDetector Instance { get; private set; }

    [Header("Key Bindings")]
    public Key jumpKey = Key.Space;
    public Key spinKey = Key.Q;
    public Key flipKey = Key.E;

    [Header("Trick Settings")]
    public float trickCooldown = 0.5f;
    public float inputBufferTime = 0.2f;

    PlayerController playerController;
    float startRotationZ;
    float totalRotation;
    int currentCombo = 1;
    float lastTrickTime = -999f;
    float airTime = 0f;
    bool wasInAir = false;
    int pendingScore = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (playerController != null && !playerController.IsGrounded())
        {
            float deltaAngle = Mathf.DeltaAngle(startRotationZ, transform.rotation.eulerAngles.z);
            totalRotation += deltaAngle;
            startRotationZ = transform.rotation.eulerAngles.z;

            airTime += Time.deltaTime;

            if (Mathf.Abs(totalRotation) >= 360f)
            {
                totalRotation = 0f;

                if (Time.time - lastTrickTime > trickCooldown)
                {
                    lastTrickTime = Time.time;
                    int multiplier = Mathf.Min(currentCombo, 10);

                    // Tích lũy điểm vào pendingScore thay vì cộng trực tiếp
                    pendingScore += 500 * multiplier;

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"FLIP! x{multiplier} (Pending)", transform.position);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound();

                    currentCombo++;
                }
            }
        }
        else
        {
            if (wasInAir) // Người chơi vừa tiếp đất
            {
                if (pendingScore > 0)
                {
                    // Tiếp đất thành công: Cộng dồn điểm pending vào điểm thực
                    if (ScoreManager.Instance != null)
                        ScoreManager.Instance.AddTrickScore(pendingScore, 1);

                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowFloatingText($"+{pendingScore} LANDED!", transform.position);

                    if (AudioManager.Instance != null)
                        AudioManager.Instance.PlayTrickSuccessSound(); // Phát âm thanh khi tiếp đất thành công

                    pendingScore = 0; // Đã cộng điểm xong
                }

                if (airTime > 0.5f)
                {
                    currentCombo = 1;
                }
            }

            startRotationZ = transform.rotation.eulerAngles.z;
            totalRotation = 0f;
            airTime = 0f;
        }

        wasInAir = playerController != null && !playerController.IsGrounded();
    }

    public void ResetCombo()
    {
        currentCombo = 1;
        totalRotation = 0f;
        airTime = 0f;
        pendingScore = 0;
    }

    public void ResetAll()
    {
        ResetCombo();
        lastTrickTime = -999f;
        wasInAir = false;
    }
}
