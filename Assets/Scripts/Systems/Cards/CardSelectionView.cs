using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class CardSelectionView : MonoBehaviour
{
    public bool InputBlocked { get; set; } = true;

    List<CardDefinition> _cards;
    Action<int>          _onCardSelected;

    CanvasGroup _overlayGroup;
    CanvasGroup _titleGroup;
    CanvasGroup _hintGroup;
    CanvasGroup _rerollGroup;
    CanvasGroup _buildPanelGroup;
    Text        _titleText;

    // Visual Polish Pass v2 — animasyon için saklanan UI refleri
    RectTransform _backdropRect;     // sinematik container (entrance scale-in)
    RectTransform _titleRootRect;    // başlık kökü (entrance slide/scale)
    RectTransform _titleSheenRect;   // başlık üzerinde gezen ışık (sheen)
    Image         _titleSheenImg;
    Text        _subtitleText;
    Text        _subtitleShadow;

    List<CardView>        _views     = new();
    List<RectTransform>   _cardRects = new();

    // Reroll UI
    RectTransform _rerollBtnBorder;
    Image         _rerollBtnBg;
    Text          _rerollCostText;
    Text          _rerollCountText;

    // Build panel
    List<Image>      _weaponSlotBgs    = new();
    List<Image>      _passiveSlotBgs   = new();
    List<Text>       _weaponSlotTexts  = new();
    List<Text>       _passiveSlotTexts = new();
    List<(bool isWeapon, int index)> _cardSlotTargets = new();

    static readonly Color SlotNormal    = new Color(0.06f, 0.04f, 0.12f, 0.60f);
    static readonly Color SlotHighlight = new Color(0.28f, 0.16f, 0.46f, 0.94f);

    int  _playerLevel;
    bool _isBossReward;

    const float CARD_W   = 276f;
    const float CARD_H   = 418f;
    const float CARD_GAP = 78f;
    const float ENTER_OFFSET = 260f;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Setup(List<CardDefinition> cards, string title, string subtitle,
        int playerLevel, Action<int> onCardSelected, bool isBossReward = false)
    {
        _cards          = cards;
        _onCardSelected = onCardSelected;
        _playerLevel    = playerLevel;
        _isBossReward   = isBossReward;
        InputBlocked    = true;
        BuildUI(title, subtitle);
        StartCoroutine(AnimateIn());
    }

    public IEnumerator DismissSelected(int selectedIndex)
    {
        if (selectedIndex >= 0 && selectedIndex < _views.Count && _views[selectedIndex] != null)
            StartCoroutine(_views[selectedIndex].SelectPulse());
        FlashCard(selectedIndex);

        for (int i = 0; i < _views.Count; i++)
        {
            var g = GetOrAddCanvasGroup(_views[i].gameObject);
            StartCoroutine(Fade(g, 1f, 0f, i == selectedIndex ? 0.30f : 0.20f));
        }
        yield return new WaitForSecondsRealtime(0.32f);
    }

    public IEnumerator FadeOutOverlay()
    {
        yield return StartCoroutine(Fade(_overlayGroup, 1f, 0f, 0.30f));
    }

    // Bir kart seçilince çağrılır: seçim kontrollerini (reroll, hint, başlık, build
    // paneli) gizler; geriye yalnız overlay + feedback paneli kalır. Input zaten
    // InputBlocked ile kapalıdır (reroll feedback state'inde çalışmaz).
    public void EnterFeedbackState()
    {
        InputBlocked = true;
        HideGroup(_rerollGroup);
        HideGroup(_hintGroup);
        HideGroup(_titleGroup);
        HideGroup(_buildPanelGroup);
    }

    void HideGroup(CanvasGroup g) => FadeGroupOut(g, 0.18f);

    void FadeGroupOut(CanvasGroup g, float dur)
    {
        if (g == null) return;
        g.blocksRaycasts = false;
        g.interactable   = false;
        StartCoroutine(Fade(g, g.alpha, 0f, dur));
    }

    // Normal kart seçimi: kartlar + sinematik backdrop/atmosfer/portre/vignette
    // (overlay altındakiler) + reroll/hint/başlık/build paneli HEPSİ birlikte hızlı
    // kapanır → seçilen kart kaybolunca geride boş dikdörtgen kalmaz (~0.20 sn).
    public IEnumerator FastDismiss(int selectedIndex)
    {
        InputBlocked = true;
        if (selectedIndex >= 0 && selectedIndex < _views.Count && _views[selectedIndex] != null)
            StartCoroutine(_views[selectedIndex].SelectPulse());
        FlashCard(selectedIndex);

        const float dur = 0.20f;
        for (int i = 0; i < _views.Count; i++)
        {
            if (_views[i] == null) continue;
            var g = GetOrAddCanvasGroup(_views[i].gameObject);
            StartCoroutine(Fade(g, g.alpha, 0f, dur));
        }
        FadeGroupOut(_overlayGroup,    dur);   // backdrop + atmosphere + portrait + vignette
        FadeGroupOut(_rerollGroup,     dur);
        FadeGroupOut(_hintGroup,       dur);
        FadeGroupOut(_titleGroup,      dur);
        FadeGroupOut(_buildPanelGroup, dur);

        yield return new WaitForSecondsRealtime(dur + 0.02f);
    }

    public GameObject BuildFeedbackPanel(CardDefinition card, List<StatDelta> deltas)
    {
        var rar     = CardThemeLibrary.GetRarityTheme(card.rarity);
        Color gold  = CardThemeLibrary.FrameGold;
        Color rarAccent = _isBossReward ? Color.Lerp(rar.accent, gold, 0.55f) : rar.accent;
        Color rarGlow   = _isBossReward ? Color.Lerp(rar.glow,   gold, 0.55f) : rar.glow;

        bool hasDeltas = deltas != null && deltas.Count > 0;
        int lineCount  = hasDeltas ? deltas.Count : 1;
        const float W = 440f, LINE_H = 30f, TOP_BLOCK = 152f, BOT_PAD = 18f;
        float panelH   = TOP_BLOCK + lineCount * LINE_H + BOT_PAD;

        var container     = new GameObject("FeedbackPanel");
        container.transform.SetParent(transform, false);
        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot     = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(W, panelH);
        containerRect.anchoredPosition = new Vector2(0f, 20f);
        var cg = container.AddComponent<CanvasGroup>();
        cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false;

        // Seçilen kartın rarity rengine göre yumuşak glow (premium his)
        var glowGo  = MakePanel(containerRect, "FBGlow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            CardThemeLibrary.WithAlpha(rarGlow, 0.22f));
        var glowImg = glowGo.GetComponent<Image>();
        glowImg.sprite = SoftGlow(); glowImg.raycastTarget = false;
        var glowRect = glowGo.GetComponent<RectTransform>();
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(W + 170f, panelH + 170f);
        glowRect.anchoredPosition = Vector2.zero;

        // İnce gold çerçeve → koyu mor/indigo gövde
        var borderGo = MakePanel(containerRect, "FBBorder", Vector2.zero, Vector2.one, CardThemeLibrary.WithAlpha(gold, 0.85f));
        borderGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        borderGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        borderGo.GetComponent<Image>().raycastTarget = false;
        var bgGo = MakePanel(borderGo.GetComponent<RectTransform>(), "FBBg", Vector2.zero, Vector2.one,
            new Color(0.055f, 0.035f, 0.110f, 0.985f));
        bgGo.GetComponent<RectTransform>().offsetMin = new Vector2(2f, 2f);
        bgGo.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);
        bgGo.GetComponent<Image>().raycastTarget = false;
        var bgRect = bgGo.GetComponent<RectTransform>();

        float y = 16f;

        // Seçilen ikon — küçük dairesel çerçeve içinde
        float iconCY = -(y + 28f);
        MakeCircle(bgRect, "FBIconGlow",   84f, CardThemeLibrary.WithAlpha(rarGlow, 0.22f),  new Vector2(0f, iconCY), soft: true);
        MakeCircle(bgRect, "FBIconAccent", 62f, CardThemeLibrary.WithAlpha(rarAccent, 0.90f), new Vector2(0f, iconCY));
        MakeCircle(bgRect, "FBIconDark",   56f, new Color(0.05f, 0.035f, 0.10f, 0.98f),       new Vector2(0f, iconCY));
        var icoGo = new GameObject("FBIcon");
        icoGo.transform.SetParent(bgRect, false);
        var icoImg = icoGo.AddComponent<Image>();
        icoImg.sprite = RewardIconLibrary.GetIcon(card.iconId, card.cardKind);
        icoImg.preserveAspect = true; icoImg.raycastTarget = false;
        var icoRect = icoGo.GetComponent<RectTransform>();
        icoRect.anchorMin = icoRect.anchorMax = new Vector2(0.5f, 1f);
        icoRect.pivot = new Vector2(0.5f, 0.5f);
        icoRect.sizeDelta = new Vector2(48f, 48f);
        icoRect.anchoredPosition = new Vector2(0f, iconCY);
        y += 60f;

        FBText(bgRect, "FBLabel", _isBossReward ? "ÖDÜL KAZANILDI" : "LÜTUF KAZANILDI", y, 22f, 15, FontStyle.Bold, CardThemeLibrary.TitleGold, TextAnchor.UpperCenter); y += 26f;
        FBText(bgRect, "FBTitle", card.title, y, 30f, 23, FontStyle.Bold, CardThemeLibrary.BodyTextSoft, TextAnchor.UpperCenter); y += 34f;
        FBDivider(bgRect, "FBDiv", y, 1.5f, CardThemeLibrary.WithAlpha(gold, 0.50f), 40f); y += 12f;
        if (hasDeltas)
        {
            foreach (var d in deltas)
            {
                Color col = d.isPositive ? new Color(0.50f, 1.00f, 0.55f, 1f) : new Color(1.00f, 0.38f, 0.38f, 1f);
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

    // ── UI Build ──────────────────────────────────────────────────────────────

    void BuildUI(string title, string subtitle)
    {
        var rootRect = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        EnsureEventSystem();

        // Overlay
        var overlayGo = MakePanel(rootRect, "Overlay", Vector2.zero, Vector2.one, CardThemeLibrary.OverlayBg);
        _overlayGroup = overlayGo.AddComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f; _overlayGroup.blocksRaycasts = true;
        var overlayRect = overlayGo.GetComponent<RectTransform>();
        BuildVignette(overlayRect);
        BuildPortraitArea(overlayRect);       // sağda büyük portre yuvası (placeholder)
        BuildCardFocusBackdrop(overlayRect);
        BuildAtlasAtmosphere(overlayRect);    // kartların arkasında portal/Atlas glow

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
        _titleRootRect = trRect;   // entrance animasyonu için

        // Başlık arkası okunabilirlik için yumuşak koyu radial plaka
        var titlePlate = MakePanel(trRect, "TitlePlate", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Color(0.02f, 0.01f, 0.05f, 0.34f));
        var tpRect = titlePlate.GetComponent<RectTransform>();
        tpRect.pivot = new Vector2(0.5f, 1f);
        tpRect.sizeDelta = new Vector2(820f, 150f);
        tpRect.anchoredPosition = new Vector2(0f, 6f);
        var tpImg = titlePlate.GetComponent<Image>();
        tpImg.sprite = SoftGlow();
        tpImg.raycastTarget = false;

        _titleText = MakeText(trRect, "TitleText", title,
            Vector2.zero, Vector2.one, new Vector2(0f, -18f), Vector2.zero,
            54, FontStyle.Bold, CardThemeLibrary.TitleGold);
        _titleText.alignment = TextAnchor.UpperCenter;

        _subtitleShadow = MakeText(trRect, "SubShadow", subtitle,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(2f, 31f), new Vector2(0f, 40f),
            25, FontStyle.BoldAndItalic, new Color(0f, 0f, 0f, 0.88f));
        _subtitleShadow.alignment = TextAnchor.LowerCenter;
        _subtitleShadow.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleShadow.verticalOverflow   = VerticalWrapMode.Overflow;

        _subtitleText = MakeText(trRect, "SubText", subtitle,
            Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, 33f), new Vector2(0f, 40f),
            25, FontStyle.BoldAndItalic, CardThemeLibrary.SubtitleTint);
        _subtitleText.alignment = TextAnchor.LowerCenter;
        _subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _subtitleText.verticalOverflow   = VerticalWrapMode.Overflow;
        BuildTitleDivider(trRect);
        BuildTitleSheen(trRect);   // başlık üzerinde periyodik ışık geçişi

        // Build panel (sol) — normal level-up ekranında göster, boss reward'da gizle
        if (!_isBossReward)
            BuildLoadoutPanel(rootRect);

        // Card area
        var cardArea = new GameObject("CardArea");
        cardArea.transform.SetParent(transform, false);
        var cardAreaRect = cardArea.AddComponent<RectTransform>();
        cardAreaRect.anchorMin = cardAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardAreaRect.pivot     = new Vector2(0.5f, 0.5f);
        float totalW = _cards.Count * CARD_W + (_cards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);
        cardAreaRect.anchoredPosition = new Vector2(0f, -24f);

        _views.Clear(); _cardRects.Clear(); _cardSlotTargets.Clear();

        for (int i = 0; i < _cards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            _views.Add(BuildCardWidget(cardAreaRect, _cards[i], xPos, i));
            _cardSlotTargets.Add(ComputeSlotTarget(_cards[i]));
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
        var hintBg = MakePanel(hintBorder.GetComponent<RectTransform>(), "HintBg", Vector2.zero, Vector2.one,
            new Color(0.025f, 0.018f, 0.060f, 0.84f));
        hintBg.GetComponent<RectTransform>().offsetMin = new Vector2(1.5f, 1.5f);
        hintBg.GetComponent<RectTransform>().offsetMax = new Vector2(-1.5f, -1.5f);
        var ht = MakeText(hintRect, "HintText", "[ 1 ]   [ 2 ]   [ 3 ]   ile seç  —  veya karta tıkla",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            21, FontStyle.Bold, new Color(0.82f, 0.80f, 1.00f, 1f));
        ht.alignment = TextAnchor.MiddleCenter;
        UIPulseGlow.Attach(ht, 2.2f, 0.15f);   // hint satırında nazik nabız
        hintBorder.GetComponent<Image>().raycastTarget = false;
        hintBg.GetComponent<Image>().raycastTarget     = false;

        // Reroll footer — normal level-up ekranında göster, boss reward'da gizle
        if (!_isBossReward)
            BuildRerollFooter();
    }

    // ── Build panel ───────────────────────────────────────────────────────────

    void BuildLoadoutPanel(RectTransform rootRect)
    {
        _weaponSlotBgs.Clear();  _weaponSlotTexts.Clear();
        _passiveSlotBgs.Clear(); _passiveSlotTexts.Clear();

        const float PANEL_W  = 240f;
        const float SLOT_H   = 34f;
        const float HEADER_H = 28f;
        const float SEC_GAP  = 18f;
        int  maxSlots  = 6;
        float panelH   = HEADER_H + maxSlots * (SLOT_H + 2f) + SEC_GAP + HEADER_H + maxSlots * (SLOT_H + 2f) + 20f;

        var panelGo   = new GameObject("BuildPanel");
        panelGo.transform.SetParent(rootRect, false);
        var panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0.5f);
        panelRect.anchorMax = new Vector2(0f, 0.5f);
        panelRect.pivot     = new Vector2(0f, 0.5f);
        panelRect.sizeDelta = new Vector2(PANEL_W, panelH);
        panelRect.anchoredPosition = new Vector2(16f, 0f);

        // Seçim tamamlanınca topluca gizlemek için CanvasGroup
        _buildPanelGroup = panelGo.AddComponent<CanvasGroup>();

        // İnce çerçeve + koyu yarı saydam iç zemin (çok parlak mor blok yerine)
        var panelBg = panelGo.AddComponent<Image>();
        panelBg.color = CardThemeLibrary.WithAlpha(CardThemeLibrary.PanelBorder, 0.50f);
        panelBg.raycastTarget = false;

        var innerGo = new GameObject("PanelInner");
        innerGo.transform.SetParent(panelRect, false);
        var innerImg = innerGo.AddComponent<Image>();
        innerImg.color = new Color(0.035f, 0.022f, 0.075f, 0.90f);
        innerImg.raycastTarget = false;
        var innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero; innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2f, 2f); innerRect.offsetMax = new Vector2(-2f, -2f);

        float y = 10f;

        // SİLAHLAR section
        BuildPanelSection(panelRect, ref y, "SİLAHLAR", HEADER_H, PANEL_W);
        var inv = WeaponInventory.Instance;
        var weaponOrder = new[] { WeaponType.KatanaAura, WeaponType.RuhKunai, WeaponType.AtlasSphere };
        for (int i = 0; i < maxSlots; i++)
        {
            bool filled = false;
            string slotText = "—";
            string slotIcon = null;
            if (inv != null && i < weaponOrder.Length && inv.HasWeapon(weaponOrder[i]))
            {
                filled = true;
                int lv = inv.GetLevel(weaponOrder[i]);
                slotText = $"{CardDatabase.GetWeaponName(weaponOrder[i])}  Lv {lv}";
                slotIcon = WeaponIconId(weaponOrder[i]);
            }
            var (bg, txt) = BuildSlot(panelRect, ref y, slotText, filled, SLOT_H, PANEL_W, slotIcon, CardKind.WeaponUnlock);
            _weaponSlotBgs.Add(bg);
            _weaponSlotTexts.Add(txt);
        }

        y += SEC_GAP;

        // PASİFLER section
        BuildPanelSection(panelRect, ref y, "PASİFLER", HEADER_H, PANEL_W);
        var rls = RunLoadoutSystem.Instance;
        for (int i = 0; i < maxSlots; i++)
        {
            bool filled = false;
            string slotText = "—";
            string slotIcon = null;
            if (rls != null && i < rls.OrderedPassives.Count)
            {
                string pid = rls.OrderedPassives[i];
                filled = true;
                int lv = rls.GetPassiveLevel(pid);
                slotText = $"{CardDatabase.GetPassiveName(pid)}  Lv {lv}";
                slotIcon = "icon_" + pid;
            }
            var (bg, txt) = BuildSlot(panelRect, ref y, slotText, filled, SLOT_H, PANEL_W, slotIcon, CardKind.PassiveUnlock);
            _passiveSlotBgs.Add(bg);
            _passiveSlotTexts.Add(txt);
        }
    }

    void BuildPanelSection(RectTransform parent, ref float y, string header, float headerH, float panelW)
    {
        // Arka plan (Image)
        var hGo   = new GameObject("SectionHeader");
        hGo.transform.SetParent(parent, false);
        var hRect = hGo.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 1f); hRect.anchorMax = new Vector2(1f, 1f);
        hRect.pivot     = new Vector2(0.5f, 1f);
        hRect.sizeDelta = new Vector2(-8f, headerH);
        hRect.anchoredPosition = new Vector2(0f, -y);
        var hImg = hGo.AddComponent<Image>();
        hImg.color = new Color(0.11f, 0.06f, 0.19f, 0.82f);
        hImg.raycastTarget = false;

        // Sol kenarda ince altın aksan çubuğu
        var accentGo = new GameObject("HeaderAccent");
        accentGo.transform.SetParent(hGo.transform, false);
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.color = CardThemeLibrary.WithAlpha(CardThemeLibrary.FrameGold, 0.75f);
        accentImg.raycastTarget = false;
        var accentRect = accentGo.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f); accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(3f, -8f);
        accentRect.anchoredPosition = new Vector2(2f, 0f);

        // Başlık metni — ayrı child GO (aynı GO'ya Image + Text eklenemez)
        var labelGo   = new GameObject("HeaderLabel");
        labelGo.transform.SetParent(hGo.transform, false);
        var labelRect = labelGo.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero; labelRect.offsetMax  = Vector2.zero;
        var ht = labelGo.AddComponent<Text>();
        ht.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ht.text      = header;
        ht.fontSize  = 14;
        ht.fontStyle = FontStyle.Bold;
        ht.color     = new Color(0.96f, 0.90f, 1.00f, 0.96f);
        ht.alignment = TextAnchor.MiddleCenter;
        ht.horizontalOverflow = HorizontalWrapMode.Overflow;
        ht.verticalOverflow   = VerticalWrapMode.Overflow;
        ht.raycastTarget = false;
        y += headerH + 4f;
    }

    (Image bg, Text txt) BuildSlot(RectTransform parent, ref float y, string content, bool filled, float h, float panelW,
        string iconId = null, CardKind iconKind = CardKind.PassiveUnlock)
    {
        // Arka plan GO (yalnızca Image)
        var slotGo   = new GameObject("Slot");
        slotGo.transform.SetParent(parent, false);
        var slotRect = slotGo.AddComponent<RectTransform>();
        slotRect.anchorMin = new Vector2(0f, 1f); slotRect.anchorMax = new Vector2(1f, 1f);
        slotRect.pivot     = new Vector2(0.5f, 1f);
        slotRect.sizeDelta = new Vector2(-8f, h);
        slotRect.anchoredPosition = new Vector2(0f, -y);
        var slotImg = slotGo.AddComponent<Image>();
        slotImg.color = SlotNormal;
        slotImg.raycastTarget = false;

        // Dolu slotlarda sol tarafa küçük ikon
        float textLeft = 6f;
        bool hasIcon = filled && !string.IsNullOrEmpty(iconId);
        if (hasIcon)
        {
            var icGo = new GameObject("SlotIcon");
            icGo.transform.SetParent(slotGo.transform, false);
            var icImg = icGo.AddComponent<Image>();
            icImg.sprite         = RewardIconLibrary.GetIcon(iconId, iconKind);
            icImg.preserveAspect = true;
            icImg.raycastTarget  = false;
            var icR = icGo.GetComponent<RectTransform>();
            icR.anchorMin = icR.anchorMax = new Vector2(0f, 0.5f);
            icR.pivot     = new Vector2(0f, 0.5f);
            icR.sizeDelta = new Vector2(28f, 28f);
            icR.anchoredPosition = new Vector2(5f, 0f);
            textLeft = 38f;
        }

        // Metin child GO (Image + Text aynı GO'da olamaz)
        var lblGo   = new GameObject("SlotLabel");
        lblGo.transform.SetParent(slotGo.transform, false);
        var lblRect = lblGo.AddComponent<RectTransform>();
        lblRect.anchorMin = Vector2.zero; lblRect.anchorMax = Vector2.one;
        lblRect.offsetMin = new Vector2(textLeft, 0f); lblRect.offsetMax = new Vector2(-6f, 0f);
        var txt = lblGo.AddComponent<Text>();
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text      = content;
        txt.fontSize  = 12;
        txt.fontStyle = filled ? FontStyle.Bold : FontStyle.Normal;
        txt.color     = filled ? new Color(0.88f, 0.92f, 0.80f, 1f) : new Color(0.42f, 0.38f, 0.55f, 0.65f);
        txt.alignment = hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow   = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;
        y += h + 2f;
        return (slotImg, txt);
    }

    static string WeaponIconId(WeaponType wt) => wt switch
    {
        WeaponType.KatanaAura  => "icon_katana_aura",
        WeaponType.RuhKunai    => "icon_ruh_kunai",
        WeaponType.AtlasSphere => "icon_atlas_kuresi",
        _                      => null
    };

    (bool isWeapon, int index) ComputeSlotTarget(CardDefinition card)
    {
        var inv = WeaponInventory.Instance;
        var rls = RunLoadoutSystem.Instance;

        switch (card.cardKind)
        {
            case CardKind.WeaponUnlock:
                int wCount = inv?.OwnedWeaponCount ?? 0;
                return (true, Mathf.Clamp(wCount, 0, RunLoadoutSystem.MaxWeapons - 1));

            case CardKind.WeaponUpgrade:
                int wSlot = card.weaponType switch
                {
                    WeaponType.KatanaAura  => 0,
                    WeaponType.RuhKunai    => 1,
                    WeaponType.AtlasSphere => 2,
                    _                      => -1
                };
                return (true, wSlot);

            case CardKind.PassiveUnlock:
                int pCount = rls?.PassiveCount ?? 0;
                return (false, Mathf.Clamp(pCount, 0, RunLoadoutSystem.MaxPassives - 1));

            case CardKind.PassiveUpgrade:
                if (rls == null) return (false, -1);
                var ordered = rls.OrderedPassives;
                for (int i = 0; i < ordered.Count; i++)
                    if (ordered[i] == card.passiveId) return (false, i);
                return (false, -1);

            default:
                return (false, -1);
        }
    }

    // ── Card widget ───────────────────────────────────────────────────────────

    CardView BuildCardWidget(RectTransform parent, CardDefinition card, float xPos, int index)
    {
        // Boss reward kartları gold/Atlas teması; normal kartlar kategori temasını kullanır
        var visual = _isBossReward
            ? CardThemeLibrary.GetVisualTheme(CardVisualCategory.BossReward)
            : CardThemeLibrary.GetVisualTheme(card.visualCategory);
        var rar    = CardThemeLibrary.GetRarityTheme(card.rarity);
        float glowAlpha = Mathf.Max(0.16f, rar.glowAlpha);

        // Rarity renkleri — boss reward'da altına çekilir (premium Atlas his)
        Color gold      = CardThemeLibrary.FrameGold;
        Color rarAccent = _isBossReward ? Color.Lerp(rar.accent, gold, 0.55f) : rar.accent;
        Color rarGlow   = _isBossReward ? Color.Lerp(rar.glow,   gold, 0.55f) : rar.glow;

        // Aura glow (rarity) — yumuşak radial
        var auraGo   = MakePanel(parent, $"Aura_{index}", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(rarGlow, glowAlpha));
        var auraRect = auraGo.GetComponent<RectTransform>();
        auraRect.anchorMin = auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot     = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(CARD_W + 60f, CARD_H + 60f);
        auraRect.anchoredPosition = new Vector2(xPos, 0f);
        var auraImg0 = auraGo.GetComponent<Image>();
        auraImg0.sprite = SoftGlow();
        var auraGroup = GetOrAddCanvasGroup(auraGo);
        auraGroup.interactable = false; auraGroup.blocksRaycasts = false;

        // Hover halo (rarity)
        var haloGo   = MakePanel(auraRect, "HoverHalo", Vector2.zero, Vector2.zero, Color.clear);
        var haloRect = haloGo.GetComponent<RectTransform>();
        haloRect.anchorMin = haloRect.anchorMax = new Vector2(0.5f, 0.5f);
        haloRect.pivot     = new Vector2(0.5f, 0.5f);
        haloRect.sizeDelta = new Vector2(CARD_W + 96f, CARD_H + 96f);
        haloRect.anchoredPosition = Vector2.zero;
        haloGo.GetComponent<Image>().sprite = SoftGlow();

        // İç glow (rarity)
        var glow2Go   = MakePanel(auraRect, "CategoryGlow", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(rarGlow, 0.14f));
        var glow2Rect = glow2Go.GetComponent<RectTransform>();
        glow2Rect.anchorMin = glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
        glow2Rect.pivot     = new Vector2(0.5f, 0.5f);
        glow2Rect.sizeDelta = new Vector2(CARD_W + 22f, CARD_H + 22f);
        glow2Rect.anchoredPosition = Vector2.zero;
        var glow2Group = GetOrAddCanvasGroup(glow2Go);
        glow2Group.interactable = false; glow2Group.blocksRaycasts = false;

        // Rim (rarity accent)
        var rimGo   = MakePanel(glow2Rect, "CategoryRim", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(rarAccent, glowAlpha * 1.8f));
        var rimRect = rimGo.GetComponent<RectTransform>();
        rimRect.anchorMin = rimRect.anchorMax = new Vector2(0.5f, 0.5f);
        rimRect.pivot     = new Vector2(0.5f, 0.5f);
        rimRect.sizeDelta = new Vector2(CARD_W + 11f, CARD_H + 11f);
        rimRect.anchoredPosition = Vector2.zero;

        // Border (rarity accent + altın karışımı)
        float goldMix = card.rarity == CardRarity.Epic ? 0.34f
                      : card.rarity == CardRarity.Legendary ? 0f : 0.14f;
        Color borderCol = Color.Lerp(rarAccent, gold, goldMix);
        var borderGo   = MakePanel(rimRect, "Border", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(borderCol, 0.86f));
        var borderRect = borderGo.GetComponent<RectTransform>();
        borderRect.anchorMin = borderRect.anchorMax = new Vector2(0.5f, 0.5f);
        borderRect.pivot     = new Vector2(0.5f, 0.5f);
        borderRect.sizeDelta = new Vector2(CARD_W + 3.5f, CARD_H + 3.5f);
        borderRect.anchoredPosition = Vector2.zero;

        // Card body — koyu indigo (rarity ile boyanmaz)
        Color bodyBase = Color.Lerp(CardThemeLibrary.CardBaseIndigo, visual.dark, 0.16f);
        var bodyGo   = MakePanel(borderRect, "CardBody", Vector2.zero, Vector2.zero, new Color(bodyBase.r, bodyBase.g, bodyBase.b, 0.985f));
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

        // Top band (rarity tinted) + accent underline
        var bandGo   = MakePanel(bodyRect, "TopBand", new Vector2(0f, 1f), new Vector2(1f, 1f), CardThemeLibrary.WithAlpha(rarAccent, 0.16f));
        var bandRect = bandGo.GetComponent<RectTransform>();
        bandRect.anchorMin = new Vector2(0f, 1f); bandRect.anchorMax = new Vector2(1f, 1f);
        bandRect.pivot     = new Vector2(0.5f, 1f);
        bandRect.sizeDelta = new Vector2(0f, 98f);
        bandRect.anchoredPosition = Vector2.zero;
        bandGo.GetComponent<Image>().raycastTarget = false;
        var bandLineGo = MakePanel(bodyRect, "TopBandLine", new Vector2(0f, 1f), new Vector2(1f, 1f), CardThemeLibrary.WithAlpha(rarAccent, 0.55f));
        var blRect = bandLineGo.GetComponent<RectTransform>();
        blRect.anchorMin = new Vector2(0.06f, 1f); blRect.anchorMax = new Vector2(0.94f, 1f);
        blRect.pivot = new Vector2(0.5f, 1f); blRect.sizeDelta = new Vector2(0f, 1.5f); blRect.anchoredPosition = new Vector2(0f, -98f);
        bandLineGo.GetComponent<Image>().raycastTarget = false;

        // Framed icon medalyonu (üst merkez): radial glow → accent disk → koyu disk → icon
        const float ICON_CY = -54f;
        var iconGlowGo = MakeCircle(bodyRect, "IconGlow",       132f, CardThemeLibrary.WithAlpha(rarGlow, 0.22f),            new Vector2(0f, ICON_CY), soft: true);
        MakeCircle(bodyRect, "IconAccentDisc", 104f, CardThemeLibrary.WithAlpha(rarAccent, 0.92f),         new Vector2(0f, ICON_CY));
        MakeCircle(bodyRect, "IconDarkDisc",    94f, new Color(0.05f, 0.035f, 0.10f, 0.98f),               new Vector2(0f, ICON_CY));

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(bodyRect, false);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.sprite         = RewardIconLibrary.GetIcon(card.iconId, card.cardKind);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot     = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(84f, 84f);
        iconRect.anchoredPosition = new Vector2(0f, ICON_CY);

        // Index rozeti (sol üst köşe, altın çerçeveli)
        var idxBorderGo = MakePanel(bodyRect, "IndexBadge", new Vector2(0f, 1f), new Vector2(0f, 1f), CardThemeLibrary.WithAlpha(gold, 0.90f));
        var idxBorderRect = idxBorderGo.GetComponent<RectTransform>();
        idxBorderRect.pivot = new Vector2(0f, 1f);
        idxBorderRect.sizeDelta = new Vector2(30f, 30f);
        idxBorderRect.anchoredPosition = new Vector2(9f, -9f);
        idxBorderGo.GetComponent<Image>().raycastTarget = false;
        var idxInnerGo = MakePanel(idxBorderRect, "IndexInner", Vector2.zero, Vector2.one, new Color(0.06f, 0.04f, 0.11f, 0.96f));
        idxInnerGo.GetComponent<RectTransform>().offsetMin = new Vector2(1.6f, 1.6f);
        idxInnerGo.GetComponent<RectTransform>().offsetMax = new Vector2(-1.6f, -1.6f);
        idxInnerGo.GetComponent<Image>().raycastTarget = false;
        var idxTxt = MakeText(idxInnerGo.GetComponent<RectTransform>(), "IndexNum", (index + 1).ToString(),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            16, FontStyle.Bold, new Color(1f, 0.90f, 0.62f, 1f));
        idxTxt.alignment = TextAnchor.MiddleCenter;

        // Rarity etiketi (sağ üst köşe)
        var rarLabelRect = MakeRegion(bodyRect, "RarityArea", 8f, 16f, 12f);
        var rarLabel = MakeText(rarLabelRect, "RarityLabel", rar.displayName,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            11, FontStyle.Bold, CardThemeLibrary.WithAlpha(rarAccent, 0.95f));
        rarLabel.alignment = TextAnchor.UpperRight;

        // Kind pill (tip + level) — koyu yarı saydam strip
        string kindLabel = GetCardKindLabel(card);
        var pillGo = MakePanel(bodyRect, "KindPill", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), CardThemeLibrary.CardPanelDark);
        var pillRect = pillGo.GetComponent<RectTransform>();
        pillRect.pivot = new Vector2(0.5f, 1f);
        pillRect.sizeDelta = new Vector2(CARD_W - 40f, 24f);
        pillRect.anchoredPosition = new Vector2(0f, -106f);
        pillGo.GetComponent<Image>().raycastTarget = false;
        var pillTxt = MakeText(pillRect, "KindText", kindLabel,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            12, FontStyle.Bold, CardThemeLibrary.WithAlpha(rarAccent, 0.95f));
        ConfigText(pillTxt, TextAnchor.MiddleCenter, 9, 12);

        // Title — daha büyük, ikonla çakışmaz, taşmada küçülür
        var titleRect = MakeRegion(bodyRect, "TitleArea", 136f, 52f, 12f);
        var titleT    = MakeText(titleRect, "CardTitle", card.title,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            26, FontStyle.Bold, CardThemeLibrary.BodyTextSoft);
        ConfigText(titleT, TextAnchor.MiddleCenter, 15, 26);

        // Level progress dots
        string levelStr = BuildLevelString(card);
        if (!string.IsNullOrEmpty(levelStr))
        {
            MakeText(bodyRect, "LevelDots", levelStr,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(CARD_W - 40f, 20f),
                13, FontStyle.Bold, CardThemeLibrary.WithAlpha(rarAccent, 0.85f));
        }

        // Divider (altın hairline)
        var divGo   = MakePanel(bodyRect, "Divider", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(gold, 0.38f));
        var divRect = divGo.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.12f, 1f); divRect.anchorMax = new Vector2(0.88f, 1f);
        divRect.pivot     = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(0f, 1.4f);
        divRect.anchoredPosition = new Vector2(0f, -216f);
        divGo.GetComponent<Image>().raycastTarget = false;

        // Description — koyu yarı saydam panel, kırık beyaz metin
        var descBgGo = MakePanel(bodyRect, "DescBg", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), CardThemeLibrary.CardPanelDark);
        var descBgRect = descBgGo.GetComponent<RectTransform>();
        descBgRect.pivot = new Vector2(0.5f, 1f);
        descBgRect.sizeDelta = new Vector2(CARD_W - 28f, 92f);
        descBgRect.anchoredPosition = new Vector2(0f, -226f);
        descBgGo.GetComponent<Image>().raycastTarget = false;
        var descT = MakeText(descBgRect, "Desc", card.description,
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, -10f),
            14, FontStyle.Normal, CardThemeLibrary.WithAlpha(CardThemeLibrary.BodyTextSoft, 0.86f));
        ConfigText(descT, TextAnchor.UpperCenter, 10, 14);

        // Effect strip — koyu mor/siyah zemin + altın/cyan metin, taşmada küçülür
        var effGo   = MakePanel(bodyRect, "EffectBg", Vector2.zero, Vector2.zero, new Color(0.04f, 0.025f, 0.09f, 0.88f));
        var effRect = effGo.GetComponent<RectTransform>();
        effRect.anchorMin = new Vector2(0.07f, 0f); effRect.anchorMax = new Vector2(0.93f, 0f);
        effRect.pivot     = new Vector2(0.5f, 0f);
        effRect.sizeDelta = new Vector2(0f, 56f);
        effRect.anchoredPosition = new Vector2(0f, 14f);
        effGo.GetComponent<Image>().raycastTarget = false;
        var effLineGo = MakePanel(effRect, "EffLine", new Vector2(0f, 1f), new Vector2(1f, 1f), CardThemeLibrary.WithAlpha(gold, 0.45f));
        var elRect = effLineGo.GetComponent<RectTransform>();
        elRect.anchorMin = new Vector2(0f, 1f); elRect.anchorMax = new Vector2(1f, 1f); elRect.pivot = new Vector2(0.5f, 1f);
        elRect.sizeDelta = new Vector2(0f, 1.2f); elRect.anchoredPosition = Vector2.zero;
        effLineGo.GetComponent<Image>().raycastTarget = false;
        Color effTextCol = (_isBossReward || card.rarity == CardRarity.Legendary)
            ? new Color(1.00f, 0.89f, 0.64f, 1f)
            : new Color(0.66f, 0.92f, 1.00f, 1f);
        var effT = MakeText(effRect, "EffText", card.effectPreview,
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-14f, -6f),
            13, FontStyle.Bold, effTextCol);
        ConfigText(effT, TextAnchor.MiddleCenter, 9, 13);

        // Hover "SEÇ" rozeti — koyu-altın
        var promptGo   = MakePanel(auraRect, "HoverPrompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), CardThemeLibrary.WithAlpha(gold, 0.90f));
        var promptRect = promptGo.GetComponent<RectTransform>();
        promptRect.pivot     = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(108f, 30f);
        promptRect.anchoredPosition = new Vector2(0f, 8f);
        var promptInnerGo = MakePanel(promptRect, "PromptInner", Vector2.zero, Vector2.one, new Color(0.08f, 0.05f, 0.13f, 0.95f));
        promptInnerGo.GetComponent<RectTransform>().offsetMin = new Vector2(1.6f, 1.6f);
        promptInnerGo.GetComponent<RectTransform>().offsetMax = new Vector2(-1.6f, -1.6f);
        var promptGroup = promptGo.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f; promptGroup.interactable = false; promptGroup.blocksRaycasts = false;
        var promptT = MakeText(promptRect, "PromptText", "SEÇ",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            14, FontStyle.Bold, new Color(1f, 0.92f, 0.66f, 1f));
        promptT.alignment = TextAnchor.MiddleCenter;

        // Raycast targets cleanup
        auraImg0.raycastTarget                          = false;
        haloGo.GetComponent<Image>().raycastTarget      = false;
        glow2Go.GetComponent<Image>().raycastTarget     = false;
        rimGo.GetComponent<Image>().raycastTarget       = false;
        borderGo.GetComponent<Image>().raycastTarget    = false;
        promptGo.GetComponent<Image>().raycastTarget    = false;
        promptInnerGo.GetComponent<Image>().raycastTarget = false;

        // CardView attach
        var view = auraGo.AddComponent<CardView>();
        view.Init(index, card, TrySelectCard);
        view.OverrideVisualTheme(visual);
        view.SetVisualRefs(
            auraImg0,
            rimGo.GetComponent<Image>(),
            borderGo.GetComponent<Image>(),
            bodyGo.GetComponent<Image>(),
            glow2Go.GetComponent<Image>(),
            haloGo.GetComponent<Image>(),
            promptGroup
        );
        view.SetIconGlow(iconGlowGo.GetComponent<Image>(), 0.22f);
        view.SetPremiumGold(_isBossReward);

        // Merkez kart vurgusu — tek sayıda kart varsa ortadaki hafifçe daha büyük durur
        int centerIndex = (_cards.Count % 2 == 1) ? _cards.Count / 2 : -1;
        view.SetBaseScale(index == centerIndex ? 1.06f : 1f);

        return view;
    }

    // ── Circle / glow helpers (icon frame) ────────────────────────────────────
    // Runtime'da üretilen daire sprite'ları — builtin resource'a bağımlı değil,
    // Unity sürümünden bağımsız çalışır (GetBuiltinResource bazı sürümlerde hata basar).

    static Sprite _solidCircleSprite;
    static Sprite _softGlowSprite;

    static Sprite SolidCircle() => _solidCircleSprite ??= BuildCircleSprite(false);
    static Sprite SoftGlow()    => _softGlowSprite    ??= BuildCircleSprite(true);

    static Sprite BuildCircleSprite(bool soft)
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode   = TextureWrapMode.Clamp,
            hideFlags  = HideFlags.HideAndDontSave,
            name       = soft ? "CardSoftGlow" : "CardSolidCircle"
        };
        float c   = (size - 1) * 0.5f;
        float rad = c;
        for (int yy = 0; yy < size; yy++)
        {
            for (int xx = 0; xx < size; xx++)
            {
                float dx = xx - c, dy = yy - c;
                float d  = Mathf.Sqrt(dx * dx + dy * dy) / rad;   // 0 merkez → 1 kenar
                float a;
                if (soft)
                {
                    a = Mathf.Clamp01(1f - d);
                    a *= a;                                       // merkeze ağırlıklı yumuşak düşüş
                }
                else
                {
                    float edge = 1.6f / rad;                      // ~1.6px anti-alias kenar
                    a = Mathf.Clamp01((1f - d) / edge);
                }
                tex.SetPixel(xx, yy, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    GameObject MakeCircle(RectTransform parent, string name, float size, Color color, Vector2 anchoredPos, bool soft = false)
    {
        var go  = MakePanel(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), color);
        var img = go.GetComponent<Image>();
        img.sprite        = soft ? SoftGlow() : SolidCircle();
        img.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = anchoredPos;
        return go;
    }

    static string GetCardKindLabel(CardDefinition card)
    {
        return card.cardKind switch
        {
            CardKind.WeaponUnlock    => "YENİ SİLAH",
            CardKind.WeaponUpgrade   => $"SİLAH  Lv {card.currentLevel} → {card.weaponLevel}",
            CardKind.PassiveUnlock   => "YENİ PASİF",
            CardKind.PassiveUpgrade  => $"PASİF  Lv {card.currentLevel} → {card.passiveLevel}",
            CardKind.Special         => "ATLAS ÖDÜLÜ",
            CardKind.WeaponAwakening => "SİLAH UYANIŞI",
            CardKind.ChestRelic      => "KALINTI",
            CardKind.ChestSeal       => "MÜHÜR",
            CardKind.ChestEconomy    => "EKONOMİ",
            CardKind.ChestSurvival   => "HAYATTA KALMA",
            CardKind.ChestAtlas      => "ATLAS",
            _                        => "ÖZEL"
        };
    }

    static string BuildLevelString(CardDefinition card)
    {
        if (card.maxLevel <= 0) return string.Empty;

        int target = card.cardKind == CardKind.WeaponUnlock || card.cardKind == CardKind.WeaponUpgrade
            ? card.weaponLevel
            : card.passiveLevel;

        int total = card.maxLevel;
        // Nokta göstergesi: max 8 için ●●●○○○○○
        int filled = Mathf.Clamp(target, 0, total);
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < total; i++)
            sb.Append(i < filled ? "●" : "○");
        return sb.ToString();
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

        int hovered = UpdateHoverFromMouse();
        UpdateSlotHighlights(hovered);
        if (TryClickFromMouse()) return;
        UpdateRerollHover();
        TryClickReroll();
        HandleKeyboard();
    }

    int UpdateHoverFromMouse()
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
        return hovered;
    }

    void UpdateSlotHighlights(int hoveredCard)
    {
        foreach (var bg in _weaponSlotBgs)  if (bg) bg.color = SlotNormal;
        foreach (var bg in _passiveSlotBgs) if (bg) bg.color = SlotNormal;

        if (hoveredCard < 0 || hoveredCard >= _cardSlotTargets.Count) return;
        var (isWeapon, slotIdx) = _cardSlotTargets[hoveredCard];
        if (slotIdx < 0) return;

        if (isWeapon && slotIdx < _weaponSlotBgs.Count)
            _weaponSlotBgs[slotIdx].color = SlotHighlight;
        else if (!isWeapon && slotIdx < _passiveSlotBgs.Count)
            _passiveSlotBgs[slotIdx].color = SlotHighlight;
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
        Color target = h ? new Color(0.40f, 0.26f, 0.09f, 0.96f) : new Color(0.28f, 0.18f, 0.06f, 0.92f);
        _rerollBtnBg.color = Color.Lerp(_rerollBtnBg.color, target, Time.unscaledDeltaTime * 12f);

        // Hover'da hafif büyüme (premium his)
        float ts = h ? 1.04f : 1f;
        _rerollBtnBorder.localScale = Vector3.Lerp(_rerollBtnBorder.localScale, Vector3.one * ts, Time.unscaledDeltaTime * 12f);
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
            var g = GetOrAddCanvasGroup(_views[i].gameObject);
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
        var cardArea = transform.Find("CardArea");
        if (cardArea == null) return;
        for (int i = cardArea.childCount - 1; i >= 0; i--)
            Destroy(cardArea.GetChild(i).gameObject);
        _views.Clear(); _cardRects.Clear(); _cardSlotTargets.Clear();

        var cardAreaRect = cardArea.GetComponent<RectTransform>();
        float totalW = _cards.Count * CARD_W + (_cards.Count - 1) * CARD_GAP;
        cardAreaRect.sizeDelta = new Vector2(totalW, CARD_H);

        for (int i = 0; i < _cards.Count; i++)
        {
            float xPos = -totalW * 0.5f + i * (CARD_W + CARD_GAP) + CARD_W * 0.5f;
            _views.Add(BuildCardWidget(cardAreaRect, _cards[i], xPos, i));
            _cardSlotTargets.Add(ComputeSlotTarget(_cards[i]));
        }
    }

    void RefreshRerollUI(BiomeRerollService svc)
    {
        bool can = svc.CanReroll();
        if (_rerollCountText != null) _rerollCountText.text = RerollCountText(svc);
        if (_rerollCostText != null)
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
        // 1) Overlay + sinematik container birlikte girer (fade + hafif scale-in)
        StartCoroutine(Fade(_overlayGroup, 0f, 1f, 0.30f));
        if (_backdropRect != null) StartCoroutine(IntroTransform(_backdropRect, Vector2.zero, 0.95f, 0.34f));
        yield return new WaitForSecondsRealtime(0.12f);

        // 2) Başlık: fade + aşağıdan süzülerek ve hafif büyüyerek
        StartCoroutine(Fade(_titleGroup, 0f, 1f, 0.30f));
        if (_titleRootRect != null) StartCoroutine(IntroTransform(_titleRootRect, new Vector2(0f, -28f), 0.92f, 0.42f));

        // 3) Kartlar: kademeli (stagger) bounce slide-in
        for (int i = 0; i < _views.Count; i++)
            StartCoroutine(AnimateCardIn(_views[i], 0.06f + 0.09f * i));
        yield return new WaitForSecondsRealtime(0.09f * Mathf.Max(0, _views.Count - 1) + 0.46f);

        // 4) Destek UI + sürekli sheen
        StartCoroutine(Fade(_hintGroup, 0f, 1f, 0.22f));
        if (_rerollGroup != null) StartCoroutine(Fade(_rerollGroup, 0f, 1f, 0.22f));
        if (_titleSheenRect != null && _titleSheenImg != null) StartCoroutine(SheenLoop());
        InputBlocked = false;
    }

    IEnumerator AnimateCardIn(CardView w, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        var rect      = w.RootRect;
        float bs      = w.BaseScale;
        float dur     = 0.40f, t = 0f;
        Vector2 start = rect.anchoredPosition + Vector2.down * ENTER_OFFSET;
        Vector2 end   = rect.anchoredPosition;
        var group     = GetOrAddCanvasGroup(w.gameObject);
        group.alpha   = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float te = BounceEaseOut(Mathf.Clamp01(t / dur));
            rect.anchoredPosition = Vector2.Lerp(start, end, te);
            rect.localScale       = Vector3.Lerp(Vector3.one * (0.80f * bs), Vector3.one * bs, te);
            group.alpha           = Mathf.Lerp(0f, 1f, Mathf.Clamp01(t / dur * 3f));
            yield return null;
        }
        rect.anchoredPosition = end;
        rect.localScale       = Vector3.one * bs;
        group.alpha           = 1f;
        w.Ready               = true;
    }

    // Bir RectTransform'u (konum offset + ölçek) başlangıçtan yerine yumuşatarak getirir.
    IEnumerator IntroTransform(RectTransform rt, Vector2 fromOffset, float fromScale, float dur)
    {
        if (rt == null) yield break;
        Vector2 baseP = rt.anchoredPosition;
        rt.anchoredPosition = baseP + fromOffset;
        rt.localScale       = Vector3.one * fromScale;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = EaseOutCubic(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.Lerp(baseP + fromOffset, baseP, k);
            rt.localScale       = Vector3.one * Mathf.Lerp(fromScale, 1f, k);
            yield return null;
        }
        rt.anchoredPosition = baseP;
        rt.localScale       = Vector3.one;
    }

    static float EaseOutCubic(float t) { t = 1f - t; return 1f - t * t * t; }

    // Başlık üzerinde periyodik geçen ışık. Ekran açık kaldıkça döner (root yok olunca durur).
    IEnumerator SheenLoop()
    {
        const float travel = 460f;
        while (true)
        {
            yield return new WaitForSecondsRealtime(2.6f);
            if (_titleSheenRect == null || _titleSheenImg == null) yield break;
            float dur = 0.7f, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                _titleSheenRect.anchoredPosition = new Vector2(Mathf.Lerp(-travel, travel, k), _titleSheenRect.anchoredPosition.y);
                var c = _titleSheenImg.color; c.a = Mathf.Sin(k * Mathf.PI) * 0.22f; _titleSheenImg.color = c;
                yield return null;
            }
            if (_titleSheenImg != null) { var cc = _titleSheenImg.color; cc.a = 0f; _titleSheenImg.color = cc; }
        }
    }

    // Başlık genişliğine kırpılmış sheen çubuğu kurar (SheenLoop bunu süzdürür).
    void BuildTitleSheen(RectTransform parent)
    {
        const float wrapW = 720f, wrapH = 92f;
        var wrapGo = new GameObject("TitleSheenMask");
        wrapGo.transform.SetParent(parent, false);
        var wrapRect = wrapGo.AddComponent<RectTransform>();
        wrapRect.anchorMin = wrapRect.anchorMax = new Vector2(0.5f, 1f);
        wrapRect.pivot      = new Vector2(0.5f, 1f);
        wrapRect.sizeDelta  = new Vector2(wrapW, wrapH);
        wrapRect.anchoredPosition = new Vector2(0f, -8f);
        wrapGo.AddComponent<RectMask2D>();

        var bar = UIGlow(wrapRect, "Sheen", new Vector2(0.5f, 0.5f), new Vector2(-wrapW, 0f),
            new Vector2(120f, wrapH * 1.7f), new Color(1f, 0.98f, 0.85f, 0f), true);
        _titleSheenRect = bar.rectTransform;
        _titleSheenImg  = bar;
    }

    // Seçilen kartın üzerinde kısa altın patlama (select flash).
    void FlashCard(int index)
    {
        if (index < 0 || index >= _views.Count || _views[index] == null) return;
        var root = _views[index].RootRect;
        if (root == null) return;
        var go  = new GameObject("SelectFlash");
        go.transform.SetParent(root, false);
        var img = go.AddComponent<Image>();
        img.sprite        = SoftGlow();
        img.raycastTarget = false;
        img.color         = new Color(1f, 0.95f, 0.72f, 0f);
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(CARD_W + 140f, CARD_H + 140f);
        r.anchoredPosition = Vector2.zero;
        StartCoroutine(FlashRoutine(img, r));
    }

    IEnumerator FlashRoutine(Image img, RectTransform r)
    {
        float dur = 0.30f, t = 0f;
        Vector2 s0 = r != null ? r.sizeDelta : Vector2.zero;
        Vector2 s1 = s0 * 1.18f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            if (img != null) { var c = img.color; c.a = Mathf.Sin(k * Mathf.PI) * 0.55f; img.color = c; }
            if (r   != null) r.sizeDelta = Vector2.Lerp(s0, s1, k);
            yield return null;
        }
        if (img != null) { var c = img.color; c.a = 0f; img.color = c; }
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    // Genel amaçlı yumuşak/keskin daire görseli (atmosfer + portre placeholder için).
    Image UIGlow(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color color, bool soft)
    {
        var go  = MakePanel(parent, name, anchor, anchor, color);
        var img = go.GetComponent<Image>();
        img.sprite        = soft ? SoftGlow() : SolidCircle();
        img.raycastTarget = false;
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = anchor;
        r.pivot     = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = anchoredPos;
        return img;
    }

    // Kartların arkasında kontrollü Atlas/portal enerjisi: mor portal + altın + kızıl kor.
    void BuildAtlasAtmosphere(RectTransform parent)
    {
        Color portalPurple = new Color(0.46f, 0.20f, 0.78f);
        Color portalGold   = CardThemeLibrary.FrameGold;
        Color emberRed     = new Color(0.72f, 0.18f, 0.24f);

        var portal = UIGlow(parent, "AtmPortal", new Vector2(0.5f, 0.50f), new Vector2(0f, 24f), new Vector2(1500f, 1080f), CardThemeLibrary.WithAlpha(portalPurple, 0.14f), true);
        var goldG  = UIGlow(parent, "AtmGold",   new Vector2(0.5f, 0.80f), new Vector2(0f, 0f),  new Vector2(1080f, 520f),  CardThemeLibrary.WithAlpha(portalGold,   0.09f), true);
        UIGlow(parent, "AtmEmber", new Vector2(0.5f, 0.15f), new Vector2(0f, 0f),  new Vector2(1320f, 460f),  CardThemeLibrary.WithAlpha(emberRed,     0.07f), true);

        // Yaşayan portal: nabız + nazik dikey süzülme (parallax)
        UIPulseGlow.Attach(portal, 1.4f, 0.30f);
        UIPulseGlow.Attach(goldG,  1.0f, 0.40f);
        UIFloat.Attach(goldG.rectTransform, new Vector2(0f, 18f), 0.5f);
    }

    // Resources yolu — buraya bir Sprite konulursa portre otomatik kullanılır.
    // (Assets/Resources/UI/Portraits/samurai_kadin.png — Sprite, alpha açık)
    const string PortraitResourcePath = "UI/Portraits/samurai_kadin";

    // Sağ tarafta büyük portre yuvası. Gerçek portre asset'i varsa onu gösterir,
    // yoksa placeholder siluet kalır. Asset yokken Resources.Load sessizce null
    // döner → her açılışta hata basmaz, NRE atmaz, ekran bozulmaz.
    void BuildPortraitArea(RectTransform parent)
    {
        var holder = new GameObject("PortraitArea");
        holder.transform.SetParent(parent, false);
        var hRect = holder.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(1f, 0f); hRect.anchorMax = new Vector2(1f, 0f);
        hRect.pivot     = new Vector2(1f, 0f);
        hRect.sizeDelta = new Vector2(620f, 940f);
        hRect.anchoredPosition = new Vector2(60f, -30f);   // hafif taşma → "looming" figür
        var hGroup = holder.AddComponent<CanvasGroup>();
        hGroup.blocksRaycasts = false; hGroup.interactable = false;
        UIFloat.Attach(hRect, new Vector2(0f, 10f), 0.5f);   // nazik nefes/parallax

        // Figürün arkasında mor portal glow (gerçek portre de placeholder da bunun önünde)
        UIGlow(hRect, "PortraitGlow", new Vector2(0.5f, 0.5f), new Vector2(-30f, 120f), new Vector2(560f, 720f),
            CardThemeLibrary.WithAlpha(new Color(0.50f, 0.24f, 0.82f), 0.16f), true);

        // Gerçek portre asset'i (varsa). Yoksa Resources.Load sessizce null döner.
        var portraitSprite = Resources.Load<Sprite>(PortraitResourcePath);

        if (portraitSprite != null)
        {
            hGroup.alpha = 0.90f;   // gerçek portre belirgin ama atmosferle bütünleşir
            var pGo = new GameObject("Portrait");
            pGo.transform.SetParent(hRect, false);
            var pImg = pGo.AddComponent<Image>();
            pImg.sprite         = portraitSprite;
            pImg.preserveAspect = true;
            pImg.raycastTarget  = false;          // input'u engellemez
            var pr = pGo.GetComponent<RectTransform>();
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
            pr.pivot     = new Vector2(0.5f, 0f);
            pr.sizeDelta = new Vector2(600f, 900f);
            pr.anchoredPosition = new Vector2(-30f, 20f);  // tabandan yükselen figür, hafif sola
        }
        else
        {
            hGroup.alpha = 0.42f;   // subtle placeholder siluet
            Color sil = new Color(0.03f, 0.018f, 0.05f, 1f);
            UIGlow(hRect, "PortraitBody",     new Vector2(0.5f, 0f), new Vector2(-40f, 70f),  new Vector2(360f, 560f), sil, false);
            UIGlow(hRect, "PortraitShoulder", new Vector2(0.5f, 0f), new Vector2(-40f, 300f), new Vector2(430f, 280f), sil, false);
            UIGlow(hRect, "PortraitHead",     new Vector2(0.5f, 0f), new Vector2(-40f, 470f), new Vector2(180f, 200f), sil, false);
        }
    }

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
        // Dış ince altın çerçeve → mor çerçeve → koyu yarı saydam container (premium container)
        var border = MakePanel(parent, "BackdropBorder", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            CardThemeLibrary.WithAlpha(CardThemeLibrary.FrameGold, 0.22f));
        var br = border.GetComponent<RectTransform>();
        br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(1264f, 560f);
        br.anchoredPosition = new Vector2(0f, -24f);
        _backdropRect = br;   // entrance scale-in için

        var purple = MakePanel(br, "BackdropPurple", Vector2.zero, Vector2.one,
            CardThemeLibrary.WithAlpha(CardThemeLibrary.PanelBorder, 0.42f));
        purple.GetComponent<RectTransform>().offsetMin = new Vector2(2f, 2f);
        purple.GetComponent<RectTransform>().offsetMax = new Vector2(-2f, -2f);

        var fill = MakePanel(purple.GetComponent<RectTransform>(), "BackdropFill", Vector2.zero, Vector2.one,
            new Color(0.030f, 0.018f, 0.065f, 0.55f));
        fill.GetComponent<RectTransform>().offsetMin = new Vector2(2.5f, 2.5f);
        fill.GetComponent<RectTransform>().offsetMax = new Vector2(-2.5f, -2.5f);

        var inner = MakePanel(fill.GetComponent<RectTransform>(), "BackdropInner", Vector2.zero, Vector2.one,
            new Color(0.006f, 0.004f, 0.018f, 0.45f));
        inner.GetComponent<RectTransform>().offsetMin = new Vector2(44f, 32f);
        inner.GetComponent<RectTransform>().offsetMax = new Vector2(-44f, -32f);

        border.GetComponent<Image>().raycastTarget = false;
        purple.GetComponent<Image>().raycastTarget = false;
        fill.GetComponent<Image>().raycastTarget   = false;
        inner.GetComponent<Image>().raycastTarget  = false;
    }

    void BuildTitleDivider(RectTransform parent)
    {
        var shadow = MakePanel(parent, "TitleLineShadow", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
        var sr = shadow.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0.24f, 0f); sr.anchorMax = new Vector2(0.76f, 0f);
        sr.pivot = new Vector2(0.5f, 0f); sr.sizeDelta = new Vector2(0f, 4f); sr.anchoredPosition = new Vector2(0f, 7f);
        // İki yana incelen zarif altın çizgi (orta boşluk + merkez elması)
        var lineL = MakePanel(parent, "TitleLineL", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(CardThemeLibrary.TitleGold, 0.9f));
        var llr = lineL.GetComponent<RectTransform>();
        llr.anchorMin = new Vector2(0.30f, 0f); llr.anchorMax = new Vector2(0.475f, 0f);
        llr.pivot = new Vector2(0.5f, 0f); llr.sizeDelta = new Vector2(0f, 1.6f); llr.anchoredPosition = new Vector2(0f, 9f);
        var lineR = MakePanel(parent, "TitleLineR", Vector2.zero, Vector2.zero, CardThemeLibrary.WithAlpha(CardThemeLibrary.TitleGold, 0.9f));
        var lrr = lineR.GetComponent<RectTransform>();
        lrr.anchorMin = new Vector2(0.525f, 0f); lrr.anchorMax = new Vector2(0.70f, 0f);
        lrr.pivot = new Vector2(0.5f, 0f); lrr.sizeDelta = new Vector2(0f, 1.6f); lrr.anchoredPosition = new Vector2(0f, 9f);
        // Merkez elması
        var diamond = MakePanel(parent, "TitleDiamond", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), CardThemeLibrary.TitleGold);
        var dr = diamond.GetComponent<RectTransform>();
        dr.pivot = new Vector2(0.5f, 0.5f); dr.sizeDelta = new Vector2(8f, 8f); dr.anchoredPosition = new Vector2(0f, 9.5f);
        diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        shadow.GetComponent<Image>().raycastTarget  = false;
        lineL.GetComponent<Image>().raycastTarget   = false;
        lineR.GetComponent<Image>().raycastTarget   = false;
        diamond.GetComponent<Image>().raycastTarget = false;
    }

    // ── Feedback helpers ──────────────────────────────────────────────────────

    static void FBText(RectTransform parent, string name, string content, float topY, float height,
        int fontSize, FontStyle style, Color color, TextAnchor align)
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

    static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        var g = go.GetComponent<CanvasGroup>();
        return g != null ? g : go.AddComponent<CanvasGroup>();
    }

    static GameObject MakePanel(RectTransform parent, string name, Vector2 amin, Vector2 amax, Color color)
    {
        var go  = new GameObject(name);
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
        var go  = new GameObject(name);
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
