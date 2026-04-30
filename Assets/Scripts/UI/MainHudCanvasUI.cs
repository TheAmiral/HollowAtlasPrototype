using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainHudCanvasUI : MonoBehaviour
{
    [Header("Premium HUD — HP Orb")]
    [SerializeField] private Image   hpOrbFill;
    [SerializeField] private Text    hpNumberText;

    [Header("Premium HUD — XP Bar")]
    [SerializeField] private Image   xpBarFill;
    [SerializeField] private Text    xpValueText;

    [Header("Premium HUD — Level")]
    [SerializeField] private Text    levelRomanText;
    [SerializeField] private Text    levelLabelText;

    [Header("Premium HUD — Gold")]
    [SerializeField] private Text    goldNumberText;

    [Header("Legacy / Optional")]
    [SerializeField] private Text    timerText;
    [SerializeField] private Image   minimapPlaceholderImage;

    [Header("Smoothing")]
    [SerializeField] private float   hudLerpSpeed   = 8f;
    [SerializeField] private float   levelUpHudAlpha = 0.18f;

    // Orb breathing ref — critical pulse devre dışı bırakmak için
    private OrbBreathing orbBreathing;

    // Runtime-generated circle/ring sprites (cached statically)
    private static Sprite _circleSprite;
    private static Sprite _ringSprite;
    private static Sprite _softGlowSprite;
    private static Sprite _hpFillSprite;
    private static Sprite _levelBadgeSprite;
    private static Sprite _coinSprite;
    private static Sprite _roundedBarSprite;
    private static Sprite _xpFillSprite;
    private static Sprite _hudOverlaySprite;

    private static readonly Color HudOverlayColor = new Color32(0x25, 0x1A, 0x40, 0x99); // #251A40 alpha 0.6
    private static readonly Color OrbBackgroundColor = new Color32(0x08, 0x04, 0x0E, 0xFF);
    private static readonly Color OrbTrackColor = new Color32(0x28, 0x14, 0x3A, 0xFF);
    private static readonly Color OrbGlowColor = new Color32(0xB4, 0x82, 0xDC, 0xFF);
    private static readonly Color TextSoftLavender = new Color32(0xF5, 0xE0, 0xFF, 0xFF);
    private static readonly Color LevelFrameColor = new Color32(0xDC, 0xB4, 0xFF, 0xFF);
    private static readonly Color XpBackgroundColor = new Color32(0x08, 0x04, 0x1A, 0xFF);
    private static readonly Color XpFrameColor = new Color32(0xB4, 0x82, 0xDC, 0xFF);
    private static readonly Color GoldTextColor = new Color32(0xFF, 0xE5, 0xA0, 0xFF);

    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        _circleSprite = MakeCircleSprite(128, 0f);
        return _circleSprite;
    }

    private static Sprite GetRingSprite()
    {
        if (_ringSprite != null) return _ringSprite;
        _ringSprite = MakeCircleSprite(128, 0.72f);
        return _ringSprite;
    }

    private static Sprite GetSoftGlowSprite()
    {
        if (_softGlowSprite != null) return _softGlowSprite;
        _softGlowSprite = MakeSoftCircleSprite(160);
        return _softGlowSprite;
    }

    private static Sprite GetHpFillSprite()
    {
        if (_hpFillSprite != null) return _hpFillSprite;
        _hpFillSprite = MakeHpFillSprite(128);
        return _hpFillSprite;
    }

    private static Sprite GetLevelBadgeSprite()
    {
        if (_levelBadgeSprite != null) return _levelBadgeSprite;
        _levelBadgeSprite = MakeLevelBadgeSprite(128);
        return _levelBadgeSprite;
    }

    private static Sprite GetCoinSprite()
    {
        if (_coinSprite != null) return _coinSprite;
        _coinSprite = MakeCoinSprite(128);
        return _coinSprite;
    }

    private static Sprite GetRoundedBarSprite()
    {
        if (_roundedBarSprite != null) return _roundedBarSprite;
        _roundedBarSprite = MakeRoundedRectSprite(256, 16, 8f, Color.white, Color.white);
        return _roundedBarSprite;
    }

    private static Sprite GetXpFillSprite()
    {
        if (_xpFillSprite != null) return _xpFillSprite;
        _xpFillSprite = MakeXpFillSprite(256, 16, 8f);
        return _xpFillSprite;
    }

    private static Sprite GetHudOverlaySprite()
    {
        if (_hudOverlaySprite != null) return _hudOverlaySprite;
        _hudOverlaySprite = MakeRoundedRectSprite(256, 96, 12f, Color.white, Color.white);
        return _hudOverlaySprite;
    }

    private static Sprite MakeCircleSprite(int size, float innerFraction)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        float outerR = center;
        float innerR = outerR * innerFraction;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                // smooth anti-alias edge
                float alpha = 1f - Mathf.Clamp01(dist - (outerR - 1f));
                bool inside = dist <= outerR && (innerFraction <= 0f || dist >= innerR);
                pixels[y * size + x] = inside ? new Color(1f, 1f, 1f, alpha) : Color.clear;
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeSoftCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        float radius = center;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                float t = Mathf.Clamp01(dist / radius);
                float alpha = Mathf.Pow(1f - t, 2.25f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeHpFillSprite(int size)
    {
        Color deepRed = new Color32(0xDC, 0x35, 0x45, 0xFF);
        Color hotRed = new Color32(0xFF, 0x6B, 0x78, 0xFF);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        float radius = center;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float edgeAlpha = 1f - Mathf.Clamp01(dist - (radius - 1f));
                float diagonal = Mathf.Clamp01((x + y) / (2f * (size - 1)));
                float innerGlow = 1f - Mathf.Clamp01(dist / radius);
                Color color = Color.Lerp(deepRed, hotRed, Mathf.Clamp01(0.18f + diagonal * 0.82f));
                color = Color.Lerp(color, hotRed, innerGlow * 0.28f);
                color.a = edgeAlpha;
                pixels[y * size + x] = color;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeLevelBadgeSprite(int size)
    {
        Color core = new Color32(0x8C, 0x50, 0xC8, 0xFF);
        Color rim = new Color32(0x1E, 0x0F, 0x37, 0xFF);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        float radius = center;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float edgeAlpha = 1f - Mathf.Clamp01(dist - (radius - 1f));
                float radial = Mathf.Clamp01(dist / radius);
                float highlight = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(center * 0.72f, center * 1.28f)) / (radius * 0.95f));
                Color color = Color.Lerp(core, rim, Mathf.Pow(radial, 1.35f));
                color = Color.Lerp(color, Color.white, highlight * 0.08f);
                color.a = edgeAlpha;
                pixels[y * size + x] = color;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeCoinSprite(int size)
    {
        Color light = new Color32(0xFF, 0xE7, 0x9B, 0xFF);
        Color mid = new Color32(0xF4, 0xC6, 0x4A, 0xFF);
        Color dark = new Color32(0x8A, 0x5A, 0x10, 0xFF);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = (size - 1) / 2f;
        float radius = center;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist > radius)
                {
                    pixels[y * size + x] = Color.clear;
                    continue;
                }

                float edgeAlpha = 1f - Mathf.Clamp01(dist - (radius - 1f));
                float diagonal = Mathf.Clamp01((x + y) / (2f * (size - 1)));
                Color color = diagonal < 0.55f
                    ? Color.Lerp(light, mid, diagonal / 0.55f)
                    : Color.Lerp(mid, dark, (diagonal - 0.55f) / 0.45f);

                float shine = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(center * 0.68f, center * 1.32f)) / (radius * 0.75f));
                color = Color.Lerp(color, light, shine * 0.35f);
                color.a = edgeAlpha;
                pixels[y * size + x] = color;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite MakeXpFillSprite(int width, int height, float radius)
    {
        Color a = new Color32(0x5A, 0x1F, 0x9C, 0xFF);
        Color b = new Color32(0x9A, 0x3D, 0xD8, 0xFF);
        Color c = new Color32(0xC8, 0x62, 0xEE, 0xFF);
        Color d = new Color32(0x6D, 0xD9, 0xF0, 0xFF);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float t = Mathf.Clamp01((float)x / (width - 1));
                Color color = EvaluateXpGradient(t, a, b, c, d);
                color.a = RoundedRectAlpha(x, y, width, height, radius);
                pixels[y * width + x] = color;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Sprite MakeRoundedRectSprite(int width, int height, float radius, Color left, Color right)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color color = Color.Lerp(left, right, Mathf.Clamp01((float)x / (width - 1)));
                color.a *= RoundedRectAlpha(x, y, width, height, radius);
                pixels[y * width + x] = color;
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static float RoundedRectAlpha(float x, float y, float width, float height, float radius)
    {
        float cx = (width - 1f) * 0.5f;
        float cy = (height - 1f) * 0.5f;
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float dx = Mathf.Max(Mathf.Abs(x - cx) - (halfW - radius), 0f);
        float dy = Mathf.Max(Mathf.Abs(y - cy) - (halfH - radius), 0f);
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        return 1f - Mathf.Clamp01(dist - (radius - 1f));
    }

    private static Color EvaluateXpGradient(float t, Color a, Color b, Color c, Color d)
    {
        if (t < 0.33f) return Color.Lerp(a, b, t / 0.33f);
        if (t < 0.66f) return Color.Lerp(b, c, (t - 0.33f) / 0.33f);
        return Color.Lerp(c, d, (t - 0.66f) / 0.34f);
    }

    // Runtime smooth state
    private float currentHpFill  = 1f;
    private float currentXpFill  = 0f;
    private bool  isCritical     = false;

    // System refs
    private PlayerHealth      playerHealth;
    private PlayerLevelSystem playerLevelSystem;
    private GoldWallet        goldWallet;
    private CanvasGroup       hudCanvasGroup;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureRuntimeHud();
        ResolveReferences();
        GetOrCreateHudCanvasGroup();
        orbBreathing = GetComponentInChildren<OrbBreathing>();

        if (playerLevelSystem != null && hpOrbFill != null)
        {
            currentHpFill = playerHealth != null && playerHealth.MaxHealth > 0
                ? Mathf.Clamp01((float)playerHealth.CurrentHealth / playerHealth.MaxHealth)
                : 1f;
            hpOrbFill.fillAmount = currentHpFill;
        }

        if (playerLevelSystem != null && xpBarFill != null)
        {
            currentXpFill = playerLevelSystem.XPToNextLevel > 0
                ? Mathf.Clamp01((float)playerLevelSystem.CurrentXP / playerLevelSystem.XPToNextLevel)
                : 0f;
            xpBarFill.fillAmount = currentXpFill;
        }
    }

    void Update()
    {
        ResolveReferences();
        UpdateBars();
        UpdateLabels();
        HandleCriticalHP();
        ForceVisibleHudState();
    }

    // ── Bar updates ─────────────────────────────────────────────────────────

    private void UpdateBars()
    {
        if (playerHealth != null && hpOrbFill != null)
        {
            float targetHp = playerHealth.MaxHealth > 0
                ? Mathf.Clamp01((float)playerHealth.CurrentHealth / playerHealth.MaxHealth)
                : 0f;
            currentHpFill     = Mathf.Lerp(currentHpFill, targetHp, Time.deltaTime * hudLerpSpeed);
            hpOrbFill.fillAmount = currentHpFill;
        }

        if (playerLevelSystem != null && xpBarFill != null)
        {
            float targetXp = playerLevelSystem.XPToNextLevel > 0
                ? Mathf.Clamp01((float)playerLevelSystem.CurrentXP / playerLevelSystem.XPToNextLevel)
                : 0f;
            currentXpFill     = Mathf.Lerp(currentXpFill, targetXp, Time.deltaTime * hudLerpSpeed);
            xpBarFill.fillAmount = currentXpFill;
        }
    }

    private void UpdateLabels()
    {
        if (hpNumberText != null && playerHealth != null)
            hpNumberText.text = playerHealth.CurrentHealth.ToString();

        if (xpValueText != null && playerLevelSystem != null)
            xpValueText.text = $"{playerLevelSystem.CurrentXP} / {playerLevelSystem.XPToNextLevel}";

        if (levelRomanText != null && playerLevelSystem != null)
            levelRomanText.text = ToRoman(playerLevelSystem.Level);

        if (levelLabelText != null && playerLevelSystem != null)
            levelLabelText.text = "ATLAS LEVEL";

        if (goldNumberText != null && goldWallet != null)
            goldNumberText.text = goldWallet.RunGold.ToString();

        if (timerText != null && GameManager.Instance != null)
            timerText.text = $"Time: {GameManager.Instance.ElapsedTime:0.0}s";

        if (minimapPlaceholderImage != null)
        {
            bool levelUpOpen = LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending;
            minimapPlaceholderImage.enabled = !levelUpOpen;
        }
    }

    // ── Critical HP ─────────────────────────────────────────────────────────

    private void HandleCriticalHP()
    {
        if (playerHealth == null) return;
        float ratio = playerHealth.MaxHealth > 0
            ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
            : 1f;

        if (ratio <= 0.25f && !isCritical)
        {
            isCritical = true;
            StartCoroutine(CriticalPulse());
        }
        else if (ratio > 0.25f && isCritical)
        {
            isCritical = false;
        }
    }

    private IEnumerator CriticalPulse()
    {
        if (orbBreathing != null) orbBreathing.enabled = false;
        Transform orbParent = hpOrbFill != null ? hpOrbFill.transform.parent : null;

        while (isCritical)
        {
            if (orbParent != null)
                orbParent.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 8f));
            yield return null;
        }

        if (orbParent != null)
            orbParent.localScale = Vector3.one;
        if (orbBreathing != null) orbBreathing.enabled = true;
    }

    // ── HUD visibility ───────────────────────────────────────────────────────

    private void ForceVisibleHudState()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && !canvas.enabled) canvas.enabled = true;

        CanvasGroup group = GetOrCreateHudCanvasGroup();
        if (group == null) return;

        bool levelUpOpen = LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending;
        group.alpha           = levelUpOpen ? Mathf.Clamp01(levelUpHudAlpha) : 1f;
        group.interactable    = !levelUpOpen;
        group.blocksRaycasts  = !levelUpOpen;
    }

    private CanvasGroup GetOrCreateHudCanvasGroup()
    {
        if (hudCanvasGroup != null) return hudCanvasGroup;
        hudCanvasGroup = GetComponent<CanvasGroup>();
        if (hudCanvasGroup == null) hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        return hudCanvasGroup;
    }

    // ── Reference resolution ─────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (playerHealth    == null) playerHealth    = FindFirstObjectByType<PlayerHealth>();
        if (playerLevelSystem == null) playerLevelSystem = FindFirstObjectByType<PlayerLevelSystem>();
        if (goldWallet      == null) goldWallet      = FindFirstObjectByType<GoldWallet>();
    }

    // ── Roman numeral helper ─────────────────────────────────────────────────

    private static readonly string[] RomanTable =
        { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
          "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX" };

    private static string ToRoman(int n) =>
        n >= 0 && n < RomanTable.Length ? RomanTable[n] : n.ToString();

    // ── Runtime HUD builder ──────────────────────────────────────────────────

    private void EnsureRuntimeHud()
    {
        // If key refs are already set (e.g. via Inspector), skip build
        if (hpOrbFill != null && xpBarFill != null && hpNumberText != null) return;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        if (GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform root = canvas.GetComponent<RectTransform>();

        BuildPremiumHud(root, font);
        BuildMinimapPlaceholder(root, font);
    }

    // ── Premium HUD hierarchy builder ────────────────────────────────────────

    private void BuildPremiumHud(RectTransform root, Font font)
    {
        // HUD_TopLeft — anchor top-left, pos 30,-30
        GameObject hudTopLeft = new GameObject("HUD_TopLeft", typeof(RectTransform));
        hudTopLeft.transform.SetParent(root, false);
        RectTransform topLeftRect = hudTopLeft.GetComponent<RectTransform>();
        topLeftRect.anchorMin       = new Vector2(0f, 1f);
        topLeftRect.anchorMax       = new Vector2(0f, 1f);
        topLeftRect.pivot           = new Vector2(0f, 1f);
        topLeftRect.anchoredPosition = new Vector2(30f, -30f);
        topLeftRect.sizeDelta       = new Vector2(420f, 150f);

        GameObject overlay = new GameObject("HUD_Dark_Purple_Overlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(topLeftRect, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin        = new Vector2(0f, 1f);
        overlayRect.anchorMax        = new Vector2(0f, 1f);
        overlayRect.pivot            = new Vector2(0f, 1f);
        overlayRect.anchoredPosition = new Vector2(-12f, 8f);
        overlayRect.sizeDelta        = new Vector2(430f, 142f);
        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.sprite = GetHudOverlaySprite();
        overlayImage.color  = HudOverlayColor;

        BuildHpOrb(topLeftRect, font);
        BuildRightStack(topLeftRect, font);
    }

    // HP Orb (130x130, radial 360 fill)
    private void BuildHpOrb(RectTransform parent, Font font)
    {
        GameObject orbGroup = new GameObject("HP_Orb_Group", typeof(RectTransform));
        orbGroup.transform.SetParent(parent, false);
        RectTransform orbRect = orbGroup.GetComponent<RectTransform>();
        orbRect.anchorMin        = new Vector2(0f, 1f);
        orbRect.anchorMax        = new Vector2(0f, 1f);
        orbRect.pivot            = new Vector2(0f, 1f);
        orbRect.anchoredPosition = Vector2.zero;
        orbRect.sizeDelta        = new Vector2(130f, 130f);

        // OrbBreathing bileşeni
        orbGroup.AddComponent<OrbBreathing>();

        // Orb_Background_Glow — en altta (büyük, saydam mor halo)
        Image orbGlow = CreateCircleImage(orbRect, "Orb_Background_Glow",
            new Color(OrbGlowColor.r, OrbGlowColor.g, OrbGlowColor.b, 0.62f),
            158f, 158f);
        orbGlow.sprite = GetSoftGlowSprite();
        orbGlow.GetComponent<RectTransform>().SetSiblingIndex(0);

        // Orb_Track (koyu arka plan dairesi)
        CreateCircleImage(orbRect, "Orb_Track",
            OrbTrackColor,
            130f, 130f);

        // Orb_Fill (HP yüzdesi — Radial 360)
        GameObject fillGo = new GameObject("Orb_Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(orbRect, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin        = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax        = new Vector2(0.5f, 0.5f);
        fillRect.pivot            = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta        = new Vector2(130f, 130f);

        hpOrbFill               = fillGo.GetComponent<Image>();
        hpOrbFill.sprite        = GetHpFillSprite();
        hpOrbFill.color         = Color.white;
        hpOrbFill.type          = Image.Type.Filled;
        hpOrbFill.fillMethod    = Image.FillMethod.Radial360;
        hpOrbFill.fillOrigin    = (int)Image.Origin360.Top;
        hpOrbFill.fillClockwise = true;
        hpOrbFill.fillAmount    = 1f;

        // Orb_Inner_Disk (orb iç karanlık disk)
        CreateCircleImage(orbRect, "Orb_Inner_Disk",
            OrbBackgroundColor,
            96f, 96f);

        // Orb_Rotating_Ring
        GameObject ringGo = new GameObject("Orb_Rotating_Ring", typeof(RectTransform), typeof(Image));
        ringGo.transform.SetParent(orbRect, false);
        RectTransform ringRect = ringGo.GetComponent<RectTransform>();
        ringRect.anchorMin        = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax        = new Vector2(0.5f, 0.5f);
        ringRect.pivot            = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = Vector2.zero;
        ringRect.sizeDelta        = new Vector2(130f, 130f);
        Image ringImage = ringGo.GetComponent<Image>();
        ringImage.sprite = GetRingSprite();
        ringImage.color = new Color(OrbGlowColor.r, OrbGlowColor.g, OrbGlowColor.b, 0.72f);
        ringGo.AddComponent<OrbRingRotator>();

        // HP Text group (orb merkezinde)
        GameObject textGroup = new GameObject("Orb_Text_Group", typeof(RectTransform));
        textGroup.transform.SetParent(orbRect, false);
        RectTransform tgRect = textGroup.GetComponent<RectTransform>();
        tgRect.anchorMin = new Vector2(0.5f, 0.5f);
        tgRect.anchorMax = new Vector2(0.5f, 0.5f);
        tgRect.pivot     = new Vector2(0.5f, 0.5f);
        tgRect.sizeDelta = new Vector2(90f, 60f);
        tgRect.anchoredPosition = Vector2.zero;

        hpNumberText = CreateText(tgRect, "HP_Number", "100",
            new Vector2(0f, 5f), new Vector2(90f, 36f),
            TextAnchor.MiddleCenter, 28, font);
        hpNumberText.fontStyle = FontStyle.Bold;
        hpNumberText.color = TextSoftLavender;

        Text vitaLabel = CreateText(tgRect, "HP_Label_Vita", "VITA",
            new Vector2(0f, -20f), new Vector2(90f, 18f),
            TextAnchor.MiddleCenter, 9, font);
        vitaLabel.color     = new Color(TextSoftLavender.r, TextSoftLavender.g, TextSoftLavender.b, 0.7f);
        vitaLabel.fontStyle = FontStyle.Bold;
    }

    // Right stack: Level row + XP bar + Gold pip
    private void BuildRightStack(RectTransform parent, Font font)
    {
        GameObject stack = new GameObject("Right_Stack", typeof(RectTransform));
        stack.transform.SetParent(parent, false);
        RectTransform stackRect = stack.GetComponent<RectTransform>();
        stackRect.anchorMin        = new Vector2(0f, 1f);
        stackRect.anchorMax        = new Vector2(0f, 1f);
        stackRect.pivot            = new Vector2(0f, 1f);
        stackRect.anchoredPosition = new Vector2(144f, 0f); // orb'un sağına
        stackRect.sizeDelta        = new Vector2(260f, 140f);

        BuildLevelRow(stackRect, font);
        BuildXpBarGroup(stackRect, font);
        BuildGoldPip(stackRect, font);
    }

    private void BuildLevelRow(RectTransform parent, Font font)
    {
        GameObject row = new GameObject("Level_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin        = new Vector2(0f, 1f);
        rowRect.anchorMax        = new Vector2(0f, 1f);
        rowRect.pivot            = new Vector2(0f, 1f);
        rowRect.anchoredPosition = new Vector2(0f, 0f);
        rowRect.sizeDelta        = new Vector2(260f, 36f);

        // Level badge background
        GameObject levelBadge = new GameObject("Level_Symbol_Bg", typeof(RectTransform), typeof(Image));
        levelBadge.transform.SetParent(rowRect, false);
        RectTransform badgeRect = levelBadge.GetComponent<RectTransform>();
        badgeRect.anchorMin        = new Vector2(0f, 0.5f);
        badgeRect.anchorMax        = new Vector2(0f, 0.5f);
        badgeRect.pivot            = new Vector2(0f, 0.5f);
        badgeRect.anchoredPosition = new Vector2(0f, 0f);
        badgeRect.sizeDelta        = new Vector2(36f, 36f);
        Image badgeImg = levelBadge.GetComponent<Image>();
        badgeImg.sprite = GetLevelBadgeSprite();
        badgeImg.color  = Color.white;

        // Badge frame
        GameObject badgeFrame = new GameObject("Level_Symbol_Frame", typeof(RectTransform), typeof(Image));
        badgeFrame.transform.SetParent(badgeRect, false);
        RectTransform badgeFrameRect = badgeFrame.GetComponent<RectTransform>();
        badgeFrameRect.anchorMin        = new Vector2(0.5f, 0.5f);
        badgeFrameRect.anchorMax        = new Vector2(0.5f, 0.5f);
        badgeFrameRect.pivot            = new Vector2(0.5f, 0.5f);
        badgeFrameRect.anchoredPosition = Vector2.zero;
        badgeFrameRect.sizeDelta        = new Vector2(38f, 38f);
        Image badgeFrameImg = badgeFrame.GetComponent<Image>();
        badgeFrameImg.sprite = GetRingSprite();
        badgeFrameImg.color  = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.9f);

        // Pulse ring
        GameObject pulse = new GameObject("Level_Symbol_Pulse", typeof(RectTransform), typeof(Image));
        pulse.transform.SetParent(badgeRect, false);
        RectTransform pulseRect = pulse.GetComponent<RectTransform>();
        pulseRect.anchorMin = new Vector2(0.5f, 0.5f);
        pulseRect.anchorMax = new Vector2(0.5f, 0.5f);
        pulseRect.pivot     = new Vector2(0.5f, 0.5f);
        pulseRect.sizeDelta = new Vector2(44f, 44f);
        pulseRect.anchoredPosition = Vector2.zero;
        Image pulseImg = pulse.GetComponent<Image>();
        pulseImg.sprite = GetRingSprite();
        pulseImg.color  = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.55f);
        LevelPulseRing pulseComp = pulse.AddComponent<LevelPulseRing>();
        pulseComp.SetPulseImage(pulse.GetComponent<Image>());

        // Roman numeral text
        levelRomanText = CreateText(rowRect, "Level_Roman_Text", "I",
            new Vector2(40f, 0f), new Vector2(50f, 36f),
            TextAnchor.MiddleLeft, 18, font);
        levelRomanText.color     = TextSoftLavender;
        levelRomanText.fontStyle = FontStyle.Bold;

        // "ATLAS LEVEL" label
        levelLabelText = CreateText(rowRect, "Level_Label_Text", "ATLAS LEVEL",
            new Vector2(96f, 0f), new Vector2(160f, 36f),
            TextAnchor.MiddleLeft, 10, font);
        levelLabelText.color = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.85f);
    }

    private void BuildXpBarGroup(RectTransform parent, Font font)
    {
        GameObject group = new GameObject("XP_Bar_Group", typeof(RectTransform));
        group.transform.SetParent(parent, false);
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin        = new Vector2(0f, 1f);
        groupRect.anchorMax        = new Vector2(0f, 1f);
        groupRect.pivot            = new Vector2(0f, 1f);
        groupRect.anchoredPosition = new Vector2(0f, -46f);
        groupRect.sizeDelta        = new Vector2(260f, 52f);

        // Frame
        GameObject frame = new GameObject("XP_Bar_Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(groupRect, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin        = new Vector2(0f, 1f);
        frameRect.anchorMax        = new Vector2(1f, 1f);
        frameRect.pivot            = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta        = new Vector2(0f, 14f);
        Image frameImage = frame.GetComponent<Image>();
        frameImage.sprite = GetRoundedBarSprite();
        frameImage.color  = XpFrameColor;

        // Background
        GameObject bg = new GameObject("XP_Bar_Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(frameRect, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(1f, 1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.sprite = GetRoundedBarSprite();
        bgImage.color  = XpBackgroundColor;

        // Fill
        GameObject fill = new GameObject("XP_Bar_Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bgRect, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        xpBarFill               = fill.GetComponent<Image>();
        xpBarFill.sprite        = GetXpFillSprite();
        xpBarFill.color         = Color.white;
        xpBarFill.type          = Image.Type.Filled;
        xpBarFill.fillMethod    = Image.FillMethod.Horizontal;
        xpBarFill.fillOrigin    = (int)Image.OriginHorizontal.Left;
        xpBarFill.fillAmount    = 0f;

        // Flow effect
        GameObject flowGo = new GameObject("XP_Flow_Effect", typeof(RectTransform), typeof(RawImage));
        flowGo.transform.SetParent(bgRect, false);
        RectTransform flowRect = flowGo.GetComponent<RectTransform>();
        flowRect.anchorMin = Vector2.zero;
        flowRect.anchorMax = Vector2.one;
        flowRect.offsetMin = Vector2.zero;
        flowRect.offsetMax = Vector2.zero;
        RawImage flowRaw = flowGo.GetComponent<RawImage>();
        flowRaw.color = new Color(0.427f, 0.851f, 0.941f, 0.10f); // #6DD9F0
        XpBarFlowEffect flowEffect = flowGo.AddComponent<XpBarFlowEffect>();
        flowEffect.SetFlowImage(flowRaw);

        // Meta text row
        GameObject metaRow = new GameObject("XP_Meta_Text_Row", typeof(RectTransform));
        metaRow.transform.SetParent(groupRect, false);
        RectTransform metaRect = metaRow.GetComponent<RectTransform>();
        metaRect.anchorMin        = new Vector2(0f, 1f);
        metaRect.anchorMax        = new Vector2(1f, 1f);
        metaRect.pivot            = new Vector2(0.5f, 1f);
        metaRect.sizeDelta        = new Vector2(0f, 20f);
        metaRect.anchoredPosition = new Vector2(0f, -18f);

        Text xpLabel = CreateText(metaRect, "XP_Label_Text", "EXPERIENCE",
            new Vector2(0f, 0f), new Vector2(120f, 18f),
            TextAnchor.MiddleLeft, 9, font);
        xpLabel.color = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.7f);

        xpValueText = CreateText(metaRect, "XP_Value_Text", "0 / 5",
            new Vector2(120f, 0f), new Vector2(138f, 18f),
            TextAnchor.MiddleRight, 10, font);
        xpValueText.color = new Color(TextSoftLavender.r, TextSoftLavender.g, TextSoftLavender.b, 0.9f);
    }

    private void BuildGoldPip(RectTransform parent, Font font)
    {
        GameObject pip = new GameObject("Gold_Pip", typeof(RectTransform));
        pip.transform.SetParent(parent, false);
        RectTransform pipRect = pip.GetComponent<RectTransform>();
        pipRect.anchorMin        = new Vector2(0f, 1f);
        pipRect.anchorMax        = new Vector2(0f, 1f);
        pipRect.pivot            = new Vector2(0f, 1f);
        pipRect.anchoredPosition = new Vector2(0f, -108f);
        pipRect.sizeDelta        = new Vector2(180f, 28f);

        // Coin image (circle)
        GameObject coin = new GameObject("Coin_Image", typeof(RectTransform), typeof(Image));
        coin.transform.SetParent(pipRect, false);
        RectTransform coinRect = coin.GetComponent<RectTransform>();
        coinRect.anchorMin        = new Vector2(0f, 0.5f);
        coinRect.anchorMax        = new Vector2(0f, 0.5f);
        coinRect.pivot            = new Vector2(0f, 0.5f);
        coinRect.anchoredPosition = Vector2.zero;
        coinRect.sizeDelta        = new Vector2(22f, 22f);
        Image coinImg = coin.GetComponent<Image>();
        coinImg.sprite = GetCoinSprite();
        coinImg.color  = Color.white;

        goldNumberText = CreateText(pipRect, "Gold_Number", "0",
            new Vector2(28f, 0f), new Vector2(150f, 28f),
            TextAnchor.MiddleLeft, 14, font);
        goldNumberText.color     = GoldTextColor;
        goldNumberText.fontStyle = FontStyle.Bold;
    }

    private void BuildMinimapPlaceholder(RectTransform root, Font font)
    {
        if (minimapPlaceholderImage != null) return;

        GameObject panel = new GameObject("MinimapPlaceholder", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(1f, 1f);
        rect.anchorMax        = new Vector2(1f, 1f);
        rect.pivot            = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta        = new Vector2(190f, 190f);
        minimapPlaceholderImage = panel.GetComponent<Image>();
        minimapPlaceholderImage.sprite = GetHudOverlaySprite();
        minimapPlaceholderImage.color = HudOverlayColor;
    }

    // ── UI helpers ───────────────────────────────────────────────────────────

    private Image CreateCircleImage(RectTransform parent, string name, Color color, float w, float h, bool ring = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.pivot            = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta        = new Vector2(w, h);
        Image img = go.GetComponent<Image>();
        img.sprite = ring ? GetRingSprite() : GetCircleSprite();
        img.color = color;
        return img;
    }

    private Text CreateText(RectTransform parent, string name, string value,
        Vector2 anchoredPos, Vector2 sizeDelta, TextAnchor alignment, int fontSize, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0f, 0.5f);
        rect.anchorMax        = new Vector2(0f, 0.5f);
        rect.pivot            = new Vector2(0f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        Text text = go.GetComponent<Text>();
        text.font               = font;
        text.fontSize           = fontSize;
        text.alignment          = alignment;
        text.color              = TextSoftLavender;
        text.text               = value;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow   = VerticalWrapMode.Overflow;
        return text;
    }
}
