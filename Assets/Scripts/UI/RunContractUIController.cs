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

        // Panel outer (Image = border colour, visible as 1px frame around inner bg)
        GameObject panelGO = new GameObject("ContractPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(root, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(1f, 0f);
        panelRect.anchorMax        = new Vector2(1f, 0f);
        panelRect.pivot            = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta        = new Vector2(270f, 112f);

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
        descText     = MakeTextRow(panelRect, "DescText",     "",      34f, 20f, DescColor,     11, FontStyle.Normal, font);
        progressText = MakeTextRow(panelRect, "ProgressText", "0 / 0", 58f, 24f, ProgressColor, 13, FontStyle.Bold,   font);
        rewardText   = MakeTextRow(panelRect, "RewardText",   "",      88f, 18f, RewardColor,   10, FontStyle.Normal, font);
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
        text.alignment          = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow   = VerticalWrapMode.Overflow;
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
