using UnityEngine;

public class AutoAttackAura : MonoBehaviour
{
    public float radius = 2.2f;
    public int damage = 10;
    public float tickInterval = 0.5f;
    public float yOffset = 0.5f;

    private float timer;

    void Start()
    {
        if (GetComponent<PlayerRangeIndicator>() == null)
            gameObject.AddComponent<PlayerRangeIndicator>();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer > 0f)
            return;

        timer = tickInterval;

        Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * yOffset, radius);

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * yOffset, radius);
    }
}