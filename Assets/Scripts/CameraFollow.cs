using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Kéo thả GameObject Player vào đây từ Hierarchy")]
    public Transform player;

    [Header("Camera Settings")]
    [Tooltip("Vị trí Z cố định của Camera (thường là -10 trong game 2D)")]
    public float zOffset = -10f;

    // LateUpdate được gọi sau khi Update (và FixedUpdate), rất phù hợp để Camera đi theo mượt mà
    // đặc biệt khi Player di chuyển bằng Physics (Rigidbody2D).
    private void LateUpdate()
    {
        if (player != null)
        {
            // Gán vị trí mới cho Camera: X và Y của Player, Z là zOffset cố định
            transform.position = new Vector3(player.position.x, player.position.y, zOffset);
        }
        else
        {
            Debug.LogWarning("Chưa gán Player cho CameraFollow!");
        }
    }
}
