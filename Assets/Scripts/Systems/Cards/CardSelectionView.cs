using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// Full-screen card selection overlay. LevelUpCardSystem attaches this to its root GO.
public class CardSelectionView : MonoBehaviour
{
    public bool InputBlocked { get; set; } = true;

    List<CardDefinition> _cards;
    Action<int>          _onCardSelected;

    CanvasGroup _overlayGroup;
    CanvasGroup _titleGroup;
    CanvasGroup _hintGroup;
    CanvasGroup _rerollGroup;
    Text        _titleText;
    Text        _subtitleText;
    Text        _subtitleShadow;

    List<CardView>        _views     = new();
    List<RectTransform>   _cardRects = new();

    // Reroll UI refs
    RectTransform _rerollBtnBorder;
    Image         _rerollBtnBg;
    Text          _rerollCostText;
    Text          _rerollCountText;
    bool          _rerollHovered;

    int _playerLevel;

    const float CARD_W   = 272f;
    const float CARD_H   = 390f;
    const float CARD_GAP = 76f;
    const float ENTER_OFFSET = 260f;

    // ── Public setup ─────────────────────────────────────────────────────────

    public void Setup(List<CardDefinition> cards, string title, string subtitle,
        int playerLevel, Action<int> onCardSelected)
    {
        _cards          = cards;
        _onCardSelected = onCardSelected;
        _playerLevel    = playerLevel;
        InputBlocked    = true;

        BuildUI(title, subtitle);
        StartCoroutine(AnimateIn());
    }

    public IEnumerator DismissSelected(int selectedIndex)
    {
        // Flash selected, fade others
        if (selectedIndex < _views.Count)
            StartCoroutine(_views[selectedIndex].SelectPulse());

        for (int i = 0; i < _views.Count; i++)
        {
            var g = _views[i].GetComponent<CanvasGroup>() ?? _views[i].gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(Fade(g, 1f, 0f, i == selectedIndex ? 0.30f : 0.20f));
        }

        yield return new WaitForSecondsRealtime(0.32f);
    }

    public IEnumerator FadeOutOverlay()
    {
        yield return StartCoroutine(Fade(_overlayGroup, 1f, 0f, 0.30f));
    }

