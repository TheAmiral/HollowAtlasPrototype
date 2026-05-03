using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using UnityEngine.Events;

public class MainMenuController : MonoBehaviour
{
    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;

    [Header("Button Positions (canvas pixels from center)")]
    [SerializeField] private Vector2 enterButtonPosition = new Vector2(0f, -90f);
    [SerializeField] private Vector2 enterButtonSize = new Vector2(520f, 66f);
    [SerializeField] private Vector2 settingsButtonPosition = new Vector2(0f, -175f);
    [SerializeField] private Vector2 settingsButtonSize = new Vector2(520f, 66f);
    [SerializeField] private Vector2 quitButtonPosition = new Vector2(0f, -260f);
    [SerializeField] private Vector2 quitButtonSize = new Vector2(520f, 66f);

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Prototype_01";

    const string MasterVolumeKey = "HollowAtlas.MasterVolume";
    const string MusicVolumeKey = "HollowAtlas.MusicVolume";
    const string SfxVolumeKey = "HollowAtlas.SfxVolume";
    const string BrightnessKey = "HollowAtlas.Brightness";
    const string FullscreenKey = "HollowAtlas.Fullscreen";
    const string ShowRangeKey = "HollowAtlas.ShowRangeIndicator";
    const float BrightnessMin = 0.80f;
    const float BrightnessMax = 1.30f;

    static readonly Color DeepBlack = new Color(0.018f, 0.014f, 0.032f, 1f);
    static readonly Color PanelDark = new Color(0.040f, 0.030f, 0.082f, 0.94f);
    static readonly Color PanelInset = new Color(0.065f, 0.046f, 0.120f, 0.96f);
    static readonly Color Gold = new Color(1.000f, 0.780f, 0.360f, 1f);
    static readonly Color PaleGold = new Color(1.000f, 0.925f, 0.650f, 1f);
    static readonly Color Cyan = new Color(0.370f, 0.925f, 1.000f, 1f);
    static readonly Color TextPrimary = new Color(0.960f, 0.945f, 0.900f, 1f);
    static readonly Color TextMuted = new Color(0.660f, 0.720f, 0.790f, 1f);
    GameObject settingsPanel;
    CanvasGroup mainGroup;
    Image menuDimImage;
    Image centerFocusImage;
    Image leftVeilImage;
    Button enterButton;
    Button settingsButton;
    Button quitButton;
    MainMenuButtonVisual enterVisual;
    MainMenuButtonVisual settingsVisual;
    MainMenuButtonVisual quitVisual;
    MainMenuButtonVisual applyVisual;
    MainMenuButtonVisual backVisual;
    Slider masterSlider;
    Slider musicSlider;
    Slider sfxSlider;
    Slider brightnessSlider;
    Toggle fullscreenToggle;
    Toggle showRangeToggle;
    Text masterValueText;
    Text musicValueText;
    Text sfxValueText;
    Text brightnessValueText;
    bool settingsOpen;

    float masterVolume;
    float musicVolume;
    float sfxVolume;
    float brightness;
    bool fullscreen;
    bool showRangeIndicator;

    static Font cachedFont;
    static TMP_FontAsset cachedTMPFont;

    static Font UiFont
    {
        get
        {
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return cachedFont;
        }
    }

    static TMP_FontAsset GetOrCreateTMPFont()
    {
        if (cachedTMPFont != null)
            return cachedTMPFont;

        // DynamicOS: glyphs from system Arial at runtime — full Turkish/Unicode support
        cachedTMPFont = TMP_FontAsset.CreateFontAsset("Arial", "Regular");
        if (cachedTMPFont != null)
            return cachedTMPFont;

        // Fallback: dynamic atlas from Unity's built-in font (also Arial on Windows)
        Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont != null)
            cachedTMPFont = TMP_FontAsset.CreateFontAsset(builtinFont);

