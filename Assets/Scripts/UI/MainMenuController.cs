using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;

public class MainMenuController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("Button Positions (canvas pixels from center)")]
    [SerializeField] private Vector2 enterButtonPosition    = new Vector2(-575f, -205f);
    [SerializeField] private Vector2 enterButtonSize        = new Vector2(660f, 85f);
    [SerializeField] private Vector2 settingsButtonPosition = new Vector2(-575f, -305f);
    [SerializeField] private Vector2 settingsButtonSize     = new Vector2(660f, 85f);

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Prototype_01";

    private GameObject settingsPanel;
    private bool settingsOpen;

    void Start()
    {
        EnsureEventSystem();
        BuildUI();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && settingsOpen)
            CloseSettings();
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform root = canvasGO.GetComponent<RectTransform>();

        BuildBackground(root);
        BuildTransparentButton(root, "EnterButton",    enterButtonPosition,    enterButtonSize,    LoadGameScene);
        BuildTransparentButton(root, "SettingsButton", settingsButtonPosition, settingsButtonSize, OpenSettings);

        settingsPanel = BuildSettingsPanel(root);
        settingsPanel.SetActive(false);
    }

    void BuildBackground(RectTransform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);

        RectTransform rt = bg.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = bg.AddComponent<Image>();

        if (backgroundSprite == null)
        {
            Debug.LogWarning("[MainMenuController] backgroundSprite atanmamış — koyu fallback rengi kullanılıyor.");
            img.color = new Color(0.04f, 0.04f, 0.06f, 1f);
        }
        else
        {
            img.sprite = backgroundSprite;
            img.preserveAspect = false;
        }
    }

    static void BuildTransparentButton(RectTransform parent, string name, Vector2 position, Vector2 size, UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = Color.clear;

        Button btn = go.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(onClick);
    }

    GameObject BuildSettingsPanel(RectTransform parent)
    {
        GameObject overlay = new GameObject("SettingsOverlay");
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRt = overlay.AddComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject card = new GameObject("SettingsCard");
        card.transform.SetParent(overlay.transform, false);

        RectTransform cardRt = card.AddComponent<RectTransform>();
        cardRt.anchoredPosition = Vector2.zero;
        cardRt.sizeDelta = new Vector2(560f, 420f);

        Image cardImg = card.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.06f, 0.09f, 0.97f);

        CreateLabel(card.transform, "TitleLabel",    "AYARLAR",                    new Vector2(0f,  160f), 32);
        CreateLabel(card.transform, "AudioLabel",    "Ses Ayarları   (yakında)",   new Vector2(0f,   65f), 22);
        CreateLabel(card.transform, "DisplayLabel",  "Görüntü        (yakında)",   new Vector2(0f,    0f), 22);
        CreateLabel(card.transform, "ControlsLabel", "Kontroller     (yakında)",   new Vector2(0f,  -65f), 22);

        // BackLabel oluşturulur önce (raycastTarget=false), ardından BackButton üste gelir
        CreateLabel(card.transform, "BackLabel", "GERİ", new Vector2(0f, -155f), 22);
        BuildTransparentButton(cardRt, "BackButton", new Vector2(0f, -155f), new Vector2(280f, 52f), CloseSettings);

        return overlay;
    }

    static void CreateLabel(Transform parent, string name, string text, Vector2 position, int fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(560f, 50f);

        Text label = go.AddComponent<Text>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.raycastTarget = false;
    }

    void LoadGameScene() => SceneManager.LoadScene(gameSceneName);
    void OpenSettings()  { settingsOpen = true;  settingsPanel.SetActive(true);  }
    void CloseSettings() { settingsOpen = false; settingsPanel.SetActive(false); }
}
