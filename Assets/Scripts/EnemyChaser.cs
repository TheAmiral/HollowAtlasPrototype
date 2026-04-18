using UnityEngine;

public class EnemyChaser : MonoBehaviour
{
    public float moveSpeed = 2.5f;
    public float turnSpeed = 10f;
    public int contactDamage = 5;
    public float damageCooldown = 1f;
    public float attackRange = 1.1f;

    private Transform target;
    private PlayerHealth playerHealth;
    private float damageTimer;

    void Start()
    {
        FindPlayer();
    }

    void Update()
    {
        if (target == null || playerHealth == null)
        {
            FindPlayer();
            return;
        }

        if (playerHealth.CurrentHealth <= 0)
            return;

        damageTimer -= Time.deltaTime;

        Vector3 toPlayer = target.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;

        if (distance > 0.05f)
        {
            Vector3 moveDir = toPlayer.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        if (distance <= attackRange && damageTimer <= 0f)
        {
            playerHealth.TakeDamage(contactDamage);
            damageTimer = damageCooldown;
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        target = player.transform;
        playerHealth = player.GetComponent<PlayerHealth>();
    }
}