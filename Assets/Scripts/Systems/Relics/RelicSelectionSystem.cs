using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RelicSelectionSystem : MonoBehaviour
{
    public static RelicSelectionSystem Instance { get; private set; }
    public bool SelectionPending { get; private set; }

    Canvas                _canvas;
    RelicSelectionView    _view;
    GameObject            _player;
    List<RelicDefinition> _currentOffers;
    bool                  _rerollRequested;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }
        RelicInventory.EnsureInstance();
    }

    void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame && !SelectionPending)
            TriggerRelicSelection();
#endif
    }

    public void TriggerRelicSelection(GameObject player = null)
    {
        if (SelectionPending) return;
        if (player != null) _player = player;
        if (_player == null) _player = FindPlayer();

        _currentOffers = RelicOfferGenerator.Generate(3);
        if (_currentOffers == null || _currentOffers.Count == 0) return;

        StartCoroutine(RunSelection());
    }

    public void ForceClose()
    {
        StopAllCoroutines();
        CleanupCanvas();
        if (SelectionPending)
        {
            Time.timeScale   = 1f;
            SelectionPending = false;
        }
    }

    // ── Core selection loop ───────────────────────────────────────────────────

    IEnumerator RunSelection()
    {
        SelectionPending = true;
        Time.timeScale   = 0f;

        while (true)
        {
            _rerollRequested = false;

            EnsureCanvas();
            _view = _canvas.gameObject.AddComponent<RelicSelectionView>();

            int  selectedIndex = -1;
            bool chosen        = false;
            _view.Setup(_currentOffers,
                idx => { selectedIndex = idx; chosen = true; },
                ()  => _rerollRequested = true);

            while (!chosen && !_rerollRequested)
                yield return null;

            if (_rerollRequested)
            {
                if (_view != null)
                    yield return StartCoroutine(_view.Dismiss(-1));
                CleanupCanvas();
                _view          = null;
                _currentOffers = RelicOfferGenerator.Generate(3);
                if (_currentOffers == null || _currentOffers.Count == 0) break;
                continue;
            }

            if (_view != null)
                yield return StartCoroutine(_view.Dismiss(selectedIndex));

            if (selectedIndex >= 0 && selectedIndex < _currentOffers.Count)
                RelicRewardApplier.Apply(_currentOffers[selectedIndex], _player);
            break;
        }

        CleanupCanvas();
        Time.timeScale   = 1f;
        SelectionPending = false;
    }

    // ── Canvas helpers ────────────────────────────────────────────────────────

    void EnsureCanvas()
    {
        if (_canvas != null) return;
        var go = new GameObject("RelicSelectionCanvas");
        DontDestroyOnLoad(go);
        _canvas              = go.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 210;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
    }

    void CleanupCanvas()
    {
        if (_canvas == null) return;
        Destroy(_canvas.gameObject);
        _canvas = null;
        _view   = null;
    }

    static GameObject FindPlayer()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        return pm != null ? pm.gameObject : null;
    }
}
