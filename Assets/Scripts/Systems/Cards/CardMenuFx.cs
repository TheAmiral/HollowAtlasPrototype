using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
//  Card menu için küçük, UI-only animasyon yardımcıları (Visual Polish Pass v2).
//  Hepsi UNSCALED zaman kullanır — kart ekranı Time.timeScale = 0 iken açıktır.
//  Yalnızca runtime'da AddComponent ile eklenir; referans yoksa sessizce durur.
// ─────────────────────────────────────────────────────────────────────────────

// Bir Graphic/Image alpha'sını yumuşakça nabız gibi değiştirir (portal/glow/hint).
public class UIPulseGlow : MonoBehaviour
{
    public float speed     = 1.5f;
    public float amplitude = 0.30f;   // baseAlpha'ya oranla salınım

    Graphic _g;
    float   _baseA;
    float   _phase;

    void Awake()
    {
        _g = GetComponent<Graphic>();
        if (_g != null) _baseA = _g.color.a;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (_g == null) return;
        float k = 1f + Mathf.Sin(Time.unscaledTime * speed + _phase) * amplitude;
        var c = _g.color;
        c.a = Mathf.Clamp01(_baseA * k);
        _g.color = c;
    }

    public static UIPulseGlow Attach(Graphic g, float speed, float amplitude)
    {
        if (g == null) return null;
        var p = g.gameObject.AddComponent<UIPulseGlow>();
        p.speed = speed; p.amplitude = amplitude;
        return p;
    }
}

// Bir RectTransform'u başlangıç konumu etrafında nazikçe gezdirir (parallax/float).
public class UIFloat : MonoBehaviour
{
    public Vector2 amplitude = new Vector2(0f, 12f);
    public float   speed     = 0.6f;

    RectTransform _rt;
    Vector2 _base;
    float   _phase;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        if (_rt != null) _base = _rt.anchoredPosition;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (_rt == null) return;
        float s = Mathf.Sin(Time.unscaledTime * speed + _phase);
        float c = Mathf.Cos(Time.unscaledTime * speed * 0.8f + _phase);
        _rt.anchoredPosition = _base + new Vector2(amplitude.x * c, amplitude.y * s);
    }

    public static UIFloat Attach(RectTransform rt, Vector2 amplitude, float speed)
    {
        if (rt == null) return null;
        var f = rt.gameObject.AddComponent<UIFloat>();
        f.amplitude = amplitude; f.speed = speed;
        return f;
    }
}
