using UnityEngine;

public class PlayerLevelSystem : MonoBehaviour
{
    [Header("Level Data")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 10;
    public int xpPerLevel = 5;

    [Header("Left UI")]
    public bool showLegacyHudOnGUI = false;
    public float uiX = 10f;
    public float uiY = 136f;
    public float uiWidth = 220f;
    public float uiHeight = 48f;

    public int Level => level;
    public int CurrentXP => currentXP;
    public int XPToNextLevel => xpToNextLevel;

    private AutoAttackAura aura;
    private PlayerMovement movement;
    private PlayerHealth playerHealth;

    [Header("Level Up Popup")]
    public float levelUpPopupDuration = 1.8f;
    public float levelUpPopupOffsetY = -40f;

    private GUIStyle levelUpStyle;
    private GUIStyle levelUpShadowStyle;
    private float levelUpPopupTimer;
    private int latestLevelPopup = 1;

    void Start()
    {
        level = 1;
        currentXP = 0;

        // Hollow Atlas XP tablosu:
        // Oyun her başladığında XP bar boş başlar ve level'a göre doğru eşik atanır.
        xpToNextLevel = GetXPRequirementForLevel(level);

        // Eski inspector değerleri çok yüksek kaydedildiyse güvenli aralığa çek.
        xpPerLevel = Mathf.Max(1, xpPerLevel);
        if (xpPerLevel > 50)
            xpPerLevel = 5;

        FindPlayerRefs();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (levelUpPopupTimer > 0f)
            levelUpPopupTimer = Mathf.Max(0f, levelUpPopupTimer - Time.deltaTime);
    }

    public void AddXP(int amount)
    {
        AddExperience(amount);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;

            // Hollow Atlas dokümanındaki XP eğrisine göre yeni level eşiğini belirle.
            xpToNextLevel = GetXPRequirementForLevel(level);

            latestLevelPopup = level;

            // Kart sistemi aktifse eski OnGUI level popup'ı yerine kart seçim ekranı kullanılır.
            if (LevelUpCardSystem.Instance == null)
                levelUpPopupTimer = levelUpPopupDuration;
            else
                levelUpPopupTimer = 0f;

            ApplyLevelReward();
        }
    }

    public void FillXPToNextLevel()
    {
        int neededXP = xpToNextLevel - currentXP;

        if (neededXP <= 0)
            neededXP = xpToNextLevel;

        AddExperience(neededXP);
    }

    int GetXPRequirementForLevel(int currentLevel)
    {
        int[] xpTable =
        {
            0,
            10,   // Level 1 -> 2
            15,   // Level 2 -> 3
            22,   // Level 3 -> 4
            30,   // Level 4 -> 5
            40,   // Level 5 -> 6
            52,   // Level 6 -> 7
            66,   // Level 7 -> 8
            82,   // Level 8 -> 9
            100   // Level 9 -> 10
        };

        if (currentLevel > 0 && currentLevel < xpTable.Length)
            return xpTable[currentLevel];

        // Level 10 sonrası için fallback formül.
        return Mathf.Max(1, 10 + currentLevel * currentLevel * 2);
    }

    void ApplyLevelReward()
    {
        // Kart sistemi varsa kart seçim ekranını aç.
        if (LevelUpCardSystem.Instance != null)
        {
            LevelUpCardSystem.Instance.TriggerSelection(level);
            return;
        }

        // Fallback: kart sistemi sahnede yoksa direkt küçük stat artışı uygula.
        FindPlayerRefs();

        if (aura != null)
        {
            aura.damage += 1;
            aura.radius += 0.05f;
        }

        if (movement != null)
            movement.moveSpeed += 0.08f;

        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(2);
            playerHealth.Heal(2);
        }
    }

    void FindPlayerRefs()
    {
        if (aura == null) aura = GetComponent<AutoAttackAura>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (playerHealth == null) playerHealth = GetComponent<PlayerHealth>();
    }

    void EnsureStyles()
    {
        if (levelUpStyle != null)
            return;

        levelUpStyle = new GUIStyle(GUI.skin.label);
        levelUpStyle.alignment = TextAnchor.MiddleCenter;
        levelUpStyle.fontSize = 72;
        levelUpStyle.fontStyle = FontStyle.Bold;
        levelUpStyle.normal.textColor = new Color(1f, 0.92f, 0.3f, 1f);

        levelUpShadowStyle = new GUIStyle(levelUpStyle);
        levelUpShadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
    }

    void OnGUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending)
            return;

        if (LevelUpCardSystem.Instance != null)
            levelUpPopupTimer = 0f;

        if (showLegacyHudOnGUI)
        {
            GUI.Box(new Rect(uiX, uiY, uiWidth, uiHeight), "Level");
            GUI.Label(new Rect(uiX + 10f, uiY + 18f, uiWidth - 20f, 18f), $"Level: {level}");
            GUI.Label(new Rect(uiX + 110f, uiY + 18f, uiWidth - 120f, 18f), $"XP: {currentXP}/{xpToNextLevel}");
        }

        if (levelUpPopupTimer <= 0f)
            return;

        EnsureStyles();

        float normalized = Mathf.Clamp01(levelUpPopupTimer / Mathf.Max(0.01f, levelUpPopupDuration));
        float alpha = Mathf.SmoothStep(0f, 1f, normalized);

        levelUpStyle.normal.textColor = new Color(1f, 0.92f, 0.3f, alpha);
        levelUpShadowStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.9f);

        float width = Screen.width;
        float height = 120f;
        float y = Screen.height * 0.5f + levelUpPopupOffsetY;
        string popupText = $"LEVEL {latestLevelPopup}";

        GUI.Label(new Rect(4f, y + 4f, width, height), popupText, levelUpShadowStyle);
        GUI.Label(new Rect(0f, y, width, height), popupText, levelUpStyle);
    }
}