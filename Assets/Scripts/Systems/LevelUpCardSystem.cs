using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ─────────────────────────────────────────────────────────────────────────────
// LevelUpCardSystem  —  orchestrator only.
//
//   PlayerLevelSystem  →  TriggerSelection(level)
//   BossRewardSystem   →  ShowCustomCards(title, subtitle, cards, onComplete)
//   PlayerMovement     →  SelectionPending
// ─────────────────────────────────────────────────────────────────────────────

public class LevelUpCardSystem : MonoBehaviour
{
    public static LevelUpCardSystem Instance { get; private set; }

    public bool SelectionPending { get; private set; }

    GameObject           _root;
    CardSelectionView    _view;
    List<CardDefinition> _currentCards;
    GameObject           _player;
    bool                 _inputBlocked;
    Action               _onSelectionComplete;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Called by PlayerLevelSystem on level-up.
    public void TriggerSelection(int playerLevel)
    {
        if (SelectionPending) return;

        _player              = GameObject.FindGameObjectWithTag("Player");
        _currentCards        = CardOfferGenerator.Generate(3, playerLevel);
        _onSelectionComplete = null;

        if (_currentCards.Count == 0) return;

        Open("ATLAS LÜTUFLARI", "Atlas bir lütuf seçmeni istiyor.", playerLevel);
    }

    // Called by BossRewardSystem with pre-built cards.
    public bool ShowCustomCards(string title, string subtitle, List<CardDefinition> cards, Action onComplete = null)
    {
        if (SelectionPending)            return false;
        if (cards == null || cards.Count == 0) return false;

        _player              = GameObject.FindGameObjectWithTag("Player");
        _currentCards        = CopiedList(cards);
        SetVisualCategory(_currentCards, CardVisualCategory.BossReward);
        _onSelectionComplete = onComplete;

        if (_currentCards.Count == 0) return false;

        Open(title, subtitle, 0);
        return true;
    }

    // ── Internal open ─────────────────────────────────────────────────────────

    void Open(string title, string subtitle, int playerLevel)
    {
        SelectionPending = true;
        Time.timeScale   = 0f;
        _inputBlocked    = true;

        if (_root != null) Destroy(_root);

        _root = new GameObject("LevelUpCardRoot");
        DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        var scaler = _root.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode       = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;

        _root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        _view = _root.AddComponent<CardSelectionView>();
        _view.Setup(_currentCards, title, subtitle, playerLevel, SelectCard);
        _inputBlocked = false;
    }

    // ── Card selection ────────────────────────────────────────────────────────

    public void SelectCard(int index)
    {
        if (!SelectionPending || _inputBlocked) return;
        if (_currentCards == null || index < 0 || index >= _currentCards.Count) return;

        _inputBlocked = true;
        if (_view != null)
            _view.InputBlocked = true;

        AudioManager.Instance?.PlayCardSelect();
        var card   = _currentCards[index];
        var deltas = CardRewardApplier.Apply(card, _player);

        StartCoroutine(AnimateOut(index, card, deltas));
    }

    IEnumerator AnimateOut(int selectedIndex, CardDefinition card, List<StatDelta> deltas)
    {
        yield return StartCoroutine(_view.DismissSelected(selectedIndex));

        if (ShouldShowResultFeedback(card))
        {
            var feedbackGo = _view.BuildFeedbackPanel(card, deltas);
            var feedbackCg = feedbackGo.GetComponent<CanvasGroup>();
            yield return StartCoroutine(FadeCanvasGroup(feedbackCg, 0f, 1f, 0.20f));
            yield return new WaitForSecondsRealtime(0.88f);
            StartCoroutine(FadeCanvasGroup(feedbackCg, 1f, 0f, 0.22f));
        }

        yield return StartCoroutine(_view.FadeOutOverlay());

        SelectionPending = false;
        Destroy(_root);
        _view    = null;
        _root    = null;
        Time.timeScale = 1f;

        if (_player != null)
        {
            var ph = _player.GetComponent<PlayerHealth>();
            ph?.GrantInvulnerability(1f);
        }

        _onSelectionComplete?.Invoke();
        _onSelectionComplete = null;
    }

    // ── Feedback gate ─────────────────────────────────────────────────────────

    static bool ShouldShowResultFeedback(CardDefinition card)
    {
        if (card == null) return false;
        return card.IsRandomLike;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static List<CardDefinition> CopiedList(List<CardDefinition> src)
    {
        var list = new List<CardDefinition>(src.Count);
        foreach (var c in src) if (c != null) list.Add(c.CreateCopy());
        return list;
    }

    static void SetVisualCategory(List<CardDefinition> cards, CardVisualCategory category)
    {
        if (cards == null) return;
        foreach (var card in cards)
            if (card != null)
                card.visualCategory = category;
    }

    IEnumerator FadeCanvasGroup(CanvasGroup g, float from, float to, float dur)
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
}
