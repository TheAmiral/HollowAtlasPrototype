using UnityEngine;
using System.Collections.Generic;

public class BossRewardSystem : MonoBehaviour
{
    public static BossRewardSystem Instance;

    public bool rewardPending;
    public bool rewardTaken;

    public bool RewardPending => rewardPending;

    private AutoAttackAura playerAura;
    private PlayerMovement playerMovement;
    private PlayerHealth   playerHealth;
    private GoldWallet     goldWallet;

    private bool rewardUiOpened;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        rewardPending  = false;
        rewardTaken    = false;
        rewardUiOpened = false;
        FindComponents();
    }

    void Update()
    {
        if (playerHealth == null || playerAura == null || playerMovement == null || goldWallet == null)
            FindComponents();

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) { rewardPending = false; return; }
        if (playerHealth != null && playerHealth.IsDead)                      { rewardPending = false; return; }
        if (rewardTaken || rewardUiOpened || BossSpawnSystem.Instance == null) return;
        if (!BossSpawnSystem.Instance.BossSpawned || !BossSpawnSystem.Instance.BossDefeated) return;
        if (LevelUpCardSystem.Instance == null || LevelUpCardSystem.Instance.SelectionPending) return;

        bool opened = LevelUpCardSystem.Instance.ShowCustomCards(
            "ATLAS ÖDÜLÜ",
            "Muhafızın gücünden bir kalıntı seç",
            BuildRewardCards(),
            OnSelectionCompleted
        );

        if (opened) { rewardPending = true; rewardUiOpened = true; AudioManager.Instance?.PlayBossReward(); }
    }

    void FindComponents()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        playerAura     = player.GetComponent<AutoAttackAura>();
        playerMovement = player.GetComponent<PlayerMovement>();
        playerHealth   = player.GetComponent<PlayerHealth>();
        goldWallet     = player.GetComponent<GoldWallet>();
    }

    List<CardDefinition> BuildRewardCards() => new List<CardDefinition>
    {
        new CardDefinition
        {
            id            = "boss_muhafiz_muhru",
            title         = "Muhafızın Mührü",
            description   = "Atlas Muhafızı'nın enerjisi saldırılarını güçlendirir.",
            effectPreview = "+8 Aura Hasar  +0.35 Aura Menzili  +35 Gold",
            cardClass     = CardClass.Atlas,
            rarity        = CardRarity.Epic,
            tags          = CardTag.Aura | CardTag.Defense,
            Apply         = _ => ApplyReward(1)
        },
        new CardDefinition
        {
            id            = "boss_golge_adim",
            title         = "Gölge Adım",
            description   = "Boss savaşından öğrendiğin ritimle daha hızlı hareket edersin.",
            effectPreview = "+0.8 Hız  -0.20 Dash CD  +3 Dash Hızı",
            cardClass     = CardClass.Hermes,
            rarity        = CardRarity.Epic,
            tags          = CardTag.Movement,
            Apply         = _ => ApplyReward(2)
        },
        new CardDefinition
        {
            id            = "boss_atlas_cekirdegi",
            title         = "Atlas Çekirdeği",
            description   = "Atlas enerjisi bedenini güçlendirir ve yüksek altın kazandırır.",
            effectPreview = "+45 Maks. Can  +45 İyileşme  +60 Gold",
            cardClass     = CardClass.Atlas,
            rarity        = CardRarity.Legendary,
            tags          = CardTag.Defense,
            Apply         = _ => ApplyReward(3)
        }
    };

    void OnSelectionCompleted() => rewardPending = false;

    void ApplyReward(int option)
    {
        FindComponents();
        switch (option)
        {
            case 1:
                if (playerAura != null) { playerAura.damage += 8; playerAura.radius += 0.35f; }
                goldWallet?.AddGold(35);
                Debug.Log("Boss ödülü: Muhafızın Mührü");
                break;
            case 2:
                if (playerMovement != null)
                {
                    playerMovement.moveSpeed += 0.8f;
                    playerMovement.dashCooldown = Mathf.Max(0.25f, playerMovement.dashCooldown - 0.20f);
                    playerMovement.dashSpeed += 3f;
                }
                Debug.Log("Boss ödülü: Gölge Adım");
                break;
            case 3:
                if (playerHealth != null) { playerHealth.IncreaseMaxHealth(45); playerHealth.Heal(45); }
                goldWallet?.AddGold(60);
                Debug.Log("Boss ödülü: Atlas Çekirdeği");
                break;
        }

        rewardPending  = false;
        rewardTaken    = true;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
            playerHealth?.GrantInvulnerability(1f);

        if (BossSpawnSystem.Instance != null)
        {
            BossSpawnSystem.Instance.ScheduleNextWave();
            rewardTaken    = false;
            rewardUiOpened = false;
        }
    }

    void OnGUI() { }
}
