using UnityEngine;
using UnityEngine.InputSystem;

// Sahneye yerleştirilen veya runtime'da spawn edilen Kalıntı Sandığı.
// Trigger gerekmez — mesafe bazlı tespit kullanır.
// Inspector: interactRadius ayarla (varsayılan 2.5). Sahne nesnesine Collider eklemek isteğe bağlı.
public class RelicChest : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("Sandığı açmak için gereken mesafe (birim).")]
    public float interactRadius = 2.5f;

    [Header("Placeholder Visual")]
    [Tooltip("Child visual yoksa otomatik placeholder oluşturulsun mu?")]
    public bool createPlaceholderVisual = true;
    [Tooltip("Placeholder'ın parent'a göre yerel ofseti.")]
    public Vector3 visualOffset = Vector3.zero;
    [Tooltip("Placeholder'ın genel ölçeği.")]
    public float visualScale = 1f;

    const string PlaceholderName = "RelicChestPlaceholderVisual";

    static bool _shaderWarningLogged;

    bool       _consumed;
    bool       _playerNearby;
    GameObject _playerRef;
    Texture2D  _white;
    GUIStyle   _promptStyle;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (createPlaceholderVisual && transform.Find(PlaceholderName) == null)
            BuildPlaceholder();
    }

    void Start()
    {
        var pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) _playerRef = pm.gameObject;
    }

    void Update()
    {
        if (_consumed || _playerRef == null) return;
        if (GameManager.Instance != null &&
            (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused)) return;
        if (RelicSelectionSystem.Instance != null &&
            RelicSelectionSystem.Instance.SelectionPending) return;
        // Suppress chest interaction while any card selection screen is open
        // (level-up, boss reward, or chest reward).
        if (LevelUpCardSystem.IsSelectionOpen) { _playerNearby = false; return; }

        _playerNearby = Vector3.Distance(
            transform.position,
            _playerRef.transform.position) <= interactRadius;

        if (_playerNearby &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            Open();
        }
    }

    void Open()
    {
        _consumed     = true;
        _playerNearby = false;

        RelicChestSpawnSystem.NotifyChestConsumed(this);

        var cards = CardOfferGenerator.GenerateChestOffer(3);
        if (cards.Count == 0)
        {
            Debug.LogWarning("[RelicChest] No chest cards available.");
            Destroy(gameObject);
            return;
        }

        if (LevelUpCardSystem.Instance == null)
        {
            Debug.LogWarning("[RelicChest] LevelUpCardSystem not found.");
            Destroy(gameObject);
            return;
        }

        bool opened = LevelUpCardSystem.Instance.ShowChestCards("ATLAS KALINTILARI", "Bir lütuf seç", cards);
        if (!opened)
        {
            Debug.LogWarning("[RelicChest] Chest reward screen could not open.");
            _consumed = false;
            return;
        }

        Destroy(gameObject);
    }

    // ── Placeholder builder ───────────────────────────────────────────────────

    void BuildPlaceholder()
    {
        float s = visualScale;

        var root = new GameObject(PlaceholderName);
        root.transform.SetParent(transform, false);
        root.transform.localPosition = visualOffset;
        root.transform.localScale    = Vector3.one;

        // Base — dark stone cube (0.7 × 0.45 × 0.7)
        var baseCube = CreatePrimitive(root, PrimitiveType.Cube, "Base",
            new Vector3(0f, 0.225f * s, 0f),
            new Vector3(0.70f * s, 0.45f * s, 0.70f * s),
            new Color(0.12f, 0.09f, 0.16f));

        // Lid — gold-tinted flat cube on top (0.65 × 0.12 × 0.65)
        CreatePrimitive(root, PrimitiveType.Cube, "Lid",
            new Vector3(0f, (0.45f + 0.06f) * s, 0f),
            new Vector3(0.65f * s, 0.12f * s, 0.65f * s),
            new Color(0.72f, 0.52f, 0.12f));

        // Rim detail — thin dark strip around base top edge
        CreatePrimitive(root, PrimitiveType.Cube, "Rim",
            new Vector3(0f, 0.43f * s, 0f),
            new Vector3(0.74f * s, 0.06f * s, 0.74f * s),
            new Color(0.55f, 0.38f, 0.08f));

        // Glowing orb — cyan/purple sphere floating above lid
        CreatePrimitive(root, PrimitiveType.Sphere, "Orb",
            new Vector3(0f, (0.45f + 0.12f + 0.20f) * s, 0f),
            new Vector3(0.18f * s, 0.18f * s, 0.18f * s),
            new Color(0.25f, 0.85f, 0.90f));
    }

    static GameObject CreatePrimitive(
        GameObject parent,
        PrimitiveType type,
        string objName,
        Vector3 localPos,
        Vector3 localScale,
        Color color)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = objName;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = localScale;

        // Remove collider — interaction is distance-based on the root
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            var mat = CreateBuildSafeMaterial(color);
            if (mat != null) rend.sharedMaterial = mat;
        }

        return go;
    }

    // ── Build-safe material ───────────────────────────────────────────────────

    static Material CreateBuildSafeMaterial(Color color)
    {
        string[] names = {
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Unlit/Color",
            "Sprites/Default"
        };
        Shader shader = null;
        foreach (var n in names) { shader = Shader.Find(n); if (shader != null) break; }
        if (shader == null)
        {
            if (!_shaderWarningLogged)
            { Debug.LogWarning("[RelicChest] No suitable shader found for placeholder material."); _shaderWarningLogged = true; }
            return null;
        }
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))     mat.color = color;
        return mat;
    }

    // ── "E tuşuna bas" prompt ─────────────────────────────────────────────────

    void EnsureStyles()
    {
        if (_white != null) return;

        _white = new Texture2D(1, 1);
        _white.SetPixel(0, 0, Color.white);
        _white.Apply();

        _promptStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 16,
            fontStyle = FontStyle.Bold
        };
        _promptStyle.normal.textColor = new Color(1f, 0.88f, 0.40f);
    }

    void OnGUI()
    {
        if (_consumed || !_playerNearby) return;
        if (RelicSelectionSystem.Instance != null &&
            RelicSelectionSystem.Instance.SelectionPending) return;
        // Hide the "[E] open chest" prompt while a card selection screen is up.
        if (LevelUpCardSystem.IsSelectionOpen) return;

        EnsureStyles();

        const float pw = 290f, ph = 36f;
        float px = Screen.width  * 0.5f - pw * 0.5f;
        float py = Screen.height * 0.65f;

        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.DrawTexture(new Rect(px, py, pw, ph), _white);

        GUI.color = new Color(1f, 0.78f, 0.36f, 0.72f);
        GUI.DrawTexture(new Rect(px, py, pw, 2f), _white);

        GUI.color = Color.white;
        GUI.Label(new Rect(px, py, pw, ph), "[E]  KALINTI SANDIĞINI AÇ", _promptStyle);
    }
}
