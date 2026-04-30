using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float lifeTime = 4f;
    public int damage = 8;

    private Vector3 moveDirection = Vector3.forward;

    public void Initialize(Vector3 direction, int projectileDamage, float projectileSpeed)
    {
        if (direction.sqrMagnitude > 0.0001f)
            moveDirection = direction.normalized;
        else
            moveDirection = Vector3.forward;

        damage = projectileDamage;
        moveSpeed = projectileSpeed;
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        lifeTime -= Time.deltaTime;
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null)
            enemy = other.GetComponentInParent<EnemyHealth>();

        if (enemy != null)
            return;

        Destroy(gameObject);
    }
}