using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

// ─────────────────────────────────────────────────────────────────────────────
// LevelUpCardSystem
//
//   Hollow Atlas kart seçim ekranı.
//   PlayerLevelSystem level-up tetikleyince veya BossRewardSystem özel ödül çağırınca
//   bu sistem devreye girer.
// ─────────────────────────────────────────────────────────────────────────────

public class LevelUpCardSystem : MonoBehaviour
{
    public static LevelUpCardSystem Instance { get; private set; }

    public bool SelectionPending { get; private set; }

    const float CARD_W = 272f;
    const float CARD_H = 390f;
    const float CARD_GAP = 32f;
    const float ENTER_OFFSET = 260f;

    public const float HOVER_SCALE = 1.055f;
    public const float HOVER_LIFT = 10f;

    GameObject _root;
    CanvasGroup _overlayGroup;
    CanvasGroup _titleGroup;
    Text _titleText;
    Text _subtitleText;
    Text _subtitleShadowText;
    Text _hintText;
    CanvasGroup _hintGroup;

    List<CardWidget> _widgets = new();
    List<LevelUpCard> _currentCards;
    private readonly List<RectTransform> _cardRects = new();

    GameObject _player;
    bool _inputBlocked;
    string _overrideTitle;
    string _overrideSubtitle;
    Action _onSelectionComplete;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TriggerSelection(int playerLevel)
    {
        if (SelectionPending)
            return;

        _player = GameObject.FindGameObjectWithTag("Player");
        _currentCards = CardPool.PickRandom(3, playerLevel);
        _overrideTitle = null;
        _overrideSubtitle = null;
        _onSelectionComplete = null;

        if (_currentCards.Count == 0)
            return;

        SelectionPending = true;
        Time.timeScale = 0f;

        BuildUI();
        StartCoroutine(AnimateIn(playerLevel));
    }

    public bool ShowCustomCards(string title, string subtitle, List<LevelUpCard> cards, Action onComplete = null)
    {
        if (SelectionPending)
            return false;

        if (cards == null || cards.Count == 0)
            return false;

        _player = GameObject.FindGameObjectWithTag("Player");
        _currentCards = cards;
        _overrideTitle = title;
        _overrideSubtitle = subtitle;
        _onSelectionComplete = onComplete;

        SelectionPending = true;
        Time.timeScale = 0f;

        BuildUI();
        StartCoroutine(AnimateIn(0));
        return true;
    }

    void BuildUI()
    {
        if (_root != null)
            Destroy(_root);

        _widgets.Clear();
        _cardRects.Clear();

        _root = new GameObject("LevelUpCardRoot");
        DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        var eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventSystem == null)
        {
            var es = new GameObject("EventSystem");
            eventSystem = es.AddComponent<EventSystem>();
        }

        var standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            Destroy(standalone);

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        var rootRect = _root.GetComponent<RectTransform>();

        var overlay = MakePanel(
            rootRect,
            "Overlay",
            Vector2.zero,
            Vector2.one,
            new Color(0.020f, 0.016f, 0.055f, 0f),
            new Vector2(0.5f, 0.5f)
        );

        _overlayGroup = overlay.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = true;

        BuildVignette(overlay.GetComponent<RectTransform>());

        var titleRoot = MakePanel(
            overlay.GetComponent<RectTransform>(),
            "TitleRoot",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            Color.clear
        );

        var titleRootRect = titleRoot.GetComponent<RectTransform>();
        titleRootRect.anchorMin = titleRootRect.anchorMax = new Vector2(0.5f, 1f);
        titleRootRect.pivot = new Vector2(0.5f, 1f);
        titleRootRect.sizeDelta = new Vector2(940f, 170f);
        titleRootRect.anchoredPosition = new Vector2(0f, -52f);

