using UnityEngine;
using UnityEngine.UI;

public class MainHudCanvasUI : MonoBehaviour
{
    [Header("Optional UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private Text xpText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text timerText;
    [SerializeField] private Image minimapPlaceholderImage;

    private PlayerHealth playerHealth;
    private PlayerLevelSystem playerLevelSystem;
    private GoldWallet goldWallet;

    void Awake()
    {
        EnsureRuntimeHud();
        ResolveReferences();
    }

    void Update()
    {
        ResolveReferences();
        UpdateHealthAndXPBars();
        UpdateLabels();
    }

    public void UpdateHealthAndXPBars()
    {
        if (healthFillImage != null && playerHealth != null)
        {
            float healthRatio = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;

            healthFillImage.fillAmount = Mathf.Clamp01(healthRatio);
        }

        if (xpFillImage != null && playerLevelSystem != null)
        {
            float xpRatio = playerLevelSystem.XPToNextLevel > 0
                ? (float)playerLevelSystem.CurrentXP / playerLevelSystem.XPToNextLevel
                : 0f;

            xpFillImage.fillAmount = Mathf.Clamp01(xpRatio);
        }
    }

    private void UpdateLabels()
    {
        if (healthText != null && playerHealth != null)
            healthText.text = $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";

        if (xpText != null && playerLevelSystem != null)
            xpText.text = $"XP: {playerLevelSystem.CurrentXP}/{playerLevelSystem.XPToNextLevel}";

        if (levelText != null && playerLevelSystem != null)
            levelText.text = $"Lv {playerLevelSystem.Level}";

        if (goldText != null && goldWallet != null)
            goldText.text = $"Gold: {goldWallet.RunGold}";

        if (timerText != null && GameManager.Instance != null)
            timerText.text = $"Time: {GameManager.Instance.ElapsedTime:0.0}s";

        if (minimapPlaceholderImage != null)
            minimapPlaceholderImage.enabled = true;
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerLevelSystem == null)
            playerLevelSystem = FindObjectOfType<PlayerLevelSystem>();

        if (goldWallet == null)
            goldWallet = FindObjectOfType<GoldWallet>();
    }

    private void EnsureRuntimeHud()
    {
        if (healthFillImage != null && xpFillImage != null && healthText != null && xpText != null && levelText != null && goldText != null && timerText != null && minimapPlaceholderImage != null)
            return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform root = canvas.GetComponent<RectTransform>();

        if (healthFillImage == null || xpFillImage == null || healthText == null || xpText == null || levelText == null)
            BuildPlayerHud(root, font);

        if (goldText == null || timerText == null)
            BuildTopLeftHud(root, font);

        if (minimapPlaceholderImage == null)
            BuildMinimapPlaceholder(root, font);
    }

    private void BuildPlayerHud(RectTransform root, Font font)
    {
        RectTransform panel = CreatePanel(root, "PlayerHudPanel", new Vector2(20f, 20f), new Vector2(350f, 140f), TextAnchor.LowerLeft, new Color(0f, 0f, 0f, 0.45f));

        if (levelText == null)
            levelText = CreateText(panel, "LevelText", "Lv 1", new Vector2(12f, -12f), new Vector2(120f, 24f), TextAnchor.UpperLeft, 20, font);

        if (healthFillImage == null || healthText == null)
            CreateBar(panel, "Health", new Vector2(12f, -46f), new Vector2(326f, 24f), new Color(0.18f, 0.18f, 0.18f, 0.95f), new Color(0.9f, 0.2f, 0.2f, 1f), "HP: 100/100", out healthFillImage, out healthText, font);

        if (xpFillImage == null || xpText == null)
            CreateBar(panel, "XP", new Vector2(12f, -84f), new Vector2(326f, 18f), new Color(0.18f, 0.18f, 0.18f, 0.95f), new Color(0.26f, 0.68f, 1f, 1f), "XP: 0/5", out xpFillImage, out xpText, font, 15);
    }

    private void BuildTopLeftHud(RectTransform root, Font font)
    {
        RectTransform panel = CreatePanel(root, "RunHudPanel", new Vector2(20f, -20f), new Vector2(250f, 74f), TextAnchor.UpperLeft, new Color(0f, 0f, 0f, 0.45f));

        if (goldText == null)
            goldText = CreateText(panel, "GoldText", "Gold: 0", new Vector2(10f, -10f), new Vector2(220f, 26f), TextAnchor.UpperLeft, 20, font);

        if (timerText == null)
            timerText = CreateText(panel, "TimerText", "Time: 0.0s", new Vector2(10f, -40f), new Vector2(220f, 24f), TextAnchor.UpperLeft, 18, font);
    }

    private void BuildMinimapPlaceholder(RectTransform root, Font font)
    {
        RectTransform panel = CreatePanel(root, "MinimapPlaceholder", new Vector2(-20f, -20f), new Vector2(190f, 190f), TextAnchor.UpperRight, new Color(0f, 0f, 0f, 0.5f));

        minimapPlaceholderImage = panel.GetComponent<Image>();

        Text label = CreateText(panel, "Label", "MINIMAP", new Vector2(0f, -10f), new Vector2(190f, 24f), TextAnchor.UpperCenter, 18, font);
        label.alignment = TextAnchor.UpperCenter;

        GameObject cross = new GameObject("Cross", typeof(RectTransform), typeof(Image));
        cross.transform.SetParent(panel, false);
        RectTransform crossRect = cross.GetComponent<RectTransform>();
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(6f, 6f);
        crossRect.anchoredPosition = new Vector2(0f, -8f);
        Image crossImage = cross.GetComponent<Image>();
        crossImage.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();

        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                break;
            case TextAnchor.UpperRight:
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                break;
            default:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                break;
        }

        rect.pivot = new Vector2(rect.anchorMin.x, rect.anchorMin.y);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;

        return rect;
    }

    private void CreateBar(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color backgroundColor, Color fillColor, string defaultText, out Image fillImage, out Text valueText, Font font, int fontSize = 16)
    {
        GameObject bg = new GameObject(name + "BarBg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);

        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 1f);
        bgRect.anchorMax = new Vector2(0f, 1f);
        bgRect.pivot = new Vector2(0f, 1f);
        bgRect.anchoredPosition = anchoredPos;
        bgRect.sizeDelta = size;

        Image bgImage = bg.GetComponent<Image>();
        bgImage.color = backgroundColor;

        GameObject fill = new GameObject(name + "BarFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fill.GetComponent<Image>();
        fillImage.color = fillColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;

        valueText = CreateText(bgRect, name + "Text", defaultText, new Vector2(8f, -2f), new Vector2(size.x - 16f, size.y), TextAnchor.MiddleLeft, fontSize, font);
    }

    private Text CreateText(RectTransform parent, string name, string value, Vector2 anchoredPos, Vector2 size, TextAnchor anchor, int fontSize, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();

        if (anchor == TextAnchor.UpperCenter)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
        }

        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.text = value;

        return text;
    }
}
