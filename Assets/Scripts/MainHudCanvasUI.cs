using UnityEngine;
using UnityEngine.UI;

public class MainHudCanvasUI : MonoBehaviour
{
    [Header("Optional UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image xpFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private Text xpText;
    [SerializeField] private Text levelText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text timerText;

    private PlayerHealth playerHealth;
    private PlayerLevelSystem playerLevelSystem;
    private GoldWallet goldWallet;

    void Awake()
    {
        ResolveReferences();
    }

    void Update()
    {
        ResolveReferences();
        UpdateHealthAndXPBars();
        UpdateLabels();
    }

    public void UpdateHealthAndXPBars()
    {
        if (healthFillImage != null && playerHealth != null)
        {
            float healthRatio = playerHealth.MaxHealth > 0
                ? (float)playerHealth.CurrentHealth / playerHealth.MaxHealth
                : 0f;

            healthFillImage.fillAmount = Mathf.Clamp01(healthRatio);
        }

        if (xpFillImage != null && playerLevelSystem != null)
        {
            float xpRatio = playerLevelSystem.XPToNextLevel > 0
                ? (float)playerLevelSystem.CurrentXP / playerLevelSystem.XPToNextLevel
                : 0f;

            xpFillImage.fillAmount = Mathf.Clamp01(xpRatio);
        }
    }

    private void UpdateLabels()
    {
        if (healthText != null && playerHealth != null)
            healthText.text = $"HP: {playerHealth.CurrentHealth}/{playerHealth.MaxHealth}";

        if (xpText != null && playerLevelSystem != null)
            xpText.text = $"XP: {playerLevelSystem.CurrentXP}/{playerLevelSystem.XPToNextLevel}";

        if (levelText != null && playerLevelSystem != null)
            levelText.text = $"Lv {playerLevelSystem.Level}";

        if (goldText != null && goldWallet != null)
            goldText.text = $"Gold: {goldWallet.RunGold}";

        if (timerText != null && GameManager.Instance != null)
            timerText.text = $"Time: {GameManager.Instance.ElapsedTime:0.0}s";
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerLevelSystem == null)
            playerLevelSystem = FindObjectOfType<PlayerLevelSystem>();

        if (goldWallet == null)
            goldWallet = FindObjectOfType<GoldWallet>();
    }
}
