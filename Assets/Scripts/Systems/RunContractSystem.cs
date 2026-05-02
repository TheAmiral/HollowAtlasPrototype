using UnityEngine;

public class RunContractSystem : MonoBehaviour
{
    public static RunContractSystem Instance;
    private const float BottomMargin = 24f;
    private const float RightMargin = 18f;

    public enum ContractType
    {
        KillEnemies,
        CollectGold,
        SurviveTime
    }

    public ContractType currentContractType;

    public int targetValue;
    public int currentValue;
    public int rewardGold = 25;

    public bool IsCompleted { get; private set; }

    private GoldWallet goldWallet;
    public string contractTitle;
    public string contractDescription;
    [SerializeField] private bool showLegacyOnGUI = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            goldWallet = player.GetComponent<GoldWallet>();
        }

        GenerateRandomContract();

        if (FindFirstObjectByType<RunContractUIController>() == null)
            new GameObject("RunContractUIController").AddComponent<RunContractUIController>();
    }

    void Update()
    {
        if (IsCompleted)
            return;

        if (currentContractType == ContractType.SurviveTime)
        {
            if (GameManager.Instance != null)
            {
                currentValue = Mathf.FloorToInt(GameManager.Instance.ElapsedTime);

                if (currentValue >= targetValue)
                {
                    CompleteContract();
                }
            }
        }
    }

    void GenerateRandomContract()
    {
        int randomIndex = Random.Range(0, 3);

        switch (randomIndex)
        {
            case 0:
                currentContractType = ContractType.KillEnemies;
                targetValue = 20;
                rewardGold = 25;
                contractTitle = "Av Kontratı";
                contractDescription = "20 düşman öldür";
                break;

            case 1:
                currentContractType = ContractType.CollectGold;
                targetValue = 60;
                rewardGold = 30;
                contractTitle = "Ganimet Kontratı";
                contractDescription = "60 gold topla";
                break;

            case 2:
                currentContractType = ContractType.SurviveTime;
                targetValue = 45;
                rewardGold = 35;
                contractTitle = "Dayanıklılık Kontratı";
                contractDescription = "45 saniye hayatta kal";
                break;
        }

        currentValue = 0;
        IsCompleted = false;
    }

    public void RegisterEnemyKill()
    {
        if (IsCompleted)
            return;

        if (currentContractType != ContractType.KillEnemies)
            return;

        currentValue++;

        if (currentValue >= targetValue)
        {
            CompleteContract();
        }
    }

    public void RegisterGoldCollected(int amount)
    {
        if (IsCompleted)
            return;

        if (currentContractType != ContractType.CollectGold)
            return;

        currentValue += amount;

        if (currentValue >= targetValue)
        {
            CompleteContract();
        }
    }

    void CompleteContract()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;
        currentValue = targetValue;

        if (goldWallet != null)
        {
            goldWallet.AddGold(rewardGold);
        }

        Debug.Log($"Kontrat tamamlandı! Ödül Gold: {rewardGold}");
    }

    void OnGUI()
    {
        if (!showLegacyOnGUI)
            return;

        if (LevelUpCardSystem.Instance != null && LevelUpCardSystem.Instance.SelectionPending)
            return;

        float width = 250f;
        float height = 105f;
        float x = Screen.width - width - RightMargin;
        float y = Screen.height - height - BottomMargin;

        GUI.Box(new Rect(x, y, width, height), contractTitle);

        GUI.Label(new Rect(x + 10f, y + 25f, width - 20f, 20f), contractDescription);

        string progressText = IsCompleted
            ? "Tamamlandı!"
            : $"{currentValue} / {targetValue}";

        GUI.Label(new Rect(x + 10f, y + 50f, width - 20f, 20f), $"İlerleme: {progressText}");
        GUI.Label(new Rect(x + 10f, y + 75f, width - 20f, 20f), $"Ödül: {rewardGold} Gold");
    }
}
