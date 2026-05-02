using UnityEngine;
using UnityEngine.UI;

public class BossHealthCanvasUI : MonoBehaviour
{
    private static readonly Color BorderColor  = new Color32(0xA0, 0x20, 0x20, 0xC0);
    private static readonly Color BgColor      = new Color32(0x08, 0x04, 0x12, 0xEE);
    private static readonly Color FillBgColor  = new Color32(0x1A, 0x08, 0x08, 0xFF);
    private static readonly Color FillColor    = new Color32(0xCC, 0x20, 0x20, 0xFF);
    private static readonly Color FillLowColor = new Color32(0xFF, 0x55, 0x10, 0xFF);
    private static readonly Color NameColor    = new Color32(0xFF, 0xF0, 0xF0, 0xFF);
    private static readonly Color HpNumColor   = new Color32(0xFF, 0xC0, 0xC0, 0xCC);

    private const float PULSE_DURATION = 0.18f;

    private CanvasGroup panelGroup;
    private RectTransform fillRect;
    private RectTransform pulseOverlayRect;
    private Image fillImage;
    private CanvasGroup pulseOverlayGroup;
    private Text bossNameText;
    private Text hpText;

    private EnemyHealth trackedBoss;
    private int previousHealth = -1;
    private float pulseTimer;
    private float displayFill = 1f;

    void Awake()
    {
        BuildUI();
    }

    void Update()
    {
        if (BossSpawnSystem.Instance == null)
        {
            SetVisible(false);
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            SetVisible(false);
            return;
        }

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
        {
            SetVisible(false);
            return;
        }

        EnemyHealth boss = BossSpawnSystem.Instance.ActiveBoss;

        if (boss == null || boss.IsDead || !boss.IsBoss)
        {
            SetVisible(false);
            trackedBoss   = null;
            previousHealth = -1;
            displayFill   = 1f;
            return;
        }

        SetVisible(true);

        // Track new boss
        if (trackedBoss != boss)
        {
            trackedBoss    = boss;
            previousHealth = boss.CurrentHealth;
            displayFill    = 1f;
            pulseTimer     = 0f;
        }

        // Detect hit
        if (previousHealth >= 0 && boss.CurrentHealth < previousHealth)
            pulseTimer = PULSE_DURATION;
        previousHealth = boss.CurrentHealth;

        if (pulseTimer > 0f)
            pulseTimer -= Time.deltaTime;

        // Smooth fill
        float targetFill = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth) : 0f;
        displayFill = Mathf.Lerp(displayFill, targetFill, Time.deltaTime * 9f);

        // Apply fill via anchorMax
        if (fillRect != null)
        {
            fillRect.anchorMax = new Vector2(displayFill, 1f);
            fillRect.offsetMax = Vector2.zero;
            fillRect.offsetMin = Vector2.zero;
        }

        if (pulseOverlayRect != null)
        {
            pulseOverlayRect.anchorMax = new Vector2(displayFill, 1f);
            pulseOverlayRect.offsetMax = Vector2.zero;
            pulseOverlayRect.offsetMin = Vector2.zero;
        }

        // Pulse alpha
        if (pulseOverlayGroup != null)
            pulseOverlayGroup.alpha = pulseTimer > 0f ? (pulseTimer / PULSE_DURATION) * 0.45f : 0f;

        // Fill color: interpolate to orange-red when HP < 25%
        if (fillImage != null)
        {
            float t = Mathf.Clamp01(1f - targetFill / 0.25f);
            fillImage.color = Color.Lerp(FillColor, FillLowColor, t);
        }

        // Texts
        if (bossNameText != null)
            bossNameText.text = boss.BossDisplayName.ToUpper();

        if (hpText != null)
            hpText.text = $"{boss.CurrentHealth} / {boss.MaxHealth}";
    }

    void SetVisible(bool visible)
    {
        if (panelGroup != null)
            panelGroup.alpha = visible ? 1f : 0f;
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 122;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Outer border panel — top center
        GameObject panelGO = new GameObject("BossHpPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(root, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 1f);
        panelRect.anchorMax        = new Vector2(0.5f, 1f);
        panelRect.pivot            = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -8f);
        panelRect.sizeDelta        = new Vector2(540f, 52f);

        panelGO.GetComponent<Image>().color = BorderColor;

        panelGroup                = panelGO.AddComponent<CanvasGroup>();
        panelGroup.alpha          = 0f;
        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = false;

        // Inner dark background (1px inset)
        GameObject bgGO = new GameObject("BgInner", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(panelRect, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(1f, 1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        bgGO.GetComponent<Image>().color = BgColor;

        RectTransform bgInnerRect = bgRect;

        // Fill background — occupies the inner bar area
        GameObject fillBgGO = new GameObject("FillBg", typeof(RectTransform), typeof(Image));
        fillBgGO.transform.SetParent(bgInnerRect, false);
        var fillBgRect = fillBgGO.GetComponent<RectTransform>();
        fillBgRect.anchorMin = Vector2.zero;
        fillBgRect.anchorMax = Vector2.one;
        fillBgRect.offsetMin = new Vector2(8f, 10f);
        fillBgRect.offsetMax = new Vector2(-8f, -10f);
        fillBgGO.GetComponent<Image>().color = FillBgColor;
        fillBgGO.GetComponent<Image>().raycastTarget = false;

        // HP fill — anchored left, width scales via anchorMax.x
        GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGO.transform.SetParent(fillBgRect, false);
        fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fillImage = fillGO.GetComponent<Image>();
        fillImage.color = FillColor;
        fillImage.raycastTarget = false;

        // Hit pulse overlay (same size as fill, white transparent)
        GameObject pulseGO = new GameObject("PulseOverlay", typeof(RectTransform), typeof(Image));
        pulseGO.transform.SetParent(fillBgRect, false);
        pulseOverlayRect = pulseGO.GetComponent<RectTransform>();
        pulseOverlayRect.anchorMin = new Vector2(0f, 0f);
        pulseOverlayRect.anchorMax = new Vector2(1f, 1f);
        pulseOverlayRect.offsetMin = Vector2.zero;
        pulseOverlayRect.offsetMax = Vector2.zero;
        pulseGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
        pulseGO.GetComponent<Image>().raycastTarget = false;
        pulseOverlayGroup         = pulseGO.AddComponent<CanvasGroup>();
        pulseOverlayGroup.alpha   = 0f;
        pulseOverlayGroup.interactable   = false;
        pulseOverlayGroup.blocksRaycasts = false;

        // Boss name text — rendered on top of fill bar (child of panelRect, not bgInner)
        bossNameText = MakeText(panelRect, "BossName", "—",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(10f, 0f), offsetMax: new Vector2(-80f, 0f),
            fontSize: 15, style: FontStyle.Bold, color: NameColor, font: font);
        bossNameText.alignment = TextAnchor.MiddleCenter;

        // HP numbers — right-aligned
        hpText = MakeText(panelRect, "HpText", "— / —",
            anchorMin: new Vector2(1f, 0f), anchorMax: new Vector2(1f, 1f),
            offsetMin: new Vector2(-80f, 0f), offsetMax: new Vector2(-10f, 0f),
            fontSize: 11, style: FontStyle.Bold, color: HpNumColor, font: font);
        hpText.alignment = TextAnchor.MiddleRight;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        int fontSize, FontStyle style, Color color, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = go.GetComponent<Text>();
        text.font               = font;
        text.text               = content;
        text.fontSize           = fontSize;
        text.fontStyle          = style;
        text.color              = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow   = VerticalWrapMode.Overflow;
        text.raycastTarget      = false;
        return text;
    }
}
