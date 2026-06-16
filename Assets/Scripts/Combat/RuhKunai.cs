using System.Collections.Generic;
using UnityEngine;

public class RuhKunai : MonoBehaviour
{
    [Header("Stats")]
    public int   damage          = 18;
    public float fireInterval    = 0.8f;
    public int   projectileCount = 1;
    public int   pierceCount     = 1;
    public float projectileSpeed = 14f;
    public float projectileLifetime = 2.5f;

    float _timer;

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = fireInterval;
        Fire();
    }

    void Fire()
    {
        var targets = FindClosestEnemies(projectileCount);
        foreach (var target in targets)
        {
            if (target == null) continue;
            SpawnProjectile(target.transform.position);
        }
        if (targets.Count == 0)
            SpawnProjectile(transform.position + Random.insideUnitSphere.normalized * 5f);
    }

    void SpawnProjectile(Vector3 targetPos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "KunaiProjectile";
        go.transform.position = transform.position + Vector3.up * 0.8f;
        go.transform.localScale = Vector3.one * 0.22f;

        // Collider kaldır — hasar OverlapSphere ile uygulanır, trigger'a gerek yok
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null) { mat.color = new Color(0.3f, 0.9f, 1f); mr.material = mat; }
        }

        var proj = go.AddComponent<KunaiProjectile>();
        proj.damage      = damage;
        proj.speed       = projectileSpeed;
        proj.lifetime    = projectileLifetime;
        proj.pierceCount = pierceCount;
        proj.direction   = (targetPos - go.transform.position).normalized;
        proj.direction.y = 0f;
        if (proj.direction == Vector3.zero) proj.direction = transform.forward;

        Debug.Log($"[RuhKunai] Fired projectile toward {targetPos}");
    }

    List<EnemyHealth> FindClosestEnemies(int count)
    {
        var all  = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        var list = new List<EnemyHealth>();
        foreach (var e in all) if (e != null && !e.IsDead) list.Add(e);
        list.Sort((a, b) =>
        {
            float da = Vector3.SqrMagnitude(a.transform.position - transform.position);
            float db = Vector3.SqrMagnitude(b.transform.position - transform.position);
            return da.CompareTo(db);
        });
        if (list.Count > count) list.RemoveRange(count, list.Count - count);
        return list;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  KunaiProjectile — RuhKunai tarafından spawn edilir.
//  OnTriggerEnter YOK — Physics.OverlapSphere ile hasar uygulanır.
// ─────────────────────────────────────────────────────────────────────────────
public class KunaiProjectile : MonoBehaviour
{
    public int     damage;
    public float   speed;
    public float   lifetime;
    public Vector3 direction;
    public int     pierceCount = 1;

    float _age;
    readonly HashSet<int> _hitIds = new();
    int   _pierced;

    const float HIT_RADIUS = 0.35f;

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        _age += Time.deltaTime;
        if (_age >= lifetime) { Destroy(gameObject); return; }

        var hits = Physics.OverlapSphere(transform.position, HIT_RADIUS);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy == null || enemy.IsDead) continue;
            int id = enemy.gameObject.GetInstanceID();
            if (_hitIds.Contains(id)) continue;

            _hitIds.Add(id);
            enemy.TakeDamage(damage);
            Debug.Log($"[RuhKunai] Hit enemy: {enemy.name} damage:{damage}");
            _pierced++;

            if (_pierced >= pierceCount) { Destroy(gameObject); return; }
        }
    }
}
