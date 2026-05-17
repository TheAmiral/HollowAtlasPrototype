using UnityEngine;

public class PortalSpawnSystem : MonoBehaviour
{
    public static PortalSpawnSystem Instance;

    public bool    PortalActive   => activePortal != null;
    public Vector3 PortalPosition => activePortal != null ? activePortal.transform.position : Vector3.zero;

    private GameObject activePortal;
    private Transform  playerTransform;
    private bool       playerEnteredPortal;
    private float      msgTimer;

    private GUIStyle msgTitleStyle;
    private GUIStyle msgSubStyle;

    private const float MsgDuration     = 5f;
    private const float TriggerRadius   = 2.8f;
    private const float MinPlayerDist   = 18f;
    private const float DefaultSpawnDist = 22f;
    private const float MinBossDist     = 8f;

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
            msgTimer -= Time.deltaTime;

        if (activePortal == null) return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            Destroy(activePortal);
            activePortal = null;
            return;
        }

        ResolvePlayer();

        if (!playerEnteredPortal && playerTransform != null)
        {
            if (Vector3.Distance(playerTransform.position, activePortal.transform.position) <= TriggerRadius)
            {
                playerEnteredPortal = true;
                msgTimer = MsgDuration;
                Debug.Log("[PortalSpawnSystem] Oyuncu portala girdi.");
            }
        }
    }

    public void OpenPortalAfterBossReward(Vector3 bossDeathPosition)
    {
        if (activePortal != null)
            Destroy(activePortal);

        playerEnteredPortal = false;
        ResolvePlayer();

        Vector3 spawnPos = FindPortalPosition(bossDeathPosition);
        activePortal = BuildPortalObject(spawnPos);

        Debug.Log($"[PortalSpawnSystem] Portal spawn: {spawnPos}  (Boss ölüm: {bossDeathPosition})");
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

    // ── Portal objesi ────────────────────────────────────────────────────────

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
        SetMaterialColor(pillar.GetComponent<Renderer>(), new Color(0.28f, 0.04f, 0.72f));

        // Tepe orb
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.transform.SetParent(portal.transform, false);
        orb.transform.localPosition = new Vector3(0f, 4.6f, 0f);
        orb.transform.localScale    = Vector3.one * 0.9f;
        Destroy(orb.GetComponent<Collider>());
        SetMaterialColor(orb.GetComponent<Renderer>(), new Color(0.62f, 0.14f, 1.0f));

        // Halka
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(portal.transform, false);
        ring.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        ring.transform.localScale    = new Vector3(2.2f, 0.08f, 2.2f);
        Destroy(ring.GetComponent<Collider>());
        SetMaterialColor(ring.GetComponent<Renderer>(), new Color(0.75f, 0.35f, 1.0f));

        return portal;
    }

    static void SetMaterialColor(Renderer rend, Color color)
    {
        if (rend == null) return;
        Material mat = rend.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
    }

    // ── Yardımcı ─────────────────────────────────────────────────────────────

    void ResolvePlayer()
    {
        if (playerTransform != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    // ── OnGUI mesajı ─────────────────────────────────────────────────────────

    void EnsureStyles()
    {
        if (msgTitleStyle != null) return;

        msgTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment  = TextAnchor.MiddleCenter,
            fontSize   = 22,
            fontStyle  = FontStyle.Bold
        };
        msgTitleStyle.normal.textColor = Color.white;

        msgSubStyle = new GUIStyle(msgTitleStyle);
        msgSubStyle.fontSize = 15;
        msgSubStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (msgTimer <= 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending) return;

        EnsureStyles();

        float alpha = Mathf.Clamp01(msgTimer / 1.2f);
        float w = 620f;
        float x = Screen.width  * 0.5f - w * 0.5f;
        float y = Screen.height * 0.5f + 80f;

        Color prev = GUI.color;

        GUI.color = new Color(0f, 0f, 0f, alpha * 0.85f);
        GUI.Label(new Rect(x + 2f, y + 2f, w, 36f), "ATLAS PORTALI BULUNDU",  msgTitleStyle);
        GUI.Label(new Rect(x + 2f, y + 43f, w, 26f), "SONRAKİ BİYOM YAKINDA", msgSubStyle);

        GUI.color = new Color(1f, 0.92f, 0.58f, alpha);
        GUI.Label(new Rect(x, y, w, 36f), "ATLAS PORTALI BULUNDU", msgTitleStyle);

        GUI.color = new Color(0.78f, 0.58f, 1f, alpha);
        GUI.Label(new Rect(x, y + 41f, w, 26f), "SONRAKİ BİYOM YAKINDA", msgSubStyle);

        GUI.color = prev;
    }
}
