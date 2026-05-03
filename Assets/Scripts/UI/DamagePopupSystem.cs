using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamagePopupSystem : MonoBehaviour
{
    static DamagePopupSystem instance;

    const int PoolSize = 24;
    const float Duration = 0.90f;
    const float RisePixels = 62f;

    RectTransform canvasRt;
    readonly PopupItem[] pool = new PopupItem[PoolSize];
    int nextIndex;

    class PopupItem
    {
        public GameObject go;
        public RectTransform rt;
        public TextMeshProUGUI label;
        public Coroutine routine;
        public bool busy;
    }

    public static void ShowDamage(Vector3 worldPos, int amount)
        => GetOrCreate()?.Spawn(worldPos, amount.ToString(), new Color(1f, 0.92f, 0.35f, 1f));

    public static void ShowGold(Vector3 worldPos, int amount)
        => GetOrCreate()?.Spawn(worldPos, "+" + amount + " G", new Color(1.0f, 0.82f, 0.22f, 1f));

    public static void ShowXP(Vector3 worldPos, int amount)
        => GetOrCreate()?.Spawn(worldPos, "+" + amount + " XP", new Color(0.42f, 1.0f, 0.52f, 1f));

    static DamagePopupSystem GetOrCreate()
    {
        if (instance != null)
            return instance;

        GameObject go = new GameObject("[DamagePopupSystem]");
        DontDestroyOnLoad(go);
        return go.AddComponent<DamagePopupSystem>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildCanvas();
        BuildPool();
    }

    void BuildCanvas()
    {
        GameObject cgo = new GameObject("Canvas");
        cgo.transform.SetParent(transform, false);

        Canvas canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRt = cgo.GetComponent<RectTransform>();
    }

    void BuildPool()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            GameObject go = new GameObject("P" + i);
            go.transform.SetParent(canvasRt, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(140f, 36f);

            TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 24f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.outlineWidth = 0.22f;
            label.outlineColor = new Color32(5, 3, 12, 210);

            go.SetActive(false);

            pool[i] = new PopupItem { go = go, rt = rt, label = label };
        }
    }

    void Spawn(Vector3 worldPos, string text, Color color)
    {
        if (Camera.main == null || canvasRt == null)
            return;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos + Vector3.up * 0.6f);
        if (screenPos.z < 0f)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRt, screenPos, null, out Vector2 localPos))
            return;

        localPos.x += Random.Range(-16f, 16f);

        PopupItem item = Claim();
        item.rt.anchoredPosition = localPos;
        item.label.text = text;
        item.label.color = color;
        item.go.SetActive(true);
        item.busy = true;

        if (item.routine != null)
            StopCoroutine(item.routine);
        item.routine = StartCoroutine(Animate(item, localPos, color));
    }

    IEnumerator Animate(PopupItem item, Vector2 origin, Color fullColor)
    {
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Duration);

            // Ease-out rise
            float ease = 1f - (1f - t) * (1f - t);
            item.rt.anchoredPosition = origin + new Vector2(0f, ease * RisePixels);

            // Solid for first 40%, fade out over remaining 60%
            float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
            Color c = fullColor;
            c.a = alpha;
            item.label.color = c;

            yield return null;
        }

        item.go.SetActive(false);
        item.busy = false;
        item.routine = null;
    }

    PopupItem Claim()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            int idx = (nextIndex + i) % PoolSize;
            if (!pool[idx].busy)
            {
                nextIndex = (idx + 1) % PoolSize;
                return pool[idx];
            }
        }

        // All busy: forcibly reclaim next in rotation
        PopupItem stolen = pool[nextIndex];
        if (stolen.routine != null)
            StopCoroutine(stolen.routine);
        stolen.busy = false;
        nextIndex = (nextIndex + 1) % PoolSize;
        return stolen;
    }
}
