using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    // Statik sayaç — EnemySpawner her frame FindObjectsByType yerine bunu okur.
    public static int AliveCount { get; private set; } = 0;

    [Header("Identity")]
    public bool isBoss = false;
    public string bossDisplayName = "Enemy";

    [Header("Health")]
    public int maxHealth = 20;

    [Header("Drops")]
    public GameObject goldPrefab;
    public int goldDropValue = 5;

    public GameObject xpPrefab;
    public int xpDropValue = 1;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;
    public bool IsBoss => isBoss;
    public string BossDisplayName => bossDisplayName;

    private int currentHealth;
    private bool isDead;

    private EnemyVisuals visuals;
    private BossSpecialAttack bossSpecialAttack;

    void Awake()
    {
        currentHealth = maxHealth;
        isDead = false;
        AliveCount++;

        visuals = GetComponent<EnemyVisuals>();
        bossSpecialAttack = GetComponent<BossSpecialAttack>();
    }

    void OnDestroy()
    {
        if (!isDead)
            AliveCount--;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth -= amount;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyHit();

        if (currentHealth > 0)
        {
            if (visuals != null)
                visuals.PlayHitFlash();
        }
        else
        {
            currentHealth = 0;
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        AliveCount--;

        if (bossSpecialAttack != null)
            bossSpecialAttack.HandleOwnerDeath();

        // Boss death sesi BossSpawnSystem tarafından tetikleniyor.
        // Normal düşmanlarda enemy death sesi burada çalıyor.
        if (!isBoss && AudioManager.Instance != null)
            AudioManager.Instance.PlayEnemyDeath();

        DropGold();
        DropXP();

        if (RunContractSystem.Instance != null)
            RunContractSystem.Instance.RegisterEnemyKill();

        if (visuals != null)
            visuals.PlayDeathAndDestroy();
        else
            Destroy(gameObject);
    }

    void DropGold()
    {
        if (goldPrefab == null)
            return;

        GameObject gold = Instantiate(
            goldPrefab,
            transform.position + Vector3.up * 0.25f,
            Quaternion.identity
        );

        GoldPickup goldPickup = gold.GetComponent<GoldPickup>();
        if (goldPickup != null)
            goldPickup.goldAmount = goldDropValue;
    }

    void DropXP()
    {
        if (xpPrefab == null)
            return;

        GameObject xp = Instantiate(
            xpPrefab,
            transform.position + Vector3.up * 0.2f,
            Quaternion.identity
        );

        ExperiencePickup xpPickup = xp.GetComponent<ExperiencePickup>();
        if (xpPickup != null)
        {
            xpPickup.fillsToNextLevel = isBoss;
            xpPickup.xpAmount = isBoss ? 0 : xpDropValue;
        }
    }
}