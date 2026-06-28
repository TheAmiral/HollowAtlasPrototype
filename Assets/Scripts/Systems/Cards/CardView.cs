using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Single card widget. Parented under CardSelectionView's card area.
public class CardView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public RectTransform RootRect  { get; private set; }
    public bool          Ready     = false;

    int              _index;
    CardDefinition   _card;
    Action<int>      _onClick;

    // Visual refs
    Image  _rarityAura;
    Image  _rarityRim;
    Image  _border;
    Image  _cardBg;
    Image  _glow2;
    Image  _hoverHalo;
    Image  _iconGlow;
    float  _iconGlowBaseA;
    bool   _premiumGold;
    CanvasGroup _hoverPromptGroup;

    CardVisualTheme _visual;
    CardRarityTheme _rar;
    bool  _hovered;
    float _hoverT;
    Vector2 _basePos;

    // Merkez kart vurgusu için temel ölçek (1 = normal). Hover bunun üzerine biner.
    public float BaseScale { get; private set; } = 1f;
    public void SetBaseScale(float s) => BaseScale = Mathf.Max(0.01f, s);

    const float HOVER_SPEED = 14f;
    const float HOVER_SCALE = 1.10f;
    const float HOVER_LIFT  = 26f;

    public void Init(int index, CardDefinition card, Action<int> onClick)
    {
        _index   = index;
        _card    = card;
        _onClick = onClick;
        _visual  = CardThemeLibrary.GetVisualTheme(card.visualCategory);
        _rar     = CardThemeLibrary.GetRarityTheme(card.rarity);
        RootRect = GetComponent<RectTransform>();
        _basePos = RootRect != null ? RootRect.anchoredPosition : Vector2.zero;
    }

    public void SetVisualRefs(Image rarityAura, Image rarityRim, Image border, Image cardBg,
        Image glow2, Image hoverHalo, CanvasGroup hoverPromptGroup)
    {
        _rarityAura        = rarityAura;
        _rarityRim         = rarityRim;
        _border            = border;
        _cardBg            = cardBg;
        _glow2             = glow2;
        _hoverHalo         = hoverHalo;
        _hoverPromptGroup  = hoverPromptGroup;
    }

    // Icon arkasındaki radial glow (hover'da hafif güçlenir)
    public void SetIconGlow(Image iconGlow, float baseAlpha)
    {
        _iconGlow      = iconGlow;
        _iconGlowBaseA = baseAlpha;
    }

    // Boss reward (Atlas Ödülü) kartları için çerçeveyi altına çek
    public void SetPremiumGold(bool on) => _premiumGold = on;

    // Gövde tonu için kullanılan visual theme'i dışarıdan ayarla (boss reward gold uyumu)
    public void OverrideVisualTheme(CardVisualTheme v) => _visual = v;

    void Update()
    {
        if (!Ready) return;

        float target = _hovered ? 1f : 0f;
        _hoverT = Mathf.Lerp(_hoverT, target, Time.unscaledDeltaTime * HOVER_SPEED);

        RootRect.localScale        = Vector3.one * (BaseScale * Mathf.Lerp(1f, HOVER_SCALE, _hoverT));
        RootRect.anchoredPosition  = _basePos + Vector2.up * Mathf.Lerp(0f, HOVER_LIFT, _hoverT);

        // Rarity pulse (Chaos)
        float pulse = _card.rarity == CardRarity.Chaos
            ? Mathf.PingPong(Time.unscaledTime * 4.5f, 1f)
            : 0f;

        // Rarity renkleri — premium gold (boss reward) modunda altına doğru çekilir
        Color gold      = CardThemeLibrary.FrameGold;
        Color rarAccent = _premiumGold ? Color.Lerp(_rar.accent, gold, 0.55f) : _rar.accent;
        Color rarGlow   = _premiumGold ? Color.Lerp(_rar.glow,   gold, 0.55f) : _rar.glow;

        // Dış aura — rarity glow (kontrollü, cinematic pass'te biraz güçlendirildi)
        if (_rarityAura != null)
        {
            Color c = rarGlow;
            c.a = Mathf.Lerp(_rar.glowAlpha * 1.25f, _rar.glowAlpha * 2.9f, _hoverT);
            if (_card.visualCategory == CardVisualCategory.Chaos) c.a += pulse * 0.06f;
            _rarityAura.color = c;
        }

        // Rim — rarity accent
        if (_rarityRim != null)
        {
            Color c = rarAccent;
            float baseA = Mathf.Clamp01(0.36f + _rar.glowAlpha);
            c.a = Mathf.Lerp(baseA, Mathf.Min(1f, baseA * 1.7f), _hoverT);
            if (_card.visualCategory == CardVisualCategory.Chaos) c.a = Mathf.Clamp01(c.a + pulse * 0.08f);
            _rarityRim.color = c;
        }

        // Border — rarity accent + altın karışımı (Epik'te daha çok altın; premium his)
        if (_border != null)
        {
            float goldMix = _card.rarity == CardRarity.Epic ? 0.34f
                          : _card.rarity == CardRarity.Legendary ? 0f
                          : 0.14f;
            Color c = Color.Lerp(rarAccent, gold, goldMix);
            c = Color.Lerp(c, Color.white, 0.06f);
            c.a = Mathf.Lerp(0.80f, 1f, _hoverT);
            _border.color = c;
        }

        // İç glow — rarity glow, kontrollü
        if (_glow2 != null)
        {
            Color c = rarGlow;
            c.a = Mathf.Lerp(_rar.glowAlpha * 0.95f, _rar.glowAlpha * 2.1f, _hoverT);
            _glow2.color = c;
        }

        // Hover halo — rarity glow beyaza yaklaşır
        if (_hoverHalo != null)
        {
            Color c = Color.Lerp(rarGlow, Color.white, 0.18f);
            c.a = Mathf.Lerp(0f, 0.38f, _hoverT);
            _hoverHalo.color = c;
        }

        // Gövde — sabit koyu indigo + çok hafif kategori tonu + hover'da hafif aydınlanma
        if (_cardBg != null)
        {
            Color baseI  = CardThemeLibrary.CardBaseIndigo;
            Color tinted = Color.Lerp(baseI, _visual.dark, 0.16f);
            float b = Mathf.Lerp(0f, 0.06f, _hoverT);
            _cardBg.color = new Color(tinted.r + b, tinted.g + b, tinted.b + b, 0.985f);
        }

        // Icon arkası radial glow — hover'da hafif güçlenir
        if (_iconGlow != null)
        {
            Color c = _iconGlow.color;
            c.a = Mathf.Lerp(_iconGlowBaseA, Mathf.Min(1f, _iconGlowBaseA * 1.9f), _hoverT);
            _iconGlow.color = c;
        }

        if (_hoverPromptGroup != null)
            _hoverPromptGroup.alpha = Mathf.SmoothStep(0f, 1f, _hoverT);
    }

    public void SetHovered(bool v)
    {
        if (v && !Ready) v = false;
        if (v && !_hovered) AudioManager.Instance?.PlayCardHover();
        _hovered = v;
    }

    public IEnumerator SelectPulse()
    {
        // Seçimde belirgin bir "punch": hızlı büyü, sonra otur. BaseScale korunur.
        float dur = 0.16f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            RootRect.localScale = Vector3.one * (BaseScale * Mathf.Lerp(HOVER_SCALE, 1.20f, t / dur));
            yield return null;
        }
        t = 0f;
        while (t < dur * 0.6f)
        {
            t += Time.unscaledDeltaTime;
            RootRect.localScale = Vector3.one * (BaseScale * Mathf.Lerp(1.20f, 1f, t / (dur * 0.6f)));
            yield return null;
        }
    }

    public void OnPointerEnter(PointerEventData e) => SetHovered(true);
    public void OnPointerExit(PointerEventData e)  => SetHovered(false);
    public void OnPointerClick(PointerEventData e) { if (Ready) _onClick?.Invoke(_index); }
}