    public GameObject BuildFeedbackPanel(CardDefinition card, List<StatDelta> deltas)
    {
        bool hasDeltas = deltas != null && deltas.Count > 0;
        int lineCount  = hasDeltas ? deltas.Count : 1;

        const float W        = 420f;
        const float LINE_H   = 30f;
        const float HEADER_H = 86f;
        const float BOT_PAD  = 16f;
        float panelH = HEADER_H + lineCount * LINE_H + BOT_PAD;

        var container     = new GameObject("FeedbackPanel");
        container.transform.SetParent(transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot     = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(W, panelH);
        containerRect.anchoredPosition = new Vector2(0f, 30f);

        var cg = container.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;

        var visualTheme = CardThemeLibrary.GetVisualTheme(card.visualCategory);

        // Outer border
        FBPanel(container.transform, "FBBorder", visualTheme.main, visualTheme.main,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var border = container.transform.Find("FBBorder");

        // Dark bg inset
        var bgGo = FBPanel(border, "FBBg",
            CardThemeLibrary.WithAlpha(visualTheme.dark, 0.97f), CardThemeLibrary.WithAlpha(visualTheme.dark, 0.97f),
            Vector2.zero, Vector2.one, new Vector2(2.5f, 2.5f), new Vector2(-2.5f, -2.5f));

        // Accent inner
        FBPanel(bgGo.transform, "FBAccent",
            CardThemeLibrary.WithAlpha(visualTheme.glow, 0.28f), CardThemeLibrary.WithAlpha(visualTheme.glow, 0.28f),
            Vector2.zero, Vector2.one, new Vector2(1f, 1f), new Vector2(-1f, -1f));

        var bgRect = bgGo.GetComponent<RectTransform>();

        float y = 14f;
        FBText(bgRect, "FBLabel", "LÜTUF KAZANILDI", y, 22f, 15, FontStyle.Bold,
            CardThemeLibrary.TitleGold, TextAnchor.UpperCenter);
        y += 26f;

        FBText(bgRect, "FBTitle", card.title, y, 30f, 23, FontStyle.Bold,
            visualTheme.text, TextAnchor.UpperCenter);
        y += 34f;

        FBDivider(bgRect, "FBDiv", y, 1.5f, CardThemeLibrary.WithAlpha(visualTheme.glow, 0.55f), 36f);
        y += 12f;

        if (hasDeltas)
        {
            foreach (var d in deltas)
            {
                Color col = d.isPositive
                    ? new Color(0.50f, 1.00f, 0.55f, 1f)
                    : new Color(1.00f, 0.38f, 0.38f, 1f);
                FBText(bgRect, "FBDelta", $"{d.valueText}  {d.label}", y, 26f, 19, FontStyle.Bold, col, TextAnchor.UpperCenter);
                y += LINE_H;
            }
        }
        else
        {
            FBText(bgRect, "FBFallback", card.effectPreview, y, 26f, 17, FontStyle.Normal,
                new Color(0.88f, 0.84f, 1.00f, 1f), TextAnchor.UpperCenter);
        }

        return container;
    }

    // ── UI Build ─────────────────────────────────────────────────────────────

    void BuildUI(string title, string subtitle)
    {
        // Canvas is on the root GO (set up by LevelUpCardSystem)
        var rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();

        // EventSystem compat
        EnsureEventSystem();

        // Overlay dim
        var overlayGo = MakePanel(rootRect, "Overlay", Vector2.zero, Vector2.one, CardThemeLibrary.OverlayBg);
        _overlayGroup = overlayGo.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = true;

        var overlayRect = overlayGo.GetComponent<RectTransform>();
        BuildVignette(overlayRect);
        BuildCardFocusBackdrop(overlayRect);

        // Title root
        var titleRoot = new GameObject("TitleRoot");
        titleRoot.transform.SetParent(rootRect, false);
        var trRect = titleRoot.AddComponent<RectTransform>();
        trRect.anchorMin = trRect.anchorMax = new Vector2(0.5f, 1f);
        trRect.pivot      = new Vector2(0.5f, 1f);
        trRect.sizeDelta  = new Vector2(980f, 158f);
        trRect.anchoredPosition = new Vector2(0f, -46f);
        _titleGroup = titleRoot.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        _titleText = MakeText(trRect, "TitleText", title,
            Vector2.zero, Vector2.one, new Vector2(0f, -18f), Vector2.zero,
            58, FontStyle.Bold, CardThemeLibrary.TitleGold);
        _titleText.alignment = TextAnchor.UpperCenter;

        _subtitleShadow = MakeText(trRect, "SubShadow", subtitle,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 31f), new Vector2(0f, 40f),
            27, FontStyle.BoldAndItalic, new Color(0f, 0f, 0f, 0.88f));
        _subtitleShadow.alignment = TextAnchor.LowerCenter;
        _subtitleShadow.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleShadow.verticalOverflow = VerticalWrapMode.Overflow;

        _subtitleText = MakeText(trRect, "SubText", subtitle,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 33f), new Vector2(0f, 40f),
            27, FontStyle.BoldAndItalic, CardThemeLibrary.SubtitleTint);
        _subtitleText.alignment = TextAnchor.LowerCenter;
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        BuildTitleDivider(trRect);

        // Card area
        var cardArea = new GameObject("CardArea");
        cardArea.transform.SetParent(transform, false);
        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot     = new Vector2(0.5f, 0.5f);
        float totalW = _cards.Count * CARD_W + (_cards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -24f);

        _views.Clear();
        _cardRects.Clear();

        for (int i = 0; i < _cards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            var view = BuildCardWidget(cardAreaRect, _cards[i], xPos, i);
            _views.Add(view);
        }

        // Input hint
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
        hintBg.GetComponent<Image>().raycastTarget = false;

        BuildRerollFooter();
    }

