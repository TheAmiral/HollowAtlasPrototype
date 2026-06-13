// ─────────────────────────────────────────────────────────────────────────────
//  RuhKunai.cs
//  Assets/Scripts/Combat/RuhKunai.cs
//
//  Otomatik projectile silah. En yakın düşmana belirli aralıklarla
//  kunai fırlatır. VS'deki Magic Wand eşdeğeri.
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class RuhKunai : MonoBehaviour
{
    [Header("Stats")]
    public int   damage          = 18;
    public float fireInterval    = 0.8f;  // saniye aralık
    public int   projectileCount = 1;     // kaç kunai atılır
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
        // En yakın N düşmanı bul
        var targets = FindClosestEnemies(projectileCount);

        foreach (var target in targets)
        {
            if (target == null) continue;
            SpawnProjectile(target.transform.position);
        }

        // Hedef bulunamazsa rastgele yön
        if (targets.Count == 0)
            SpawnProjectile(transform.position + Random.insideUnitSphere.normalized * 5f);
    }

    void SpawnProjectile(Vector3 targetPos)
    {
        // Basit primitive — gerçek sprite ile değiştirilebilir
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "KunaiProjectile";
        go.transform.position = transform.position + Vector3.up * 0.8f;
        go.transform.localScale = Vector3.one * 0.22f;

        // Renk
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat != null)
            {
                mat.color = new Color(0.3f, 0.9f, 1f);  // cyan/teal
                mr.material = mat;
            }
        }

        // Collider'ı trigger yap
        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Projectile bileşeni ekle
        var proj = go.AddComponent<KunaiProjectile>();
        proj.damage   = damage;
        proj.speed    = projectileSpeed;
        proj.lifetime = projectileLifetime;
        proj.direction = (targetPos - go.transform.position).normalized;
        proj.direction.y = 0f; // yatay yüzey
        if (proj.direction == Vector3.zero) proj.direction = transform.forward;
    }

    List<EnemyHealth> FindClosestEnemies(int count)
    {
        // Sahnedeki tüm düşmanları bul, en yakını seç
        var all = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        var list = new List<EnemyHealth>();

        foreach (var e in all)
            if (e != null && !e.IsDead) list.Add(e);

        // Mesafeye göre sırala
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
//  KunaiProjectile — RuhKunai tarafından spawn edilir, bağımsız hareket eder
// ─────────────────────────────────────────────────────────────────────────────
public class KunaiProjectile : MonoBehaviour
{
    public int   damage;
    public float speed;
    public float lifetime;
    public Vector3 direction;

    float _age;

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        _age += Time.deltaTime;
        if (_age >= lifetime) Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null || enemy.IsDead) return;

        enemy.TakeDamage(damage);
        Destroy(gameObject);
    }
}
