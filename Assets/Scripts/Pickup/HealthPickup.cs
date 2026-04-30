using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Can Pickup Ayarları")]
    public int healAmount = 25;
    public float rotateSpeed = 120f;

    private SphereCollider pickupCollider;
    private bool collected;

    void Awake()
    {
        // 3D oyun — SphereCollider kullan.
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

    void OnTriggerEnter(Collider other)
    {
        if (collected)
            return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();

        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        collected = true;

        playerHealth.Heal(healAmount);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayHealthPickup();

        Debug.Log($"Oyuncu {healAmount} can topladı! HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}");

        Destroy(gameObject);
    }
}