    CardView BuildCardWidget(RectTransform parent, CardDefinition card, float xPos, int index)
    {
        var cls = CardThemeLibrary.GetClassTheme(card.cardClass);
        var visual = CardThemeLibrary.GetVisualTheme(card.visualCategory);
        var rar = CardThemeLibrary.GetRarityTheme(card.rarity);
        float glowAlpha = Mathf.Max(0.16f, rar.glowAlpha);

        // Outer rarity aura glow
        var auraGo   = MakePanel(parent, $"Aura_{index}", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.glow, glowAlpha));
        var auraRect = auraGo.GetComponent<RectTransform>();
        auraRect.anchorMin = auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot     = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(CARD_W + 48f, CARD_H + 48f);
        auraRect.anchoredPosition = new Vector2(xPos, 0f);
        var auraGroup = auraGo.GetComponent<CanvasGroup>() ?? auraGo.AddComponent<CanvasGroup>();
        auraGroup.interactable = false; auraGroup.blocksRaycasts = false;

        // Hover halo
        var haloGo   = MakePanel(auraRect, "HoverHalo", Vector2.zero, Vector2.zero, Color.clear);
        var haloRect = haloGo.GetComponent<RectTransform>();
        haloRect.anchorMin = haloRect.anchorMax = new Vector2(0.5f, 0.5f);
        haloRect.pivot     = new Vector2(0.5f, 0.5f);
        haloRect.sizeDelta = new Vector2(CARD_W + 80f, CARD_H + 80f);
        haloRect.anchoredPosition = Vector2.zero;

