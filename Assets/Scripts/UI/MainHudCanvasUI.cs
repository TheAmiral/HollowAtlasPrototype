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
    private PlayerMovement    _playerMovement;
    private CanvasGroup       hudCanvasGroup;

    // Feedback UI refs (cached during runtime build)
    private Image     _xpFrameImage;
    private Image     dashBarFill;
    private Transform _levelRowTransform;
    private Transform _goldPipTransform;
    private Text      killText;

    // Feedback state
    private int  _prevHp;
    private int  _prevXp;
    private int  _prevGold;
    private int  _prevLevel;
    private bool _feedbackInitialized;
    private bool _hpFlashing;
    private bool _xpPulsing;
    private bool _goldPopping;
    private bool _levelBursting;
    private bool _hudBuilt;

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
        DetectAndTriggerFeedback();
        UpdateLoadoutHud();
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

        if (_playerMovement != null && dashBarFill != null)
            dashBarFill.fillAmount = _playerMovement.DashCooldownNormalized;
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

        if (killText != null)
            killText.text = RunStatsSystem.Instance != null
                ? $"KILL: {RunStatsSystem.Instance.KillsThisRun}"
                : "KILL: 0";

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
        if (playerHealth      == null) playerHealth      = FindFirstObjectByType<PlayerHealth>();
        if (playerLevelSystem == null) playerLevelSystem = FindFirstObjectByType<PlayerLevelSystem>();
        if (goldWallet        == null) goldWallet        = FindFirstObjectByType<GoldWallet>();
        if (_playerMovement   == null) _playerMovement   = FindFirstObjectByType<PlayerMovement>();
    }

    // ── HUD reactive feedback ────────────────────────────────────────────────

    private void DetectAndTriggerFeedback()
    {
        // First frame: establish baseline without triggering effects.
        if (!_feedbackInitialized)
        {
            _prevHp    = playerHealth?.CurrentHealth ?? 0;
            _prevXp    = playerLevelSystem?.CurrentXP ?? 0;
            _prevGold  = goldWallet?.RunGold ?? 0;
            _prevLevel = playerLevelSystem?.Level ?? 1;
            _feedbackInitialized = true;
            return;
        }

        if (playerHealth != null)
        {
            int hp = playerHealth.CurrentHealth;
            if (!_hpFlashing)
            {
                if (hp < _prevHp) StartCoroutine(HpDamageFlash());
                else if (hp > _prevHp) StartCoroutine(HealFlash());
            }
            _prevHp = hp;
        }

        if (playerLevelSystem != null)
        {
            int lvl = playerLevelSystem.Level;
            int xp  = playerLevelSystem.CurrentXP;

            if (lvl > _prevLevel && !_levelBursting)
                StartCoroutine(LevelUpBurst());
            else if (xp != _prevXp && lvl == _prevLevel && !_xpPulsing)
                StartCoroutine(XpPulse());

            _prevLevel = lvl;
            _prevXp    = xp;
        }

        if (goldWallet != null)
        {
            int gold = goldWallet.RunGold;
            if (gold > _prevGold && !_goldPopping)
                StartCoroutine(GoldPop());
            _prevGold = gold;
        }
    }

    private IEnumerator HpDamageFlash()
    {
        _hpFlashing = true;
        if (hpOrbFill == null) { _hpFlashing = false; yield break; }

        Color orig  = hpOrbFill.color;
        Color flash = new Color(1f, 0.18f, 0.14f, 1f);
        float dur   = 0.25f;
        float t     = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            if (hpOrbFill != null)
                hpOrbFill.color = Color.Lerp(flash, orig, Mathf.Clamp01(t / dur));
            yield return null;
        }

        if (hpOrbFill != null) hpOrbFill.color = orig;
        _hpFlashing = false;
    }

    private IEnumerator HealFlash()
    {
        _hpFlashing = true;
        if (hpOrbFill == null) { _hpFlashing = false; yield break; }

        Color orig  = hpOrbFill.color;
        Color flash = new Color(0.38f, 1f, 0.65f, 1f);
        float half  = 0.14f;
        float t     = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (hpOrbFill != null)
                hpOrbFill.color = Color.Lerp(orig, flash, Mathf.Clamp01(t / half));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (hpOrbFill != null)
                hpOrbFill.color = Color.Lerp(flash, orig, Mathf.Clamp01(t / half));
            yield return null;
        }

        if (hpOrbFill != null) hpOrbFill.color = orig;
        _hpFlashing = false;
    }

    private IEnumerator XpPulse()
    {
        _xpPulsing = true;
        if (_xpFrameImage == null) { _xpPulsing = false; yield break; }

        Color orig  = _xpFrameImage.color;
        Color flash = new Color(0.52f, 0.95f, 1.00f, 1f);
        float half  = 0.18f;
        float t     = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (_xpFrameImage != null)
                _xpFrameImage.color = Color.Lerp(orig, flash, Mathf.Clamp01(t / half));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (_xpFrameImage != null)
                _xpFrameImage.color = Color.Lerp(flash, orig, Mathf.Clamp01(t / half));
            yield return null;
        }

        if (_xpFrameImage != null) _xpFrameImage.color = orig;
        _xpPulsing = false;
    }

    private IEnumerator GoldPop()
    {
        _goldPopping = true;
        if (_goldPipTransform == null) { _goldPopping = false; yield break; }

        Vector3 baseScale = Vector3.one;
        Vector3 peak      = Vector3.one * 1.24f;
        float   half      = 0.09f;
        float   t         = 0f;

        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (_goldPipTransform != null)
                _goldPipTransform.localScale = Vector3.Lerp(baseScale, peak, Mathf.Clamp01(t / half));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.unscaledDeltaTime;
            if (_goldPipTransform != null)
                _goldPipTransform.localScale = Vector3.Lerp(peak, baseScale, Mathf.Clamp01(t / half));
            yield return null;
        }

        if (_goldPipTransform != null) _goldPipTransform.localScale = baseScale;
        _goldPopping = false;
    }

    private IEnumerator LevelUpBurst()
    {
        _levelBursting = true;
        if (_levelRowTransform == null) { _levelBursting = false; yield break; }

        Vector3 baseScale  = Vector3.one;
        Vector3 peak       = Vector3.one * 1.30f;
        Color   origText   = levelRomanText != null ? levelRomanText.color : Color.white;
        Color   burstColor = new Color(1f, 0.88f, 0.28f, 1f);
        float   riseT      = 0.12f;
        float   fallT      = 0.22f;
        float   t          = 0f;

        while (t < riseT)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / riseT);
            if (_levelRowTransform != null) _levelRowTransform.localScale = Vector3.Lerp(baseScale, peak, p);
            if (levelRomanText != null)     levelRomanText.color          = Color.Lerp(origText, burstColor, p);
            yield return null;
        }
        t = 0f;
        while (t < fallT)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fallT);
            if (_levelRowTransform != null) _levelRowTransform.localScale = Vector3.Lerp(peak, baseScale, p);
            if (levelRomanText != null)     levelRomanText.color          = Color.Lerp(burstColor, origText, p);
            yield return null;
        }

        if (_levelRowTransform != null) _levelRowTransform.localScale = baseScale;
        if (levelRomanText != null)     levelRomanText.color          = origText;
        _levelBursting = false;
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
        if (_hudBuilt) return;
        _hudBuilt = true;

        // Destroy editor-placed children so the programmatic build starts clean.
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        hpOrbFill    = null;
        xpBarFill    = null;
        hpNumberText = null;

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
    }

    // ── Premium HUD hierarchy builder ────────────────────────────────────────

    private void BuildPremiumHud(RectTransform root, Font font)
    {
        BuildBottomLeftInfo(root, font);
        BuildBottomCombatHud(root, font);
        BuildMinimapPlaceholder(root, font);
        BuildLoadoutHud(root, font);
    }

    // ── HUD_BottomLeft_Info: small gold + timer ───────────────────────────────

    private void BuildBottomLeftInfo(RectTransform root, Font font)
    {
        GameObject panel = new GameObject("HUD_BottomLeft_Info", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0f, 0f);
        panelRect.anchorMax        = new Vector2(0f, 0f);
        panelRect.pivot            = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(16f, 28f);
        panelRect.sizeDelta        = new Vector2(210f, 88f);

        GameObject bg = new GameObject("TopLeft_Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(panelRect, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        Image bgImg = bg.GetComponent<Image>();
        bgImg.sprite = GetHudOverlaySprite();
        bgImg.color  = new Color(HudOverlayColor.r, HudOverlayColor.g, HudOverlayColor.b, 0.72f);

        BuildGoldPip(panelRect, font);

        // KILL row — between gold (top) and timer (bottom)
        killText = CreateText(panelRect, "Kill_Text", "KILL: 0",
            new Vector2(8f, -4f), new Vector2(190f, 20f),
            TextAnchor.MiddleLeft, 12, font);
        killText.color = new Color(0.85f, 0.85f, 0.85f, 0.80f);

        timerText = CreateText(panelRect, "Timer_Text", "Time: 0.0s",
            new Vector2(8f, -26f), new Vector2(190f, 20f),
            TextAnchor.MiddleLeft, 10, font);
        timerText.color = new Color(TextSoftLavender.r, TextSoftLavender.g, TextSoftLavender.b, 0.62f);
    }

    // ── HUD_BottomCenter: HP orb + Level + XP ────────────────────────────────

    private void BuildBottomCombatHud(RectTransform root, Font font)
    {
        GameObject combat = new GameObject("HUD_BottomCenter", typeof(RectTransform));
        combat.transform.SetParent(root, false);
        RectTransform combatRect = combat.GetComponent<RectTransform>();
        combatRect.anchorMin        = new Vector2(0.5f, 0f);
        combatRect.anchorMax        = new Vector2(0.5f, 0f);
        combatRect.pivot            = new Vector2(0.5f, 0f);
        combatRect.anchoredPosition = new Vector2(0f, 28f);
        combatRect.sizeDelta        = new Vector2(760f, 130f);

        // Thin purple border behind panel
        GameObject border = new GameObject("Combat_Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(combatRect, false);
        RectTransform borderRect = border.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2f, -2f);
        borderRect.offsetMax = new Vector2(2f,  2f);
        Image borderImg = border.GetComponent<Image>();
        borderImg.sprite = GetHudOverlaySprite();
        borderImg.color  = new Color(XpFrameColor.r, XpFrameColor.g, XpFrameColor.b, 0.42f);

        // Dark panel background
        GameObject bgPanel = new GameObject("Combat_Panel", typeof(RectTransform), typeof(Image));
        bgPanel.transform.SetParent(combatRect, false);
        RectTransform bgPanelRect = bgPanel.GetComponent<RectTransform>();
        bgPanelRect.anchorMin = Vector2.zero;
        bgPanelRect.anchorMax = Vector2.one;
        bgPanelRect.offsetMin = Vector2.zero;
        bgPanelRect.offsetMax = Vector2.zero;
        Image bgPanelImg = bgPanel.GetComponent<Image>();
        bgPanelImg.sprite = GetHudOverlaySprite();
        bgPanelImg.color  = new Color(HudOverlayColor.r, HudOverlayColor.g, HudOverlayColor.b, 0.88f);

        BuildDashBar(combatRect, font);
        BuildHpOrb(combatRect, font);
        BuildRightStack(combatRect, font);
    }

    // ── HP Orb (108×108, radial 360 fill, centered in bottom panel) ─────────

    private void BuildHpOrb(RectTransform parent, Font font)
    {
        const float OrbSize = 108f;

        GameObject orbGroup = new GameObject("HP_Orb_Group", typeof(RectTransform));
        orbGroup.transform.SetParent(parent, false);
        RectTransform orbRect = orbGroup.GetComponent<RectTransform>();
        orbRect.anchorMin        = new Vector2(0.5f, 0.5f);
        orbRect.anchorMax        = new Vector2(0.5f, 0.5f);
        orbRect.pivot            = new Vector2(0.5f, 0.5f);
        orbRect.anchoredPosition = Vector2.zero;
        orbRect.sizeDelta        = new Vector2(OrbSize, OrbSize);

        orbGroup.AddComponent<OrbBreathing>();

        Image orbGlow = CreateCircleImage(orbRect, "Orb_Background_Glow",
            new Color(OrbGlowColor.r, OrbGlowColor.g, OrbGlowColor.b, 0.52f),
            132f, 132f);
        orbGlow.sprite = GetSoftGlowSprite();
        orbGlow.GetComponent<RectTransform>().SetSiblingIndex(0);

        CreateCircleImage(orbRect, "Orb_Track", OrbTrackColor, OrbSize, OrbSize);

        GameObject fillGo = new GameObject("Orb_Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(orbRect, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin        = new Vector2(0.5f, 0.5f);
        fillRect.anchorMax        = new Vector2(0.5f, 0.5f);
        fillRect.pivot            = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta        = new Vector2(OrbSize, OrbSize);

        hpOrbFill               = fillGo.GetComponent<Image>();
        hpOrbFill.sprite        = GetHpFillSprite();
        hpOrbFill.color         = Color.white;
        hpOrbFill.type          = Image.Type.Filled;
        hpOrbFill.fillMethod    = Image.FillMethod.Radial360;
        hpOrbFill.fillOrigin    = (int)Image.Origin360.Top;
        hpOrbFill.fillClockwise = true;
        hpOrbFill.fillAmount    = 1f;

        CreateCircleImage(orbRect, "Orb_Inner_Disk", OrbBackgroundColor, 74f, 74f);

        GameObject ringGo = new GameObject("Orb_Rotating_Ring", typeof(RectTransform), typeof(Image));
        ringGo.transform.SetParent(orbRect, false);
        RectTransform ringRect = ringGo.GetComponent<RectTransform>();
        ringRect.anchorMin        = new Vector2(0.5f, 0.5f);
        ringRect.anchorMax        = new Vector2(0.5f, 0.5f);
        ringRect.pivot            = new Vector2(0.5f, 0.5f);
        ringRect.anchoredPosition = Vector2.zero;
        ringRect.sizeDelta        = new Vector2(OrbSize, OrbSize);
        ringGo.GetComponent<Image>().sprite = GetRingSprite();
        ringGo.GetComponent<Image>().color  = new Color(OrbGlowColor.r, OrbGlowColor.g, OrbGlowColor.b, 0.72f);
        ringGo.AddComponent<OrbRingRotator>();

        GameObject textGroup = new GameObject("Orb_Text_Group", typeof(RectTransform));
        textGroup.transform.SetParent(orbRect, false);
        RectTransform tgRect = textGroup.GetComponent<RectTransform>();
        tgRect.anchorMin        = new Vector2(0.5f, 0.5f);
        tgRect.anchorMax        = new Vector2(0.5f, 0.5f);
        tgRect.pivot            = new Vector2(0.5f, 0.5f);
        tgRect.anchoredPosition = Vector2.zero;
        tgRect.sizeDelta        = new Vector2(72f, 50f);

        hpNumberText           = CreateText(tgRect, "HP_Number", "100",
            new Vector2(0f, 4f), new Vector2(72f, 28f),
            TextAnchor.MiddleCenter, 26, font);
        hpNumberText.fontStyle = FontStyle.Bold;
        hpNumberText.color     = TextSoftLavender;
        hpNumberText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        Text vitaLabel = CreateText(tgRect, "HP_Label_Vita", "VITA",
            new Vector2(0f, -16f), new Vector2(72f, 16f),
            TextAnchor.MiddleCenter, 8, font);
        vitaLabel.color     = new Color(TextSoftLavender.r, TextSoftLavender.g, TextSoftLavender.b, 0.62f);
        vitaLabel.fontStyle = FontStyle.Bold;
    }

    // ── Right stack: Level row + XP bar, right of centered HP orb ────────────

    private void BuildRightStack(RectTransform parent, Font font)
    {
        GameObject stack = new GameObject("Right_Stack", typeof(RectTransform));
        stack.transform.SetParent(parent, false);
        RectTransform stackRect = stack.GetComponent<RectTransform>();
        stackRect.anchorMin        = new Vector2(1f, 0.5f);
        stackRect.anchorMax        = new Vector2(1f, 0.5f);
        stackRect.pivot            = new Vector2(1f, 0.5f);
        stackRect.anchoredPosition = new Vector2(-22f, 0f);
        stackRect.sizeDelta        = new Vector2(286f, 104f);

        BuildLevelRow(stackRect, font);
        BuildXpBarGroup(stackRect, font);
    }

    private void BuildLevelRow(RectTransform parent, Font font)
    {
        GameObject row = new GameObject("Level_Row", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin        = new Vector2(0f, 1f);
        rowRect.anchorMax        = new Vector2(1f, 1f);
        rowRect.pivot            = new Vector2(0.5f, 1f);
        rowRect.anchoredPosition = Vector2.zero;
        rowRect.sizeDelta        = new Vector2(0f, 36f);
        _levelRowTransform       = row.transform;

        GameObject levelBadge = new GameObject("Level_Symbol_Bg", typeof(RectTransform), typeof(Image));
        levelBadge.transform.SetParent(rowRect, false);
        RectTransform badgeRect = levelBadge.GetComponent<RectTransform>();
        badgeRect.anchorMin        = new Vector2(0f, 0.5f);
        badgeRect.anchorMax        = new Vector2(0f, 0.5f);
        badgeRect.pivot            = new Vector2(0f, 0.5f);
        badgeRect.anchoredPosition = Vector2.zero;
        badgeRect.sizeDelta        = new Vector2(36f, 36f);
        levelBadge.GetComponent<Image>().sprite = GetLevelBadgeSprite();
        levelBadge.GetComponent<Image>().color  = Color.white;

        GameObject badgeFrame = new GameObject("Level_Symbol_Frame", typeof(RectTransform), typeof(Image));
        badgeFrame.transform.SetParent(badgeRect, false);
        RectTransform badgeFrameRect = badgeFrame.GetComponent<RectTransform>();
        badgeFrameRect.anchorMin        = new Vector2(0.5f, 0.5f);
        badgeFrameRect.anchorMax        = new Vector2(0.5f, 0.5f);
        badgeFrameRect.pivot            = new Vector2(0.5f, 0.5f);
        badgeFrameRect.anchoredPosition = Vector2.zero;
        badgeFrameRect.sizeDelta        = new Vector2(38f, 38f);
        badgeFrame.GetComponent<Image>().sprite = GetRingSprite();
        badgeFrame.GetComponent<Image>().color  = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.9f);

        GameObject pulse = new GameObject("Level_Symbol_Pulse", typeof(RectTransform), typeof(Image));
        pulse.transform.SetParent(badgeRect, false);
        RectTransform pulseRect = pulse.GetComponent<RectTransform>();
        pulseRect.anchorMin        = new Vector2(0.5f, 0.5f);
        pulseRect.anchorMax        = new Vector2(0.5f, 0.5f);
        pulseRect.pivot            = new Vector2(0.5f, 0.5f);
        pulseRect.anchoredPosition = Vector2.zero;
        pulseRect.sizeDelta        = new Vector2(44f, 44f);
        pulse.GetComponent<Image>().sprite = GetRingSprite();
        pulse.GetComponent<Image>().color  = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.50f);
        LevelPulseRing pulseComp = pulse.AddComponent<LevelPulseRing>();
        pulseComp.SetPulseImage(pulse.GetComponent<Image>());

        levelRomanText           = CreateText(rowRect, "Level_Roman_Text", "I",
            new Vector2(40f, 0f), new Vector2(52f, 36f),
            TextAnchor.MiddleLeft, 24, font);
        levelRomanText.color     = TextSoftLavender;
        levelRomanText.fontStyle = FontStyle.Bold;
        levelRomanText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);

        levelLabelText = CreateText(rowRect, "Level_Label_Text", "ATLAS LEVEL",
            new Vector2(98f, 0f), new Vector2(220f, 36f),
            TextAnchor.MiddleLeft, 12, font);
        levelLabelText.color = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.80f);
    }

    private void BuildXpBarGroup(RectTransform parent, Font font)
    {
        // Anchored to bottom of right stack, stretches full width
        GameObject group = new GameObject("XP_Bar_Group", typeof(RectTransform));
        group.transform.SetParent(parent, false);
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin        = new Vector2(0f, 0f);
        groupRect.anchorMax        = new Vector2(1f, 0f);
        groupRect.pivot            = new Vector2(0.5f, 0f);
        groupRect.anchoredPosition = Vector2.zero;
        groupRect.sizeDelta        = new Vector2(0f, 52f);

        // XP bar frame (border, top of group)
        GameObject frame = new GameObject("XP_Bar_Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(groupRect, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin        = new Vector2(0f, 1f);
        frameRect.anchorMax        = new Vector2(1f, 1f);
        frameRect.pivot            = new Vector2(0.5f, 1f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta        = new Vector2(0f, 18f);
        Image frameImage           = frame.GetComponent<Image>();
        frameImage.sprite          = GetRoundedBarSprite();
        frameImage.color           = XpFrameColor;
        _xpFrameImage              = frameImage;

        // Background
        GameObject bg = new GameObject("XP_Bar_Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(frameRect, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(1f, 1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        bg.GetComponent<Image>().sprite = GetRoundedBarSprite();
        bg.GetComponent<Image>().color  = XpBackgroundColor;

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

        // Animated flow shimmer
        GameObject flowGo = new GameObject("XP_Flow_Effect", typeof(RectTransform), typeof(RawImage));
        flowGo.transform.SetParent(bgRect, false);
        RectTransform flowRect = flowGo.GetComponent<RectTransform>();
        flowRect.anchorMin = Vector2.zero;
        flowRect.anchorMax = Vector2.one;
        flowRect.offsetMin = Vector2.zero;
        flowRect.offsetMax = Vector2.zero;
        RawImage flowRaw   = flowGo.GetComponent<RawImage>();
        flowRaw.color = new Color(0.427f, 0.851f, 0.941f, 0.10f);
        XpBarFlowEffect flowEffect = flowGo.AddComponent<XpBarFlowEffect>();
        flowEffect.SetFlowImage(flowRaw);

        // Meta text row below the bar
        GameObject metaRow = new GameObject("XP_Meta_Text_Row", typeof(RectTransform));
        metaRow.transform.SetParent(groupRect, false);
        RectTransform metaRect = metaRow.GetComponent<RectTransform>();
        metaRect.anchorMin        = new Vector2(0f, 1f);
        metaRect.anchorMax        = new Vector2(1f, 1f);
        metaRect.pivot            = new Vector2(0.5f, 1f);
        metaRect.anchoredPosition = new Vector2(0f, -22f);
        metaRect.sizeDelta        = new Vector2(0f, 20f);

        Text xpLabel = CreateText(metaRect, "XP_Label_Text", "EXPERIENCE",
            new Vector2(0f, 0f), new Vector2(130f, 18f),
            TextAnchor.MiddleLeft, 10, font);
        xpLabel.color = new Color(LevelFrameColor.r, LevelFrameColor.g, LevelFrameColor.b, 0.70f);

        xpValueText = CreateText(metaRect, "XP_Value_Text", "0 / 5",
            new Vector2(132f, 0f), new Vector2(260f, 18f),
            TextAnchor.MiddleLeft, 11, font);
        xpValueText.color = new Color(TextSoftLavender.r, TextSoftLavender.g, TextSoftLavender.b, 0.88f);
    }

    private void BuildDashBar(RectTransform parent, Font font)
    {
        // Left zone of the bottom-center combat panel, left of the HP orb
        GameObject group = new GameObject("Dash_Bar_Group", typeof(RectTransform));
        group.transform.SetParent(parent, false);
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin        = new Vector2(0f, 0.5f);
        groupRect.anchorMax        = new Vector2(0f, 0.5f);
        groupRect.pivot            = new Vector2(0f, 0.5f);
        groupRect.anchoredPosition = new Vector2(22f, 0f);
        groupRect.sizeDelta        = new Vector2(286f, 20f);

        // Label
        Text dashLabel = CreateText(groupRect, "Dash_Label", "DASH",
            new Vector2(0f, 0f), new Vector2(44f, 20f),
            TextAnchor.MiddleLeft, 9, font);
        dashLabel.color     = new Color(0.42f, 0.95f, 1f, 0.75f);
        dashLabel.fontStyle = FontStyle.Bold;

        // Bar frame
        GameObject frame = new GameObject("Dash_Bar_Frame", typeof(RectTransform), typeof(Image));
        frame.transform.SetParent(groupRect, false);
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin        = new Vector2(0f, 0.5f);
        frameRect.anchorMax        = new Vector2(1f, 0.5f);
        frameRect.pivot            = new Vector2(0f, 0.5f);
        frameRect.anchoredPosition = new Vector2(48f, 0f);
        frameRect.sizeDelta        = new Vector2(-48f, 14f);
        Image frameImg = frame.GetComponent<Image>();
        frameImg.sprite = GetRoundedBarSprite();
        frameImg.color  = new Color(0.42f, 0.95f, 1f, 0.38f);

        // Background
        GameObject bg = new GameObject("Dash_Bar_Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(frameRect, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(1f, 1f);
        bgRect.offsetMax = new Vector2(-1f, -1f);
        bg.GetComponent<Image>().sprite = GetRoundedBarSprite();
        bg.GetComponent<Image>().color  = new Color(0.04f, 0.06f, 0.12f, 1f);

        // Fill
        GameObject fill = new GameObject("Dash_Bar_Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bgRect, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        dashBarFill               = fill.GetComponent<Image>();
        dashBarFill.sprite        = GetRoundedBarSprite();
        dashBarFill.color         = new Color(0.15f, 0.90f, 1f, 0.90f);
        dashBarFill.type          = Image.Type.Filled;
        dashBarFill.fillMethod    = Image.FillMethod.Horizontal;
        dashBarFill.fillOrigin    = (int)Image.OriginHorizontal.Left;
        dashBarFill.fillAmount    = 1f;
    }

    private void BuildGoldPip(RectTransform parent, Font font)
    {
        // In new layout: parent is HUD_TopLeft_Info panel (anchor top-left)
        GameObject pip = new GameObject("Gold_Pip", typeof(RectTransform));
        pip.transform.SetParent(parent, false);
        RectTransform pipRect = pip.GetComponent<RectTransform>();
        pipRect.anchorMin        = new Vector2(0f, 1f);
        pipRect.anchorMax        = new Vector2(0f, 1f);
        pipRect.pivot            = new Vector2(0f, 1f);
        pipRect.anchoredPosition = new Vector2(8f, -8f);
        pipRect.sizeDelta        = new Vector2(170f, 26f);
        _goldPipTransform        = pip.transform;

        GameObject coin = new GameObject("Coin_Image", typeof(RectTransform), typeof(Image));
        coin.transform.SetParent(pipRect, false);
        RectTransform coinRect = coin.GetComponent<RectTransform>();
        coinRect.anchorMin        = new Vector2(0f, 0.5f);
        coinRect.anchorMax        = new Vector2(0f, 0.5f);
        coinRect.pivot            = new Vector2(0f, 0.5f);
        coinRect.anchoredPosition = Vector2.zero;
        coinRect.sizeDelta        = new Vector2(20f, 20f);
        coin.GetComponent<Image>().sprite = GetCoinSprite();
        coin.GetComponent<Image>().color  = Color.white;

        goldNumberText           = CreateText(pipRect, "Gold_Number", "0",
            new Vector2(26f, 0f), new Vector2(140f, 26f),
            TextAnchor.MiddleLeft, 15, font);
        goldNumberText.color     = GoldTextColor;
        goldNumberText.fontStyle = FontStyle.Bold;
        goldNumberText.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);
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

    // ── BUILD HUD — gameplay sırasında oyuncunun build'ini gösteren sol panel ──
    //    Bölümler: SİLAHLAR / PASİFLER / KALINTILAR
    //    Veri kaynakları: WeaponInventory, RunLoadoutSystem (pasifler + chest ödülleri)

    private const float LOADOUT_W   = 198f;
    private const float SEC_H       = 19f;   // bölüm başlığı yüksekliği
    private const float ROW_H       = 30f;
    private const float ROW_ICON    = 24f;
    private const float ROW_GAP     = 3f;
    private const float SEC_GAP     = 7f;
    private const float TOP_PAD     = 6f;
    private const float BOT_PAD     = 8f;

    private static readonly WeaponType[] WeaponOrder =
        { WeaponType.KatanaAura, WeaponType.RuhKunai, WeaponType.AtlasSphere };

    private static readonly Color LoadoutPanelBg = new Color(0.04f, 0.02f, 0.09f, 0.80f);
    private static readonly Color LoadoutSecBg   = new Color(0.18f, 0.07f, 0.36f, 0.92f);
    private static readonly Color LoadoutSecTx   = new Color(1.00f, 0.85f, 0.32f, 1f);
    private static readonly Color LoadoutRowBg   = new Color(0.10f, 0.06f, 0.22f, 0.85f);
    private static readonly Color LoadoutRowTx   = new Color(0.92f, 0.90f, 1.00f, 1f);

    private RectTransform _loadoutRoot;
    private Font          _loadoutFont;
    private int           _lastLoadoutSig = int.MinValue;
    private bool          _loadoutVisible = true; // ilk frame'de kesin uygulansın diye farklı başlat
    private float         _loadoutBuildY;          // rebuild sırasında akan y konumu

    private void BuildLoadoutHud(RectTransform root, Font font)
    {
        _loadoutFont = font;
        EnsureLoadoutRoot();
    }

    // Build sırasından bağımsız olarak paneli garantiye al (lazy-init).
    private void EnsureLoadoutRoot()
    {
        if (_loadoutRoot != null) return;

        if (_loadoutFont == null)
            _loadoutFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("HUD_Build", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        _loadoutRoot = go.GetComponent<RectTransform>();
        _loadoutRoot.anchorMin = _loadoutRoot.anchorMax = new Vector2(0f, 0.5f);
        _loadoutRoot.pivot     = new Vector2(0f, 0.5f);
        _loadoutRoot.anchoredPosition = new Vector2(16f, 0f);
        _loadoutRoot.sizeDelta = new Vector2(LOADOUT_W, 0f);
        _loadoutRoot.SetAsLastSibling();

        Debug.Log("[MainHudCanvasUI] Build HUD initialized");
    }

    private void UpdateLoadoutHud()
    {
        EnsureLoadoutRoot();

        // İçerik yalnızca build değişince yeniden çizilir
        int sig = ComputeLoadoutSignature();
        if (sig != _lastLoadoutSig)
        {
            _lastLoadoutSig = sig;
            RebuildLoadoutHud();
        }

        // Görünürlük: kart seçim ekranları açıkken gizle, gameplay'de göster
        bool selectionOpen = Time.timeScale <= 0f
                             || (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending);
        bool shouldShow = !selectionOpen;

        if (shouldShow != _loadoutVisible)
        {
            _loadoutVisible = shouldShow;
            _loadoutRoot.gameObject.SetActive(shouldShow);
            Debug.Log($"[MainHudCanvasUI] Build HUD visible {shouldShow}");
        }
    }

    // Silah/pasif/kalıntı durumunu özetleyen hafif imza — değişince rebuild tetikler.
    private int ComputeLoadoutSignature()
    {
        int sig = 17;

        var inv = WeaponInventory.Instance;
        if (inv != null)
        {
            foreach (var w in WeaponOrder)
            {
                sig = sig * 31 + (inv.HasWeapon(w) ? inv.GetLevel(w) : 0);
                sig = sig * 31 + (inv.IsWeaponAwakened(w) ? 1 : 0);
            }
        }

        var rls = RunLoadoutSystem.Instance;
        if (rls != null)
        {
            var po = rls.OrderedPassives;
            sig = sig * 31 + po.Count;
            for (int i = 0; i < po.Count; i++)
            {
                sig = sig * 31 + po[i].GetHashCode();
                sig = sig * 31 + rls.GetPassiveLevel(po[i]);
            }
            sig = sig * 31 + rls.ChestRewards.Count;
        }

        return sig;
    }

    private void RebuildLoadoutHud()
    {
        for (int i = _loadoutRoot.childCount - 1; i >= 0; i--)
            Destroy(_loadoutRoot.GetChild(i).gameObject);

        // Arka plan — ilk eklenir (en altta), anchors ile root'a yayılır
        var bg = LoadoutImage(_loadoutRoot, "PanelBg", LoadoutPanelBg);
        bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = Vector2.zero; bg.rectTransform.offsetMax = Vector2.zero;

        _loadoutBuildY = TOP_PAD;

        // SİLAHLAR
        BuildSectionHeader("SİLAHLAR");
        var inv = WeaponInventory.Instance;
        if (inv != null)
        {
            foreach (var w in WeaponOrder)
            {
                if (!inv.HasWeapon(w)) continue;
                int  lv  = inv.GetLevel(w);
                bool awk = inv.IsWeaponAwakened(w);
                BuildLoadoutRow(WeaponIconId(w, awk), WeaponRowName(w, lv, awk),
                    awk ? CardKind.WeaponAwakening : CardKind.WeaponUpgrade);
            }
        }

        _loadoutBuildY += SEC_GAP;

        // PASİFLER
        BuildSectionHeader("PASİFLER");
        var rls = RunLoadoutSystem.Instance;
        if (rls != null)
        {
            var po = rls.OrderedPassives;
            for (int i = 0; i < po.Count; i++)
            {
                string pid = po[i];
                int lv = rls.GetPassiveLevel(pid);
                BuildLoadoutRow("icon_" + pid,
                    $"{CardDatabase.GetPassiveName(pid)} Lv {lv}", CardKind.PassiveUnlock);
            }
        }

        _loadoutBuildY += SEC_GAP;

        // KALINTILAR
        BuildSectionHeader("KALINTILAR");
        if (rls != null)
        {
            var rewards = rls.ChestRewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                var e = rewards[i];
                BuildLoadoutRow(e.iconId, e.title, e.kind);
            }
        }

        float totalH = _loadoutBuildY + BOT_PAD;
        _loadoutRoot.sizeDelta = new Vector2(LOADOUT_W, totalH);
    }

    private void BuildSectionHeader(string title)
    {
        var header = LoadoutImage(_loadoutRoot, "Section", LoadoutSecBg);
        var hr = header.rectTransform;
        hr.anchorMin = new Vector2(0f, 1f); hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 1f);
        hr.sizeDelta = new Vector2(-4f, SEC_H);
        hr.anchoredPosition = new Vector2(0f, -_loadoutBuildY);

        var t = LoadoutLabel(header.rectTransform, "SecLabel", title, 11, FontStyle.Bold,
            LoadoutSecTx, TextAnchor.MiddleLeft);
        t.rectTransform.offsetMin = new Vector2(6f, 0f);
        t.rectTransform.offsetMax = new Vector2(-4f, 0f);

        _loadoutBuildY += SEC_H + ROW_GAP;
    }

    private void BuildLoadoutRow(string iconId, string name, CardKind kind)
    {
        var row = LoadoutImage(_loadoutRoot, "Row", LoadoutRowBg);
        var rr = row.rectTransform;
        rr.anchorMin = new Vector2(0f, 1f); rr.anchorMax = new Vector2(1f, 1f);
        rr.pivot = new Vector2(0.5f, 1f);
        rr.sizeDelta = new Vector2(-4f, ROW_H);
        rr.anchoredPosition = new Vector2(0f, -_loadoutBuildY);

        // İkon
        var icon = LoadoutImage(row.rectTransform, "Icon", Color.white);
        icon.sprite         = RewardIconLibrary.GetIcon(iconId, kind);
        icon.preserveAspect = true;
        var ir = icon.rectTransform;
        ir.anchorMin = new Vector2(0f, 0.5f); ir.anchorMax = new Vector2(0f, 0.5f);
        ir.pivot = new Vector2(0f, 0.5f);
        ir.sizeDelta = new Vector2(ROW_ICON, ROW_ICON);
        ir.anchoredPosition = new Vector2(3f, 0f);

        // İsim
        var txt = LoadoutLabel(row.rectTransform, "Name", name, 11, FontStyle.Bold,
            LoadoutRowTx, TextAnchor.MiddleLeft);
        var tr = txt.rectTransform;
        tr.offsetMin = new Vector2(ROW_ICON + 7f, 0f);
        tr.offsetMax = new Vector2(-4f, 0f);

        _loadoutBuildY += ROW_H + ROW_GAP;
    }

    // ── İkon / isim eşlemeleri ────────────────────────────────────────────────

    private static string WeaponIconId(WeaponType w, bool awakened) => w switch
    {
        WeaponType.KatanaAura  => awakened ? "icon_kanli_hilal"    : "icon_katana_aura",
        WeaponType.RuhKunai    => awakened ? "icon_ruh_firtinasi"  : "icon_ruh_kunai",
        WeaponType.AtlasSphere => awakened ? "icon_atlas_halosu"   : "icon_atlas_kuresi",
        _                      => "icon_unknown"
    };

    private static string WeaponRowName(WeaponType w, int lv, bool awakened)
    {
        if (awakened)
            return w switch
            {
                WeaponType.KatanaAura  => "Kanlı Hilal",
                WeaponType.RuhKunai    => "Ruh Fırtınası",
                WeaponType.AtlasSphere => "Atlas Halosu",
                _                      => CardDatabase.GetWeaponName(w)
            };
        return $"{CardDatabase.GetWeaponName(w)} Lv {lv}";
    }

    // ── UI yardımcıları ───────────────────────────────────────────────────────

    private Image LoadoutImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text LoadoutLabel(Transform parent, string name, string content, int size,
        FontStyle style, Color color, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var txt = go.AddComponent<Text>();
        txt.font      = _loadoutFont;
        txt.text      = content;
        txt.fontSize  = size;
        txt.fontStyle = style;
        txt.color     = color;
        txt.alignment = anchor;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow   = VerticalWrapMode.Truncate;
        txt.raycastTarget = false;
        return txt;
    }
}
