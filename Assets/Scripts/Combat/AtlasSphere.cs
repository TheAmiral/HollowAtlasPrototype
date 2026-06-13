// ─────────────────────────────────────────────────────────────────────────────
//  AtlasSphere.cs
//  Assets/Scripts/Combat/AtlasSphere.cs
//
//  Oyuncunun etrafında dönen orbit silah. VS'deki King Bible eşdeğeri.
//  Level arttıkça küre sayısı ve hasar artar.
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class AtlasSphere : MonoBehaviour
{
    [Header("Stats")]
    public int   damage       = 22;
    public float orbitRadius  = 2.8f;
    public float orbitSpeed   = 120f;   // derece / saniye
    public int   sphereCount  = 1;
    public float hitCooldown  = 0.5f;   // aynı düşmana tekrar vurma süresi

    // Runtime orb listesi
    readonly List<GameObject> _orbs     = new();
    float _angle;

    // Düşman başına cooldown takibi
    readonly Dictionary<EnemyHealth, float> _hitTimers = new();

    void Start() => RebuildOrbs();

    void Update()
    {
        // Küre sayısı değiştiyse yeniden oluştur
        if (_orbs.Count != sphereCount) RebuildOrbs();

        _angle += orbitSpeed * Time.deltaTime;
        float step = 360f / Mathf.Max(1, sphereCount);

        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;

            float rad = Mathf.Deg2Rad * (_angle + step * i);
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
            _orbs[i].transform.position = transform.position + offset + Vector3.up * 0.8f;
        }

        // Hit cooldown temizle
        var toRemove = new List<EnemyHealth>();
        var keys = new List<EnemyHealth>(_hitTimers.Keys);
        foreach (var k in keys)
        {
            _hitTimers[k] -= Time.deltaTime;
            if (_hitTimers[k] <= 0f) toRemove.Add(k);
        }
        foreach (var k in toRemove) _hitTimers.Remove(k);
    }

    void OnDestroy()
    {
        foreach (var orb in _orbs)
            if (orb != null) Destroy(orb);
        _orbs.Clear();
    }

    // ── Orb oluşturma ─────────────────────────────────────────────────────────

    void RebuildOrbs()
    {
        // Önce mevcut orb'ları temizle
        foreach (var o in _orbs) if (o != null) Destroy(o);
        _orbs.Clear();

        for (int i = 0; i < sphereCount; i++)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "AtlasOrb";
            orb.transform.localScale = Vector3.one * 0.38f;

            // Renk — altın/mor Atlas tonu
            var mr = orb.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(1f, 0.8f, 0.1f);   // altın
                    mat.SetFloat("_Smoothness", 0.9f);
                    mr.material = mat;
                }
            }

            // Collider trigger
            var col = orb.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Hit bileşeni
            var hit = orb.AddComponent<AtlasOrbHit>();
            hit.owner = this;

            _orbs.Add(orb);
        }
    }

    // ── Dış erişim: OrbHit tarafından çağrılır ────────────────────────────────

    public void OnOrbHit(EnemyHealth enemy)
    {
        if (enemy == null || enemy.IsDead) return;
        if (_hitTimers.ContainsKey(enemy)) return;   // cooldown'da

        enemy.TakeDamage(damage);
        _hitTimers[enemy] = hitCooldown;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  AtlasOrbHit — Her orb'a takılır, trigger'ları AtlasSphere'e iletir
// ─────────────────────────────────────────────────────────────────────────────
public class AtlasOrbHit : MonoBehaviour
{
    public AtlasSphere owner;

    void OnTriggerEnter(Collider other)
    {
        if (owner == null) return;
        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null) owner.OnOrbHit(enemy);
    }

    void OnTriggerStay(Collider other)
    {
        if (owner == null) return;
        var enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null) owner.OnOrbHit(enemy);
    }
}
