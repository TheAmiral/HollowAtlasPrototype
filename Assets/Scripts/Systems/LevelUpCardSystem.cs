using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ─────────────────────────────────────────────────────────────────────────────
// LevelUpCardSystem
//
//   Hades 2 stilinde kart seçim ekranı.
//   PlayerLevelSystem level-up tetikleyince bu sistem devreye girer:
//     1. Ekran karartılır (overlay fade-in)
//     2. "SEVİYE X" başlığı kayar
//     3. 3 kart staggered şekilde yukarı fırlar (bounce easing)
//     4. Mouse hover → kart büyür + parlama
//     5. Seçim → selected pulse, diğerleri solar, ekran kapanır
//     6. Oyun devam eder
// ─────────────────────────────────────────────────────────────────────────────

public class LevelUpCardSystem : MonoBehaviour
{
    public static LevelUpCardSystem Instance { get; private set; }

    // Dışarıdan okunabilir: seçim bekleniyor mu?
    public bool SelectionPending { get; private set; }

    // ── Çözünürlük sabitleri ─────────────────────────────────────────────────
    const float CARD_W       = 272f;
    const float CARD_H       = 390f;
    const float CARD_GAP     = 32f;
    const float ENTER_OFFSET = 260f;   // başlangıç Y kayması
    const float HOVER_SCALE  = 1.055f;
    const float HOVER_LIFT   = 10f;

    // ── İç durum ─────────────────────────────────────────────────────────────
    GameObject   _root;
    CanvasGroup  _overlayGroup;
    CanvasGroup  _titleGroup;
    Text         _titleText;
    Text         _subtitleText;
    Text         _hintText;
    CanvasGroup  _hintGroup;

    List<CardWidget> _widgets = new();
    List<LevelUpCard> _currentCards;
    GameObject       _player;
    bool             _inputBlocked;

    // ── Yaşam döngüsü ────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Dışarıdan çağrı ──────────────────────────────────────────────────────
    public void TriggerSelection(int playerLevel)
    {
        if (SelectionPending) return;

        _player = GameObject.FindGameObjectWithTag("Player");
        _currentCards = CardPool.PickRandom(3, playerLevel);

        if (_currentCards.Count == 0) return;

        SelectionPending = true;
        Time.timeScale = 0f;

        BuildUI();
        StartCoroutine(AnimateIn(playerLevel));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI İNŞASI
    // ─────────────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        // Zaten varsa temizle
        if (_root != null) Destroy(_root);
        _widgets.Clear();

        // ── Kök canvas ───────────────────────────────────────────────────────
        _root = new GameObject("LevelUpCardRoot");
        DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var rootRect = _root.GetComponent<RectTransform>();

        // ── Koyu overlay (çok katmanlı derinlik efekti) ──────────────────────
        var overlay = MakePanel(rootRect, "Overlay",
            Vector2.zero, Vector2.one,               // stretch full screen
            new Color(0.020f, 0.016f, 0.055f, 0f),  // #050412 — çok koyu mor
            new Vector2(0.5f, 0.5f));
        _overlayGroup = overlay.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = true;

        // Kenarlarda hafif vignette simülasyonu (iç içe panel katmanları)
        BuildVignette(overlay.GetComponent<RectTransform>());

        // ── Başlık grubu ─────────────────────────────────────────────────────
        var titleRoot = MakePanel(overlay.GetComponent<RectTransform>(), "TitleRoot",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            Color.clear);
        var titleRootRect = titleRoot.GetComponent<RectTransform>();
        titleRootRect.anchorMin = titleRootRect.anchorMax = new Vector2(0.5f, 1f);
        titleRootRect.pivot     = new Vector2(0.5f, 1f);
        titleRootRect.sizeDelta = new Vector2(800f, 140f);
        titleRootRect.anchoredPosition = new Vector2(0f, -60f);

        _titleGroup = titleRoot.AddComponent<CanvasGroup>();
        _titleGroup.alpha = 0f;

        _titleText = MakeText(titleRootRect, "TitleText",
            "SEVİYE ATLA",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(0f, -28f), new Vector2(0f, 0f),
            60, FontStyle.Bold,
            new Color(0.988f, 0.816f, 0.000f, 1f));  // altın
        _titleText.alignment = TextAnchor.UpperCenter;

        _subtitleText = MakeText(titleRootRect, "SubText",
            "Bir lütuf seç",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 28f), new Vector2(0f, 28f),
            24, FontStyle.Italic,
            new Color(0.7f, 0.7f, 0.9f, 1f));
        _subtitleText.alignment = TextAnchor.LowerCenter;

