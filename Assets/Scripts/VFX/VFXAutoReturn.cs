using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class VFXAutoReturn : MonoBehaviour
{
    [SerializeField] float _fallbackLifetime = 5f;

    ParticleSystem _ps;
    Coroutine      _watchRoutine;
    string         _poolKey;

    void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    void OnEnable()
    {
        if (_ps == null) return;
        _ps.Clear(true);
        _ps.Play(true);
    }

    void OnDisable()
    {
        if (_watchRoutine == null) return;
        StopCoroutine(_watchRoutine);
        _watchRoutine = null;
    }

    public void StartWatching(string poolKey)
    {
        _poolKey = poolKey;
        if (_watchRoutine != null) StopCoroutine(_watchRoutine);
        _watchRoutine = StartCoroutine(Watch());
    }

    IEnumerator Watch()
    {
        float elapsed = 0f;
        yield return null; // skip first frame so PS has started
        while (_ps != null && _ps.IsAlive(true))
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= _fallbackLifetime) break;
            yield return null;
        }
        ReturnToPool();
    }

    void ReturnToPool()
    {
        if (!string.IsNullOrEmpty(_poolKey) && VFXSpawner.Instance != null)
            VFXSpawner.Instance.ReturnToPool(_poolKey, gameObject);
        else
            gameObject.SetActive(false);
    }
}