        // Class glow (inner)
        var glow2Go   = MakePanel(auraRect, "CategoryGlow", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.glow, 0.14f));
        var glow2Rect = glow2Go.GetComponent<RectTransform>();
        glow2Rect.anchorMin = glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        glow2Rect.pivot     = new Vector2(0.5f, 0.5f);
        glow2Rect.sizeDelta = new Vector2(CARD_W + 20f, CARD_H + 20f);
        glow2Rect.anchoredPosition = Vector2.zero;
        var glow2Group = glow2Go.AddComponent<CanvasGroup>();
        glow2Group.interactable = false; glow2Group.blocksRaycasts = false;

        // Rarity rim
        var rimGo   = MakePanel(glow2Rect, "CategoryRim", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.main, glowAlpha * 1.8f));
        var rimRect = rimGo.GetComponent<RectTransform>();
        rimRect.anchorMin = rimRect.anchorMax = new Vector2(0.5f, 0.5f);
        rimRect.pivot     = new Vector2(0.5f, 0.5f);
        rimRect.sizeDelta = new Vector2(CARD_W + 8f, CARD_H + 8f);
        rimRect.anchoredPosition = Vector2.zero;

        // Thin rarity border
        var borderGo   = MakePanel(rimRect, "Border", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.main, 0.85f));
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot     = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(CARD_W + 3f, CARD_H + 3f);
        borderRect.anchoredPosition = Vector2.zero;

        // Card body: class dark
        var bodyGo   = MakePanel(borderRect, "CardBody", Vector2.zero, Vector2.zero, new Color(visual.dark.r, visual.dark.g, visual.dark.b, 0.97f));
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
        btn.onClick.RemoveAllListeners();
        int capturedIndex = index;
        btn.onClick.AddListener(() => TrySelectCard(capturedIndex));

        // Top band (class main color)
        var bandGo   = MakePanel(bodyRect, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), CardThemeLibrary.WithAlpha(visual.main, 0.55f));
        var bandRect = bandGo.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0f, 1f);
        bandRect.anchorMax = new Vector2(1f, 1f);
        bandRect.pivot     = new Vector2(0.5f, 1f);
        bandRect.sizeDelta = new Vector2(0f, 88f);
        bandRect.anchoredPosition = Vector2.zero;

        // Class icon
        var iconRect = MakeRegion(bodyRect, "IconArea", 38f, 54f, 0f);
        var iconT = MakeText(iconRect, "Icon", CardThemeLibrary.GetClassIcon(card.cardClass),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 48, FontStyle.Normal, CardThemeLibrary.WithAlpha(cls.text, 0.72f));
        iconT.alignment = TextAnchor.MiddleCenter;

        // Rarity badge — top right corner
        BuildCategoryMarker(bodyRect, card.visualCategory, visual);

        // Class name badge below icon
        var classLabel = MakePanel(bodyRect, "ClassBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), CardThemeLibrary.WithAlpha(cls.main, 0.16f));
        var clsRect    = classLabel.GetComponent<RectTransform>();
        clsRect.pivot  = new Vector2(0.5f, 1f);
        clsRect.sizeDelta = new Vector2(180f, 28f);
        clsRect.anchoredPosition = new Vector2(0f, -96f);
        var clsText = MakeText(clsRect, "ClassName", CardThemeLibrary.GetClassDisplayName(card.cardClass).ToUpper(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            13, FontStyle.Bold, CardThemeLibrary.WithAlpha(cls.text, 0.82f));
        ConfigText(clsText, TextAnchor.MiddleCenter, 10, 13);

        // Title
        var titleRect = MakeRegion(bodyRect, "TitleArea", 132f, 54f, 20f);
        var titleT    = MakeText(titleRect, "CardTitle", card.title, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            24, FontStyle.Bold, visual.text);
        ConfigText(titleT, TextAnchor.MiddleCenter, 15, 24);

        // Divider
        var divGo   = MakePanel(bodyRect, "Divider", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.glow, 0.40f));
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot     = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.5f);
        divRect.anchoredPosition = new Vector2(0f, -196f);

        // Description
        var descRect = MakeRegion(bodyRect, "DescArea", 206f, 96f, 22f);
        var descT    = MakeText(descRect, "Desc", card.description, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            15, FontStyle.Normal, new Color(visual.text.r, visual.text.g, visual.text.b, 0.75f));
        ConfigText(descT, TextAnchor.UpperCenter, 11, 15);

        // Effect preview strip
        var effGo   = MakePanel(bodyRect, "EffectBg", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(visual.dark, 0.80f));
        var effRect = effGo.GetComponent<RectTransform>();
        effRect.anchorMin = new Vector2(0.07f, 0f);
        effRect.anchorMax = new Vector2(0.93f, 0f);
        effRect.pivot     = new Vector2(0.5f, 0f);
        effRect.sizeDelta = new Vector2(0f, 58f);
        effRect.anchoredPosition = new Vector2(0f, 12f);
        var effT = MakeText(effRect, "EffText", card.effectPreview,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            14, FontStyle.Bold, visual.glow);
        ConfigText(effT, TextAnchor.MiddleCenter, 10, 15);

        // Index number
        var numT = MakeText(bodyRect, "IndexNum", $"[ {index + 1} ]",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 70f), new Vector2(0f, 20f),
            14, FontStyle.Normal, new Color(visual.text.r, visual.text.g, visual.text.b, 0.45f));
        ConfigText(numT, TextAnchor.MiddleCenter, 10, 14);

        // Hover prompt
        var promptGo   = MakePanel(auraRect, "HoverPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Color(0.06f, 0.04f, 0.12f, 0.92f));
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

        // Raycast targets cleanup
        auraGo.GetComponent<Image>().raycastTarget  = false;
        haloGo.GetComponent<Image>().raycastTarget  = false;
        glow2Go.GetComponent<Image>().raycastTarget = false;
        rimGo.GetComponent<Image>().raycastTarget   = false;
        borderGo.GetComponent<Image>().raycastTarget = false;
        bandGo.GetComponent<Image>().raycastTarget  = false;
        classLabel.GetComponent<Image>().raycastTarget = false;
        divGo.GetComponent<Image>().raycastTarget   = false;
        effGo.GetComponent<Image>().raycastTarget   = false;
        promptGo.GetComponent<Image>().raycastTarget = false;

        // Attach CardView
        var view = auraGo.AddComponent<CardView>();
        view.Init(index, card, TrySelectCard);
        view.SetVisualRefs(
            auraGo.GetComponent<Image>(),
            rimGo.GetComponent<Image>(),
            borderGo.GetComponent<Image>(),
            bodyGo.GetComponent<Image>(),
            glow2Go.GetComponent<Image>(),
            haloGo.GetComponent<Image>(),
            promptGroup
        );

        return view;
    }

    // ── Reroll footer ─────────────────────────────────────────────────────────

    void BuildRerollFooter()
    {
        var svc = BiomeRerollService.EnsureInstance();

        var footRoot = new GameObject("RerollFooter");
        footRoot.transform.SetParent(transform, false);
        var footRect = footRoot.AddComponent<RectTransform>();
        footRect.anchorMin = footRect.anchorMax = new Vector2(0.5f, 0f);
        footRect.pivot     = new Vector2(0.5f, 0f);
        footRect.sizeDelta = new Vector2(780f, 48f);
        footRect.anchoredPosition = new Vector2(0f, 90f);
        _rerollGroup = footRoot.AddComponent<CanvasGroup>();
        _rerollGroup.alpha = 0f;

        // Left: count
        var countBg = MakePanel(footRect, "CountBg", new Vector2(0f, 0f), new Vector2(0.42f, 1f),
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
        bool canReroll = svc.CanReroll();
        Color borderCol = canReroll ? new Color(0.90f, 0.68f, 0.15f, 0.75f) : new Color(0.30f, 0.25f, 0.40f, 0.60f);

        var btnBorder = MakePanel(footRect, "RerollBtnBorder", new Vector2(0.44f, 0f), new Vector2(1f, 1f), borderCol);
        _rerollBtnBorder = btnBorder.GetComponent<RectTransform>();
        btnBorder.GetComponent<Image>().raycastTarget = false;

        Color btnBgCol = canReroll ? new Color(0.28f, 0.18f, 0.06f, 0.92f) : new Color(0.12f, 0.10f, 0.16f, 0.88f);
        var btnBg = MakePanel(_rerollBtnBorder, "RerollBtnBg", Vector2.zero, Vector2.one, btnBgCol);
        _rerollBtnBg = btnBg.GetComponent<Image>();
        _rerollBtnBg.raycastTarget = false;
        btnBg.GetComponent<RectTransform>().offsetMin = new Vector2(1.5f, 1.5f);
        btnBg.GetComponent<RectTransform>().offsetMax = new Vector2(-1.5f, -1.5f);

        Color costCol = canReroll ? new Color(0.98f, 0.82f, 0.40f, 1f) : new Color(0.45f, 0.42f, 0.55f, 1f);
        string costStr = canReroll ? $"YENİLE  —  {svc.GetCurrentRerollCost()} Gold" : "YENİLE  (hak kalmadı)";
        _rerollCostText = MakeText(_rerollBtnBorder, "RerollCostText", costStr,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            18, FontStyle.Bold, costCol);
        _rerollCostText.alignment = TextAnchor.MiddleCenter;
        _rerollCostText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _rerollCostText.verticalOverflow   = VerticalWrapMode.Overflow;
        _rerollCostText.raycastTarget = false;
    }

    // ── Update / input ────────────────────────────────────────────────────────

    void Update()
    {
        if (InputBlocked) return;

        UpdateHoverFromMouse();
        if (TryClickFromMouse()) return;
        UpdateRerollHover();
        TryClickReroll();
        HandleKeyboard();
    }

    void UpdateHoverFromMouse()
    {
        int hovered = -1;
        if (Mouse.current != null)
        {
            Vector2 mp = Mouse.current.position.ReadValue();
            for (int i = 0; i < _cardRects.Count; i++)
            {
                if (_cardRects[i] == null) continue;
                if (i < _views.Count && _views[i] != null && !_views[i].Ready) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(_cardRects[i], mp, null))
                    hovered = i;
            }
        }
        for (int i = 0; i < _views.Count; i++)
            _views[i]?.SetHovered(i == hovered);
    }

    bool TryClickFromMouse()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return false;
        Vector2 mp = Mouse.current.position.ReadValue();
        for (int i = 0; i < _cardRects.Count; i++)
        {
            if (_cardRects[i] == null) continue;
            if (i < _views.Count && _views[i] != null && !_views[i].Ready) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(_cardRects[i], mp, null))
            {
                TrySelectCard(i);
                return true;
            }
        }
        return false;
    }

    void HandleKeyboard()
    {
        if (Keyboard.current == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) TrySelectCard(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) TrySelectCard(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) TrySelectCard(2);
    }

    void TrySelectCard(int index)
    {
        if (InputBlocked) return;
        _onCardSelected?.Invoke(index);
    }

    void UpdateRerollHover()
    {
        if (_rerollBtnBorder == null || _rerollBtnBg == null) return;
        var svc = BiomeRerollService.EnsureInstance();
        if (!svc.CanReroll()) return;
        bool h = Mouse.current != null &&
                 RectTransformUtility.RectangleContainsScreenPoint(_rerollBtnBorder, Mouse.current.position.ReadValue(), null);
        _rerollHovered = h;
        Color target = h ? new Color(0.40f, 0.26f, 0.09f, 0.96f) : new Color(0.28f, 0.18f, 0.06f, 0.92f);
        _rerollBtnBg.color = Color.Lerp(_rerollBtnBg.color, target, Time.unscaledDeltaTime * 12f);
    }

    void TryClickReroll()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
        if (_rerollBtnBorder == null) return;
        if (RectTransformUtility.RectangleContainsScreenPoint(_rerollBtnBorder, Mouse.current.position.ReadValue(), null))
            StartCoroutine(DoReroll());
    }

    IEnumerator DoReroll()
    {
        var svc = BiomeRerollService.EnsureInstance();
        if (!svc.CanReroll()) yield break;
        int cost = svc.GetCurrentRerollCost();
        var wallet = FindFirstObjectByType<GoldWallet>();
        if (wallet == null || wallet.RunGold < cost)
        {
            if (_rerollCostText != null) StartCoroutine(FlashText(_rerollCostText, new Color(0.9f, 0.2f, 0.2f, 1f), 0.6f));
            yield break;
        }
        wallet.SpendGold(cost);
        svc.TrySpendReroll();
        StartCoroutine(FlashRerollBtn());
        InputBlocked = true;

        for (int i = 0; i < _views.Count; i++)
        {
            var g = _views[i].GetComponent<CanvasGroup>() ?? _views[i].gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(Fade(g, 1f, 0f, 0.15f));
        }
        yield return new WaitForSecondsRealtime(0.20f);

        _cards = CardOfferGenerator.Generate(3, _playerLevel);
        RebuildCards();
        RefreshRerollUI(svc);

        for (int i = 0; i < _views.Count; i++)
            StartCoroutine(AnimateCardIn(_views[i], 0.08f * i));
        yield return new WaitForSecondsRealtime(0.08f * (_views.Count - 1) + 0.30f);
        InputBlocked = false;
    }

    void RebuildCards()
    {
        // Find card area
        var cardArea = transform.Find("CardArea");
        if (cardArea == null) return;
        for (int i = cardArea.childCount - 1; i >= 0; i--)
            Destroy(cardArea.GetChild(i).gameObject);
        _views.Clear();
        _cardRects.Clear();

        var cardAreaRect = cardArea.GetComponent<RectTransform>();
        float totalW = _cards.Count * CARD_W + (_cards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);

        for (int i = 0; i < _cards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            _views.Add(BuildCardWidget(cardAreaRect, _cards[i], xPos, i));
        }
    }

    void RefreshRerollUI(BiomeRerollService svc)
    {
        bool can = svc.CanReroll();
        if (_rerollCountText != null) _rerollCountText.text = RerollCountText(svc);
        if (_rerollCostText  != null)
        {
            _rerollCostText.text  = can ? $"YENİLE  —  {svc.GetCurrentRerollCost()} Gold" : "YENİLE  (hak kalmadı)";
            _rerollCostText.color = can ? new Color(0.98f, 0.82f, 0.40f, 1f) : new Color(0.45f, 0.42f, 0.55f, 1f);
        }
        if (_rerollBtnBorder != null)
        {
            var img = _rerollBtnBorder.GetComponent<Image>();
            if (img) img.color = can ? new Color(0.90f, 0.68f, 0.15f, 0.75f) : new Color(0.30f, 0.25f, 0.40f, 0.60f);
        }
        if (_rerollBtnBg != null)
            _rerollBtnBg.color = can ? new Color(0.28f, 0.18f, 0.06f, 0.92f) : new Color(0.12f, 0.10f, 0.16f, 0.88f);
    }

    static string RerollCountText(BiomeRerollService svc) =>
        $"Kalan Reroll: {svc.RerollsRemaining}/{BiomeRerollService.MaxRerolls}";

    // ── Animations ────────────────────────────────────────────────────────────

    IEnumerator AnimateIn()
    {
        yield return StartCoroutine(Fade(_overlayGroup, 0f, 1f, 0.28f));
        StartCoroutine(Fade(_titleGroup, 0f, 1f, 0.25f));

        for (int i = 0; i < _views.Count; i++)
            StartCoroutine(AnimateCardIn(_views[i], 0.10f * i));

        yield return new WaitForSecondsRealtime(0.10f * (_views.Count - 1) + 0.35f);
        yield return StartCoroutine(Fade(_hintGroup, 0f, 1f, 0.20f));
        if (_rerollGroup != null) StartCoroutine(Fade(_rerollGroup, 0f, 1f, 0.20f));
        InputBlocked = false;
    }

    IEnumerator AnimateCardIn(CardView w, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var rect = w.RootRect;
        float dur = 0.38f, t = 0f;
        Vector2 startPos  = rect.anchoredPosition + Vector2.down * ENTER_OFFSET;
        Vector2 endPos    = rect.anchoredPosition;
        var group = w.GetComponent<CanvasGroup>() ?? w.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float te = BounceEaseOut(Mathf.Clamp01(t / dur));
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, te);
            rect.localScale       = Vector3.Lerp(Vector3.one * 0.80f, Vector3.one, te);
            group.alpha           = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / dur * 3f));
            yield return null;
        }
        rect.anchoredPosition = endPos;
        rect.localScale       = Vector3.one;
        group.alpha           = 1f;
        w.Ready               = true;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    void BuildVignette(RectTransform parent)
    {
        float s = 420f;
        Color c = new Color(0f, 0f, 0f, 0.68f);
        StretchPanel(parent, "VigL", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(s, 0f));
        StretchPanel(parent, "VigR", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(s, 0f));
        StretchPanel(parent, "VigU", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, s));
        StretchPanel(parent, "VigD", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, s));
        foreach (Transform ch in parent)
            if (ch.name.StartsWith("Vig")) { var img = ch.GetComponent<Image>(); if (img) { img.color = c; img.raycastTarget = false; } }
    }

    void StretchPanel(RectTransform parent, string name, Vector2 amin, Vector2 amax, Vector2 sz)
    {
        var go   = MakePanel(parent, name, amin, amax, Color.clear);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = amin; rect.anchorMax = amax;
        rect.sizeDelta = sz; rect.anchoredPosition = Vector2.zero;
    }

    void BuildCardFocusBackdrop(RectTransform parent)
    {
        var outer = MakePanel(parent, "Backdrop", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Color(0.055f, 0.028f, 0.110f, 0.44f));
        var or = outer.GetComponent<RectTransform>();
        or.pivot = new Vector2(0.5f, 0.5f);
        or.sizeDelta = new Vector2(1260f, 520f);
        or.anchoredPosition = new Vector2(0f, -28f);
        var inner = MakePanel(or, "BackdropInner", Vector2.zero, Vector2.one, new Color(0.006f, 0.004f, 0.018f, 0.58f));
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

    static void BuildCategoryMarker(RectTransform parent, CardVisualCategory category, CardVisualTheme theme)
    {
        string label = CardThemeLibrary.GetVisualCornerLabel(category);
        if (string.IsNullOrEmpty(label))
        {
            BuildCornerFlare(parent, theme.main, theme.glow);
            return;
        }

        var markerGo = MakePanel(parent, "CategoryMarker", new Vector2(1f, 1f), new Vector2(1f, 1f),
            CardThemeLibrary.WithAlpha(theme.main, 0.88f));
        var markerRect = markerGo.GetComponent<RectTransform>();
        markerRect.pivot = new Vector2(1f, 1f);
        markerRect.sizeDelta = new Vector2(76f, 24f);
        markerRect.anchoredPosition = new Vector2(-12f, -12f);
        markerGo.GetComponent<Image>().raycastTarget = false;

        var markerBg = MakePanel(markerRect, "CategoryMarkerBg", Vector2.zero, Vector2.one,
            CardThemeLibrary.WithAlpha(theme.dark, 0.94f));
        var markerBgRect = markerBg.GetComponent<RectTransform>();
        markerBgRect.offsetMin = new Vector2(1.2f, 1.2f);
        markerBgRect.offsetMax = new Vector2(-1.2f, -1.2f);
        markerBg.GetComponent<Image>().raycastTarget = false;

        var labelText = MakeText(markerBgRect, "CategoryMarkerText", label,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            11, FontStyle.Bold, theme.text);
        labelText.alignment = TextAnchor.MiddleCenter;
    }

    static void BuildCornerFlare(RectTransform parent, Color rarAccent, Color clsGlow)
    {
        var flareGo = new GameObject("CornerFlare");
        flareGo.transform.SetParent(parent, false);
        var flareImg = flareGo.AddComponent<Image>();
        flareImg.color = CardThemeLibrary.WithAlpha(rarAccent, 0.80f);
        flareImg.raycastTarget = false;
        var flareRect = flareGo.GetComponent<RectTransform>();
        flareRect.anchorMin = flareRect.anchorMax = new Vector2(1f, 1f);
        flareRect.pivot     = new Vector2(0.5f, 0.5f);
        flareRect.sizeDelta = new Vector2(22f, 22f);
        flareRect.anchoredPosition = new Vector2(-18f, -18f);
        flareRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    // ── Feedback helpers ──────────────────────────────────────────────────────

    static GameObject FBPanel(Transform parent, string name, Color c1, Color c2, Vector2 amin, Vector2 amax, Vector2 offMin, Vector2 offMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = c1; img.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax;
        r.offsetMin = offMin; r.offsetMax = offMax;
        return go;
    }

    static void FBText(RectTransform parent, string name, string content, float topY, float height, int fontSize, FontStyle style, Color color, TextAnchor align)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content; txt.fontSize = fontSize; txt.fontStyle = style;
        txt.color = color; txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(-24f, height);
        r.anchoredPosition = new Vector2(0f, -topY);
    }

    static void FBDivider(RectTransform parent, string name, float topY, float height, Color color, float inset)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color; img.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0f, 1f); r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.sizeDelta = new Vector2(-inset * 2f, height);
        r.anchoredPosition = new Vector2(0f, -topY);
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

    IEnumerator FlashRerollBtn()
    {
        if (_rerollBtnBorder == null) yield break;
        var img = _rerollBtnBorder.GetComponent<Image>();
        if (img == null) yield break;
        Color orig = img.color, flash = new Color(0.45f, 0.92f, 1.00f, 0.95f);
        float half = 0.10f, t = 0f;
        while (t < half) { t += Time.unscaledDeltaTime; if (img) img.color = Color.Lerp(orig, flash, t / half); yield return null; }
        t = 0f;
        while (t < half) { t += Time.unscaledDeltaTime; if (img) img.color = Color.Lerp(flash, orig, t / half); yield return null; }
        if (img) img.color = orig;
    }

    static float BounceEaseOut(float t)
    {
        if (t < 0.727f) return 7.5625f * t * t;
        if (t < 0.909f) { t -= 0.818f; return 7.5625f * t * t + 0.75f; }
        if (t < 0.977f) { t -= 0.954f; return 7.5625f * t * t + 0.9375f; }
        t -= 0.988f; return 7.5625f * t * t + 0.984375f;
    }

    // ── Low-level UI factories ────────────────────────────────────────────────

    static void EnsureEventSystem()
    {
        var es = FindFirstObjectByType<EventSystem>();
        if (es == null) { var go = new GameObject("EventSystem"); es = go.AddComponent<EventSystem>(); }
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null) Destroy(standalone);
        if (es.GetComponent<InputSystemUIInputModule>() == null)
            es.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    static GameObject MakePanel(RectTransform parent, string name, Vector2 amin, Vector2 amax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.offsetMin = r.offsetMax = Vector2.zero;
        return go;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        Vector2 amin, Vector2 amax, Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, FontStyle style, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content; txt.fontSize = fontSize; txt.fontStyle = style;
        txt.color = color;
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
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(-hInset * 2f, height);
        rect.anchoredPosition = new Vector2(0f, -topOffset);
        return rect;
    }

    static void ConfigText(Text t, TextAnchor align, int minSz, int maxSz)
    {
        if (t == null) return;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Truncate;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize    = minSz;
        t.resizeTextMaxSize    = maxSz;
    }
}
