using System.Collections.Generic;
using UnityEngine;

public class VFXPool
{
    readonly GameObject _prefab;
    readonly Transform  _parent;
    readonly Queue<GameObject> _inactive = new Queue<GameObject>();

    public VFXPool(GameObject prefab, Transform parent, int prewarm = 4)
    {
        _prefab = prefab;
        _parent = parent;
        for (int i = 0; i < prewarm; i++)
        {
            var go = Object.Instantiate(_prefab, _parent);
            go.SetActive(false);
            _inactive.Enqueue(go);
        }
    }

    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject go = _inactive.Count > 0
            ? _inactive.Dequeue()
            : Object.Instantiate(_prefab, _parent);

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        if (_parent != null)
            go.transform.SetParent(_parent, false);
        _inactive.Enqueue(go);
    }
}
