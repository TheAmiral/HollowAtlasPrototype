using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class GameOverUIController : MonoBehaviour
{
    private static readonly Color OverlayColor = new Color32(0x04, 0x03, 0x10, 0xC8);
    private static readonly Color BorderColor = new Color32(0xC9, 0x9A, 0x44, 0xD8);
    private static readonly Color PanelColor = new Color32(0x12, 0x09, 0x22, 0xF2);
    private static readonly Color PanelInnerColor = new Color32(0x07, 0x04, 0x12, 0xF0);
    private static readonly Color TitleColor = new Color32(0xFF, 0xE3, 0x9A, 0xFF);
    private static readonly Color TextColor = new Color32(0xE8, 0xDE, 0xFF, 0xFF);
    private static readonly Color MutedTextColor = new Color32(0xA9, 0x96, 0xC8, 0xFF);
    private static readonly Color ButtonColor = new Color32(0x49, 0x25, 0x75, 0xF0);
    private static readonly Color ButtonHoverColor = new Color32(0x62, 0x36, 0x94, 0xFF);

    private CanvasGroup rootGroup;
    private Text timeText;

    void Awake()
    {
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        GameManager manager = GameManager.Instance;
        bool show = manager != null && manager.IsGameOver;

        SetVisible(show);

        if (!show || timeText == null)
            return;

        timeText.text = $"S\u00FCre: {manager.ElapsedTime:0.0}s";
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 240;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        rootGroup = gameObject.AddComponent<CanvasGroup>();

        EnsureEventSystem();

        RectTransform root = GetComponent<RectTransform>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject overlay = MakePanel(root, "Overlay", OverlayColor);
        Stretch(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject focus = MakePanel(root, "CenterFocus", new Color32(0x25, 0x10, 0x46, 0x64));
        RectTransform focusRect = focus.GetComponent<RectTransform>();
        focusRect.anchorMin = focusRect.anchorMax = new Vector2(0.5f, 0.5f);
        focusRect.pivot = new Vector2(0.5f, 0.5f);
        focusRect.sizeDelta = new Vector2(720f, 360f);
        focusRect.anchoredPosition = Vector2.zero;

        GameObject panel = MakePanel(root, "GameOverPanel", BorderColor);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520f, 300f);
        panelRect.anchoredPosition = Vector2.zero;

        GameObject bg = MakePanel(panelRect, "PanelBg", PanelColor);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        Stretch(bgRect, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

        GameObject inner = MakePanel(bgRect, "PanelInner", PanelInnerColor);
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        Stretch(innerRect, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));

        Text title = MakeText(innerRect, "Title", "GAME OVER", 34f, 64f, TitleColor, 48, FontStyle.Bold, font);
        title.alignment = TextAnchor.MiddleCenter;

        GameObject line = MakePanel(innerRect, "TitleLine", new Color32(0xFF, 0xD2, 0x70, 0xCC));
        RectTransform lineRect = line.GetComponent<RectTransform>();
        lineRect.anchorMin = lineRect.anchorMax = new Vector2(0.5f, 1f);
        lineRect.pivot = new Vector2(0.5f, 1f);
        lineRect.sizeDelta = new Vector2(240f, 2f);
        lineRect.anchoredPosition = new Vector2(0f, -104f);

        timeText = MakeText(innerRect, "TimeText", "S\u00FCre: 0.0s", 120f, 32f, TextColor, 22, FontStyle.Bold, font);
        timeText.alignment = TextAnchor.MiddleCenter;

        Text hint = MakeText(innerRect, "RestartHint", "R ile yeniden ba\u015Flat", 154f, 28f, MutedTextColor, 18, FontStyle.Normal, font);
        hint.alignment = TextAnchor.MiddleCenter;

        Button restartButton = MakeButton(innerRect, font);
        restartButton.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartGame();
        });

        overlay.GetComponent<Image>().raycastTarget = true;
        focus.GetComponent<Image>().raycastTarget = false;
        panel.GetComponent<Image>().raycastTarget = false;
        bg.GetComponent<Image>().raycastTarget = false;
        inner.GetComponent<Image>().raycastTarget = false;
        line.GetComponent<Image>().raycastTarget = false;
    }

    void SetVisible(bool visible)
    {
        if (rootGroup == null)
            return;

        rootGroup.alpha = visible ? 1f : 0f;
        rootGroup.interactable = visible;
        rootGroup.blocksRaycasts = visible;
    }

    static Button MakeButton(RectTransform parent, Font font)
    {
        GameObject go = MakePanel(parent, "RestartButton", ButtonColor);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(230f, 44f);
        rect.anchoredPosition = new Vector2(0f, -204f);

        Image image = go.GetComponent<Image>();
        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = new Color32(0x2E, 0x18, 0x55, 0xFF);
        colors.selectedColor = ButtonHoverColor;
        colors.disabledColor = new Color32(0x18, 0x12, 0x25, 0x88);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text label = MakeText(rect, "Label", "Yeniden Ba\u015Flat", 0f, 44f, Color.white, 18, FontStyle.Bold, font);
        label.alignment = TextAnchor.MiddleCenter;

        return button;
    }

    static Text MakeText(RectTransform parent, string name, string content, float topOffset, float height,
        Color color, int fontSize, FontStyle style, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        rect.sizeDelta = new Vector2(-32f, height);

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    static GameObject MakePanel(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    static void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            Destroy(standalone);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }
}
