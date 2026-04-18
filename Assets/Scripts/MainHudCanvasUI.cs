using UnityEngine;
using UnityEngine.UI;

public class MainHudCanvasUI : MonoBehaviour
{
    [Header("References")]
    public PlayerHealth playerHealth;
    public PlayerLevelSystem playerLevelSystem;
    public GoldWallet goldWallet;

    [Header("Theme")]
    public Color panelColor = new Color(0.05f, 0.05f, 0.06f, 0.82f);
    public Color borderColor = new Color(0.42f, 0.38f, 0.3f, 0.95f);
    public Color hpFillColor = new Color(0.64f, 0.13f, 0.16f, 0.95f);
    public Color xpFillColor = new Color(0.29f, 0.38f, 0.66f, 0.95f);
    public Color minimapPlaceholderColor = new Color(0.03f, 0.03f, 0.05f, 0.7f);

    private Text timeText;
    private Text goldText;
    private Text hpValueText;
    private Text levelText;
    private Text xpValueText;
    private Image hpFill;
    private Image xpFill;

    void Awake()
    {
        EnsureReferences();
        BuildHud();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        EnsureReferences();
        RefreshHud();
    }

    void EnsureReferences()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerLevelSystem == null)
            playerLevelSystem = FindObjectOfType<PlayerLevelSystem>();

        if (goldWallet == null)
            goldWallet = FindObjectOfType<GoldWallet>();
    }

    void BuildHud()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        RectTransform root = GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        CreateTopLeftInfo();
        CreateBottomLeftPlayerHud();
        CreateMinimapPlaceholder();
        RefreshHud();
    }

    void CreateTopLeftInfo()
    {
        RectTransform topLeft = CreatePanel("TopLeftInfo", transform, new Vector2(20f, -20f), new Vector2(245f, 80f), new Vector2(0f, 1f), new Vector2(0f, 1f), panelColor, borderColor, 2f);
        timeText = CreateText("TimeText", topLeft, "0,0", 28, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(12f, -9f), new Vector2(220f, 34f));
        goldText = CreateText("GoldText", topLeft, "Gold: 0", 19, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(12f, -44f), new Vector2(220f, 28f));
        goldText.color = new Color(0.91f, 0.82f, 0.47f, 1f);
    }

    void CreateBottomLeftPlayerHud()
    {
        RectTransform playerHud = CreatePanel("PlayerHud", transform, new Vector2(20f, 20f), new Vector2(360f, 112f), new Vector2(0f, 0f), new Vector2(0f, 0f), panelColor, borderColor, 2f);

        levelText = CreateText("LevelText", playerHud, "LVL 1", 18, FontStyle.Bold, TextAnchor.UpperLeft, new Vector2(14f, -8f), new Vector2(120f, 24f));
        hpValueText = CreateText("HpValueText", playerHud, "100 / 100", 15, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(236f, -35f), new Vector2(110f, 20f));
        xpValueText = CreateText("XpValueText", playerHud, "0 / 5", 14, FontStyle.Bold, TextAnchor.MiddleRight, new Vector2(246f, -75f), new Vector2(100f, 18f));

        hpFill = CreateBar(playerHud, "HpBar", new Vector2(14f, -32f), new Vector2(332f, 26f), hpFillColor, new Color(0.15f, 0.07f, 0.08f, 0.92f));
        xpFill = CreateBar(playerHud, "XpBar", new Vector2(14f, -70f), new Vector2(332f, 18f), xpFillColor, new Color(0.08f, 0.1f, 0.16f, 0.92f));
    }

    void CreateMinimapPlaceholder()
    {
        RectTransform miniMapHolder = CreatePanel("MinimapPlaceholder", transform, new Vector2(-20f, 20f), new Vector2(196f, 196f), new Vector2(1f, 0f), new Vector2(1f, 0f), minimapPlaceholderColor, borderColor, 2f);
        Text placeholderText = CreateText("PlaceholderText", miniMapHolder, "MINIMAP\n(HAZIRLIK)", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(196f, 196f));
        placeholderText.color = new Color(0.67f, 0.67f, 0.71f, 0.9f);
    }

    void RefreshHud()
    {
        float elapsed = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;
        string time = elapsed.ToString("0.0").Replace('.', ',');
        if (timeText != null)
            timeText.text = time;

        if (goldText != null && goldWallet != null)
            goldText.text = $"Gold: {goldWallet.RunGold}";

        if (playerHealth != null)
        {
            float hpPercent = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;

            if (hpFill != null)
                hpFill.fillAmount = Mathf.Clamp01(hpPercent);

            if (hpValueText != null)
                hpValueText.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        }

        if (playerLevelSystem != null)
        {
            float xpPercent = playerLevelSystem.XPToNextLevel > 0
                ? (float)playerLevelSystem.CurrentXP / playerLevelSystem.XPToNextLevel
                : 0f;

            if (xpFill != null)
                xpFill.fillAmount = Mathf.Clamp01(xpPercent);

            if (levelText != null)
                levelText.text = $"LVL {playerLevelSystem.Level}";

            if (xpValueText != null)
                xpValueText.text = $"{playerLevelSystem.CurrentXP} / {playerLevelSystem.XPToNextLevel}";
        }
    }

    RectTransform CreatePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Color bgColor, Color frameColor, float borderThickness)
    {
        GameObject panelObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        panelObj.transform.SetParent(parent, false);

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image image = panelObj.GetComponent<Image>();
        image.color = bgColor;

        Outline outline = panelObj.GetComponent<Outline>();
        outline.effectColor = frameColor;
        outline.effectDistance = new Vector2(borderThickness, -borderThickness);
        outline.useGraphicAlpha = true;

        return rect;
    }

    Text CreateText(string name, RectTransform parent, string value, int fontSize, FontStyle style, TextAnchor alignment, Vector2 anchoredPos, Vector2 size)
    {
        GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text uiText = textObj.GetComponent<Text>();
        uiText.text = value;
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = fontSize;
        uiText.fontStyle = style;
        uiText.alignment = alignment;
        uiText.color = new Color(0.92f, 0.92f, 0.95f, 1f);

        return uiText;
    }

    Image CreateBar(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color fillColor, Color backgroundColor)
    {
        GameObject bgObj = new GameObject(name + "BG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(parent, false);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 1f);
        bgRect.anchoredPosition = anchoredPos;
        bgRect.sizeDelta = size;

        Image bgImage = bgObj.GetComponent<Image>();
        bgImage.color = backgroundColor;

        GameObject fillObj = new GameObject(name + "Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);

        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;

        return fillImage;
    }
}
