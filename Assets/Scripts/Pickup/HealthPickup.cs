using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Can Pickup Ayarları")]
    public int healAmount = 25;
    public float rotateSpeed = 120f;

    private SphereCollider pickupCollider;

    void Awake()
    {
        // 3D oyun — SphereCollider kullan (CircleCollider2D değil!)
        pickupCollider = GetComponent<SphereCollider>();
        if (pickupCollider == null)
        {
            pickupCollider = gameObject.AddComponent<SphereCollider>();
            pickupCollider.radius = 0.5f;
            pickupCollider.isTrigger = true;
        }
    }

    void Update()
    {
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    // 3D trigger — OnTriggerEnter(Collider) kullan, OnTriggerEnter2D değil!
    void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.Heal(healAmount);
            Debug.Log($"Oyuncu {healAmount} can topladı! HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");
            Destroy(gameObject);
        }
    }
}