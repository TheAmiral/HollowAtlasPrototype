using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Can Pickup Ayarları")]
    public int healAmount = 25;
    
    private CircleCollider2D pickupCollider;
    
    void Awake()
    {
        if (GetComponent<CircleCollider2D>() == null)
        {
            pickupCollider = gameObject.AddComponent<CircleCollider2D>();
            pickupCollider.radius = 0.5f;
            pickupCollider.isTrigger = true;
        }
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.Heal(healAmount);
            Debug.Log($"Oyuncu {healAmount} can topladı!");
            Destroy(gameObject);
        }
    }
}