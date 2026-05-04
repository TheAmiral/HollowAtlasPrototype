using UnityEngine;
using System.Collections.Generic;

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

    private bool rewardUiOpened;

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
        rewardUiOpened = false;

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

        if (rewardTaken || rewardUiOpened || BossSpawnSystem.Instance == null)
            return;

        if (!BossSpawnSystem.Instance.BossSpawned || !BossSpawnSystem.Instance.BossDefeated)
            return;

        if (LevelUpCardSystem.Instance == null || LevelUpCardSystem.Instance.SelectionPending)
            return;

        List<LevelUpCard> cards = BuildBossRewardCards();

        if (cards == null || cards.Count == 0)
            return;

        bool opened = LevelUpCardSystem.Instance.ShowCustomCards(
            "ATLAS ÖDÜLÜ",
            "Muhafızın gücünden bir kalıntı seç",
            cards,
            OnBossRewardSelectionCompleted
        );

        if (opened)
        {
            rewardPending = true;
            rewardUiOpened = true;
            AudioManager.Instance?.PlayBossReward();
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
                ApplyGuardianSeal();
                break;

            case 2:
                ApplyShadowStep();
                break;

            case 3:
                ApplyAtlasCore();
                break;
        }

        rewardPending = false;
        rewardTaken = true;

        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver)
        {
            if (playerHealth != null)
                playerHealth.GrantInvulnerability(1f);
        }

        if (BossSpawnSystem.Instance != null)
        {
            BossSpawnSystem.Instance.ScheduleNextWave();
            rewardTaken    = false;
            rewardUiOpened = false;
        }
    }

    List<LevelUpCard> BuildBossRewardCards()
    {
        return new List<LevelUpCard>
        {
            new LevelUpCard(
                "boss_atlas_muhafiz_muhru",
                "Muhafızın Mührü",
                "Atlas Muhafızı'nın enerjisi saldırılarını güçlendirir. Aura hasarın ve menzilin artar.",
                "+8 Aura Hasar  +0.35 Aura Menzili  +35 Gold",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Epic,
                _ => ApplyReward(1)
            ),

            new LevelUpCard(
                "boss_golge_adim",
                "Gölge Adım",
                "Boss savaşından öğrendiğin ritimle daha hızlı hareket eder ve dash'i daha sık kullanırsın.",
                "+0.8 Hız  -0.20 Dash CD  +3 Dash Hızı",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Epic,
                _ => ApplyReward(2)
            ),

            new LevelUpCard(
                "boss_atlas_cekirdegi",
                "Atlas Çekirdeği",
                "Atlas enerjisi bedenini güçlendirir. Maksimum canın artar, iyileşirsin ve yüksek altın kazanırsın.",
                "+45 Maks. Can  +45 İyileşme  +60 Gold",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Legendary,
                _ => ApplyReward(3)
            ),
        };
    }

    void OnBossRewardSelectionCompleted()
    {
        rewardPending = false;
    }

    void ApplyGuardianSeal()
    {
        if (aura != null)
        {
            aura.damage += 8;
            aura.radius += 0.35f;
        }

        if (goldWallet != null)
            goldWallet.AddGold(35);

        Debug.Log("Boss ödülü seçildi: Muhafızın Mührü");
    }

    void ApplyShadowStep()
    {
        if (movement != null)
        {
            movement.moveSpeed += 0.8f;
            movement.dashCooldown = Mathf.Max(0.25f, movement.dashCooldown - 0.20f);
            movement.dashSpeed += 3f;
        }

        Debug.Log("Boss ödülü seçildi: Gölge Adım");
    }

    void ApplyAtlasCore()
    {
        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(45);
            playerHealth.Heal(45);
        }

        if (goldWallet != null)
            goldWallet.AddGold(60);

        Debug.Log("Boss ödülü seçildi: Atlas Çekirdeği");
    }

    void OnGUI()
    {
        return;
    }
}