        // Başlık altı dekoratif çizgi
        BuildTitleDivider(titleRootRect);

        // ── Kart alanı ───────────────────────────────────────────────────────
        var cardArea = new GameObject("CardArea");
        cardArea.transform.SetParent(_root.transform, false);
        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot     = new Vector2(0.5f, 0.5f);

        float totalW = _currentCards.Count * CARD_W + (_currentCards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta      = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -20f);

        for (int i = 0; i < _currentCards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            var widget = BuildCard(cardAreaRect, _currentCards[i], xPos, i);
            _widgets.Add(widget);
        }

        // ── Alt ipucu ────────────────────────────────────────────────────────
        var hintRoot = new GameObject("HintRoot");
        hintRoot.transform.SetParent(_root.transform, false);
        var hintRect = hintRoot.AddComponent<RectTransform>();
        hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(600f, 40f);
        hintRect.anchoredPosition = new Vector2(0f, 38f);

        _hintGroup = hintRoot.AddComponent<CanvasGroup>();
        _hintGroup.alpha = 0f;

        _hintText = MakeText(hintRect, "HintText",
            "[ 1 ]  [ 2 ]  [ 3 ]  ile seç  —  veya karta tıkla",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            20, FontStyle.Normal,
            new Color(0.55f, 0.55f, 0.75f, 1f));
        _hintText.alignment = TextAnchor.MiddleCenter;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // KART İNŞASI
    // ─────────────────────────────────────────────────────────────────────────
    CardWidget BuildCard(RectTransform parent, LevelUpCard card, float xPos, int index)
    {
        Color godCol    = CardPool.GodColor(card.god);
        Color rarityCol = CardPool.RarityColor(card.rarity);

        // ── Dış parlama katmanı (glow simülasyonu) ───────────────────────────
        var glowGo = MakePanel(parent, $"Glow_{index}",
            Vector2.zero, Vector2.zero,
            new Color(godCol.r, godCol.g, godCol.b, 0.18f));
        var glowRect = glowGo.GetComponent<RectTransform>();
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(CARD_W + 28f, CARD_H + 28f);
        glowRect.anchoredPosition = new Vector2(xPos, 0f);

        // ── Orta parlama ─────────────────────────────────────────────────────
        var glow2Go = MakePanel(glowRect, $"Glow2",
            Vector2.zero, Vector2.zero,
            new Color(godCol.r, godCol.g, godCol.b, 0.30f));
        var glow2Rect = glow2Go.GetComponent<RectTransform>();
        glow2Rect.anchorMin = glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        glow2Rect.pivot = new Vector2(0.5f, 0.5f);
        glow2Rect.sizeDelta = new Vector2(CARD_W + 12f, CARD_H + 12f);
        glow2Rect.anchoredPosition = Vector2.zero;

        // ── Kenarlık ─────────────────────────────────────────────────────────
        var borderGo = MakePanel(glow2Rect, "Border",
            Vector2.zero, Vector2.zero,
            godCol);
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(CARD_W + 4f, CARD_H + 4f);
        borderRect.anchoredPosition = Vector2.zero;

        // ── Kart arka plan ───────────────────────────────────────────────────
        var cardBg = MakePanel(borderRect, "CardBg",
            Vector2.zero, Vector2.zero,
            new Color(0.060f, 0.050f, 0.120f, 0.97f));  // #0F0D1E koyu mor-lacivert
        var cardRect = cardBg.GetComponent<RectTransform>();
        cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(CARD_W, CARD_H);
        cardRect.anchoredPosition = Vector2.zero;

        // ── Üst renkli şerit (tanrı rengi) ──────────────────────────────────
        var topStrip = MakePanel(cardRect, "TopStrip",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Color(godCol.r, godCol.g, godCol.b, 0.25f));
        var topRect = topStrip.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 108f);
        topRect.anchoredPosition = Vector2.zero;

        // Tanrı ikonu
        MakeText(topRect, "GodIcon",
            card.godIcon,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(0f, 10f), new Vector2(0f, 64f),
            54, FontStyle.Normal, godCol)
            .alignment = TextAnchor.MiddleCenter;

