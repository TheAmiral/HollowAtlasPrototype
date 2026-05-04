using UnityEngine;
using UnityEngine.UI;

public class RunContractUIController : MonoBehaviour
{
    private static readonly Color BorderColor         = new Color32(0xB4, 0x82, 0xDC, 0x90);
    private static readonly Color PanelBgColor        = new Color32(0x10, 0x08, 0x1E, 0xD8);
    private static readonly Color TitleColor          = new Color32(0xFF, 0xE5, 0xA0, 0xFF);
    private static readonly Color DescColor           = new Color32(0xC0, 0x98, 0xF0, 0xCC);
    private static readonly Color ProgressColor       = new Color32(0xF5, 0xE0, 0xFF, 0xFF);
    private static readonly Color ProgressDoneColor   = new Color32(0x64, 0xF1, 0xE6, 0xFF);
    private static readonly Color RewardColor         = new Color32(0xFF, 0xE0, 0x90, 0xCC);

    private const float PanelWidth = 250f;
    private const float PanelMinHeight = 117f;
    private const float PanelSpacing = 8f;
    private const float RightMargin = 20f;
    private const float BottomMargin = 20f;

    private CanvasGroup panelGroup;
    private Text        titleText;
    private Text        descText;
    private Text        progressText;
    private Text        rewardText;

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

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(sys.contractTitle) ? "Kontrat" : sys.contractTitle;

        if (descText != null)
            descText.text = GetObjectiveText(sys);

        if (progressText != null)
        {
            bool done          = sys.IsCompleted;
            progressText.text  = GetProgressText(sys);
            progressText.color = done ? ProgressDoneColor : ProgressColor;
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

        GameObject stackGO = new GameObject(
            "ContractStack",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );
        stackGO.transform.SetParent(root, false);

        RectTransform stackRect = stackGO.GetComponent<RectTransform>();
        stackRect.anchorMin        = new Vector2(1f, 0f);
        stackRect.anchorMax        = new Vector2(1f, 0f);
        stackRect.pivot            = new Vector2(1f, 0f);
        stackRect.anchoredPosition = new Vector2(-RightMargin, BottomMargin);
        stackRect.sizeDelta        = new Vector2(PanelWidth, 0f);

        VerticalLayoutGroup stackLayout = stackGO.GetComponent<VerticalLayoutGroup>();
        stackLayout.padding                = new RectOffset(0, 0, 0, 0);
        stackLayout.spacing                = PanelSpacing;
        stackLayout.childAlignment         = TextAnchor.LowerRight;
        stackLayout.childControlWidth      = true;
        stackLayout.childControlHeight     = true;
        stackLayout.childForceExpandWidth  = true;
        stackLayout.childForceExpandHeight = false;

        ContentSizeFitter stackFitter = stackGO.GetComponent<ContentSizeFitter>();
        stackFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        stackFitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        panelGroup                = stackGO.AddComponent<CanvasGroup>();
        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = false;

        BuildContractPanel(stackRect, font);
    }

    void BuildContractPanel(RectTransform parent, Font font)
    {
        GameObject panelGO = new GameObject(
            "ContractPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement)
        );
        panelGO.transform.SetParent(parent, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelMinHeight);

        Image panelImage = panelGO.GetComponent<Image>();
        panelImage.color = BorderColor;
        panelImage.raycastTarget = false;

        LayoutElement panelLayout = panelGO.GetComponent<LayoutElement>();
        panelLayout.minWidth        = PanelWidth;
        panelLayout.preferredWidth  = PanelWidth;
        panelLayout.minHeight       = PanelMinHeight;
        panelLayout.preferredHeight = PanelMinHeight;
        panelLayout.flexibleWidth   = 0f;
        panelLayout.flexibleHeight  = 0f;

        GameObject bgGO = MakeChild(panelRect, "Background", PanelBgColor);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2( 1f,  1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);

        GameObject contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentGO.transform.SetParent(panelRect, false);

        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(12f, 8f);
        contentRect.offsetMax = new Vector2(-12f, -8f);

        VerticalLayoutGroup contentLayout = contentGO.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding                = new RectOffset(0, 0, 0, 0);
        contentLayout.spacing                = 2f;
        contentLayout.childAlignment         = TextAnchor.UpperLeft;
        contentLayout.childControlWidth      = true;
        contentLayout.childControlHeight     = true;
        contentLayout.childForceExpandWidth  = true;
        contentLayout.childForceExpandHeight = false;

        titleText    = MakeTextRow(contentRect, "TitleText",    "—",     22f, TitleColor,    13, FontStyle.Bold,   font);
        descText     = MakeTextRow(contentRect, "DescText",     "",      20f, DescColor,     11, FontStyle.Normal, font);
        progressText = MakeTextRow(contentRect, "ProgressText", "0 / 0", 24f, ProgressColor, 13, FontStyle.Bold,   font);
        rewardText   = MakeTextRow(contentRect, "RewardText",   "",      18f, RewardColor,   10, FontStyle.Normal, font);
    }

    static GameObject MakeChild(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    static Text MakeTextRow(RectTransform parent, string name, string content,
        float height, Color color, int fontSize, FontStyle style, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot     = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, height);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minHeight       = height;
        layout.preferredHeight = height;
        layout.flexibleHeight  = 0f;

        Text text = go.GetComponent<Text>();
        text.font               = font;
        text.text               = content;
        text.fontSize           = fontSize;
        text.fontStyle          = style;
        text.color              = color;
        text.alignment          = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow   = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize    = Mathf.Max(8, fontSize - 3);
        text.resizeTextMaxSize    = fontSize;
        text.raycastTarget      = false;
        return text;
    }

    static string GetObjectiveText(RunContractSystem sys)
    {
        if (sys == null)
            return string.Empty;

        switch (sys.currentContractType)
        {
            case RunContractSystem.ContractType.SurviveTime:
                return sys.targetValue > 0
                    ? $"{sys.targetValue} saniye hayatta kal"
                    : FallbackDescription(sys);

            case RunContractSystem.ContractType.CollectGold:
                return "Altın topla";

            case RunContractSystem.ContractType.KillEnemies:
                return "Düşman öldür";

            default:
                return FallbackDescription(sys);
        }
    }

    static string GetProgressText(RunContractSystem sys)
    {
        if (sys == null)
            return string.Empty;

        if (sys.IsCompleted)
            return "TAMAMLANDI";

        int target = Mathf.Max(0, sys.targetValue);
        int current = Mathf.Clamp(sys.currentValue, 0, target);

        switch (sys.currentContractType)
        {
            case RunContractSystem.ContractType.SurviveTime:
                float elapsed = GameManager.Instance != null
                    ? GameManager.Instance.ElapsedTime
                    : sys.currentValue;
                float remaining = Mathf.Max(0f, sys.targetValue - elapsed);
                return $"Kalan: {remaining:0.0} sn";

            case RunContractSystem.ContractType.CollectGold:
                return $"{current} / {target} Gold";

            case RunContractSystem.ContractType.KillEnemies:
                return $"{current} / {target}";

            default:
                return target > 0 ? $"{current} / {target}" : FallbackDescription(sys);
        }
    }

    static string FallbackDescription(RunContractSystem sys)
    {
        return string.IsNullOrWhiteSpace(sys.contractDescription)
            ? "Görev hedefi"
            : sys.contractDescription;
    }
}
