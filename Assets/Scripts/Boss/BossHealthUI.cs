using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private bool showLegacyOnGUI = false;

    public Color panelColor = new Color(0f, 0f, 0f, 0.35f);
    public Color barBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
    public Color barFillColor = new Color(0.85f, 0.12f, 0.12f, 1f);

    [Header("Layout")]
    public float topMargin = 6f;
    public float reservedLeftWidth = 230f;
    public float reservedRightWidth = 260f;
    public float preferredBarWidth = 520f;
    public float minBarWidth = 320f;
    public float barHeight = 28f;

    [Header("Hit Pulse")]
    public float pulseDuration = 0.12f;
    public float pulseMaxAlpha = 0.38f;

    private Texture2D whiteTexture;
    private GUIStyle bossNameStyle;
    private GUIStyle hpStyle;

    private EnemyHealth trackedBoss;
    private int previousHealth = -1;
    private float pulseTimer;

    void Awake()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
    }

    void Update()
    {
        if (pulseTimer > 0f)
        {
            pulseTimer -= Time.deltaTime;
            if (pulseTimer < 0f)
                pulseTimer = 0f;
        }

        if (BossSpawnSystem.Instance == null)
        {
            trackedBoss = null;
            previousHealth = -1;
            return;
        }

        EnemyHealth boss = BossSpawnSystem.Instance.ActiveBoss;

        if (boss == null || boss.IsDead || !boss.IsBoss)
        {
            trackedBoss = null;
            previousHealth = -1;
            return;
        }

        if (trackedBoss != boss)
        {
            trackedBoss = boss;
            previousHealth = boss.CurrentHealth;
            pulseTimer = 0f;
            return;
        }

        if (previousHealth >= 0 && boss.CurrentHealth < previousHealth)
            pulseTimer = pulseDuration;

        previousHealth = boss.CurrentHealth;
    }

    void EnsureStyles()
    {
        if (bossNameStyle != null)
            return;

        bossNameStyle = new GUIStyle(GUI.skin.label);
        bossNameStyle.alignment = TextAnchor.MiddleCenter;
        bossNameStyle.fontSize = 17;
        bossNameStyle.fontStyle = FontStyle.Bold;
        bossNameStyle.normal.textColor = Color.white;

        hpStyle = new GUIStyle(GUI.skin.label);
        hpStyle.alignment = TextAnchor.MiddleRight;
        hpStyle.fontSize = 12;
        hpStyle.fontStyle = FontStyle.Bold;
        hpStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (!showLegacyOnGUI)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
            return;

        EnsureStyles();

        if (BossSpawnSystem.Instance == null)
            return;

        EnemyHealth boss = BossSpawnSystem.Instance.ActiveBoss;
        if (boss == null || boss.IsDead || !boss.IsBoss)
            return;

        float safeAreaX = reservedLeftWidth;
        float safeAreaWidth = Screen.width - reservedLeftWidth - reservedRightWidth;

        if (safeAreaWidth < minBarWidth)
        {
            safeAreaX = 20f;
            safeAreaWidth = Screen.width - 40f;
        }

        float totalWidth = Mathf.Clamp(preferredBarWidth, minBarWidth, safeAreaWidth - 20f);
        float x = safeAreaX + (safeAreaWidth - totalWidth) * 0.5f;
        float y = topMargin;

        Color oldColor = GUI.color;

        GUI.color = panelColor;
        GUI.DrawTexture(new Rect(x - 8f, y - 4f, totalWidth + 16f, barHeight + 8f), whiteTexture);

        GUI.color = barBackgroundColor;
        GUI.DrawTexture(new Rect(x, y, totalWidth, barHeight), whiteTexture);

        float fillPercent = Mathf.Clamp01((float)boss.CurrentHealth / boss.MaxHealth);
        float fillWidth = totalWidth * fillPercent;

        GUI.color = barFillColor;
        GUI.DrawTexture(new Rect(x, y, fillWidth, barHeight), whiteTexture);

        DrawHitPulse(x, y, fillWidth);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, totalWidth, barHeight), boss.BossDisplayName.ToUpper(), bossNameStyle);
        GUI.Label(new Rect(x + totalWidth - 110f, y, 100f, barHeight), $"{boss.CurrentHealth} / {boss.MaxHealth}", hpStyle);

        GUI.color = oldColor;
    }

    void DrawHitPulse(float x, float y, float fillWidth)
    {
        if (pulseTimer <= 0f || fillWidth <= 0f)
            return;

        float pulse01 = Mathf.Clamp01(pulseTimer / pulseDuration);
        float alpha = pulse01 * pulseMaxAlpha;

        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(new Rect(x, y, fillWidth, barHeight), whiteTexture);
    }

    void OnDestroy()
    {
        if (whiteTexture != null)
            Destroy(whiteTexture);
    }
}