        // Tanrı adı
        MakeText(topRect, "GodName",
            card.godName.ToUpper(),
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 14f), new Vector2(0f, 24f),
            16, FontStyle.Bold,
            new Color(godCol.r * 1.3f, godCol.g * 1.3f, godCol.b * 1.3f, 0.9f))
            .alignment = TextAnchor.MiddleCenter;

        // ── Rarity rozeti ────────────────────────────────────────────────────
        var badgeBg = MakePanel(cardRect, "Badge",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Color(rarityCol.r * 0.6f, rarityCol.g * 0.6f, rarityCol.b * 0.6f, 0.9f));
        var badgeRect = badgeBg.GetComponent<RectTransform>();
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.sizeDelta = new Vector2(88f, 22f);
        badgeRect.anchoredPosition = new Vector2(0f, -102f);

        MakeText(badgeRect, "BadgeText",
            CardPool.RarityLabel(card.rarity),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            12, FontStyle.Bold, rarityCol)
            .alignment = TextAnchor.MiddleCenter;

        // ── Kart başlığı ─────────────────────────────────────────────────────
        var titleT = MakeText(cardRect, "CardTitle",
            card.title,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -140f), new Vector2(-24f, 48f),
            26, FontStyle.Bold,
            new Color(0.96f, 0.94f, 1.00f, 1f));
        titleT.alignment = TextAnchor.UpperCenter;

        // ── İnce renkli çizgi ────────────────────────────────────────────────
        var divPanel = MakePanel(cardRect, "Divider",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Color(godCol.r, godCol.g, godCol.b, 0.55f));
        var divRect = divPanel.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.1f, 1f);
        divRect.anchorMax = new Vector2(0.9f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.5f);
        divRect.anchoredPosition = new Vector2(0f, -194f);

        // ── Açıklama metni ───────────────────────────────────────────────────
        var descT = MakeText(cardRect, "Desc",
            card.description,
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -204f), new Vector2(-28f, 110f),
            17, FontStyle.Normal,
            new Color(0.72f, 0.70f, 0.85f, 1f));
        descT.alignment     = TextAnchor.UpperCenter;
        descT.horizontalOverflow = HorizontalWrapMode.Wrap;
        descT.verticalOverflow   = VerticalWrapMode.Overflow;

        // ── Efekt önizleme ───────────────────────────────────────────────────
        var effectBg = MakePanel(cardRect, "EffectBg",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Color(godCol.r * 0.15f, godCol.g * 0.15f, godCol.b * 0.15f, 0.8f));
        var effectRect = effectBg.GetComponent<RectTransform>();
        effectRect.anchorMin = new Vector2(0.08f, 0f);
        effectRect.anchorMax = new Vector2(0.92f, 0f);
        effectRect.pivot     = new Vector2(0.5f, 0f);
        effectRect.sizeDelta = new Vector2(0f, 44f);
        effectRect.anchoredPosition = new Vector2(0f, 16f);

        var effectT = MakeText(effectRect, "EffectText",
            card.effectPreview,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            16, FontStyle.Bold,
            new Color(godCol.r * 2f, godCol.g * 2f, godCol.b * 2f, 1f));
        effectT.alignment = TextAnchor.MiddleCenter;

        // ── Kart index numarası (klavye ipucu) ───────────────────────────────
        var numT = MakeText(cardRect, "IndexNum",
            $"[ {index + 1} ]",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 66f), new Vector2(0f, 22f),
            15, FontStyle.Normal,
            new Color(0.45f, 0.43f, 0.65f, 0.9f));
        numT.alignment = TextAnchor.MiddleCenter;

        // ── Widget wrapper (hover/click handler) ─────────────────────────────
        var widget = glowGo.AddComponent<CardWidget>();
        widget.Init(index, glowRect, cardBg.GetComponent<Image>(),
                    borderGo.GetComponent<Image>(),
                    glow2Go.GetComponent<Image>(),
                    godCol, this);

        return widget;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ANİMASYONLAR
    // ─────────────────────────────────────────────────────────────────────────
    IEnumerator AnimateIn(int level)
    {
        // 1. Overlay fade-in
        yield return StartCoroutine(Fade(_overlayGroup, 0f, 1f, 0.28f));

        // 2. Başlık text güncelle
        _titleText.text    = $"SEVİYE {level}";
        _subtitleText.text = "Bir lütuf seç";

        // 3. Başlık kayar iner
        StartCoroutine(Fade(_titleGroup, 0f, 1f, 0.25f));

        // 4. Kartlar staggered girer
        float stagger = 0.10f;
        for (int i = 0; i < _widgets.Count; i++)
        {
            int idx = i; // closure capture
            StartCoroutine(AnimateCardIn(_widgets[idx], stagger * i));
        }

        // 5. İpucu belirir
        yield return new WaitForSecondsRealtime(stagger * (_widgets.Count - 1) + 0.35f);
        yield return StartCoroutine(Fade(_hintGroup, 0f, 1f, 0.20f));

        // 6. Input kabul etmeye başla
        _inputBlocked = false;
    }

    IEnumerator AnimateCardIn(CardWidget w, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        var rect   = w.RootRect;
        float duration = 0.38f;
        float elapsed  = 0f;

        Vector2 startPos = rect.anchoredPosition + Vector2.down * ENTER_OFFSET;
        Vector2 endPos   = rect.anchoredPosition;
        Vector3 startScale = Vector3.one * 0.80f;
        Vector3 endScale   = Vector3.one;

        var group = w.GetComponent<CanvasGroup>() ?? w.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t  = Mathf.Clamp01(elapsed / duration);
            float te = BounceEaseOut(t);

            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, te);
            rect.localScale       = Vector3.Lerp(startScale, endScale, te);
            group.alpha           = Mathf.Lerp(0f, 1f, t * 3f); // alpha hızlı gelir
            yield return null;
        }

        rect.anchoredPosition = endPos;
        rect.localScale       = endScale;
        group.alpha           = 1f;
        w.Ready               = true;
    }

    // ── Update — klavye seçimi ────────────────────────────────────────────────
    void Update()
    {
        if (!SelectionPending || _inputBlocked) return;

        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            SelectCard(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            SelectCard(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            SelectCard(2);
    }

    // ── Kart seçildi ─────────────────────────────────────────────────────────
    public void SelectCard(int index)
    {
        if (!SelectionPending || _inputBlocked) return;
        if (index < 0 || index >= _currentCards.Count) return;

        _inputBlocked = true;

        // Efekti uygula
        if (_player != null)
            _currentCards[index].Apply?.Invoke(_player);

        StartCoroutine(AnimateOut(index));
    }

    IEnumerator AnimateOut(int selectedIndex)
    {
        // Seçilen kart büyür + parlama pulse
        if (selectedIndex < _widgets.Count)
            StartCoroutine(_widgets[selectedIndex].SelectPulse());

        // Diğerleri solar
        for (int i = 0; i < _widgets.Count; i++)
        {
            if (i == selectedIndex) continue;
            var group = _widgets[i].GetComponent<CanvasGroup>() ??
                        _widgets[i].gameObject.AddComponent<CanvasGroup>();
            StartCoroutine(Fade(group, 1f, 0f, 0.20f));
        }

        yield return new WaitForSecondsRealtime(0.40f);

        // Tüm ekran kapanır
        yield return StartCoroutine(Fade(_overlayGroup, 1f, 0f, 0.28f));

        // Temizle ve devam et
        SelectionPending = false;
        Destroy(_root);
        _widgets.Clear();
        Time.timeScale = 1f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YARDIMCI — UI İNŞASI
    // ─────────────────────────────────────────────────────────────────────────

    void BuildVignette(RectTransform parent)
    {
        // 4 kenar paneli — koyu, yarı saydam — vignette simüle eder
        float vigSize = 320f;
        Color vigCol  = new Color(0f, 0f, 0f, 0.55f);

        // Sol
        var l = MakePanel(parent, "VigL", Vector2.zero, new Vector2(0f, 1f), vigCol);
        SetStretch(l.GetComponent<RectTransform>(), new Vector2(0f,0f), new Vector2(0f,1f), new Vector2(vigSize, 0f));

        // Sağ
        var r = MakePanel(parent, "VigR", new Vector2(1f,0f), new Vector2(1f,1f), vigCol);
        SetStretch(r.GetComponent<RectTransform>(), new Vector2(1f,0f), new Vector2(1f,1f), new Vector2(vigSize, 0f));

        // Üst
        var u = MakePanel(parent, "VigU", new Vector2(0f,1f), Vector2.one, vigCol);
        SetStretch(u.GetComponent<RectTransform>(), new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f, vigSize));

        // Alt
        var d = MakePanel(parent, "VigD", Vector2.zero, new Vector2(1f,0f), vigCol);
        SetStretch(d.GetComponent<RectTransform>(), new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0f, vigSize));
    }

    void BuildTitleDivider(RectTransform parent)
    {
        var line = MakePanel(parent, "TitleLine",
            new Vector2(0f,0f), new Vector2(1f,0f),
            new Color(0.988f, 0.816f, 0.000f, 0.5f));
        var lr = line.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0.2f, 0f);
        lr.anchorMax = new Vector2(0.8f, 0f);
        lr.pivot = new Vector2(0.5f, 0f);
        lr.sizeDelta = new Vector2(0f, 1.5f);
        lr.anchoredPosition = new Vector2(0f, 6f);
    }

    static GameObject MakePanel(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color,
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
        r.anchorMin = amin; r.anchorMax = amax;
        r.sizeDelta = size; r.anchoredPosition = Vector2.zero;
    }

    static Text MakeText(RectTransform parent, string name, string content,
        Vector2 amin, Vector2 amax, Vector2 anchoredPos, Vector2 sizeDelta,
        int fontSize, FontStyle style, Color color)
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
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = amin; rect.anchorMax = amax;
        rect.sizeDelta = sizeDelta; rect.anchoredPosition = anchoredPos;
        return txt;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // YARDIMCI — ANİMASYON
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator Fade(CanvasGroup g, float from, float to, float dur)
    {
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

    // Bounce easing — üstten yavaşlar, hafif aşar, geri döner
    static float BounceEaseOut(float t)
    {
        if (t < 0.727f)  return 7.5625f * t * t;
        if (t < 0.909f) { t -= 0.818f; return 7.5625f * t * t + 0.75f; }
        if (t < 0.977f) { t -= 0.954f; return 7.5625f * t * t + 0.9375f; }
        t -= 0.988f; return 7.5625f * t * t + 0.984375f;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// CardWidget — bir kartın hover/click mantığı
// ─────────────────────────────────────────────────────────────────────────────

public class CardWidget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public RectTransform RootRect   { get; private set; }
    public bool          Ready      = false;

    int              _index;
    Image            _cardBg;
    Image            _border;
    Image            _glow2;
    Color            _godColor;
    LevelUpCardSystem _system;

    bool  _hovered;
    float _hoverT;   // 0..1 interpolasyon

    Vector2 _basePos;
    Vector3 _baseScale = Vector3.one;

    const float HOVER_SPEED = 8f;

    public void Init(int index, RectTransform rootRect,
                     Image cardBg, Image border, Image glow2,
                     Color godColor, LevelUpCardSystem system)
    {
        _index    = index;
        RootRect  = rootRect;
        _cardBg   = cardBg;
        _border   = border;
        _glow2    = glow2;
        _godColor = godColor;
        _system   = system;
        _basePos  = rootRect.anchoredPosition;
    }

    void Update()
    {
        if (!Ready) return;

        // Hover interpolasyon
        float target = _hovered ? 1f : 0f;
        _hoverT = Mathf.Lerp(_hoverT, target, Time.unscaledDeltaTime * HOVER_SPEED);

        // Scale
        float s = Mathf.Lerp(1f, LevelUpCardSystem.HOVER_SCALE, _hoverT);
        RootRect.localScale = Vector3.one * s;

        // Y lift
        float lift = Mathf.Lerp(0f, LevelUpCardSystem.HOVER_LIFT, _hoverT);
        RootRect.anchoredPosition = _basePos + Vector2.up * lift;

        // Sınır parlaması
        if (_border != null)
        {
            Color bc = _godColor;
            bc.a = Mathf.Lerp(0.70f, 1.00f, _hoverT);
            _border.color = bc;
        }

        // Dış glow yoğunlaşması
        if (_glow2 != null)
        {
            Color gc = _godColor;
            gc.a = Mathf.Lerp(0.22f, 0.45f, _hoverT);
            _glow2.color = gc;
        }

        // Kart arka plan hafifçe aydınlanır
        if (_cardBg != null)
        {
            float brightAdd = Mathf.Lerp(0f, 0.06f, _hoverT);
            _cardBg.color = new Color(0.060f + brightAdd, 0.050f + brightAdd, 0.120f + brightAdd, 0.97f);
        }
    }

    public void OnPointerEnter(PointerEventData e) { if (Ready) _hovered = true; }
    public void OnPointerExit(PointerEventData e)  { _hovered = false; }
    public void OnPointerClick(PointerEventData e) { if (Ready) _system.SelectCard(_index); }

    // Seçim animasyonu — önce büyür sonra normal döner
    public IEnumerator SelectPulse()
    {
        float dur = 0.18f;
        float t   = 0f;

        // Phase 1: büyü
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Lerp(LevelUpCardSystem.HOVER_SCALE, 1.12f, t / dur);
            RootRect.localScale = Vector3.one * s;

            // Border beyaza flash
            if (_border != null)
                _border.color = Color.Lerp(_godColor, Color.white, t / dur);

            yield return null;
        }

        // Phase 2: küçül
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
