using UnityEngine;
using UnityEngine.SceneManagement;

// Biyom başına ortak reroll hakkı — Atlas Lütufları ve Sandık Kalıntıları için paylaşımlı.
public class BiomeRerollService : MonoBehaviour
{
    public static BiomeRerollService Instance { get; private set; }

    public const int MaxRerolls = 3;
    const string MainMenuScene = "MainMenu";

    public int RerollsRemaining { get; private set; } = MaxRerolls;
    public int RerollsUsed { get; private set; } = 0;

    static readonly int[] Costs = { 15, 30, 60 };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Yeni bir gameplay run'ı başladığında reroll'ü sıfırla.
        // Ana menü yüklenince sıfırlama — gerekli değil, oyuna tekrar girilince reset yeterli.
        if (scene.name != MainMenuScene)
            ResetForNewRun();
    }

    public static BiomeRerollService EnsureInstance()
    {
        if (Instance != null)
            return Instance;
        var go = new GameObject("BiomeRerollService");
        go.AddComponent<BiomeRerollService>();
        return Instance;
    }

    public int GetCurrentRerollCost() =>
        RerollsUsed < Costs.Length ? Costs[RerollsUsed] : Costs[Costs.Length - 1];

    public bool CanReroll() => RerollsRemaining > 0;

    public bool TrySpendReroll()
    {
        if (!CanReroll()) return false;
        RerollsRemaining--;
        RerollsUsed++;
        return true;
    }

    // Yeni run başında çağrılır — scene yüklenince otomatik tetiklenir.
    public void ResetForNewRun()
    {
        RerollsRemaining = MaxRerolls;
        RerollsUsed = 0;
    }

    // Yeni biyoma geçişte çağır — ileride BossSpawnSystem veya biyom geçiş sistemi kullanacak.
    public void ResetForNewBiome(int biomeDepth = 1)
    {
        RerollsRemaining = MaxRerolls;
        RerollsUsed = 0;
    }
}
