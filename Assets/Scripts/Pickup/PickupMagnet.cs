using UnityEngine;

public class PickupMagnet : MonoBehaviour
{
    public float magnetRange = 2.2f;
    public float moveSpeed = 6f;

    private Transform target;

    void Awake()
    {
        // Awake'te bir kez bul — Update'te her frame aramak yerine
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    void Update()
    {
        // Fallback: Awake'te bulunamadıysa tekrar dene (geç spawn durumu)
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
            else
                return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= magnetRange)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
}