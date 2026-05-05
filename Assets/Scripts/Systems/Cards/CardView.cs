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
    CanvasGroup _hoverPromptGroup;

    CardVisualTheme _visual;
    CardRarityTheme _rar;
    bool  _hovered;
    float _hoverT;
    Vector2 _basePos;

    const float HOVER_SPEED = 14f;
    const float HOVER_SCALE = 1.085f;
    const float HOVER_LIFT  = 20f;

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

    void Update()
    {
        if (!Ready) return;

        float target = _hovered ? 1f : 0f;
        _hoverT = Mathf.Lerp(_hoverT, target, Time.unscaledDeltaTime * HOVER_SPEED);

        RootRect.localScale        = Vector3.one * Mathf.Lerp(1f, HOVER_SCALE, _hoverT);
        RootRect.anchoredPosition  = _basePos + Vector2.up * Mathf.Lerp(0f, HOVER_LIFT, _hoverT);

        // Rarity pulse (Chaos)
        float pulse = _card.rarity == CardRarity.Chaos
            ? Mathf.PingPong(Time.unscaledTime * 4.5f, 1f)
            : 0f;

        Color categoryAccent = _visual.main;
        if (_card.visualCategory == CardVisualCategory.Chaos)
            categoryAccent = Color.Lerp(_visual.main, _visual.secondaryGlow, 0.4f + pulse * 0.6f);

        if (_rarityAura != null)
        {
            Color c = _visual.glow;
            c.a = Mathf.Lerp(_rar.glowAlpha, _rar.glowAlpha * 2.5f, _hoverT);
            if (_card.visualCategory == CardVisualCategory.Chaos) c.a += pulse * 0.06f;
            _rarityAura.color = c;
        }

        if (_rarityRim != null)
        {
            Color c = categoryAccent;
            float baseA = _rar.glowAlpha * 1.8f;
            c.a = Mathf.Lerp(baseA, Mathf.Min(1f, baseA * 2.2f), _hoverT);
            if (_card.visualCategory == CardVisualCategory.Chaos) c.a = Mathf.Clamp01(c.a + pulse * 0.08f);
            _rarityRim.color = c;
        }

        if (_border != null)
        {
            Color c = Color.Lerp(categoryAccent, Color.white, 0.1f);
            c.a = Mathf.Lerp(0.72f, 1f, _hoverT);
            _border.color = c;
        }

        if (_glow2 != null)
        {
            Color c = _visual.glow;
            c.a = Mathf.Lerp(0.14f, 0.48f, _hoverT);
            _glow2.color = c;
        }

        if (_hoverHalo != null)
        {
            Color c = Color.Lerp(_visual.glow, Color.white, 0.15f);
            c.a = Mathf.Lerp(0f, 0.30f, _hoverT);
            _hoverHalo.color = c;
        }

        if (_cardBg != null)
        {
            float b = Mathf.Lerp(0f, 0.10f, _hoverT);
            Color d = _visual.dark;
            _cardBg.color = new Color(d.r + b, d.g + b, d.b + b, 0.97f);
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
        float dur = 0.18f, t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            RootRect.localScale = Vector3.one * Mathf.Lerp(HOVER_SCALE, 1.12f, t / dur);
            yield return null;
        }
        t = 0f;
        while (t < dur * 0.5f)
        {
            t += Time.unscaledDeltaTime;
            RootRect.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, t / (dur * 0.5f));
            yield return null;
        }
    }

    public void OnPointerEnter(PointerEventData e) => SetHovered(true);
    public void OnPointerExit(PointerEventData e)  => SetHovered(false);
    public void OnPointerClick(PointerEventData e) { if (Ready) _onClick?.Invoke(_index); }
}
