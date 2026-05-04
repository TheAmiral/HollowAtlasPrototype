using UnityEngine;
using UnityEngine.UI;

public class RunContractUIController : MonoBehaviour
{
    private static readonly Color BorderColor         = new Color32(0xB4, 0x82, 0xDC, 0x90);
    private static readonly Color PanelBgColor        = new Color32(0x10, 0x08, 0x1E, 0xD8);
    private static readonly Color TitleColor          = new Color32(0xFF, 0xE5, 0xA0, 0xFF);
    private static readonly Color DescColor           = new Color32(0xC0, 0x98, 0xF0, 0xCC);
    private static readonly Color ProgressColor       = new Color32(0xF5, 0xE0, 0xFF, 0xFF);
    private static readonly Color ProgressDoneColor   = new Color32(0x5F, 0xD8, 0x80, 0xFF);
    private static readonly Color RewardColor         = new Color32(0xFF, 0xE0, 0x90, 0xCC);
    private static readonly Color BarTrackColor       = new Color32(0x28, 0x14, 0x3A, 0xFF);
    private static readonly Color BarFillColor        = new Color32(0x8C, 0x50, 0xC8, 0xFF);
    private static readonly Color BarFillDoneColor    = new Color32(0x5F, 0xD8, 0x80, 0xFF);

    private CanvasGroup panelGroup;
    private Text        titleText;
    private Text        descText;
    private Text        progressText;
    private Text        rewardText;
    private Image       progressFill;

    void Awake()
    {
        BuildUI();
    }

    void Update()
    {
        bool hide =
            RunContractSystem.Instance == null                                                   ||
            (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending) ||
            (BossRewardSystem.Instance  != null && BossRewardSystem.Instance.RewardPending);

        if (panelGroup != null)
            panelGroup.alpha = hide ? 0f : 1f;

        if (hide)
            return;

        RunContractSystem sys = RunContractSystem.Instance;

        if (titleText    != null) titleText.text    = sys.contractTitle;
        if (descText     != null) descText.text     = sys.contractDescription;

        if (progressText != null)
        {
            bool done          = sys.IsCompleted;
            progressText.text  = done ? "TAMAMLANDI" : $"{sys.currentValue}  /  {sys.targetValue}";
            progressText.color = done ? ProgressDoneColor : ProgressColor;
        }

        if (progressFill != null && sys.targetValue > 0)
        {
            bool done = sys.IsCompleted;
            float ratio = Mathf.Clamp01((float)sys.currentValue / sys.targetValue);
            RectTransform rt = progressFill.rectTransform;
            rt.anchorMax = new Vector2(ratio, 1f);
            progressFill.color = done ? BarFillDoneColor : BarFillColor;
        }

        if (rewardText != null)
            rewardText.text = $"Ödül: {sys.rewardGold} Gold";
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Panel outer (Image = border colour, visible as 1px frame around inner bg)
        GameObject panelGO = new GameObject("ContractPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(root, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(1f, 0f);
        panelRect.anchorMax        = new Vector2(1f, 0f);
        panelRect.pivot            = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta        = new Vector2(250f, 122f);

        panelGO.GetComponent<Image>().color = BorderColor;

        panelGroup                = panelGO.AddComponent<CanvasGroup>();
        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = false;

        // Inner background — 1 px inset so border colour shows as frame
        GameObject bgGO = MakeChild(panelRect, "Background", PanelBgColor);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2( 1f,  1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);

        // Text rows (anchored to top of panel, padding via sizeDelta.x = -20)
        titleText    = MakeTextRow(panelRect, "TitleText",    "—",     10f, 22f, TitleColor,    13, FontStyle.Bold,   font);
        descText     = MakeTextRow(panelRect, "DescText",     "",      36f, 18f, DescColor,     10, FontStyle.Normal, font);
        progressText = MakeTextRow(panelRect, "ProgressText", "0 / 0", 58f, 20f, ProgressColor, 12, FontStyle.Bold,   font);

        // Progress fill bar
        GameObject barTrack = MakeChild(panelRect, "BarTrack", BarTrackColor);
        RectTransform barTrackRect = barTrack.GetComponent<RectTransform>();
        barTrackRect.anchorMin        = new Vector2(0f, 1f);
        barTrackRect.anchorMax        = new Vector2(1f, 1f);
        barTrackRect.pivot            = new Vector2(0.5f, 1f);
        barTrackRect.anchoredPosition = new Vector2(0f, -82f);
        barTrackRect.sizeDelta        = new Vector2(-16f, 5f);

        GameObject barFillGo = MakeChild(barTrackRect, "BarFill", BarFillColor);
        RectTransform barFillRect = barFillGo.GetComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = new Vector2(0f, 1f);
        barFillRect.pivot     = new Vector2(0f, 0.5f);
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        barFillRect.sizeDelta = Vector2.zero;
        progressFill = barFillGo.GetComponent<Image>();
        progressFill.type = Image.Type.Simple;

        rewardText   = MakeTextRow(panelRect, "RewardText",   "",      92f, 18f, RewardColor,   10, FontStyle.Normal, font);
    }

    static GameObject MakeChild(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static Text MakeTextRow(RectTransform parent, string name, string content,
        float yFromTop, float height, Color color, int fontSize, FontStyle style, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 1f);
        rect.anchorMax        = new Vector2(1f, 1f);
        rect.pivot            = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -yFromTop);
        rect.sizeDelta        = new Vector2(-20f, height);

        Text text = go.GetComponent<Text>();
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