        _titleGroup = titleRoot.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        _titleText = MakeText(
            titleRootRect,
            "TitleText",
            "SEVİYE ATLA",
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, -18f),
            new Vector2(0f, 0f),
            60,
            FontStyle.Bold,
            new Color(0.988f, 0.816f, 0.000f, 1f)
        );
        _titleText.alignment = TextAnchor.UpperCenter;

        _subtitleShadowText = MakeText(
            titleRootRect,
            "SubTextShadow",
            "Bir lütuf seç",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(2f, 35f),
            new Vector2(0f, 42f),
            30,
            FontStyle.BoldAndItalic,
            new Color(0f, 0f, 0f, 0.88f)
        );
        _subtitleShadowText.alignment = TextAnchor.LowerCenter;
        _subtitleShadowText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleShadowText.verticalOverflow = VerticalWrapMode.Overflow;

        _subtitleText = MakeText(
            titleRootRect,
            "SubText",
            "Bir lütuf seç",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 37f),
            new Vector2(0f, 42f),
            30,
            FontStyle.BoldAndItalic,
            new Color(0.94f, 0.90f, 1.00f, 1f)
        );
        _subtitleText.alignment = TextAnchor.LowerCenter;
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleText.verticalOverflow = VerticalWrapMode.Overflow;

        BuildTitleDivider(titleRootRect);

        var cardArea = new GameObject("CardArea");
        cardArea.transform.SetParent(_root.transform, false);

        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot = new Vector2(0.5f, 0.5f);

        float totalW = _currentCards.Count * CARD_W + (_currentCards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -20f);

        for (int i = 0; i < _currentCards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            var widget = BuildCard(cardAreaRect, _currentCards[i], xPos, i);
            _widgets.Add(widget);
        }

        var hintRoot = new GameObject("HintRoot");
        hintRoot.transform.SetParent(_root.transform, false);

        var hintRect = hintRoot.AddComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(720f, 44f);
        hintRect.anchoredPosition = new Vector2(0f, 38f);

        _hintGroup = hintRoot.AddComponent<CanvasGroup>();
        _hintGroup.alpha = 0f;

        _hintText = MakeText(
            hintRect,
            "HintText",
            "[ 1 ]  [ 2 ]  [ 3 ]  ile seç  —  veya karta tıkla",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            20,
            FontStyle.Normal,
            new Color(0.62f, 0.62f, 0.88f, 1f)
        );
        _hintText.alignment = TextAnchor.MiddleCenter;
    }

    CardWidget BuildCard(RectTransform parent, LevelUpCard card, float xPos, int index)
    {
        Color godCol = CardPool.GodColor(card.god);
        Color rarityCol = CardPool.RarityColor(card.rarity);

        var glowGo = MakePanel(
            parent,
            $"Glow_{index}",
            Vector2.zero,
            Vector2.zero,
            new Color(godCol.r, godCol.g, godCol.b, 0.18f)
        );

        var glowRect = glowGo.GetComponent<RectTransform>();
        var glowGroup = EnsureCanvasGroup(glowGo);

        if (glowGroup != null)
        {
            glowGroup.alpha = 1f;
            glowGroup.interactable = false;
            glowGroup.blocksRaycasts = false;
        }

        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(CARD_W + 28f, CARD_H + 28f);
        glowRect.anchoredPosition = new Vector2(xPos, 0f);

        var glow2Go = MakePanel(
            glowRect,
            "Glow2",
            Vector2.zero,
            Vector2.zero,
            new Color(godCol.r, godCol.g, godCol.b, 0.30f)
        );

        var glow2Rect = glow2Go.GetComponent<RectTransform>();
        var glow2Group = EnsureCanvasGroup(glow2Go);

        if (glow2Group != null)
        {
            glow2Group.alpha = 1f;
            glow2Group.interactable = false;
            glow2Group.blocksRaycasts = false;
        }

        glow2Rect.anchorMin = glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        glow2Rect.pivot = new Vector2(0.5f, 0.5f);
        glow2Rect.sizeDelta = new Vector2(CARD_W + 12f, CARD_H + 12f);
        glow2Rect.anchoredPosition = Vector2.zero;

        var borderGo = MakePanel(glow2Rect, "Border", Vector2.zero, Vector2.zero, godCol);
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(CARD_W + 4f, CARD_H + 4f);
        borderRect.anchoredPosition = Vector2.zero;

        var cardBg = MakePanel(
            borderRect,
            "CardBg",
            Vector2.zero,
            Vector2.zero,
            new Color(0.060f, 0.050f, 0.120f, 0.97f)
        );

        var cardRect = cardBg.GetComponent<RectTransform>();
        var cardBgImage = cardBg.GetComponent<Image>();
        cardBgImage.raycastTarget = true;

        var cardButton = cardBg.GetComponent<Button>();
        if (cardButton == null)
            cardButton = cardBg.AddComponent<Button>();

        cardButton.transition = Selectable.Transition.None;
        cardButton.targetGraphic = cardBgImage;
        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(() => SelectCard(index));

        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(CARD_W, CARD_H);
        cardRect.anchoredPosition = Vector2.zero;

        _cardRects.Add(cardRect);

        var topStrip = MakePanel(
            cardRect,
            "TopStrip",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Color(godCol.r, godCol.g, godCol.b, 0.25f)
        );

        var topRect = topStrip.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 108f);
        topRect.anchoredPosition = Vector2.zero;

        MakeText(
            topRect,
            "GodIcon",
            card.godIcon,
            new Vector2(0f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0f, 10f),
            new Vector2(0f, 64f),
            54,
            FontStyle.Normal,
            godCol
        ).alignment = TextAnchor.MiddleCenter;

        MakeText(
            topRect,
            "GodName",
            card.godName.ToUpper(),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 14f),
            new Vector2(0f, 24f),
            16,
            FontStyle.Bold,
            new Color(godCol.r * 1.3f, godCol.g * 1.3f, godCol.b * 1.3f, 0.9f)
        ).alignment = TextAnchor.MiddleCenter;

        var badgeBg = MakePanel(
            cardRect,
            "Badge",
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Color(rarityCol.r * 0.6f, rarityCol.g * 0.6f, rarityCol.b * 0.6f, 0.9f)
        );

        var badgeRect = badgeBg.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.sizeDelta = new Vector2(88f, 22f);
        badgeRect.anchoredPosition = new Vector2(0f, -102f);

        var badgeText = MakeText(
            badgeRect,
            "BadgeText",
            CardPool.RarityLabel(card.rarity),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            12,
            FontStyle.Bold,
            rarityCol
        );
        ConfigureCardText(badgeText, TextAnchor.MiddleCenter, 10, 12);

        var titleT = MakeText(
            cardRect,
            "CardTitle",
            card.title,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -132f),
            new Vector2(-22f, 56f),
            24,
            FontStyle.Bold,
            new Color(0.96f, 0.94f, 1.00f, 1f)
        );
        ConfigureCardText(titleT, TextAnchor.UpperCenter, 16, 24);

        var divPanel = MakePanel(
            cardRect,
            "Divider",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Color(godCol.r, godCol.g, godCol.b, 0.55f)
        );

        var divRect = divPanel.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.5f);
        divRect.anchoredPosition = new Vector2(0f, -198f);

        var descT = MakeText(
            cardRect,
            "Desc",
            card.description,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -210f),
            new Vector2(-26f, 128f),
            16,
            FontStyle.Normal,
            new Color(0.72f, 0.70f, 0.85f, 1f)
        );
        ConfigureCardText(descT, TextAnchor.UpperCenter, 13, 17);

        var effectBg = MakePanel(
            cardRect,
            "EffectBg",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Color(godCol.r * 0.15f, godCol.g * 0.15f, godCol.b * 0.15f, 0.8f)
        );

        var effectRect = effectBg.GetComponent<RectTransform>();
        effectRect.anchorMin = new Vector2(0.07f, 0f);
        effectRect.anchorMax = new Vector2(0.93f, 0f);
        effectRect.pivot = new Vector2(0.5f, 0f);
        effectRect.sizeDelta = new Vector2(0f, 56f);
        effectRect.anchoredPosition = new Vector2(0f, 12f);

        var effectT = MakeText(
            effectRect,
            "EffectText",
            card.effectPreview,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            15,
            FontStyle.Bold,
            new Color(godCol.r * 2f, godCol.g * 2f, godCol.b * 2f, 1f)
        );
        ConfigureCardText(effectT, TextAnchor.MiddleCenter, 12, 16);

        var numT = MakeText(
            cardRect,
            "IndexNum",
            $"[ {index + 1} ]",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 70f),
            new Vector2(0f, 20f),
            14,
            FontStyle.Normal,
            new Color(0.45f, 0.43f, 0.65f, 0.9f)
        );
        ConfigureCardText(numT, TextAnchor.MiddleCenter, 11, 14);

        var widget = glowGo.AddComponent<CardWidget>();
        widget.Init(
            index,
            glowRect,
            cardBg.GetComponent<Image>(),
            borderGo.GetComponent<Image>(),
            glow2Go.GetComponent<Image>(),
            godCol,
            this
        );

        glowGo.GetComponent<Image>().raycastTarget = false;
        glow2Go.GetComponent<Image>().raycastTarget = false;
        borderGo.GetComponent<Image>().raycastTarget = false;
        topStrip.GetComponent<Image>().raycastTarget = false;
        badgeBg.GetComponent<Image>().raycastTarget = false;
        divPanel.GetComponent<Image>().raycastTarget = false;
        effectBg.GetComponent<Image>().raycastTarget = false;

        return widget;
    }

    IEnumerator AnimateIn(int level)
    {
        yield return StartCoroutine(Fade(_overlayGroup, 0f, 1f, 0.28f));

        string title = string.IsNullOrWhiteSpace(_overrideTitle)
            ? $"SEVİYE {level}"
            : _overrideTitle;

        string subtitle = string.IsNullOrWhiteSpace(_overrideSubtitle)
            ? "Bir lütuf seç"
            : _overrideSubtitle;

        _titleText.text = title;
        _subtitleText.text = subtitle;

        if (_subtitleShadowText != null)
            _subtitleShadowText.text = subtitle;

        StartCoroutine(Fade(_titleGroup, 0f, 1f, 0.25f));

        float stagger = 0.10f;
        for (int i = 0; i < _widgets.Count; i++)
        {
            int idx = i;
            StartCoroutine(AnimateCardIn(_widgets[idx], stagger * i));
        }

        yield return new WaitForSecondsRealtime(stagger * (_widgets.Count - 1) + 0.35f);
        yield return StartCoroutine(Fade(_hintGroup, 0f, 1f, 0.20f));

        _inputBlocked = false;
    }

    IEnumerator AnimateCardIn(CardWidget w, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        var rect = w.RootRect;
        float duration = 0.38f;
        float elapsed = 0f;

        Vector2 startPos = rect.anchoredPosition + Vector2.down * ENTER_OFFSET;
        Vector2 endPos = rect.anchoredPosition;
        Vector3 startScale = Vector3.one * 0.80f;
        Vector3 endScale = Vector3.one;

        var group = w.GetComponent<CanvasGroup>() ?? w.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float te = BounceEaseOut(t);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, te);
            rect.localScale = Vector3.Lerp(startScale, endScale, te);
            group.alpha = Mathf.Lerp(0f, 1f, t * 3f);

            yield return null;
        }

        rect.anchoredPosition = endPos;
        rect.localScale = endScale;
        group.alpha = 1f;
        w.Ready = true;
    }

    void Update()
    {
        if (!SelectionPending || _inputBlocked)
            return;

        if (TrySelectCardFromMouseClick())
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            SelectCard(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            SelectCard(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            SelectCard(2);
    }

    bool TrySelectCardFromMouseClick()
    {
        if (Mouse.current == null)
            return false;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        int count = Mathf.Min(_cardRects.Count, _currentCards != null ? _currentCards.Count : 0);

        for (int i = 0; i < count; i++)
        {
            RectTransform rect = _cardRects[i];

            if (rect == null)
                continue;

            if (i < _widgets.Count && _widgets[i] != null && !_widgets[i].Ready)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, null))
            {
                SelectCard(i);
                return true;
            }
        }

        return false;
    }

    public void SelectCard(int index)
    {
        if (!SelectionPending || _inputBlocked)
            return;

        if (index < 0 || index >= _currentCards.Count)
            return;

        _inputBlocked = true;

        if (_player != null)
            _currentCards[index].Apply?.Invoke(_player);

        StartCoroutine(AnimateOut(index));
    }

    IEnumerator AnimateOut(int selectedIndex)
    {
        if (selectedIndex < _widgets.Count)
            StartCoroutine(_widgets[selectedIndex].SelectPulse());

        for (int i = 0; i < _widgets.Count; i++)
        {
            if (i == selectedIndex)
                continue;

            var group = _widgets[i].GetComponent<CanvasGroup>() ??
                        _widgets[i].gameObject.AddComponent<CanvasGroup>();

            StartCoroutine(Fade(group, 1f, 0f, 0.20f));
        }

        yield return new WaitForSecondsRealtime(0.40f);
        yield return StartCoroutine(Fade(_overlayGroup, 1f, 0f, 0.28f));

        SelectionPending = false;
        Destroy(_root);
        _widgets.Clear();
        Time.timeScale = 1f;

        if (_player != null)
        {
            PlayerHealth ph = _player.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.GrantInvulnerability(1f);
        }

        _onSelectionComplete?.Invoke();
        _onSelectionComplete = null;
        _overrideTitle = null;
        _overrideSubtitle = null;
    }

    void BuildVignette(RectTransform parent)
    {
        float vigSize = 320f;
        Color vigCol = new Color(0f, 0f, 0f, 0.55f);

        var l = MakePanel(parent, "VigL", Vector2.zero, new Vector2(0f, 1f), vigCol);
        SetStretch(l.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(vigSize, 0f));

        var r = MakePanel(parent, "VigR", new Vector2(1f, 0f), new Vector2(1f, 1f), vigCol);
        SetStretch(r.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(vigSize, 0f));

        var u = MakePanel(parent, "VigU", new Vector2(0f, 1f), Vector2.one, vigCol);
        SetStretch(u.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, vigSize));

        var d = MakePanel(parent, "VigD", Vector2.zero, new Vector2(1f, 0f), vigCol);
        SetStretch(d.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, vigSize));
    }

    void BuildTitleDivider(RectTransform parent)
    {
        var lineShadow = MakePanel(
            parent,
            "TitleLineShadow",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Color(0f, 0f, 0f, 0.65f)
        );

        var lsr = lineShadow.GetComponent<RectTransform>();
        lsr.anchorMin = new Vector2(0.18f, 0f);
        lsr.anchorMax = new Vector2(0.82f, 0f);
        lsr.pivot = new Vector2(0.5f, 0f);
        lsr.sizeDelta = new Vector2(0f, 3f);
        lsr.anchoredPosition = new Vector2(0f, 4f);

        var line = MakePanel(
            parent,
            "TitleLine",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Color(0.988f, 0.816f, 0.000f, 0.75f)
        );

        var lr = line.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.22f, 0f);
        lr.anchorMax = new Vector2(0.78f, 0f);
        lr.pivot = new Vector2(0.5f, 0f);
        lr.sizeDelta = new Vector2(0f, 2f);
        lr.anchoredPosition = new Vector2(0f, 6f);
    }

    static GameObject MakePanel(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        Vector2? pivot = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = color;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return go;
    }

    static void SetStretch(RectTransform r, Vector2 amin, Vector2 amax, Vector2 size)
    {
        r.anchorMin = amin;
        r.anchorMax = amax;
        r.sizeDelta = size;
        r.anchoredPosition = Vector2.zero;
    }

    static Text MakeText(
        RectTransform parent,
        string name,
        string content,
        Vector2 amin,
        Vector2 amax,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        int fontSize,
        FontStyle style,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = fontSize;
        txt.fontStyle = style;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = amin;
        rect.anchorMax = amax;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPos;

        return txt;
    }

    static void ConfigureCardText(Text text, TextAnchor alignment, int minSize, int maxSize)
    {
        if (text == null)
            return;

        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
    }

    CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (go == null)
            return null;

        var group = go.GetComponent<CanvasGroup>();

        if (group == null)
            group = go.AddComponent<CanvasGroup>();

        return group;
    }

    IEnumerator Fade(CanvasGroup g, float from, float to, float dur)
    {
        if (g == null)
            yield break;

        float t = 0f;
        g.alpha = from;

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
        if (t < 0.727f)
            return 7.5625f * t * t;

        if (t < 0.909f)
        {
            t -= 0.818f;
            return 7.5625f * t * t + 0.75f;
        }

        if (t < 0.977f)
        {
            t -= 0.954f;
            return 7.5625f * t * t + 0.9375f;
        }

        t -= 0.988f;
        return 7.5625f * t * t + 0.984375f;
    }
}

