using System;
using System.Collections.Generic;
using UnityEngine;

public class VFXSpawner : MonoBehaviour
{
    public static VFXSpawner Instance { get; private set; }

    [Serializable]
    public struct VFXEntry
    {
        public string     key;
        public GameObject prefab;
        [Min(1)] public int prewarmCount;
    }

    [SerializeField] VFXEntry[] _entries;

    readonly Dictionary<string, VFXPool> _pools = new Dictionary<string, VFXPool>();

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // Intentionally NOT DontDestroyOnLoad — lives in the game scene and is
        // cleaned up naturally on scene unload, avoiding stale pool state.
        InitPools();
    }

    void InitPools()
    {
        _pools.Clear();
        if (_entries == null) return;
        foreach (var entry in _entries)
        {
            if (string.IsNullOrEmpty(entry.key) || entry.prefab == null) continue;
            int prewarm = Mathf.Max(1, entry.prewarmCount);
            _pools[entry.key] = new VFXPool(entry.prefab, transform, prewarm);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void Spawn(string key, Vector3 position)
        => Spawn(key, position, Quaternion.identity);

    public void Spawn(string key, Vector3 position, Quaternion rotation)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[VFXSpawner] No pool registered for key '{key}'. Assign the prefab in Inspector.");
#endif
            return;
        }

        var go = pool.Get(position, rotation);
        go.GetComponent<VFXAutoReturn>()?.StartWatching(key);
    }

    public void ReturnToPool(string key, GameObject go)
    {
        if (go == null) return;
        if (_pools.TryGetValue(key, out var pool))
            pool.Return(go);
        else
            go.SetActive(false);
    }
}