        return cachedTMPFont;
    }

    void Start()
    {
        EnsureEventSystem();
        LoadSettings();
        ApplySettingsToRuntime();
        BuildUI();
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (settingsOpen)
            CloseSettings();
    }

    void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            GameObject es = new GameObject("EventSystem");
            eventSystem = es.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
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
        BuildMainPanel(root);

        settingsPanel = BuildSettingsPanel(root);
        settingsPanel.SetActive(false);
    }

    void BuildBackground(RectTransform parent)
    {
        GameObject bg = MakePanel(parent, "Background", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, DeepBlack);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.raycastTarget = false;

        if (backgroundSprite != null)
        {
            Image artImage = MakePanel(parent, "BackgroundArt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white).GetComponent<Image>();
            artImage.sprite = backgroundSprite;
            artImage.preserveAspect = false;
            artImage.raycastTarget = false;
        }

        menuDimImage = MakePanel(parent, "MenuDim", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.008f, 0.005f, 0.016f, 0.12f)).GetComponent<Image>();
        menuDimImage.raycastTarget = false;

        RectTransform focus = MakePanel(parent, "CenterFocusShade", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1180f, 720f), new Color(0.010f, 0.006f, 0.020f, 0.04f)).GetComponent<RectTransform>();
        centerFocusImage = focus.GetComponent<Image>();
        centerFocusImage.raycastTarget = false;

        leftVeilImage = MakePanel(parent, "LeftVeil", Vector2.zero, new Vector2(0.60f, 1f), Vector2.zero, Vector2.zero, new Color(0.005f, 0.003f, 0.010f, 0.38f)).GetComponent<Image>();
        leftVeilImage.raycastTarget = false;

        ApplyMenuBrightnessVisuals();
    }

    void BuildMainPanel(RectTransform parent)
    {
        GameObject main = new GameObject("MainMenuRoot");
        main.transform.SetParent(parent, false);
        RectTransform mainRt = main.AddComponent<RectTransform>();
        mainRt.anchorMin = mainRt.anchorMax = new Vector2(0.5f, 0.5f);
        mainRt.pivot = new Vector2(0.5f, 0.5f);
        mainRt.sizeDelta = new Vector2(700f, 700f);
        mainRt.anchoredPosition = new Vector2(-360f, 0f);

        mainGroup = main.AddComponent<CanvasGroup>();

        TextMeshProUGUI title = MakeTMPText(mainRt, "Title", "HOLLOW ATLAS", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 185f), new Vector2(620f, 100f), 68f, FontStyles.Bold, TextPrimary);
        title.outlineWidth = 0.20f;
        title.outlineColor = new Color32(12, 6, 25, 210);
        title.characterSpacing = 6f;

        TextMeshProUGUI subtitle = MakeTMPText(mainRt, "Subtitle", "KAYIP HARITANIN DERİNLİKLERİ", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(580f, 28f), 14f, FontStyles.Normal, TextMuted);
        subtitle.characterSpacing = 5f;

        Image topLine = MakePanel(mainRt, "TitleGoldLine", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 82f), new Vector2(420f, 2f), Gold).GetComponent<Image>();
        topLine.raycastTarget = false;

        Image cyanLineImage = MakePanel(mainRt, "TitleCyanLine", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 76f), new Vector2(230f, 2f), Cyan).GetComponent<Image>();
        cyanLineImage.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f);
        cyanLineImage.raycastTarget = false;

        ResolveMainButtonLayout(out Vector2 enterPos, out Vector2 enterSize, out Vector2 settingsPos, out Vector2 settingsSize, out Vector2 quitPos, out Vector2 quitSize);
        enterButton = BuildMenuButton(mainRt, "EnterButton", "ATLAS'A GİR", enterPos, enterSize, LoadGameScene, true);
        enterVisual = enterButton.GetComponent<MainMenuButtonVisual>();
        settingsButton = BuildMenuButton(mainRt, "SettingsButton", "AYARLAR", settingsPos, settingsSize, OpenSettings, false);
        settingsVisual = settingsButton.GetComponent<MainMenuButtonVisual>();
        quitButton = BuildMenuButton(mainRt, "QuitButton", "OYUNDAN ÇIK", quitPos, quitSize, QuitGame, false);
        quitVisual = quitButton.GetComponent<MainMenuButtonVisual>();

        main.AddComponent<MenuAtmosphereAnimator>().Configure(cyanLineImage);
    }

    void ResolveMainButtonLayout(out Vector2 enterPos, out Vector2 enterSize, out Vector2 settingsPos, out Vector2 settingsSize, out Vector2 quitPos, out Vector2 quitSize)
    {
        bool sceneHasLegacyHitboxLayout = enterButtonPosition.x < -400f || settingsButtonPosition.x < -400f;

        enterPos = sceneHasLegacyHitboxLayout ? new Vector2(0f, -90f) : enterButtonPosition;
        settingsPos = sceneHasLegacyHitboxLayout ? new Vector2(0f, -175f) : settingsButtonPosition;
        quitPos = sceneHasLegacyHitboxLayout ? new Vector2(0f, -260f) : quitButtonPosition;
        enterSize = sceneHasLegacyHitboxLayout ? new Vector2(520f, 66f) : enterButtonSize;
        settingsSize = sceneHasLegacyHitboxLayout ? new Vector2(520f, 66f) : settingsButtonSize;
        quitSize = sceneHasLegacyHitboxLayout ? new Vector2(520f, 66f) : quitButtonSize;
    }

    Button BuildMenuButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size, UnityAction onClick, bool primary)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image raycastSurface = go.AddComponent<Image>();
        raycastSurface.color = new Color(0f, 0f, 0f, 0.001f);
        raycastSurface.raycastTarget = true;

        Image glow = MakePanel(rt, "Glow", Vector2.zero, Vector2.one, new Vector2(-10f, -10f), new Vector2(10f, 10f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.0f)).GetComponent<Image>();
        glow.raycastTarget = false;

        Image border = MakePanel(rt, "Border", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, primary ? new Color(Gold.r, Gold.g, Gold.b, 0.82f) : new Color(Cyan.r, Cyan.g, Cyan.b, 0.70f)).GetComponent<Image>();
        border.raycastTarget = false;

        Image bg = MakePanel(rt, "ButtonBg", Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f), primary ? new Color(0.070f, 0.035f, 0.088f, 0.15f) : new Color(0.020f, 0.022f, 0.055f, 0.12f)).GetComponent<Image>();
        bg.raycastTarget = false;

        Text text = MakeText(rt, "Label", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, FontStyle.Bold, primary ? PaleGold : TextPrimary);
        text.alignment = TextAnchor.MiddleCenter;

        Button button = go.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = raycastSurface;
        button.onClick.AddListener(onClick);

        MainMenuButtonVisual visual = go.AddComponent<MainMenuButtonVisual>();
        visual.Configure(rt, bg, border, glow, text, primary);

        return button;
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
        overlayImg.color = new Color(0.006f, 0.004f, 0.014f, 0.50f);
        overlayImg.raycastTarget = true;

        CanvasGroup overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;

        GameObject card = MakePanel(overlayRt, "SettingsCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 700f), PanelDark);
        RectTransform cardRt = card.GetComponent<RectTransform>();
        Image cardImg = card.GetComponent<Image>();
        cardImg.raycastTarget = true;
        card.AddComponent<Outline>().effectColor = new Color(0.95f, 0.72f, 0.36f, 0.50f);

        Image inset = MakePanel(cardRt, "Inset", Vector2.zero, Vector2.one, new Vector2(14f, 14f), new Vector2(-14f, -14f), new Color(0.024f, 0.017f, 0.050f, 0.88f)).GetComponent<Image>();
        inset.raycastTarget = false;

        Text title = MakeText(cardRt, "Title", "AYARLAR", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(600f, 56f), 34, FontStyle.Bold, TextPrimary);
        title.alignment = TextAnchor.MiddleCenter;

        Image line = MakePanel(cardRt, "TitleLine", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(500f, 2f), Gold).GetComponent<Image>();
        line.raycastTarget = false;

        BuildSectionLabel(cardRt, "AudioSection", "SES", -124f);
        masterSlider = BuildSlider(cardRt, "MasterVolume", "Ana Ses", new Vector2(0f, -160f), masterVolume, out masterValueText);
        musicSlider = BuildSlider(cardRt, "MusicVolume", "Müzik", new Vector2(0f, -210f), musicVolume, out musicValueText);
        sfxSlider = BuildSlider(cardRt, "SfxVolume", "Efekt Sesi", new Vector2(0f, -260f), sfxVolume, out sfxValueText);

        BuildSectionLabel(cardRt, "DisplaySection", "GÖRÜNTÜ", -315f);
        brightnessSlider = BuildSlider(cardRt, "Brightness", "Parlaklık", new Vector2(0f, -352f), brightness, out brightnessValueText, BrightnessMin, BrightnessMax);
        fullscreenToggle = BuildToggle(cardRt, "FullscreenToggle", "Tam Ekran", new Vector2(-178f, -402f), fullscreen);

        BuildSectionLabel(cardRt, "UiGameplaySection", "ARAYÜZ / OYNANIŞ", -462f);
        showRangeToggle = BuildToggle(cardRt, "ShowRangeToggle", "Menzil Halkasını Göster", new Vector2(-178f, -502f), showRangeIndicator);

        Button applyButton = BuildMenuButton(cardRt, "ApplyButton", "UYGULA", new Vector2(-135f, -300f), new Vector2(235f, 54f), ApplySettings, true);
        applyVisual = applyButton.GetComponent<MainMenuButtonVisual>();
        Button backButton = BuildMenuButton(cardRt, "BackButton", "GERİ", new Vector2(135f, -300f), new Vector2(235f, 54f), CloseSettings, false);
        backVisual = backButton.GetComponent<MainMenuButtonVisual>();

        Navigation applyNav = applyButton.navigation;
        applyNav.mode = Navigation.Mode.Automatic;
        applyButton.navigation = applyNav;

        Navigation backNav = backButton.navigation;
        backNav.mode = Navigation.Mode.Automatic;
        backButton.navigation = backNav;

        WireSettingsValueLabels();

        return overlay;
    }

    void BuildSectionLabel(RectTransform parent, string name, string label, float y)
    {
        Text text = MakeText(parent, name, label, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-270f, y), new Vector2(180f, 30f), 16, FontStyle.Bold, Cyan);
        text.alignment = TextAnchor.MiddleLeft;

        Image line = MakePanel(parent, name + "Line", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(90f, y - 1f), new Vector2(370f, 1.5f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.38f)).GetComponent<Image>();
        line.raycastTarget = false;
    }

    Slider BuildSlider(RectTransform parent, string name, string label, Vector2 position, float value, out Text valueText, float minValue = 0f, float maxValue = 1f)
    {
        GameObject row = new GameObject(name + "Row");
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = position;
        rowRt.sizeDelta = new Vector2(620f, 42f);

        Text labelText = MakeText(rowRt, "Label", label, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(95f, 0f), new Vector2(190f, 34f), 17, FontStyle.Normal, TextPrimary);
        labelText.alignment = TextAnchor.MiddleLeft;

        valueText = MakeText(rowRt, "Value", string.Empty, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-29f, 0f), new Vector2(58f, 34f), 15, FontStyle.Bold, PaleGold);
        valueText.alignment = TextAnchor.MiddleRight;

        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(rowRt, false);
        RectTransform sliderRt = sliderGo.AddComponent<RectTransform>();
        sliderRt.anchorMin = sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(64f, 0f);
        sliderRt.sizeDelta = new Vector2(360f, 34f);

        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp(value, minValue, maxValue);

        Image bg = MakePanel(sliderRt, "Background", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 8f), new Color(0.018f, 0.018f, 0.042f, 0.96f)).GetComponent<Image>();
        bg.raycastTarget = false;

        RectTransform fillArea = new GameObject("FillArea").AddComponent<RectTransform>();
        fillArea.transform.SetParent(sliderRt, false);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(4f, -4f);
        fillArea.offsetMax = new Vector2(-4f, 4f);

        Image fill = MakePanel(fillArea, "Fill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Color(0.88f, 0.62f, 0.28f, 0.95f)).GetComponent<Image>();
        fill.raycastTarget = false;

        RectTransform handleArea = new GameObject("HandleArea").AddComponent<RectTransform>();
        handleArea.transform.SetParent(sliderRt, false);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(8f, 0f);
        handleArea.offsetMax = new Vector2(-8f, 0f);

        Image handle = MakePanel(handleArea, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f), PaleGold).GetComponent<Image>();
        handle.raycastTarget = true;

        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;

        return slider;
    }

    Toggle BuildToggle(RectTransform parent, string name, string label, Vector2 position, bool value)
    {
        GameObject row = new GameObject(name);
        row.transform.SetParent(parent, false);
        RectTransform rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0f, 0.5f);
        rowRt.anchoredPosition = position;
        rowRt.sizeDelta = new Vector2(440f, 42f);

        Image raycastSurface = row.AddComponent<Image>();
        raycastSurface.color = new Color(0f, 0f, 0f, 0.001f);
        raycastSurface.raycastTarget = true;

        Toggle toggle = row.AddComponent<Toggle>();
        toggle.transition = Selectable.Transition.ColorTint;
        toggle.isOn = value;

        Image box = MakePanel(rowRt, "Box", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(21f, 0f), new Vector2(30f, 30f), new Color(0.030f, 0.025f, 0.060f, 1f)).GetComponent<Image>();
        box.raycastTarget = false;
        box.gameObject.AddComponent<Outline>().effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.65f);

        Image check = MakePanel(box.rectTransform, "Check", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18f, 18f), Cyan).GetComponent<Image>();
        check.raycastTarget = false;

        Text labelText = MakeText(rowRt, "Label", label, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(240f, 0f), new Vector2(380f, 34f), 17, FontStyle.Normal, TextPrimary);
        labelText.alignment = TextAnchor.MiddleLeft;

        toggle.graphic = check;
        toggle.targetGraphic = raycastSurface;

        return toggle;
    }

    void WireSettingsValueLabels()
    {
        UpdateSliderText(masterSlider, masterValueText);
        UpdateSliderText(musicSlider, musicValueText);
        UpdateSliderText(sfxSlider, sfxValueText);
        UpdateBrightnessText(brightnessSlider, brightnessValueText);

        masterSlider.onValueChanged.AddListener(_ => UpdateSliderText(masterSlider, masterValueText));
        musicSlider.onValueChanged.AddListener(_ => UpdateSliderText(musicSlider, musicValueText));
        sfxSlider.onValueChanged.AddListener(_ => UpdateSliderText(sfxSlider, sfxValueText));
        brightnessSlider.onValueChanged.AddListener(_ =>
        {
            UpdateBrightnessText(brightnessSlider, brightnessValueText);
            brightness = brightnessSlider.value;
            ApplyMenuBrightnessVisuals();
        });
    }

    static void UpdateSliderText(Slider slider, Text valueText)
    {
        if (slider == null || valueText == null)
            return;

        valueText.text = Mathf.RoundToInt(slider.value * 100f).ToString();
    }

    static void UpdateBrightnessText(Slider slider, Text valueText)
    {
        if (slider == null || valueText == null)
            return;

        int pct = Mathf.RoundToInt((slider.value - BrightnessMin) / (BrightnessMax - BrightnessMin) * 100f);
        valueText.text = pct.ToString();
    }

    void LoadSettings()
    {
        float defaultMaster = AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f;
        float defaultMusic = AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.55f;
        float defaultSfx = AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 0.85f;

        masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMaster);
        musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, defaultMusic);
        sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, defaultSfx);
        brightness = Mathf.Clamp(PlayerPrefs.GetFloat(BrightnessKey, 1f), BrightnessMin, BrightnessMax);
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        showRangeIndicator = PlayerPrefs.GetInt(ShowRangeKey, 1) == 1;
    }

    void ApplySettings()
    {
        if (masterSlider != null)
            masterVolume = masterSlider.value;

        if (musicSlider != null)
            musicVolume = musicSlider.value;

        if (sfxSlider != null)
            sfxVolume = sfxSlider.value;

        if (brightnessSlider != null)
            brightness = brightnessSlider.value;

        if (fullscreenToggle != null)
            fullscreen = fullscreenToggle.isOn;

        if (showRangeToggle != null)
            showRangeIndicator = showRangeToggle.isOn;

        SaveSettings();
        ApplySettingsToRuntime();
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, Mathf.Clamp01(masterVolume));
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(musicVolume));
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(sfxVolume));
        PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp(brightness, BrightnessMin, BrightnessMax));
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ShowRangeKey, showRangeIndicator ? 1 : 0);
        PlayerPrefs.Save();
    }

    void ApplySettingsToRuntime()
    {
        Screen.fullScreen = fullscreen;
        ApplyMenuBrightnessVisuals();

        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMasterVolume(masterVolume);
        AudioManager.Instance.SetMusicVolume(musicVolume);
        AudioManager.Instance.SetSfxVolume(sfxVolume);
    }

    void ApplyMenuBrightnessVisuals()
    {
        brightness = Mathf.Clamp(brightness, BrightnessMin, BrightnessMax);

        float brighter = Mathf.InverseLerp(1f, BrightnessMax, brightness);
        float darker = Mathf.InverseLerp(1f, BrightnessMin, brightness);

        if (menuDimImage != null)
            menuDimImage.color = new Color(0.008f, 0.005f, 0.016f, Mathf.Clamp01(Mathf.Lerp(0.12f, 0.06f, brighter) + darker * 0.05f));

        if (centerFocusImage != null)
            centerFocusImage.color = new Color(0.010f, 0.006f, 0.020f, Mathf.Clamp01(Mathf.Lerp(0.04f, 0.02f, brighter) + darker * 0.02f));

        if (leftVeilImage != null)
            leftVeilImage.color = new Color(0.005f, 0.003f, 0.010f, Mathf.Clamp01(Mathf.Lerp(0.38f, 0.26f, brighter) + darker * 0.07f));
    }

    void SyncSettingsControls()
    {
        if (masterSlider != null)
            masterSlider.SetValueWithoutNotify(masterVolume);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(musicVolume);

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfxVolume);

        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(brightness);

        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(fullscreen);

        if (showRangeToggle != null)
            showRangeToggle.SetIsOnWithoutNotify(showRangeIndicator);

        UpdateSliderText(masterSlider, masterValueText);
        UpdateSliderText(musicSlider, musicValueText);
        UpdateSliderText(sfxSlider, sfxValueText);
        UpdateBrightnessText(brightnessSlider, brightnessValueText);
    }

    void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenSettings()
    {
        settingsOpen = true;
        SyncSettingsControls();
        settingsPanel.SetActive(true);
        SetMainButtonsInteractable(false);
        applyVisual?.ResetState();
        backVisual?.ResetState();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void CloseSettings()
    {
        settingsOpen = false;
        settingsPanel.SetActive(false);
        LoadSettings();
        ApplySettingsToRuntime();
        SetMainButtonsInteractable(true);
        ResetButtonVisualStates();
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void SetMainButtonsInteractable(bool interactable)
    {
        if (enterButton != null)
            enterButton.interactable = interactable;

        if (settingsButton != null)
            settingsButton.interactable = interactable;

        if (quitButton != null)
            quitButton.interactable = interactable;

        if (mainGroup != null)
        {
            mainGroup.interactable = interactable;
            mainGroup.blocksRaycasts = interactable;
            mainGroup.alpha = interactable ? 1f : 0.05f;
        }

        if (!interactable)
        {
            enterVisual?.ResetState();
            settingsVisual?.ResetState();
            quitVisual?.ResetState();
        }
    }

    void ResetButtonVisualStates()
    {
        enterVisual?.ResetState();
        settingsVisual?.ResetState();
        quitVisual?.ResetState();
        applyVisual?.ResetState();
        backVisual?.ResetState();
    }

    static GameObject MakePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPositionOrOffsetMin, Vector2 sizeOrOffsetMax, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        if (anchorMin == anchorMax)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPositionOrOffsetMin;
            rt.sizeDelta = sizeOrOffsetMax;
        }
        else
        {
            rt.offsetMin = anchoredPositionOrOffsetMin;
            rt.offsetMax = sizeOrOffsetMax;
        }

        Image image = go.AddComponent<Image>();
        image.color = color;

        return go;
    }

    static Text MakeText(RectTransform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 positionOrOffsetMin, Vector2 sizeOrOffsetMax, int fontSize, FontStyle style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        if (anchorMin == anchorMax)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positionOrOffsetMin;
            rt.sizeDelta = sizeOrOffsetMax;
        }
        else
        {
            rt.offsetMin = positionOrOffsetMin;
            rt.offsetMax = sizeOrOffsetMax;
        }

        Text label = go.AddComponent<Text>();
        label.text = text;
        label.font = UiFont;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;

        return label;
    }

    static TextMeshProUGUI MakeTMPText(RectTransform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 positionOrOffsetMin, Vector2 sizeOrOffsetMax, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;

        if (anchorMin == anchorMax)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positionOrOffsetMin;
            rt.sizeDelta = sizeOrOffsetMax;
        }
        else
        {
            rt.offsetMin = positionOrOffsetMin;
            rt.offsetMax = sizeOrOffsetMax;
        }

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        TMP_FontAsset tmpFont = GetOrCreateTMPFont();
        if (tmpFont != null)
            label.font = tmpFont;

        return label;
    }
}