public class CardWidget : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public RectTransform RootRect { get; private set; }
    public bool Ready = false;

    int _index;
    Image _cardBg;
    Image _border;
    Image _glow2;
    Color _godColor;
    LevelUpCardSystem _system;

    bool _hovered;
    float _hoverT;

    Vector2 _basePos;

    const float HOVER_SPEED = 8f;
    const float WIDGET_SCALE = 1.055f;
    const float WIDGET_LIFT = 10f;

    public void Init(
        int index,
        RectTransform rootRect,
        Image cardBg,
        Image border,
        Image glow2,
        Color godColor,
        LevelUpCardSystem system)
    {
        _index = index;
        RootRect = rootRect;
        _cardBg = cardBg;
        _border = border;
        _glow2 = glow2;
        _godColor = godColor;
        _system = system;
        _basePos = rootRect.anchoredPosition;
    }

    void Update()
    {
        if (!Ready)
            return;

        float target = _hovered ? 1f : 0f;
        _hoverT = Mathf.Lerp(_hoverT, target, Time.unscaledDeltaTime * HOVER_SPEED);

        float s = Mathf.Lerp(1f, WIDGET_SCALE, _hoverT);
        RootRect.localScale = Vector3.one * s;

        float lift = Mathf.Lerp(0f, WIDGET_LIFT, _hoverT);
        RootRect.anchoredPosition = _basePos + Vector2.up * lift;

        if (_border != null)
        {
            Color bc = _godColor;
            bc.a = Mathf.Lerp(0.70f, 1.00f, _hoverT);
            _border.color = bc;
        }

        if (_glow2 != null)
        {
            Color gc = _godColor;
            gc.a = Mathf.Lerp(0.22f, 0.45f, _hoverT);
            _glow2.color = gc;
        }

        if (_cardBg != null)
        {
            float brightAdd = Mathf.Lerp(0f, 0.06f, _hoverT);
            _cardBg.color = new Color(0.060f + brightAdd, 0.050f + brightAdd, 0.120f + brightAdd, 0.97f);
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (Ready)
            _hovered = true;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _hovered = false;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (Ready)
            _system.SelectCard(_index);
    }

    public IEnumerator SelectPulse()
    {
        float dur = 0.18f;
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(WIDGET_SCALE, 1.12f, t / dur);
            RootRect.localScale = Vector3.one * s;

            if (_border != null)
                _border.color = Color.Lerp(_godColor, Color.white, t / dur);

            yield return null;
        }

        t = 0f;

        while (t < dur * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(1.12f, 1.0f, t / (dur * 0.5f));
            RootRect.localScale = Vector3.one * s;
            yield return null;
        }
    }
}