using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] Transform target; // Kéo thả nhân vật vào đây hoặc script sẽ tự động tìm Tag "Player"
    [SerializeField] Vector3 offset = new Vector3(0, 0, -10f); // Giữ khoảng cách z=-10 để Camera nhìn được không gian 2D
    [SerializeField] float smoothSpeed = 5f;

    void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("CameraController không tìm thấy đối tượng nào có Tag là 'Player'!");
            }
        }
    }

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desiredPosition = target.position + offset;
            // Dùng Lerp để Camera theo dõi nhân vật một cách mượt mà
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
