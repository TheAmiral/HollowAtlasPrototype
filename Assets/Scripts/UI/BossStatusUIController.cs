using UnityEngine;
using UnityEngine.UI;

public class BossStatusUIController : MonoBehaviour
{
    private static readonly Color BorderColor    = new Color32(0xB4, 0x82, 0xDC, 0x90);
    private static readonly Color PanelBgColor   = new Color32(0x10, 0x08, 0x1E, 0xD8);
    private static readonly Color TitleColor     = new Color32(0xFF, 0xE5, 0xA0, 0xFF);
    private static readonly Color CountdownColor = new Color32(0xF5, 0xE0, 0xFF, 0xFF);
    private static readonly Color ActiveColor    = new Color32(0xFF, 0x66, 0x44, 0xFF);
    private static readonly Color DefeatedColor  = new Color32(0x5F, 0xD8, 0x80, 0xFF);

    private CanvasGroup panelGroup;
    private Text titleText;
    private Text infoText;

    void Awake()
    {
        BuildUI();
    }

    void Update()
    {
        bool hide =
            BossSpawnSystem.Instance == null ||
            (GameManager.Instance != null && GameManager.Instance.IsGameOver) ||
            (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending) ||
            (BossRewardSystem.Instance  != null && BossRewardSystem.Instance.RewardPending);

        if (panelGroup != null)
            panelGroup.alpha = hide ? 0f : 1f;

        if (hide)
            return;

        var sys = BossSpawnSystem.Instance;

        if (titleText != null)
            titleText.text = sys.bossDisplayName.ToUpper();

        if (infoText != null)
        {
            if (!sys.BossSpawned)
            {
                float elapsed   = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;
                float remaining = Mathf.Max(0f, sys.NextBossTime - elapsed);
                infoText.text  = $"{remaining:0.0}s sonra geliyor";
                infoText.color = CountdownColor;
            }
            else if (!sys.BossDefeated && sys.ActiveBoss != null)
            {
                infoText.text  = "Haritada aktif";
                infoText.color = ActiveColor;
            }
            else
            {
                infoText.text  = "Yenildi";
                infoText.color = DefeatedColor;
            }
        }
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 119;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Panel — sits directly above the contract panel (contract: y=20, h=115 → top=135; gap=10 → y=145)
        GameObject panelGO = new GameObject("BossStatusPanel", typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(root, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(1f, 0f);
        panelRect.anchorMax        = new Vector2(1f, 0f);
        panelRect.pivot            = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 145f);
        panelRect.sizeDelta        = new Vector2(250f, 55f);

        panelGO.GetComponent<Image>().color = BorderColor;

        panelGroup                = panelGO.AddComponent<CanvasGroup>();
        panelGroup.interactable   = false;
        panelGroup.blocksRaycasts = false;

        // Inner background
        GameObject bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(panelRect, false);
        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2( 1f,  1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        bgGO.GetComponent<Image>().color = PanelBgColor;

        titleText = MakeTextRow(panelRect, "BossTitle", "—",  8f, 22f, TitleColor,     11, FontStyle.Bold,   font);
        infoText  = MakeTextRow(panelRect, "BossInfo",  "",  30f, 18f, CountdownColor, 10, FontStyle.Normal, font);
    }

    static Text MakeTextRow(RectTransform parent, string name, string content,
        float yFromTop, float height, Color color, int fontSize, FontStyle style, Font font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 1f);
        rect.anchorMax        = new Vector2(1f, 1f);
        rect.pivot            = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -yFromTop);
        rect.sizeDelta        = new Vector2(-20f, height);

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
