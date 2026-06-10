using UnityEngine;
using UnityEngine.UI;

// Atlas Rezonans göstergesi — sağ-orta konumunda bağımsız panel.
// sortingOrder 130: kontrat panelinin (120) ve ana HUD'ın (100) önünde.
// Portal aktif olduğunda görünür; oyun bitti / seçim ekranı açık ise gizlenir.
public class AtlasResonanceHudController : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Image       borderImage;
    private Text        labelText;
    private Text        levelText;
    private Transform   playerTransform;

    private const float DistFar  = 25f;
    private const float DistNear = 12f;

    // Border renkleri
    private static readonly Color BorderWeak   = new Color32(0x66, 0x55, 0x88, 0xB4);
    private static readonly Color BorderMid    = new Color32(0x88, 0x44, 0xCC, 0xFF);
    private static readonly Color BorderStrong = new Color32(0x22, 0xEE, 0xFF, 0xFF);

    // Seviye yazı renkleri
    private static readonly Color LevelColorWeak   = new Color32(0xA0, 0x90, 0xCC, 0xFF);
    private static readonly Color LevelColorMid    = new Color32(0xCC, 0x88, 0xFF, 0xFF);
    private static readonly Color LevelColorStrong = new Color32(0x44, 0xFF, 0xFF, 0xFF);

    void Awake()
    {
        BuildCanvas();
    }

    void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 130;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // CanvasGroup controls visibility of the entire resonance HUD
        canvasGroup                = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha          = 0f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform root = GetComponent<RectTransform>();
        BuildPanel(root, font);
    }

    void BuildPanel(RectTransform root, Font font)
    {
        const float W = 300f;
        const float H = 68f;

        // ── Dış border paneli ─────────────────────────────────────────────────
        GameObject borderGO = new GameObject("Resonance_Border", typeof(RectTransform), typeof(Image));
        borderGO.transform.SetParent(root, false);
        RectTransform borderRect = borderGO.GetComponent<RectTransform>();
        borderRect.anchorMin        = new Vector2(1f, 0.5f);
        borderRect.anchorMax        = new Vector2(1f, 0.5f);
        borderRect.pivot            = new Vector2(1f, 0.5f);
        borderRect.anchoredPosition = new Vector2(-32f, 0f);
        borderRect.sizeDelta        = new Vector2(W, H);
        borderImage                 = borderGO.GetComponent<Image>();
        borderImage.color           = BorderWeak;
        borderImage.raycastTarget   = false;

        // ── Koyu iç arka plan ─────────────────────────────────────────────────
        GameObject bgGO = new GameObject("Resonance_Bg", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(borderRect, false);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(2f,  2f);
        bgRect.offsetMax = new Vector2(-2f, -2f);
        Image bgImg = bgGO.GetComponent<Image>();
        bgImg.color          = new Color32(0x10, 0x08, 0x1E, 0xEE);
        bgImg.raycastTarget  = false;

        // ── Üst etiket: "ATLAS REZONANSI" ────────────────────────────────────
        GameObject labelGO = new GameObject("Resonance_Label", typeof(RectTransform), typeof(Text));
        labelGO.transform.SetParent(borderRect, false);
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin        = new Vector2(0f, 0.5f);
        labelRect.anchorMax        = new Vector2(1f, 0.5f);
        labelRect.pivot            = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 14f);
        labelRect.sizeDelta        = new Vector2(0f, 18f);
        labelText                   = labelGO.GetComponent<Text>();
        labelText.font              = font;
        labelText.text              = "ATLAS REZONANSI";
        labelText.fontSize          = 11;
        labelText.fontStyle         = FontStyle.Bold;
        labelText.alignment         = TextAnchor.MiddleCenter;
        labelText.color             = new Color(0.75f, 0.65f, 0.92f, 0.72f);
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow   = VerticalWrapMode.Overflow;
        labelText.raycastTarget      = false;

        // ── Alt seviye yazısı: "ZAYIF" / "ORTA" / "GÜÇLÜ" ───────────────────
        GameObject levelGO = new GameObject("Resonance_Level", typeof(RectTransform), typeof(Text));
        levelGO.transform.SetParent(borderRect, false);
        RectTransform levelRect = levelGO.GetComponent<RectTransform>();
        levelRect.anchorMin        = new Vector2(0f, 0.5f);
        levelRect.anchorMax        = new Vector2(1f, 0.5f);
        levelRect.pivot            = new Vector2(0.5f, 0.5f);
        levelRect.anchoredPosition = new Vector2(0f, -12f);
        levelRect.sizeDelta        = new Vector2(0f, 26f);
        levelText                   = levelGO.GetComponent<Text>();
        levelText.font              = font;
        levelText.text              = "◦  ZAYIF";
        levelText.fontSize          = 18;
        levelText.fontStyle         = FontStyle.Bold;
        levelText.alignment         = TextAnchor.MiddleCenter;
        levelText.color             = LevelColorWeak;
        levelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        levelText.verticalOverflow   = VerticalWrapMode.Overflow;
        levelText.raycastTarget      = false;
        levelGO.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.85f);
    }

    void Update()
    {
        if (canvasGroup == null) return;

        ResolvePlayer();

        bool portalActive = PortalSpawnSystem.Instance != null
                         && PortalSpawnSystem.Instance.PortalActive;
        bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        bool paused   = GameManager.Instance != null && GameManager.Instance.IsPaused;
        bool selectionOpen =
            (LevelUpCardSystem.Instance    != null && LevelUpCardSystem.Instance.SelectionPending)   ||
            (RelicSelectionSystem.Instance != null && RelicSelectionSystem.Instance.SelectionPending) ||
            (BossRewardSystem.Instance     != null && BossRewardSystem.Instance.RewardPending);

        if (!portalActive || playerTransform == null || gameOver || paused || selectionOpen)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        canvasGroup.alpha = 1f;

        float dist = Vector3.Distance(
            playerTransform.position,
            PortalSpawnSystem.Instance.PortalPosition);

        if (dist > DistFar)
        {
            if (borderImage != null) borderImage.color = BorderWeak;
            if (levelText  != null)
            {
                levelText.text  = "◦  ZAYIF";
                levelText.color = LevelColorWeak;
            }
        }
        else if (dist > DistNear)
        {
            if (borderImage != null) borderImage.color = BorderMid;
            if (levelText  != null)
            {
                levelText.text  = "◈  ORTA";
                levelText.color = LevelColorMid;
            }
        }
        else
        {
            if (borderImage != null) borderImage.color = BorderStrong;
            if (levelText  != null)
            {
                levelText.text  = "◈◈  GÜÇLÜ";
                levelText.color = LevelColorStrong;
            }
        }
    }

    void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }
}
