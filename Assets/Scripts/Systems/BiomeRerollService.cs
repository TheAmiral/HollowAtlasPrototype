using UnityEngine;

// Biyom başına ortak reroll hakkı — Atlas Lütufları ve Sandık Kalıntıları için paylaşımlı.
public class BiomeRerollService : MonoBehaviour
{
    public static BiomeRerollService Instance { get; private set; }

    public const int MaxRerolls = 3;

    public int RerollsRemaining { get; private set; } = MaxRerolls;
    public int RerollsUsed { get; private set; } = 0;

    static readonly int[] Costs = { 15, 30, 60 };

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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

    // Yeni biyoma geçişte çağır — ileride BossSpawnSystem veya biyom geçiş sistemi kullanacak.
    public void ResetForNewBiome(int biomeDepth = 1)
    {
        RerollsRemaining = MaxRerolls;
        RerollsUsed = 0;
    }
}
