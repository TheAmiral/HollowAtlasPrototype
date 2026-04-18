using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLevelSystem : MonoBehaviour
{
    [Header("Level Data")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 5;
    public int xpGrowthPerLevel = 4;

    [Header("Left UI")]
    public float uiX = 10f;
    public float uiY = 136f;
    public float uiWidth = 220f;
    public float uiHeight = 48f;

    public int Level => level;
    public int CurrentXP => currentXP;
    public int XPToNextLevel => xpToNextLevel;

    private int pendingLevelRewards;

    private AutoAttackAura aura;
    private PlayerMovement movement;
    private PlayerHealth playerHealth;

    private GUIStyle popupTitleStyle;
    private GUIStyle popupBodyStyle;
    private GUIStyle popupHintStyle;

    void Start()
    {
        FindPlayerRefs();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
            return;

        if (pendingLevelRewards <= 0)
            return;

        if (Time.timeScale != 0f)
            Time.timeScale = 0f;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            ApplyReward(1);
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            ApplyReward(2);
            return;
        }

        if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            ApplyReward(3);
            return;
        }
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
            xpToNextLevel += xpGrowthPerLevel;
            pendingLevelRewards++;
        }
    }

    void ApplyReward(int option)
    {
        FindPlayerRefs();

        switch (option)
        {
            case 1:
                if (aura != null)
                {
                    aura.damage += 2;
                    aura.radius += 0.15f;
                }
                break;

            case 2:
                if (movement != null)
                {
                    movement.moveSpeed += 0.4f;
                }
                break;

            case 3:
                if (playerHealth != null)
                {
                    playerHealth.IncreaseMaxHealth(10);
                    playerHealth.Heal(10);
                }
                break;
        }

        pendingLevelRewards--;

        if (pendingLevelRewards <= 0)
        {
            pendingLevelRewards = 0;

            if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
                Time.timeScale = 1f;
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
        if (popupTitleStyle != null)
            return;

        popupTitleStyle = new GUIStyle(GUI.skin.label);
        popupTitleStyle.alignment = TextAnchor.MiddleCenter;
        popupTitleStyle.fontSize = 16;
        popupTitleStyle.fontStyle = FontStyle.Bold;
        popupTitleStyle.normal.textColor = Color.white;

        popupBodyStyle = new GUIStyle(GUI.skin.label);
        popupBodyStyle.alignment = TextAnchor.UpperLeft;
        popupBodyStyle.fontSize = 12;
        popupBodyStyle.fontStyle = FontStyle.Bold;
        popupBodyStyle.normal.textColor = Color.white;
        popupBodyStyle.wordWrap = true;

        popupHintStyle = new GUIStyle(GUI.skin.label);
        popupHintStyle.alignment = TextAnchor.MiddleCenter;
        popupHintStyle.fontSize = 11;
        popupHintStyle.fontStyle = FontStyle.Bold;
        popupHintStyle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        GUI.Box(new Rect(uiX, uiY, uiWidth, uiHeight), "Level");
        GUI.Label(new Rect(uiX + 10f, uiY + 18f, uiWidth - 20f, 18f), $"Level: {level}");
        GUI.Label(new Rect(uiX + 110f, uiY + 18f, uiWidth - 120f, 18f), $"XP: {currentXP}/{xpToNextLevel}");

        if (pendingLevelRewards <= 0)
            return;

        if (BossRewardSystem.Instance != null && BossRewardSystem.Instance.RewardPending)
            return;

        EnsureStyles();

        float panelWidth = 360f;
        float panelHeight = 150f;
        float x = Screen.width * 0.5f - panelWidth * 0.5f;
        float y = Screen.height * 0.5f - panelHeight * 0.5f;

        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");
        GUI.Label(new Rect(x, y + 8f, panelWidth, 22f), "LEVEL UP!", popupTitleStyle);
        GUI.Label(new Rect(x + 16f, y + 36f, panelWidth - 32f, 18f), "Bir güçlendirme seç:", popupBodyStyle);
        GUI.Label(new Rect(x + 16f, y + 60f, panelWidth - 32f, 18f), "1 - Aura güçlenir (+hasar, +menzil)", popupBodyStyle);
        GUI.Label(new Rect(x + 16f, y + 84f, panelWidth - 32f, 18f), "2 - Hareket hızı artar", popupBodyStyle);
        GUI.Label(new Rect(x + 16f, y + 108f, panelWidth - 32f, 18f), "3 - Maksimum can ve iyileşme", popupBodyStyle);
        GUI.Label(new Rect(x, y + 128f, panelWidth, 16f), "[1] [2] [3] ile seç", popupHintStyle);
    }
}