using UnityEngine;
using UnityEngine.UI;

public class AtlasResonanceHudController : MonoBehaviour
{
    private Text      signalText;
    private Transform playerTransform;

    private const float DistFar    = 25f;
    private const float DistNear   = 12f;

    private static readonly Color ColorFar    = new Color(0.55f, 0.55f, 0.75f, 0.80f);
    private static readonly Color ColorMid    = new Color(0.68f, 0.38f, 1.00f, 1.00f);
    private static readonly Color ColorNear   = new Color(1.00f, 0.60f, 1.00f, 1.00f);

    void Awake()
    {
        BuildCanvas();
    }

    void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject textGo = new GameObject("ResonanceSignal", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(canvas.transform, false);

        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(1f, 0f);
        rect.anchorMax        = new Vector2(1f, 0f);
        rect.pivot            = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-22f, 22f);
        rect.sizeDelta        = new Vector2(320f, 52f);

        signalText                   = textGo.GetComponent<Text>();
        signalText.font              = font;
        signalText.fontSize          = 15;
        signalText.fontStyle         = FontStyle.Bold;
        signalText.alignment         = TextAnchor.MiddleRight;
        signalText.horizontalOverflow = HorizontalWrapMode.Overflow;
        signalText.verticalOverflow  = VerticalWrapMode.Overflow;
        signalText.enabled           = false;
    }

    void Update()
    {
        if (signalText == null) return;

        ResolvePlayer();

        bool portalActive = PortalSpawnSystem.Instance != null && PortalSpawnSystem.Instance.PortalActive;

        if (!portalActive || playerTransform == null
            || (GameManager.Instance != null && GameManager.Instance.IsGameOver))
        {
            signalText.enabled = false;
            return;
        }

        float dist = Vector3.Distance(playerTransform.position, PortalSpawnSystem.Instance.PortalPosition);

        signalText.enabled = true;

        if (dist > DistFar)
        {
            signalText.text  = "◦  REZONANS ZAYIF";
            signalText.color = ColorFar;
        }
        else if (dist > DistNear)
        {
            signalText.text  = "◈  REZONANS ORTA";
            signalText.color = ColorMid;
        }
        else
        {
            signalText.text  = "◈◈  REZONANS GÜÇLÜ";
            signalText.color = ColorNear;
        }
    }

    void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }
}
