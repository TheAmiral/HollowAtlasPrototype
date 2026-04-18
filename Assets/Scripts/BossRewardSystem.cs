using UnityEngine;
using UnityEngine.InputSystem;

public class BossRewardSystem : MonoBehaviour
{
    public static BossRewardSystem Instance;

    public bool rewardPending;
    public bool rewardTaken;

    public bool RewardPending => rewardPending;

    private AutoAttackAura aura;
    private PlayerMovement movement;
    private PlayerHealth playerHealth;
    private GoldWallet goldWallet;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardBodyStyle;
    private GUIStyle shadowStyle;
    private GUIStyle hintStyle;

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
        rewardPending = false;
        rewardTaken = false;
        FindPlayerComponents();
    }

    void Update()
    {
        if (playerHealth == null || aura == null || movement == null || goldWallet == null)
            FindPlayerComponents();

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            rewardPending = false;
            return;
        }

        if (playerHealth != null && playerHealth.IsDead)
        {
            rewardPending = false;
            return;
        }

        if (!rewardTaken && !rewardPending && BossSpawnSystem.Instance != null)
        {
            if (BossSpawnSystem.Instance.BossSpawned && BossSpawnSystem.Instance.BossDefeated)
            {
                rewardPending = true;
                Time.timeScale = 0f;
            }
        }

        if (!rewardPending || Keyboard.current == null)
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

    void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        aura = player.GetComponent<AutoAttackAura>();
        movement = player.GetComponent<PlayerMovement>();
        playerHealth = player.GetComponent<PlayerHealth>();
        goldWallet = player.GetComponent<GoldWallet>();
    }

    void ApplyReward(int option)
    {
        FindPlayerComponents();

        switch (option)
        {
            case 1:
                ApplyAtlasFury();
                break;
            case 2:
                ApplyPhantomStep();
                break;
            case 3:
                ApplyVitalCore();
                break;
        }

        rewardPending = false;
        rewardTaken = true;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            Time.timeScale = 1f;
    }

    void ApplyAtlasFury()
    {
        if (aura != null)
        {
            aura.damage += 4;
            aura.radius += 0.25f;
        }

        if (goldWallet != null)
            goldWallet.AddGold(15);

        Debug.Log("Boss ödülü seçildi: Atlas Fury");
    }

    void ApplyPhantomStep()
    {
        if (movement != null)
        {
            movement.moveSpeed += 0.8f;
            movement.dashCooldown = Mathf.Max(0.25f, movement.dashCooldown - 0.15f);
            movement.dashSpeed += 2f;
        }

        Debug.Log("Boss ödülü seçildi: Phantom Step");
    }

    void ApplyVitalCore()
    {
        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(25);
            playerHealth.Heal(25);
        }

        if (goldWallet != null)
            goldWallet.AddGold(10);

        Debug.Log("Boss ödülü seçildi: Vital Core");
    }

    void EnsureStyles()
    {
        if (titleStyle != null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 17;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;

        shadowStyle = new GUIStyle(titleStyle);
        shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.alignment = TextAnchor.MiddleLeft;
        subtitleStyle.fontSize = 12;
        subtitleStyle.fontStyle = FontStyle.Bold;
        subtitleStyle.normal.textColor = Color.white;

        cardTitleStyle = new GUIStyle(GUI.skin.label);
        cardTitleStyle.alignment = TextAnchor.UpperLeft;
        cardTitleStyle.fontSize = 12;
        cardTitleStyle.fontStyle = FontStyle.Bold;
        cardTitleStyle.normal.textColor = Color.white;
        cardTitleStyle.wordWrap = true;

        cardBodyStyle = new GUIStyle(GUI.skin.label);
        cardBodyStyle.alignment = TextAnchor.UpperLeft;
        cardBodyStyle.fontSize = 11;
        cardBodyStyle.fontStyle = FontStyle.Bold;
        cardBodyStyle.normal.textColor = Color.white;
        cardBodyStyle.wordWrap = true;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.alignment = TextAnchor.MiddleCenter;
        hintStyle.fontSize = 11;
        hintStyle.fontStyle = FontStyle.Bold;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.9f);
    }

    void OnGUI()
    {
        if (!rewardPending)
            return;

        EnsureStyles();

        float panelWidth = 620f;
        float panelHeight = 185f;
        float x = Screen.width * 0.5f - panelWidth * 0.5f;
        float y = Screen.height * 0.5f - panelHeight * 0.5f;

        GUI.Box(new Rect(x, y, panelWidth, panelHeight), "");

        GUI.Label(new Rect(x + 2f, y + 8f, panelWidth, 24f), "BOSS ODULU", shadowStyle);
        GUI.Label(new Rect(x, y + 6f, panelWidth, 24f), "BOSS ODULU", titleStyle);

        GUI.Label(new Rect(x + 18f, y + 34f, panelWidth - 36f, 18f), "Bir guclendirme sec:", subtitleStyle);

        DrawOptionBox(
            x + 14f, y + 58f, 184f, 96f,
            "1 - ATLAS FURY",
            "+4 aura hasari\n+0.25 aura yaricapi\n+15 gold"
        );

        DrawOptionBox(
            x + 218f, y + 58f, 184f, 96f,
            "2 - PHANTOM STEP",
            "+0.8 hareket hizi\n-0.15 dash bekleme\n+2 dash hizi"
        );

        DrawOptionBox(
            x + 422f, y + 58f, 184f, 96f,
            "3 - VITAL CORE",
            "+25 maksimum can\n+25 iyilesme\n+10 gold"
        );

        GUI.Label(new Rect(x, y + 160f, panelWidth, 16f), "[1] [2] [3] ile sec", hintStyle);
    }

    void DrawOptionBox(float x, float y, float width, float height, string title, string body)
    {
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Label(new Rect(x + 10f, y + 8f, width - 20f, 20f), title, cardTitleStyle);
        GUI.Label(new Rect(x + 10f, y + 30f, width - 20f, height - 36f), body, cardBodyStyle);
    }
}