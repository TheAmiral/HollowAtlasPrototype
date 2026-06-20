using System.Collections.Generic;
using UnityEngine;

public class AtlasSphere : MonoBehaviour
{
    [Header("Stats")]
    public int   damage       = 22;
    public float orbitRadius  = 2.8f;
    public float orbitSpeed   = 120f;
    public int   sphereCount  = 1;
    public float hitCooldown  = 0.5f;

    [SerializeField] bool debugLogs = false;

    readonly List<GameObject> _orbs = new();
    float _angle;

    // Düşman başına cooldown: EnemyHealth ref yerine instanceID kullan
    // (destroy edilmiş objeye referans tutmamak için)
    readonly Dictionary<int, float> _hitTimers = new();

    const float HIT_RADIUS = 0.35f;

    void Start() => RebuildOrbs();

    void Update()
    {
        if (_orbs.Count != sphereCount) RebuildOrbs();

        _angle += orbitSpeed * Time.deltaTime;
        float step = 360f / Mathf.Max(1, sphereCount);

        for (int i = 0; i < _orbs.Count; i++)
        {
            if (_orbs[i] == null) continue;

            float rad    = Mathf.Deg2Rad * (_angle + step * i);
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * orbitRadius;
            Vector3 orbPos = transform.position + offset + Vector3.up * 0.8f;
            _orbs[i].transform.position = orbPos;

            // OverlapSphere hasar tespiti — Rigidbody/trigger gerektirmez
            var hits = Physics.OverlapSphere(orbPos, HIT_RADIUS);
            foreach (var hit in hits)
            {
                var enemy = hit.GetComponent<EnemyHealth>();
                if (enemy == null || enemy.IsDead) continue;
                int id = enemy.gameObject.GetInstanceID();
                if (_hitTimers.ContainsKey(id)) continue;

                enemy.TakeDamage(damage);
                _hitTimers[id] = hitCooldown;
                if (debugLogs) Debug.Log($"[AtlasSphere] Hit enemy: {enemy.name} damage:{damage}");
            }
        }

        // Hit cooldown temizle
        var keys = new List<int>(_hitTimers.Keys);
        foreach (var k in keys)
        {
            _hitTimers[k] -= Time.deltaTime;
            if (_hitTimers[k] <= 0f) _hitTimers.Remove(k);
        }
    }

    void OnDestroy()
    {
        foreach (var orb in _orbs) if (orb != null) Destroy(orb);
        _orbs.Clear();
    }

    void RebuildOrbs()
    {
        foreach (var o in _orbs) if (o != null) Destroy(o);
        _orbs.Clear();

        for (int i = 0; i < sphereCount; i++)
        {
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "AtlasOrb";
            orb.transform.localScale = Vector3.one * 0.38f;

            // Collider kaldır — hasar OverlapSphere ile uygulanır
            var col = orb.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var mr = orb.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat != null)
                {
                    mat.color = new Color(1f, 0.8f, 0.1f);
                    mat.SetFloat("_Smoothness", 0.9f);
                    mr.material = mat;
                }
            }

            _orbs.Add(orb);
        }
    }
}
