using UnityEngine;
using UnityEngine.InputSystem;

public class PortalSpawnSystem : MonoBehaviour
{
    public static PortalSpawnSystem Instance;

    public bool    PortalActive   => activePortal != null;
    public Vector3 PortalPosition => activePortal != null ? activePortal.transform.position : Vector3.zero;

    private GameObject activePortal;
    private Transform  portalOrb;
    private Transform  playerTransform;
    private bool       portalActivated;
    private bool       playerInRange;
    private float      msgTimer;

    private GUIStyle msgTitleStyle;
    private GUIStyle msgSubStyle;
    private GUIStyle promptStyle;

    private const float MsgDuration      = 4f;
    private const float TriggerRadius    = 3.5f;
    private const float MinPlayerDist    = 18f;
    private const float DefaultSpawnDist = 22f;
    private const float MinBossDist      = 8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() => Instance = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (FindFirstObjectByType<AtlasResonanceHudController>() == null)
            new GameObject("AtlasResonanceHud").AddComponent<AtlasResonanceHudController>();
    }

    void Update()
    {
        if (msgTimer > 0f)
            msgTimer -= Time.unscaledDeltaTime;

        if (activePortal == null)
        {
            playerInRange = false;
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            Destroy(activePortal);
            activePortal  = null;
            portalOrb     = null;
            playerInRange = false;
            return;
        }

        AnimatePortal();
        ResolvePlayer();

        playerInRange = false;
        if (!portalActivated && playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, activePortal.transform.position);
            playerInRange = dist <= TriggerRadius;
        }

        // E etkileşimi — yalnız menzildeyken ve güvenli durumda (timeScale=1, hiçbir seçim ekranı açık değil).
        if (playerInRange && CanInteract()
            && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            ActivatePortal();
        }
    }

    // Kart / boss reward / relic / pause ekranı açıkken veya oyun donmuşken portal girişi çalışmaz.
    bool CanInteract()
    {
        if (Time.timeScale <= 0f) return false;
        if (LevelUpCardSystem.Instance    != null && LevelUpCardSystem.Instance.SelectionPending)    return false;
        if (BossRewardSystem.Instance     != null && BossRewardSystem.Instance.RewardPending)         return false;
        if (RelicSelectionSystem.Instance != null && RelicSelectionSystem.Instance.SelectionPending)  return false;
        return true;
    }

    void ActivatePortal()
    {
        portalActivated = true;
        playerInRange   = false;
        msgTimer        = MsgDuration;

        // Portal "tükenir" ve kaybolur — sahne değişmeden continuation hissi (rezonans HUD'u da kapanır).
        if (activePortal != null) Destroy(activePortal);
        activePortal = null;
        portalOrb    = null;

        Debug.Log("[Portal] Activated");
    }

    public void OpenPortalAfterBossReward(Vector3 bossDeathPosition)
    {
        if (activePortal != null)
            Destroy(activePortal);

        portalActivated = false;
        playerInRange   = false;
        ResolvePlayer();

        Vector3 spawnPos = FindPortalPosition(bossDeathPosition);
        activePortal = BuildPortalObject(spawnPos);

        Debug.Log("[Portal] Spawned");
    }

    void AnimatePortal()
    {
        if (portalOrb == null) return;

        float pulse = 0.9f + 0.12f * Mathf.Sin(Time.unscaledTime * 3.2f);
        portalOrb.localScale = Vector3.one * pulse;
        portalOrb.Rotate(0f, 55f * Time.unscaledDeltaTime, 0f, Space.Self);
    }

    // ── Pozisyon algoritması ─────────────────────────────────────────────────

    Vector3 FindPortalPosition(Vector3 bossDeathPos)
    {
        Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;

        float[] distances = { DefaultSpawnDist, 18f, 26f, 30f };
        Vector2[] dirs =
        {
            Vector2.up, Vector2.down, Vector2.right, Vector2.left,
            new Vector2( 1f,  1f).normalized,
            new Vector2( 1f, -1f).normalized,
            new Vector2(-1f,  1f).normalized,
            new Vector2(-1f, -1f).normalized,
        };

        foreach (float dist in distances)
        {
            foreach (Vector2 d in dirs)
            {
                Vector3 candidate = playerPos + new Vector3(d.x, 0f, d.y) * dist;
                float distFromBoss   = Vector3.Distance(candidate, bossDeathPos);
                float distFromPlayer = Vector3.Distance(candidate, playerPos);

                if (distFromBoss >= MinBossDist && distFromPlayer >= MinPlayerDist)
                    return candidate;
            }
        }

        // Fallback: boss'tan uzak yöne
        Vector3 away = (playerPos - bossDeathPos).normalized;
        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
        return playerPos + away * DefaultSpawnDist;
    }

    // ── Portal objesi (placeholder: mor/altın glow) ──────────────────────────

    GameObject BuildPortalObject(Vector3 position)
    {
        GameObject portal = new GameObject("AtlasPortal");
        portal.transform.position = position;

        // Ana sütun
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(portal.transform, false);
        pillar.transform.localPosition = new Vector3(0f, 2f, 0f);
        pillar.transform.localScale    = new Vector3(1.4f, 2f, 1.4f);
        Destroy(pillar.GetComponent<Collider>());
        StylePiece(pillar.GetComponent<Renderer>(), new Color(0.28f, 0.04f, 0.72f), new Color(0.22f, 0.05f, 0.55f));

        // Tepe orb (nabız atar + döner)
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.SetParent(portal.transform, false);
        orb.transform.localPosition = new Vector3(0f, 4.6f, 0f);
        orb.transform.localScale    = Vector3.one * 0.9f;
        Destroy(orb.GetComponent<Collider>());
        StylePiece(orb.GetComponent<Renderer>(), new Color(0.62f, 0.14f, 1.0f), new Color(0.95f, 0.78f, 0.45f));
        portalOrb = orb.transform;

        // Halka
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(portal.transform, false);
        ring.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        ring.transform.localScale    = new Vector3(2.2f, 0.08f, 2.2f);
        Destroy(ring.GetComponent<Collider>());
        StylePiece(ring.GetComponent<Renderer>(), new Color(0.75f, 0.35f, 1.0f), new Color(0.55f, 0.30f, 0.95f));

        return portal;
    }

    static void StylePiece(Renderer rend, Color baseColor, Color emission)
    {
        if (rend == null) return;

        Material mat = rend.material;
        if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor", baseColor);
        else if (mat.HasProperty("_Color"))  mat.SetColor("_Color", baseColor);

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
        }

        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows    = false;
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    // ── OnGUI: prompt + aktivasyon mesajı ────────────────────────────────────

    void EnsureStyles()
    {
        if (msgTitleStyle != null) return;

        msgTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 22,
            fontStyle = FontStyle.Bold
        };
        msgTitleStyle.normal.textColor = Color.white;

        msgSubStyle = new GUIStyle(msgTitleStyle) { fontSize = 15 };
        msgSubStyle.normal.textColor = Color.white;

        promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize  = 18,
            fontStyle = FontStyle.Bold
        };
        promptStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (GameManager.Instance != null && (GameManager.Instance.IsGameOver || GameManager.Instance.IsPaused))
            return;
        if (LevelUpCardSystem.Instance    != null && LevelUpCardSystem.Instance.SelectionPending)   return;
        if (BossRewardSystem.Instance     != null && BossRewardSystem.Instance.RewardPending)        return;
        if (RelicSelectionSystem.Instance != null && RelicSelectionSystem.Instance.SelectionPending) return;

        EnsureStyles();

        if (msgTimer > 0f && portalActivated)
            DrawActivationMessage();

        if (playerInRange && !portalActivated && activePortal != null)
            DrawPrompt();
    }

    void DrawPrompt()
    {
        const string text = "[E] ATLAS PORTALINI AÇ";
        float w = 360f, h = 34f;
        float x, y;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 sp = cam.WorldToScreenPoint(activePortal.transform.position + Vector3.up * 5.6f);
            if (sp.z <= 0f) return; // kameranın arkasında
            x = sp.x - w * 0.5f;
            y = Screen.height - sp.y - h * 0.5f;
        }
        else
        {
            x = Screen.width * 0.5f - w * 0.5f;
            y = Screen.height * 0.72f;
        }

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(x + 2f, y + 2f, w, h), text, promptStyle);
        GUI.color = new Color(1f, 0.90f, 0.50f, 1f);
        GUI.Label(new Rect(x, y, w, h), text, promptStyle);
        GUI.color = prev;
    }

    void DrawActivationMessage()
    {
        float alpha = Mathf.Clamp01(msgTimer / 1.2f);
        float w = 620f;
        float x = Screen.width  * 0.5f - w * 0.5f;
        float y = Screen.height * 0.5f + 80f;

        Color prev = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, alpha * 0.85f);
        GUI.Label(new Rect(x + 2f, y + 2f,  w, 36f), "ATLAS YOLU AÇILDI", msgTitleStyle);
        GUI.Label(new Rect(x + 2f, y + 43f, w, 26f), "PORTAL UYANDI",     msgSubStyle);

        GUI.color = new Color(1f, 0.92f, 0.58f, alpha);
        GUI.Label(new Rect(x, y, w, 36f), "ATLAS YOLU AÇILDI", msgTitleStyle);

        GUI.color = new Color(0.78f, 0.58f, 1f, alpha);
        GUI.Label(new Rect(x, y + 41f, w, 26f), "PORTAL UYANDI", msgSubStyle);

        GUI.color = prev;
    }
}
