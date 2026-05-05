using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class RelicSelectionView : MonoBehaviour
{
    public bool InputBlocked { get; set; } = true;

    List<RelicDefinition> _relics;
    Action<int>           _onSelected;
    bool                  _selectionMade;

    CanvasGroup _overlayGroup;
    CanvasGroup _titleGroup;
    CanvasGroup _hintGroup;

    // Body rects for hit detection; aura rects for position animation
    readonly List<RectTransform> _cardRects  = new();
    readonly List<RectTransform> _auraRects  = new();
    readonly List<CanvasGroup>   _cardGroups = new();

    const float CARD_W       = 260f;
    const float CARD_H       = 430f;
    const float CARD_GAP     = 36f;
    const float ENTER_OFFSET = 220f;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(List<RelicDefinition> relics, Action<int> onSelected)
    {
        _relics        = relics;
        _onSelected    = onSelected;
        _selectionMade = false;
        InputBlocked   = true;
        BuildUI();
        StartCoroutine(AnimateIn());
    }

    public IEnumerator Dismiss(int selectedIndex)
    {
        InputBlocked = true;
        if (_titleGroup != null) StartCoroutine(Fade(_titleGroup, 1f, 0f, 0.15f));
        if (_hintGroup  != null) StartCoroutine(Fade(_hintGroup,  1f, 0f, 0.15f));
        for (int i = 0; i < _cardGroups.Count; i++)
        {
            float dur = (i == selectedIndex) ? 0.30f : 0.18f;
            StartCoroutine(Fade(_cardGroups[i], 1f, 0f, dur));
        }
        yield return new WaitForSecondsRealtime(0.36f);
        yield return StartCoroutine(Fade(_overlayGroup, 1f, 0f, 0.25f));
    }

    // ── UI Build ──────────────────────────────────────────────────────────────

    void BuildUI()
    {
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
        }

        var rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();

        // Overlay dim
        var overlayGo = MakePanel(rootRect, "Overlay", Vector2.zero, Vector2.one, CardThemeLibrary.OverlayBg);
        _overlayGroup = overlayGo.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = true;
        var overlayRect = overlayGo.GetComponent<RectTransform>();
        BuildVignette(overlayRect);

        // Focus backdrop
        var backdropGo = MakePanel(overlayRect, "Backdrop",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.04f, 0.02f, 0.10f, 0.50f));
        var bdr = backdropGo.GetComponent<RectTransform>();
        bdr.pivot     = new Vector2(0.5f, 0.5f);
        bdr.sizeDelta = new Vector2(1060f, 500f);
        bdr.anchoredPosition = new Vector2(0f, -14f);
        backdropGo.GetComponent<Image>().raycastTarget = false;

        // Title group
        var titleRoot = new GameObject("TitleRoot");
        titleRoot.transform.SetParent(rootRect, false);
        var trRect = titleRoot.AddComponent<RectTransform>();
        trRect.anchorMin = trRect.anchorMax = new Vector2(0.5f, 1f);
        trRect.pivot     = new Vector2(0.5f, 1f);
        trRect.sizeDelta = new Vector2(860f, 128f);
        trRect.anchoredPosition = new Vector2(0f, -50f);
        _titleGroup = titleRoot.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        var titleT = MakeText(trRect, "TitleText", "ATLAS KALINTILARI",
            Vector2.zero, Vector2.one, new Vector2(0f, -18f), Vector2.zero,
            46, FontStyle.Bold, CardThemeLibrary.TitleGold);
        titleT.alignment = TextAnchor.UpperCenter;

        var subShadow = MakeText(trRect, "SubShadow", "Sandığın içinden eski bir güç yankılanıyor.",
            Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 31f), new Vector2(0f, 40f),
            22, FontStyle.BoldAndItalic, new Color(0f, 0f, 0f, 0.88f));
        subShadow.alignment = TextAnchor.LowerCenter;
        subShadow.horizontalOverflow = HorizontalWrapMode.Wrap;

        var subT = MakeText(trRect, "SubText", "Sandığın içinden eski bir güç yankılanıyor.",
            Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 33f), new Vector2(0f, 40f),
            22, FontStyle.BoldAndItalic, CardThemeLibrary.SubtitleTint);
        subT.alignment = TextAnchor.LowerCenter;
        subT.horizontalOverflow = HorizontalWrapMode.Wrap;

        BuildTitleDivider(trRect);

        // Card area
        var cardArea = new GameObject("RelicCardArea");
        cardArea.transform.SetParent(transform, false);
        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot     = new Vector2(0.5f, 0.5f);
        float totalW = _relics.Count * CARD_W + (_relics.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -18f);

        _cardRects.Clear();
        _auraRects.Clear();
        _cardGroups.Clear();

        for (int i = 0; i < _relics.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            BuildRelicWidget(cardAreaRect, _relics[i], xPos, i);
        }

        // Input hint
        var hintRoot = new GameObject("HintRoot");
        hintRoot.transform.SetParent(transform, false);
        var hintRect = hintRoot.AddComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot     = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(600f, 44f);
        hintRect.anchoredPosition = new Vector2(0f, 30f);
        _hintGroup = hintRoot.AddComponent<CanvasGroup>();
        _hintGroup.alpha = 0f;

        var hintBorder = MakePanel(hintRect, "HintBorder", Vector2.zero, Vector2.one, CardThemeLibrary.PanelBorder);
        var hintBg     = MakePanel(hintBorder.GetComponent<RectTransform>(), "HintBg", Vector2.zero, Vector2.one,
            new Color(0.025f, 0.018f, 0.060f, 0.84f));
        hintBg.GetComponent<RectTransform>().offsetMin = new Vector2(1.5f, 1.5f);
        hintBg.GetComponent<RectTransform>().offsetMax = new Vector2(-1.5f, -1.5f);
        var ht = MakeText(hintRect, "HintText", "[ 1 ]   [ 2 ]   [ 3 ]   ile seç  —  veya karta tıkla",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            18, FontStyle.Bold, new Color(0.82f, 0.80f, 1.00f, 1f));
        ht.alignment = TextAnchor.MiddleCenter;
        hintBorder.GetComponent<Image>().raycastTarget = false;
        hintBg.GetComponent<Image>().raycastTarget     = false;
    }

    void BuildRelicWidget(RectTransform parent, RelicDefinition relic, float xPos, int index)
    {
        var theme    = GetRelicTheme(relic.category);
        float gAlpha = GetGlowAlpha(relic.rarity);

        // Aura (root of this card's hierarchy)
        var auraGo   = MakePanel(parent, $"RelicAura_{index}", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.main, gAlpha));
        var auraRect = auraGo.GetComponent<RectTransform>();
        auraRect.anchorMin = auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot     = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(CARD_W + 48f, CARD_H + 48f);
        auraRect.anchoredPosition = new Vector2(xPos, 0f);
        auraGo.GetComponent<Image>().raycastTarget = false;
        _auraRects.Add(auraRect);

        var cg = auraGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        _cardGroups.Add(cg);

        // Rim
        var rimGo   = MakePanel(auraRect, "Rim", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.main, gAlpha * 1.8f));
        var rimRect = rimGo.GetComponent<RectTransform>();
        rimRect.anchorMin = rimRect.anchorMax = new Vector2(0.5f, 0.5f);
        rimRect.pivot     = new Vector2(0.5f, 0.5f);
        rimRect.sizeDelta = new Vector2(CARD_W + 8f, CARD_H + 8f);
        rimRect.anchoredPosition = Vector2.zero;
        rimGo.GetComponent<Image>().raycastTarget = false;

        // Border
        var borderGo   = MakePanel(rimRect, "Border", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.main, 0.85f));
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot     = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(CARD_W + 3f, CARD_H + 3f);
        borderRect.anchoredPosition = Vector2.zero;
        borderGo.GetComponent<Image>().raycastTarget = false;

        // Body
        var bodyGo   = MakePanel(borderRect, "Body", Vector2.zero, Vector2.zero,
            new Color(theme.dark.r, theme.dark.g, theme.dark.b, 0.97f));
        var bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot     = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(CARD_W, CARD_H);
        bodyRect.anchoredPosition = Vector2.zero;
        var bodyImg = bodyGo.GetComponent<Image>();
        bodyImg.raycastTarget = true;
        _cardRects.Add(bodyRect);

        // Button
        var btn = bodyGo.AddComponent<Button>();
        btn.transition    = Selectable.Transition.None;
        btn.targetGraphic = bodyImg;
        int captured = index;
        btn.onClick.AddListener(() => TrySelect(captured));

        // Top band
        var bandGo   = MakePanel(bodyRect, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.55f));
        var bandRect = bandGo.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0f, 1f);
        bandRect.anchorMax = new Vector2(1f, 1f);
        bandRect.pivot     = new Vector2(0.5f, 1f);
        bandRect.sizeDelta = new Vector2(0f, 75f);
        bandRect.anchoredPosition = Vector2.zero;
        bandGo.GetComponent<Image>().raycastTarget = false;

        // Category icon
        var iconRect = MakeRegion(bodyRect, "IconArea", 28f, 46f, 0f);
        var iconT    = MakeText(iconRect, "Icon", GetCategoryIcon(relic.category),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            36, FontStyle.Normal, CardThemeLibrary.WithAlpha(theme.text, 0.72f));
        iconT.alignment = TextAnchor.MiddleCenter;

        // Category label badge
        var catGo   = MakePanel(bodyRect, "CatBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.20f));
        var catRect = catGo.GetComponent<RectTransform>();
        catRect.pivot     = new Vector2(0.5f, 1f);
        catRect.sizeDelta = new Vector2(170f, 26f);
        catRect.anchoredPosition = new Vector2(0f, -82f);
        catGo.GetComponent<Image>().raycastTarget = false;
        var catT = MakeText(catRect, "CatText", GetCategoryName(relic.category).ToUpper(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            13, FontStyle.Bold, CardThemeLibrary.WithAlpha(theme.text, 0.85f));
        catT.alignment = TextAnchor.MiddleCenter;

        // Corner marker
        string cornerLabel = GetCornerLabel(relic);
        if (!string.IsNullOrEmpty(cornerLabel))
            BuildCornerMarker(bodyRect, cornerLabel, theme);
        else
            BuildCornerFlare(bodyRect, theme.main);

        // Title
        var titleRect = MakeRegion(bodyRect, "TitleArea", 116f, 52f, 16f);
        var titleT    = MakeText(titleRect, "RelicTitle", relic.title,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            21, FontStyle.Bold, theme.text);
        ConfigText(titleT, TextAnchor.MiddleCenter, 13, 21);

        // Divider
        var divGo   = MakePanel(bodyRect, "Divider", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.glow, 0.40f));
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot     = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.5f);
        divRect.anchoredPosition = new Vector2(0f, -177f);
        divGo.GetComponent<Image>().raycastTarget = false;

        // Description
        var descRect = MakeRegion(bodyRect, "DescArea", 188f, 132f, 18f);
        var descT    = MakeText(descRect, "Desc", relic.description,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            14, FontStyle.Normal, new Color(theme.text.r, theme.text.g, theme.text.b, 0.78f));
        ConfigText(descT, TextAnchor.UpperCenter, 10, 14);

        // Index number
        var numT = MakeText(bodyRect, "IndexNum", $"[ {index + 1} ]",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 68f), new Vector2(0f, 22f),
            14, FontStyle.Normal, new Color(theme.text.r, theme.text.g, theme.text.b, 0.45f));
        ConfigText(numT, TextAnchor.MiddleCenter, 10, 14);
    }

    // ── Relic themes ──────────────────────────────────────────────────────────

    struct RelicTheme { public Color main, dark, glow, text; }

    static RelicTheme GetRelicTheme(RelicCategory cat) => cat switch
    {
        RelicCategory.Survival   => new RelicTheme { main = Hex("2FBF71"), dark = Hex("0C2E1D"), glow = Hex("7DFFAE"), text = Hex("E8FFF0") },
        RelicCategory.Combat     => new RelicTheme { main = Hex("C43737"), dark = Hex("2A0909"), glow = Hex("FF7B7B"), text = Hex("FFEAEA") },
        RelicCategory.Economy    => new RelicTheme { main = Hex("D6A62E"), dark = Hex("332304"), glow = Hex("FFE07A"), text = Hex("FFF6DA") },
        RelicCategory.Portal     => new RelicTheme { main = Hex("2F7DFF"), dark = Hex("0A1838"), glow = Hex("86B8FF"), text = Hex("EEF5FF") },
        RelicCategory.Boss       => new RelicTheme { main = Hex("D6A62E"), dark = Hex("332304"), glow = Hex("FFE07A"), text = Hex("FFF6DA") },
        RelicCategory.RiskReward => new RelicTheme { main = Hex("9B2FB5"), dark = Hex("250530"), glow = Hex("C4A2FF"), text = Hex("F3ECFF") },
        RelicCategory.Utility    => new RelicTheme { main = Hex("8758E8"), dark = Hex("1C1036"), glow = Hex("C4A2FF"), text = Hex("F3ECFF") },
        _ => new RelicTheme { main = Color.white, dark = Color.black, glow = Color.white, text = Color.white }
    };

    static float GetGlowAlpha(RelicRarity r) => r switch
    {
        RelicRarity.Common    => 0.10f,
        RelicRarity.Rare      => 0.20f,
        RelicRarity.Epic      => 0.28f,
        RelicRarity.Legendary => 0.36f,
        RelicRarity.Cursed    => 0.30f,
        _ => 0.08f
    };

    static string GetCategoryIcon(RelicCategory cat) => cat switch
    {
        RelicCategory.Survival   => "♥",
        RelicCategory.Combat     => "⚔",
        RelicCategory.Economy    => "◈",
        RelicCategory.Portal     => "◎",
        RelicCategory.Boss       => "★",
        RelicCategory.RiskReward => "☯",
        RelicCategory.Utility    => "∞",
        _ => "?"
    };

    static string GetCategoryName(RelicCategory cat) => cat switch
    {
        RelicCategory.Survival   => "Hayatta Kalma",
        RelicCategory.Combat     => "Savaş",
        RelicCategory.Economy    => "Ekonomi",
        RelicCategory.Portal     => "Portal",
        RelicCategory.Boss       => "Boss",
        RelicCategory.RiskReward => "Risk & Ödül",
        RelicCategory.Utility    => "Yardımcı",
        _ => "Bilinmeyen"
    };

    static string GetCornerLabel(RelicDefinition relic)
    {
        if (relic.rarity == RelicRarity.Cursed) return "LANETLİ";
        if (relic.category == RelicCategory.RiskReward ||
            (relic.tags & RelicTag.RiskReward) != 0)    return "RİSK";
        if (relic.category == RelicCategory.Boss)        return "BOSS";
        if (relic.category == RelicCategory.Portal)      return "PORTAL";
        return null;
    }

    void BuildCornerMarker(RectTransform parent, string label, RelicTheme theme)
    {
        var go = MakePanel(parent, "CornerMarker",
            new Vector2(1f, 1f), new Vector2(1f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.88f));
        var r = go.GetComponent<RectTransform>();
        r.pivot            = new Vector2(1f, 1f);
        r.sizeDelta        = new Vector2(76f, 24f);
        r.anchoredPosition = new Vector2(-12f, -12f);
        go.GetComponent<Image>().raycastTarget = false;

        var bgGo = MakePanel(r, "CornerBg", Vector2.zero, Vector2.one,
            CardThemeLibrary.WithAlpha(theme.dark, 0.94f));
        bgGo.GetComponent<RectTransform>().offsetMin = new Vector2(1.2f, 1.2f);
        bgGo.GetComponent<RectTransform>().offsetMax = new Vector2(-1.2f, -1.2f);
        bgGo.GetComponent<Image>().raycastTarget     = false;

        var t = MakeText(r, "CornerLabel", label,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            11, FontStyle.Bold, theme.text);
        t.alignment = TextAnchor.MiddleCenter;
    }

    static void BuildCornerFlare(RectTransform parent, Color accent)
    {
        var go   = new GameObject("CornerFlare");
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = CardThemeLibrary.WithAlpha(accent, 0.80f);
        img.raycastTarget = false;
        var r   = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(22f, 22f);
        r.anchoredPosition = new Vector2(-18f, -18f);
        r.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (InputBlocked) return;
        HandleMouseHover();
        if (HandleMouseClick()) return;
        HandleKeyboard();
    }

    void HandleMouseHover()
    {
        if (Mouse.current == null) return;
        Vector2 mp = Mouse.current.position.ReadValue();
        for (int i = 0; i < _cardRects.Count; i++)
        {
            if (_cardRects[i] == null) continue;
            bool hovered = RectTransformUtility.RectangleContainsScreenPoint(_cardRects[i], mp, null);
            _cardRects[i].localScale = Vector3.Lerp(
                _cardRects[i].localScale,
                hovered ? Vector3.one * 1.04f : Vector3.one,
                Time.unscaledDeltaTime * 14f);
        }
    }

    bool HandleMouseClick()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
        Vector2 mp = Mouse.current.position.ReadValue();
        for (int i = 0; i < _cardRects.Count; i++)
        {
            if (_cardRects[i] == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(_cardRects[i], mp, null))
            {
                TrySelect(i);
                return true;
            }
        }
        return false;
    }

    void HandleKeyboard()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) TrySelect(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) TrySelect(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) TrySelect(2);
    }

    void TrySelect(int index)
    {
        if (InputBlocked || _selectionMade) return;
        _selectionMade = true;
        _onSelected?.Invoke(index);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator AnimateIn()
    {
        yield return StartCoroutine(Fade(_overlayGroup, 0f, 1f, 0.28f));
        StartCoroutine(Fade(_titleGroup, 0f, 1f, 0.22f));

        for (int i = 0; i < _cardGroups.Count; i++)
            StartCoroutine(AnimateCardIn(i, 0.10f * i));

        yield return new WaitForSecondsRealtime(0.10f * (_cardGroups.Count - 1) + 0.35f);
        yield return StartCoroutine(Fade(_hintGroup, 0f, 1f, 0.18f));
        InputBlocked = false;
    }

    IEnumerator AnimateCardIn(int index, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var cg       = _cardGroups[index];
        var auraRect = _auraRects[index];
        float dur    = 0.36f, t = 0f;
        Vector2 start = auraRect.anchoredPosition + Vector2.down * ENTER_OFFSET;
        Vector2 end   = auraRect.anchoredPosition;
        cg.alpha = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float te = BounceEaseOut(Mathf.Clamp01(t / dur));
            auraRect.anchoredPosition = Vector2.Lerp(start, end, te);
            auraRect.localScale       = Vector3.Lerp(Vector3.one * 0.82f, Vector3.one, te);
            cg.alpha                  = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / dur * 3f));
            yield return null;
        }
        auraRect.anchoredPosition = end;
        auraRect.localScale       = Vector3.one;
        cg.alpha                  = 1f;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    void BuildVignette(RectTransform parent)
    {
        Color c = new Color(0f, 0f, 0f, 0.65f);
        float s = 420f;
        Vig(parent, "VigL", new Vector2(0f,0f), new Vector2(0f,1f), new Vector2(s,0f), c);
        Vig(parent, "VigR", new Vector2(1f,0f), new Vector2(1f,1f), new Vector2(s,0f), c);
        Vig(parent, "VigU", new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f,s), c);
        Vig(parent, "VigD", new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0f,s), c);
    }

    void Vig(RectTransform parent, string name, Vector2 amin, Vector2 amax, Vector2 size, Color c)
    {
        var go   = MakePanel(parent, name, amin, amax, c);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = amin; rect.anchorMax = amax;
        rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().raycastTarget = false;
    }

    void BuildTitleDivider(RectTransform parent)
    {
        var shadow = MakePanel(parent, "TitleLineShadow", Vector2.zero, Vector2.zero, new Color(0f,0f,0f,0.72f));
        var sr = shadow.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.24f, 0f); sr.anchorMax = new Vector2(0.76f, 0f);
        sr.pivot = new Vector2(0.5f, 0f); sr.sizeDelta = new Vector2(0f, 4f); sr.anchoredPosition = new Vector2(0f, 7f);
        var line = MakePanel(parent, "TitleLine", Vector2.zero, Vector2.zero, CardThemeLibrary.TitleGold);
        var lr = line.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.28f, 0f); lr.anchorMax = new Vector2(0.72f, 0f);
        lr.pivot = new Vector2(0.5f, 0f); lr.sizeDelta = new Vector2(0f, 2f); lr.anchoredPosition = new Vector2(0f, 9f);
        shadow.GetComponent<Image>().raycastTarget = false;
        line.GetComponent<Image>().raycastTarget   = false;
    }

    // ── Coroutine helpers ─────────────────────────────────────────────────────

    IEnumerator Fade(CanvasGroup g, float from, float to, float dur)
    {
        if (g == null) yield break;
        float t = 0f; g.alpha = from;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        g.alpha = to;
    }

    static float BounceEaseOut(float t)
    {
        if (t < 0.727f) return 7.5625f * t * t;
        if (t < 0.909f) { t -= 0.818f; return 7.5625f * t * t + 0.75f; }
        if (t < 0.977f) { t -= 0.954f; return 7.5625f * t * t + 0.9375f; }
        t -= 0.988f; return 7.5625f * t * t + 0.984375f;
    }

    // ── Low-level UI factories ────────────────────────────────────────────────

    static GameObject MakePanel(RectTransform parent, string name, Vector2 amin, Vector2 amax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r   = go.GetComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax;
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        return go;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        Vector2 amin, Vector2 amax, Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, FontStyle style, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text      = content;
        txt.fontSize  = fontSize;
        txt.fontStyle = style;
        txt.color     = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax;
        r.sizeDelta = sizeDelta; r.anchoredPosition = anchoredPos;
        return txt;
    }

    static RectTransform MakeRegion(RectTransform parent, string name, float topOffset, float height, float hInset)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot     = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-hInset * 2f, height);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        return rect;
    }

    static void ConfigText(Text t, TextAnchor align, int minSz, int maxSz)
    {
        if (t == null) return;
        t.alignment            = align;
        t.horizontalOverflow   = HorizontalWrapMode.Wrap;
        t.verticalOverflow     = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize    = minSz;
        t.resizeTextMaxSize    = maxSz;
    }

    static Color Hex(string hex, float a = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        c.a = a;
        return c;
    }
}
