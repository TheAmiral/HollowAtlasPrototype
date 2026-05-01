using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Player Health")]
    public int maxHealth = 100;

    [Header("UI")]
    public bool showLegacyOnGUI = false;
    public float uiX = 10f;
    public float uiY = 202f;
    public float uiWidth = 220f;
    public float uiHeight = 38f;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private int currentHealth;
    private bool isDead;
    private bool isInvulnerable;
    private float invulnerabilityExpiry;

    void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        isDead = false;
    }

    void Update()
    {
        if (isInvulnerable && Time.unscaledTime >= invulnerabilityExpiry)
            isInvulnerable = false;
    }

    public void GrantInvulnerability(float duration)
    {
        float newExpiry = Time.unscaledTime + duration;
        invulnerabilityExpiry = Mathf.Max(invulnerabilityExpiry, newExpiry);
        isInvulnerable = true;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
            return;

        if (isInvulnerable)
            return;

        currentHealth -= amount;

        if (currentHealth < 0)
            currentHealth = 0;

        Debug.Log($"Player damaged: {amount} | HP: {currentHealth}/{maxHealth}");

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerDamage();

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayPlayerDeath();

        Debug.Log("Player died.");

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameOver();
        else
            Time.timeScale = 0f;
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    public void IncreaseMaxHealth(int amount)
    {
        if (amount == 0)
            return;

        maxHealth += amount;

        if (maxHealth < 1)
            maxHealth = 1;

        if (amount > 0)
            currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        if (currentHealth < 1 && !isDead)
            currentHealth = 1;
    }

    void OnGUI()
    {
        if (!showLegacyOnGUI)
            return;

        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        GUI.Box(new Rect(uiX, uiY, uiWidth, uiHeight), "Player");
        GUI.Label(new Rect(uiX + 10f, uiY + 18f, uiWidth - 20f, 18f), $"HP: {currentHealth}/{maxHealth}");
    }
}