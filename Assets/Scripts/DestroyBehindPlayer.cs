using UnityEngine;

public class DestroyBehindPlayer : MonoBehaviour
{
    private Transform player;
    private bool hasPassed = false;
    public float delayAfterPass = 2f; // Rút ngắn xuống 2 giây để dọn dẹp nhanh hơn

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
        }
        else
        {
            // Fallback nếu không tìm thấy player, hủy sau 30s
            Destroy(gameObject, 30f);
        }
    }

    void Update()
    {
        if (player != null && !hasPassed)
        {
            // Kiểm tra xem player đã trượt qua vật thể này chưa
            if (player.position.x > transform.position.x + 2f) 
            {
                hasPassed = true;
                Destroy(gameObject, delayAfterPass);
            }
        }
        
        // Safety check: if player falls way below or something goes wrong
        if (transform.position.y < -50f)
        {
            Destroy(gameObject);
        }
    }
}
