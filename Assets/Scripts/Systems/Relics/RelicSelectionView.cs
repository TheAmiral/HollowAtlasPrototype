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
    Action                _onRerollRequested;
    bool                  _selectionMade;

    CanvasGroup _overlayGroup;
    CanvasGroup _titleGroup;
    CanvasGroup _hintGroup;
    CanvasGroup _rerollGroup;

    readonly List<RectTransform> _cardRects    = new();
    readonly List<RectTransform> _auraRects    = new();
    readonly List<CanvasGroup>   _cardGroups   = new();
    readonly List<CanvasGroup>   _promptGroups = new();

    RectTransform _rerollBtnBorder;
    Image         _rerollBtnBg;
    Text          _rerollCostText;
    Text          _rerollCountText;

    const float CARD_W       = 272f;
    const float CARD_H       = 390f;
    const float CARD_GAP     = 76f;
    const float ENTER_OFFSET = 260f;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(List<RelicDefinition> relics, Action<int> onSelected, Action onRerollRequested = null)
    {
        _relics            = relics;
        _onSelected        = onSelected;
        _onRerollRequested = onRerollRequested;
        _selectionMade     = false;
        InputBlocked       = true;
        BuildUI();
        StartCoroutine(AnimateIn());
    }

    public IEnumerator Dismiss(int selectedIndex)
    {
        InputBlocked = true;
        if (_titleGroup  != null) StartCoroutine(Fade(_titleGroup,  1f, 0f, 0.15f));
        if (_hintGroup   != null) StartCoroutine(Fade(_hintGroup,   1f, 0f, 0.15f));
        if (_rerollGroup != null) StartCoroutine(Fade(_rerollGroup, 1f, 0f, 0.15f));
        for (int i = 0; i < _cardGroups.Count; i++)
            StartCoroutine(Fade(_cardGroups[i], 1f, 0f, i == selectedIndex ? 0.30f : 0.18f));
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

        var overlayGo   = MakePanel(rootRect, "Overlay", Vector2.zero, Vector2.one, CardThemeLibrary.OverlayBg);
        _overlayGroup   = overlayGo.AddComponent<CanvasGroup>();
        _overlayGroup.alpha          = 0f;
        _overlayGroup.blocksRaycasts = true;
        var overlayRect = overlayGo.GetComponent<RectTransform>();
        BuildVignette(overlayRect);
        BuildFocusBackdrop(overlayRect);

        // Title root — 980×158 anchored at (0,-46) matching CardSelectionView
        var titleRoot = new GameObject("TitleRoot");
        titleRoot.transform.SetParent(rootRect, false);
        var trRect = titleRoot.AddComponent<RectTransform>();
        trRect.anchorMin = trRect.anchorMax = new Vector2(0.5f, 1f);
        trRect.pivot     = new Vector2(0.5f, 1f);
        trRect.sizeDelta = new Vector2(980f, 158f);
        trRect.anchoredPosition = new Vector2(0f, -46f);
        _titleGroup = titleRoot.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        var titleT = MakeText(trRect, "TitleText", "ATLAS KALINTILARI",
            Vector2.zero, Vector2.one, new Vector2(0f, -18f), Vector2.zero,
            58, FontStyle.Bold, CardThemeLibrary.TitleGold);
        titleT.alignment = TextAnchor.UpperCenter;

        var subShadow = MakeText(trRect, "SubShadow", "Sandığın içinden eski bir güç yankılanıyor.",
            Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 31f), new Vector2(0f, 40f),
            27, FontStyle.BoldAndItalic, new Color(0f, 0f, 0f, 0.88f));
        subShadow.alignment = TextAnchor.LowerCenter;
        subShadow.horizontalOverflow = HorizontalWrapMode.Wrap;
        subShadow.verticalOverflow   = VerticalWrapMode.Overflow;

        var subT = MakeText(trRect, "SubText", "Sandığın içinden eski bir güç yankılanıyor.",
            Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 33f), new Vector2(0f, 40f),
            27, FontStyle.BoldAndItalic, CardThemeLibrary.SubtitleTint);
        subT.alignment = TextAnchor.LowerCenter;
        subT.horizontalOverflow = HorizontalWrapMode.Wrap;
        subT.verticalOverflow   = VerticalWrapMode.Overflow;

        BuildTitleDivider(trRect);

        // Card area
        var cardArea     = new GameObject("RelicCardArea");
        cardArea.transform.SetParent(transform, false);
        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot     = new Vector2(0.5f, 0.5f);
        float totalW = _relics.Count * CARD_W + (_relics.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -24f);

        _cardRects.Clear(); _auraRects.Clear(); _cardGroups.Clear(); _promptGroups.Clear();
        for (int i = 0; i < _relics.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            BuildRelicWidget(cardAreaRect, _relics[i], xPos, i);
        }

        // Hint bar — 780×50 at y=28
        var hintRoot = new GameObject("HintRoot");
        hintRoot.transform.SetParent(transform, false);
        var hintRect = hintRoot.AddComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot     = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(780f, 50f);
        hintRect.anchoredPosition = new Vector2(0f, 28f);
        _hintGroup = hintRoot.AddComponent<CanvasGroup>();
        _hintGroup.alpha = 0f;

        var hintBorder = MakePanel(hintRect, "HintBorder", Vector2.zero, Vector2.one, CardThemeLibrary.PanelBorder);
        var hintBg     = MakePanel(hintBorder.GetComponent<RectTransform>(), "HintBg", Vector2.zero, Vector2.one,
            new Color(0.025f, 0.018f, 0.060f, 0.84f));
        hintBg.GetComponent<RectTransform>().offsetMin = new Vector2(1.5f, 1.5f);
        hintBg.GetComponent<RectTransform>().offsetMax = new Vector2(-1.5f, -1.5f);
        var ht = MakeText(hintRect, "HintText", "[ 1 ]   [ 2 ]   [ 3 ]   ile seç  —  veya karta tıkla",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            21, FontStyle.Bold, new Color(0.82f, 0.80f, 1.00f, 1f));
        ht.alignment = TextAnchor.MiddleCenter;
        hintBorder.GetComponent<Image>().raycastTarget = false;
        hintBg.GetComponent<Image>().raycastTarget     = false;

        BuildRerollFooter();
    }

    void BuildRelicWidget(RectTransform parent, RelicDefinition relic, float xPos, int index)
    {
        var theme    = GetRelicTheme(relic.category);
        float gAlpha = GetGlowAlpha(relic.rarity);

        // Aura — root of card hierarchy, used for enter animation
        var auraGo   = MakePanel(parent, $"RelicAura_{index}", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.glow, gAlpha));
        var auraRect = auraGo.GetComponent<RectTransform>();
        auraRect.anchorMin = auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot     = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(CARD_W + 48f, CARD_H + 48f);
        auraRect.anchoredPosition = new Vector2(xPos, 0f);
        auraGo.GetComponent<Image>().raycastTarget = false;
        _auraRects.Add(auraRect);

        var cg = auraGo.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        _cardGroups.Add(cg);

        // Hover halo
        var haloGo   = MakePanel(auraRect, "HoverHalo", Vector2.zero, Vector2.zero, Color.clear);
        var haloRect = haloGo.GetComponent<RectTransform>();
        haloRect.anchorMin = haloRect.anchorMax = new Vector2(0.5f, 0.5f);
        haloRect.pivot     = new Vector2(0.5f, 0.5f);
        haloRect.sizeDelta = new Vector2(CARD_W + 80f, CARD_H + 80f);
        haloRect.anchoredPosition = Vector2.zero;
        haloGo.GetComponent<Image>().raycastTarget = false;

        // Inner category glow
        var glow2Go   = MakePanel(auraRect, "CategoryGlow", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.glow, 0.14f));
        var glow2Rect = glow2Go.GetComponent<RectTransform>();
        glow2Rect.anchorMin = glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        glow2Rect.pivot     = new Vector2(0.5f, 0.5f);
        glow2Rect.sizeDelta = new Vector2(CARD_W + 20f, CARD_H + 20f);
        glow2Rect.anchoredPosition = Vector2.zero;
        glow2Go.GetComponent<Image>().raycastTarget = false;

        // Rim
        var rimGo   = MakePanel(glow2Rect, "CategoryRim", Vector2.zero, Vector2.zero,
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
        var bodyGo   = MakePanel(borderRect, "CardBody", Vector2.zero, Vector2.zero,
            new Color(theme.dark.r, theme.dark.g, theme.dark.b, 0.97f));
        var bodyRect = bodyGo.GetComponent<RectTransform>();
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot     = new Vector2(0.5f, 0.5f);
        bodyRect.sizeDelta = new Vector2(CARD_W, CARD_H);
        bodyRect.anchoredPosition = Vector2.zero;
        var bodyImg = bodyGo.GetComponent<Image>();
        bodyImg.raycastTarget = true;
        _cardRects.Add(bodyRect);

        var btn = bodyGo.AddComponent<Button>();
        btn.transition    = Selectable.Transition.None;
        btn.targetGraphic = bodyImg;
        int captured = index;
        btn.onClick.AddListener(() => TrySelect(captured));

        // Top band — 88h
        var bandGo   = MakePanel(bodyRect, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.55f));
        var bandRect = bandGo.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0f, 1f);
        bandRect.anchorMax = new Vector2(1f, 1f);
        bandRect.pivot     = new Vector2(0.5f, 1f);
        bandRect.sizeDelta = new Vector2(0f, 88f);
        bandRect.anchoredPosition = Vector2.zero;
        bandGo.GetComponent<Image>().raycastTarget = false;

        // Category icon
        var iconRect = MakeRegion(bodyRect, "IconArea", 38f, 54f, 0f);
        var iconT    = MakeText(iconRect, "Icon", GetCategoryIcon(relic.category),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            48, FontStyle.Normal, CardThemeLibrary.WithAlpha(theme.text, 0.72f));
        iconT.alignment = TextAnchor.MiddleCenter;

        // Category badge — 180×28 at y=-96
        var catGo   = MakePanel(bodyRect, "CatBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.20f));
        var catRect = catGo.GetComponent<RectTransform>();
        catRect.pivot     = new Vector2(0.5f, 1f);
        catRect.sizeDelta = new Vector2(180f, 28f);
        catRect.anchoredPosition = new Vector2(0f, -96f);
        catGo.GetComponent<Image>().raycastTarget = false;
        var catT = MakeText(catRect, "CatText", GetCategoryName(relic.category).ToUpper(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            13, FontStyle.Bold, CardThemeLibrary.WithAlpha(theme.text, 0.82f));
        ConfigText(catT, TextAnchor.MiddleCenter, 10, 13);

        // Corner marker or flare
        string cornerLabel = GetCornerLabel(relic);
        if (!string.IsNullOrEmpty(cornerLabel)) BuildCornerMarker(bodyRect, cornerLabel, theme);
        else                                     BuildCornerFlare(bodyRect, theme.main);

        // Title — top 132, height 54
        var titleRect = MakeRegion(bodyRect, "TitleArea", 132f, 54f, 20f);
        var titleT    = MakeText(titleRect, "RelicTitle", relic.title,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            24, FontStyle.Bold, theme.text);
        ConfigText(titleT, TextAnchor.MiddleCenter, 15, 24);

        // Divider at y=-196
        var divGo   = MakePanel(bodyRect, "Divider", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.glow, 0.40f));
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot     = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.5f);
        divRect.anchoredPosition = new Vector2(0f, -196f);
        divGo.GetComponent<Image>().raycastTarget = false;

        // Description — top 206, height 96
        var descRect = MakeRegion(bodyRect, "DescArea", 206f, 96f, 22f);
        var descT    = MakeText(descRect, "Desc", relic.description,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            15, FontStyle.Normal, new Color(theme.text.r, theme.text.g, theme.text.b, 0.75f));
        ConfigText(descT, TextAnchor.UpperCenter, 11, 15);

        // Effect preview strip at bottom
        var effGo   = MakePanel(bodyRect, "EffectBg", Vector2.zero, Vector2.zero,
            CardThemeLibrary.WithAlpha(theme.dark, 0.80f));
        var effRect = effGo.GetComponent<RectTransform>();
        effRect.anchorMin = new Vector2(0.07f, 0f);
        effRect.anchorMax = new Vector2(0.93f, 0f);
        effRect.pivot     = new Vector2(0.5f, 0f);
        effRect.sizeDelta = new Vector2(0f, 58f);
        effRect.anchoredPosition = new Vector2(0f, 12f);
        var effT = MakeText(effRect, "EffText", GetEffectPreview(relic),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            14, FontStyle.Bold, theme.glow);
        ConfigText(effT, TextAnchor.MiddleCenter, 10, 15);
        effGo.GetComponent<Image>().raycastTarget = false;

        // Index number
        var numT = MakeText(bodyRect, "IndexNum", $"[ {index + 1} ]",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 70f), new Vector2(0f, 20f),
            14, FontStyle.Normal, new Color(theme.text.r, theme.text.g, theme.text.b, 0.45f));
        ConfigText(numT, TextAnchor.MiddleCenter, 10, 14);

        // Hover prompt — on aura, bottom edge
        var promptGo   = MakePanel(auraRect, "HoverPrompt",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Color(0.06f, 0.04f, 0.12f, 0.92f));
        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.pivot     = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(94f, 26f);
        promptRect.anchoredPosition = new Vector2(0f, -2f);
        var promptGroup = promptGo.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f; promptGroup.interactable = false; promptGroup.blocksRaycasts = false;
        var promptT = MakeText(promptRect, "PromptText", "SEÇ",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            13, FontStyle.Bold, new Color(1f, 0.93f, 0.70f, 1f));
        promptT.alignment = TextAnchor.MiddleCenter;
        promptGo.GetComponent<Image>().raycastTarget = false;
        _promptGroups.Add(promptGroup);
    }

    // ── Reroll footer ─────────────────────────────────────────────────────────

    void BuildRerollFooter()
    {
        var svc       = BiomeRerollService.EnsureInstance();
        bool canReroll = svc.CanReroll();

        var footRoot = new GameObject("RerollFooter");
        footRoot.transform.SetParent(transform, false);
        var footRect = footRoot.AddComponent<RectTransform>();
        footRect.anchorMin = footRect.anchorMax = new Vector2(0.5f, 0f);
        footRect.pivot     = new Vector2(0.5f, 0f);
        footRect.sizeDelta = new Vector2(780f, 48f);
        footRect.anchoredPosition = new Vector2(0f, 90f);
        _rerollGroup = footRoot.AddComponent<CanvasGroup>();
        _rerollGroup.alpha = 0f;

        // Left: count label
        var countBg = MakePanel(footRect, "CountBg",
            new Vector2(0f, 0f), new Vector2(0.42f, 1f),
            new Color(0.04f, 0.03f, 0.08f, 0.80f));
        countBg.GetComponent<Image>().raycastTarget = false;
        _rerollCountText = MakeText(countBg.GetComponent<RectTransform>(), "RerollCount",
            RerollCountText(svc), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            17, FontStyle.Bold, new Color(0.70f, 0.65f, 0.88f, 1f));
        _rerollCountText.alignment = TextAnchor.MiddleCenter;
        _rerollCountText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _rerollCountText.verticalOverflow   = VerticalWrapMode.Overflow;
        _rerollCountText.raycastTarget = false;

        // Right: reroll button
        Color borderCol = canReroll
            ? new Color(0.90f, 0.68f, 0.15f, 0.75f)
            : new Color(0.30f, 0.25f, 0.40f, 0.60f);
        var btnBorder = MakePanel(footRect, "RerollBtnBorder",
            new Vector2(0.44f, 0f), new Vector2(1f, 1f), borderCol);
        _rerollBtnBorder = btnBorder.GetComponent<RectTransform>();
        btnBorder.GetComponent<Image>().raycastTarget = false;

        Color btnBgCol = canReroll
            ? new Color(0.28f, 0.18f, 0.06f, 0.92f)
            : new Color(0.12f, 0.10f, 0.16f, 0.88f);
        var btnBg = MakePanel(_rerollBtnBorder, "RerollBtnBg", Vector2.zero, Vector2.one, btnBgCol);
        _rerollBtnBg = btnBg.GetComponent<Image>();
        _rerollBtnBg.raycastTarget = false;
        btnBg.GetComponent<RectTransform>().offsetMin = new Vector2(1.5f, 1.5f);
        btnBg.GetComponent<RectTransform>().offsetMax = new Vector2(-1.5f, -1.5f);

        Color costCol = canReroll ? new Color(0.98f, 0.82f, 0.40f, 1f) : new Color(0.45f, 0.42f, 0.55f, 1f);
        string costStr = canReroll
            ? $"YENİLE  —  {svc.GetCurrentRerollCost()} Gold"
            : "YENİLE  (hak kalmadı)";
        _rerollCostText = MakeText(_rerollBtnBorder, "RerollCostText", costStr,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            18, FontStyle.Bold, costCol);
        _rerollCostText.alignment = TextAnchor.MiddleCenter;
        _rerollCostText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _rerollCostText.verticalOverflow   = VerticalWrapMode.Overflow;
        _rerollCostText.raycastTarget = false;
    }

    // ── Theme helpers ─────────────────────────────────────────────────────────

    struct RelicTheme { public Color main, dark, glow, text; }

    static RelicTheme GetRelicTheme(RelicCategory cat) => cat switch
    {
        RelicCategory.Survival   => MkTheme("2FBF71", "0C2E1D", "7DFFAE", "E8FFF0"),
        RelicCategory.Combat     => MkTheme("C43737", "2A0909", "FF7B7B", "FFEAEA"),
        RelicCategory.Economy    => MkTheme("D6A62E", "332304", "FFE07A", "FFF6DA"),
        RelicCategory.Portal     => MkTheme("2F7DFF", "0A1838", "86B8FF", "EEF5FF"),
        RelicCategory.Boss       => MkTheme("D6A62E", "332304", "FFE07A", "FFF6DA"),
        RelicCategory.RiskReward => MkTheme("9B2FB5", "250530", "C4A2FF", "F3ECFF"),
        RelicCategory.Utility    => MkTheme("8758E8", "1C1036", "C4A2FF", "F3ECFF"),
        _                        => MkTheme("FFFFFF", "000000", "FFFFFF", "FFFFFF")
    };

    static RelicTheme MkTheme(string m, string d, string g, string t) =>
        new RelicTheme { main = Hex(m), dark = Hex(d), glow = Hex(g), text = Hex(t) };

    static float GetGlowAlpha(RelicRarity r) => r switch
    {
        RelicRarity.Common    => 0.10f,
        RelicRarity.Rare      => 0.20f,
        RelicRarity.Epic      => 0.28f,
        RelicRarity.Legendary => 0.36f,
        RelicRarity.Cursed    => 0.30f,
        _                     => 0.08f
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
        _                        => "?"
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
        _                        => "Bilinmeyen"
    };

    static string GetCornerLabel(RelicDefinition relic)
    {
        if (relic.rarity == RelicRarity.Cursed)                            return "LANETLİ";
        if (relic.category == RelicCategory.RiskReward ||
            (relic.tags & RelicTag.RiskReward) != 0)                       return "RİSK";
        if (relic.category == RelicCategory.Boss)                          return "BOSS";
        if (relic.category == RelicCategory.Portal)                        return "PORTAL";
        return null;
    }

    static string GetEffectPreview(RelicDefinition relic)
    {
        if (string.IsNullOrEmpty(relic.description)) return string.Empty;
        int sep = relic.description.IndexOf(". ");
        return sep >= 0 ? relic.description.Substring(sep + 2) : relic.description;
    }

    static string RerollCountText(BiomeRerollService svc) =>
        $"Kalan Reroll: {svc.RerollsRemaining}/{BiomeRerollService.MaxRerolls}";

    // ── Input ─────────────────────────────────────────────────────────────────

    void Update()
    {
        if (InputBlocked) return;
        UpdateHover();
        if (TryMouseClick()) return;
        UpdateRerollHover();
        TryClickReroll();
        HandleKeyboard();
    }

    void UpdateHover()
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
            if (i < _promptGroups.Count && _promptGroups[i] != null)
                _promptGroups[i].alpha = Mathf.Lerp(_promptGroups[i].alpha, hovered ? 1f : 0f, Time.unscaledDeltaTime * 14f);
        }
    }

    bool TryMouseClick()
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

    void UpdateRerollHover()
    {
        if (_rerollBtnBorder == null || _rerollBtnBg == null) return;
        if (!BiomeRerollService.EnsureInstance().CanReroll()) return;
        bool h = Mouse.current != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(
                     _rerollBtnBorder, Mouse.current.position.ReadValue(), null);
        Color target = h ? new Color(0.40f, 0.26f, 0.09f, 0.96f) : new Color(0.28f, 0.18f, 0.06f, 0.92f);
        _rerollBtnBg.color = Color.Lerp(_rerollBtnBg.color, target, Time.unscaledDeltaTime * 12f);
    }

    void TryClickReroll()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_rerollBtnBorder == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(
                _rerollBtnBorder, Mouse.current.position.ReadValue(), null))
            StartCoroutine(DoReroll());
    }

    IEnumerator DoReroll()
    {
        var svc = BiomeRerollService.EnsureInstance();
        if (!svc.CanReroll()) yield break;
        int cost   = svc.GetCurrentRerollCost();
        var wallet = FindFirstObjectByType<GoldWallet>();
        if (wallet == null || wallet.RunGold < cost)
        {
            if (_rerollCostText != null)
                StartCoroutine(FlashText(_rerollCostText, new Color(0.9f, 0.2f, 0.2f, 1f), 0.6f));
            yield break;
        }
        wallet.SpendGold(cost);
        svc.TrySpendReroll();
        InputBlocked = true;
        _onRerollRequested?.Invoke();
    }

    void HandleKeyboard()
    {
        if (Keyboard.current == null) return;
        if      (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) TrySelect(0);
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
        if (_rerollGroup != null) StartCoroutine(Fade(_rerollGroup, 0f, 1f, 0.18f));
        InputBlocked = false;
    }

    IEnumerator AnimateCardIn(int index, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var cg       = _cardGroups[index];
        var auraRect = _auraRects[index];
        float dur = 0.36f, t = 0f;
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

    // ── Corner decorations ────────────────────────────────────────────────────

    void BuildCornerMarker(RectTransform parent, string label, RelicTheme theme)
    {
        var go = MakePanel(parent, "CornerMarker", new Vector2(1f, 1f), new Vector2(1f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.88f));
        var r = go.GetComponent<RectTransform>();
        r.pivot = new Vector2(1f, 1f);
        r.sizeDelta = new Vector2(76f, 24f);
        r.anchoredPosition = new Vector2(-12f, -12f);
        go.GetComponent<Image>().raycastTarget = false;

        var bgGo = MakePanel(r, "CornerBg", Vector2.zero, Vector2.one,
            CardThemeLibrary.WithAlpha(theme.dark, 0.94f));
        bgGo.GetComponent<RectTransform>().offsetMin = new Vector2(1.2f, 1.2f);
        bgGo.GetComponent<RectTransform>().offsetMax = new Vector2(-1.2f, -1.2f);
        bgGo.GetComponent<Image>().raycastTarget     = false;

        var t = MakeText(r, "CornerLabel", label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            11, FontStyle.Bold, theme.text);
        t.alignment = TextAnchor.MiddleCenter;
    }

    static void BuildCornerFlare(RectTransform parent, Color accent)
    {
        var go  = new GameObject("CornerFlare");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = CardThemeLibrary.WithAlpha(accent, 0.80f);
        img.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(1f, 1f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(22f, 22f);
        r.anchoredPosition = new Vector2(-18f, -18f);
        r.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    void BuildVignette(RectTransform parent)
    {
        Color c = new Color(0f, 0f, 0f, 0.65f);
        float s = 420f;
        Vig(parent, "VigL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(s, 0f), c);
        Vig(parent, "VigR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(s, 0f), c);
        Vig(parent, "VigU", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, s), c);
        Vig(parent, "VigD", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, s), c);
    }

    void Vig(RectTransform parent, string name, Vector2 amin, Vector2 amax, Vector2 size, Color c)
    {
        var go   = MakePanel(parent, name, amin, amax, c);
        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().raycastTarget = false;
    }

    void BuildFocusBackdrop(RectTransform parent)
    {
        var outer = MakePanel(parent, "Backdrop",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.055f, 0.028f, 0.110f, 0.44f));
        var or = outer.GetComponent<RectTransform>();
        or.pivot = new Vector2(0.5f, 0.5f);
        or.sizeDelta = new Vector2(1260f, 520f);
        or.anchoredPosition = new Vector2(0f, -28f);
        var inner = MakePanel(or, "BackdropInner", Vector2.zero, Vector2.one,
            new Color(0.006f, 0.004f, 0.018f, 0.58f));
        inner.GetComponent<RectTransform>().offsetMin = new Vector2(2f, 2f);
        inner.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);
        outer.GetComponent<Image>().raycastTarget = false;
        inner.GetComponent<Image>().raycastTarget = false;
    }

    void BuildTitleDivider(RectTransform parent)
    {
        var shadow = MakePanel(parent, "TitleLineShadow", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
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

    IEnumerator FlashText(Text t, Color flash, float dur)
    {
        if (t == null) yield break;
        Color orig = t.color; t.color = flash;
        yield return new WaitForSecondsRealtime(dur);
        if (t != null) t.color = orig;
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