class MainMenuButtonVisual : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    const float HoverScale = 1.045f;
    const float PressScale = 0.985f;
    const float HoverLift = 4f;
    const float Speed = 12f;

    RectTransform root;
    Image background;
    Image border;
    Image glow;
    Text label;
    Selectable selectable;
    bool primary;
    bool hovered;
    bool pressed;
    bool selected;
    float hoverT;
    Vector2 basePosition;
    Color normalBg;
    Color hoverBg;
    Color normalBorder;
    Color hoverBorder;
    Color normalText;
    Color hoverText;

    public void Configure(RectTransform rootRect, Image bg, Image borderImage, Image glowImage, Text labelText, bool isPrimary)
    {
        root = rootRect;
        background = bg;
        border = borderImage;
        glow = glowImage;
        label = labelText;
        selectable = GetComponent<Selectable>();
        primary = isPrimary;
        basePosition = root.anchoredPosition;

        normalBg = primary ? new Color(0.070f, 0.035f, 0.088f, 0.15f) : new Color(0.020f, 0.022f, 0.055f, 0.12f);
        hoverBg = primary ? new Color(0.190f, 0.100f, 0.155f, 0.90f) : new Color(0.060f, 0.075f, 0.150f, 0.86f);
        normalBorder = primary ? new Color(1.000f, 0.780f, 0.360f, 0.78f) : new Color(0.370f, 0.925f, 1.000f, 0.55f);
        hoverBorder = Color.Lerp(new Color(1.000f, 0.925f, 0.650f, 1f), new Color(0.370f, 0.925f, 1.000f, 1f), primary ? 0.30f : 0.58f);
        normalText = primary ? new Color(1.000f, 0.925f, 0.650f, 1f) : new Color(0.960f, 0.945f, 0.900f, 1f);
        hoverText = Color.white;
    }

    public void ResetState()
    {
        hovered = false;
        pressed = false;
        selected = false;
        hoverT = 0f;

        if (root != null)
        {
            root.localScale = Vector3.one;
            root.anchoredPosition = basePosition;
        }

        if (background != null) background.color = normalBg;
        if (border != null) border.color = normalBorder;
        if (glow != null) { Color c = glow.color; c.a = 0f; glow.color = c; }
        if (label != null) label.color = normalText;
    }

    void Update()
    {
        if (root == null)
            return;

        bool interactable = selectable == null || selectable.IsInteractable();
        bool manualHover = interactable && IsMouseInside();
        bool active = interactable && (hovered || manualHover || selected);
        float target = active ? 1f : 0f;
        hoverT = Mathf.MoveTowards(hoverT, target, Time.unscaledDeltaTime * Speed);

        if (!interactable)
            pressed = false;

        float scale = pressed && interactable ? PressScale : Mathf.Lerp(1f, HoverScale, hoverT);
        root.localScale = new Vector3(scale, scale, 1f);
        root.anchoredPosition = basePosition + new Vector2(0f, Mathf.Lerp(0f, HoverLift, hoverT));

        if (background != null)
            background.color = Color.Lerp(normalBg, hoverBg, hoverT);

        if (border != null)
            border.color = Color.Lerp(normalBorder, hoverBorder, hoverT);

        if (glow != null)
        {
            Color glowColor = primary ? new Color(1.000f, 0.720f, 0.300f, 1f) : new Color(0.370f, 0.925f, 1.000f, 1f);
            glowColor.a = Mathf.Lerp(0f, 0.48f, hoverT);
            glow.color = glowColor;
        }

        if (label != null)
            label.color = Color.Lerp(normalText, hoverText, hoverT);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
        pressed = false;
    }

    bool IsMouseInside()
    {
        if (root == null || !root.gameObject.activeInHierarchy || Mouse.current == null)
            return false;

        return RectTransformUtility.RectangleContainsScreenPoint(root, Mouse.current.position.ReadValue(), null);
    }
}

class MenuAtmosphereAnimator : MonoBehaviour
{
    Image cyanLine;

    public void Configure(Image cyanLineImg)
    {
        cyanLine = cyanLineImg;
    }

    void Update()
    {
        if (cyanLine == null)
            return;

        float t = Time.unscaledTime;
        Color c = cyanLine.color;
        c.a = Mathf.Lerp(0.38f, 1.00f, Mathf.Sin(t * 0.55f + 2.10f) * 0.5f + 0.5f);
        cyanLine.color = c;
    }
}
