using UnityEngine;

public class BossSpawnSystem : MonoBehaviour
{
    public static BossSpawnSystem Instance;

    public GameObject miniBossPrefab;
    public Transform player;

    public float firstBossTime = 60f;
    public float spawnDistance = 14f;
    public float warningDuration = 3f;
    public float bossSpawnHeight = 1f;

    public EnemyHealth ActiveBoss => activeBoss;
    public bool BossSpawned => bossSpawned;
    public bool BossDefeated => bossDefeated;

    private EnemyHealth activeBoss;
    private bool bossSpawned;
    private bool bossDefeated;
    private float warningTimer;
    private string warningMessage = "";

    private GUIStyle centerWarningStyle;
    private GUIStyle centerWarningShadowStyle;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (warningTimer > 0f)
            warningTimer -= Time.deltaTime;

        if (!bossSpawned)
        {
            float elapsedTime = 0f;

            if (GameManager.Instance != null)
                elapsedTime = GameManager.Instance.ElapsedTime;

            if (elapsedTime >= firstBossTime)
                SpawnBoss();

            return;
        }

        if (!bossDefeated && activeBoss == null)
        {
            bossDefeated = true;
            warningMessage = "MINI BOSS YENILDI";
            warningTimer = warningDuration;
            Debug.Log("Mini Boss yenildi!");
        }
    }

    void SpawnBoss()
    {
        if (miniBossPrefab == null || player == null)
            return;

        Vector2 circle = Random.insideUnitCircle.normalized;
        if (circle == Vector2.zero)
            circle = Vector2.right;

        Vector3 spawnPosition = player.position + new Vector3(circle.x, 0f, circle.y) * spawnDistance;
        spawnPosition.y = bossSpawnHeight;

        GameObject bossObject = Instantiate(miniBossPrefab, spawnPosition, Quaternion.identity);

        activeBoss = bossObject.GetComponent<EnemyHealth>();
        bossSpawned = true;
        bossDefeated = false;

        warningMessage = "MINI BOSS GELDI";
        warningTimer = warningDuration;

        Debug.Log("Mini Boss doğdu!");
    }

    void EnsureGuiStyles()
    {
        if (centerWarningStyle != null)
            return;

        centerWarningStyle = new GUIStyle(GUI.skin.label);
        centerWarningStyle.alignment = TextAnchor.MiddleCenter;
        centerWarningStyle.fontSize = 24;
        centerWarningStyle.fontStyle = FontStyle.Bold;
        centerWarningStyle.normal.textColor = Color.white;

        centerWarningShadowStyle = new GUIStyle(centerWarningStyle);
        centerWarningShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
    }

    void OnGUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
            return;

        EnsureGuiStyles();

        float rightPanelX = Screen.width - 260f;

        if (!bossSpawned)
        {
            float elapsedTime = 0f;

            if (GameManager.Instance != null)
                elapsedTime = GameManager.Instance.ElapsedTime;

            float remaining = Mathf.Max(0f, firstBossTime - elapsedTime);

            GUI.Box(new Rect(rightPanelX, 120f, 250f, 60f), "Mini Boss");
            GUI.Label(new Rect(rightPanelX + 10f, 145f, 220f, 20f), $"{remaining:0.0}s sonra geliyor");
        }

        if (bossSpawned && !bossDefeated && activeBoss != null)
        {
            GUI.Box(new Rect(rightPanelX, 190f, 250f, 55f), "Mini Boss");
            GUI.Label(new Rect(rightPanelX + 10f, 215f, 220f, 20f), "Haritada aktif");
        }

        DrawCenterWarning();
    }

    void DrawCenterWarning()
    {
        if (warningTimer <= 0f || string.IsNullOrEmpty(warningMessage))
            return;

        float normalized = Mathf.Clamp01(warningTimer / warningDuration);
        float alpha = GetFadeAlpha(normalized);

        Color mainColor = bossDefeated
            ? new Color(1f, 0.9f, 0.35f, alpha)
            : new Color(1f, 0.35f, 0.35f, alpha);

        Color shadowColor = new Color(0f, 0f, 0f, alpha * 0.9f);

        centerWarningStyle.normal.textColor = mainColor;
        centerWarningShadowStyle.normal.textColor = shadowColor;

        float width = 560f;
        float height = 64f;
        float x = Screen.width * 0.5f - width * 0.5f;
        float y = Screen.height * 0.5f - height * 0.5f - 55f;

        GUI.Label(new Rect(x + 3f, y + 3f, width, height), warningMessage, centerWarningShadowStyle);
        GUI.Label(new Rect(x, y, width, height), warningMessage, centerWarningStyle);
    }

    float GetFadeAlpha(float normalized)
    {
        if (normalized > 0.66f)
            return Mathf.Lerp(0f, 1f, (1f - normalized) / 0.34f);

        if (normalized > 0.33f)
            return 1f;

        return Mathf.Lerp(1f, 0f, (0.33f - normalized) / 0.33f);
    }
}