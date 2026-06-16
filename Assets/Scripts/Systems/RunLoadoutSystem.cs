using System.Collections.Generic;
using UnityEngine;

public struct ChestRewardEntry
{
    public string   id;
    public string   title;
    public string   iconId;
    public CardKind kind;
}

public class RunLoadoutSystem : MonoBehaviour
{
    public static RunLoadoutSystem Instance { get; private set; }

    public const int MaxWeapons      = 6;
    public const int MaxPassives     = 6;
    public const int MaxPassiveLevel = 5;

    readonly Dictionary<string, int>    _passiveLevels = new();
    readonly List<string>               _passiveOrder  = new();
    readonly List<ChestRewardEntry>     _chestRewards  = new();

    public IReadOnlyList<ChestRewardEntry> ChestRewards => _chestRewards;

    public void RegisterChestReward(string id, string title, string iconId, CardKind kind = CardKind.ChestRelic)
    {
        _chestRewards.Add(new ChestRewardEntry { id = id, title = title, iconId = iconId, kind = kind });
        Debug.Log($"[RunLoadoutSystem] RegisterChestReward id:{id} title:{title} iconId:{iconId} kind:{kind}");
    }

    public bool HasChestReward(string id)
    {
        foreach (var entry in _chestRewards)
            if (entry.id == id) return true;
        return false;
    }

    // ── Weapon delegation (WeaponInventory is source of truth) ───────────────

    public int  WeaponCount  => WeaponInventory.Instance?.OwnedWeaponCount ?? 0;
    public bool CanAddWeapon => WeaponCount < MaxWeapons;

    // ── Passive tracking ──────────────────────────────────────────────────────

    public int  PassiveCount  => _passiveLevels.Count;
    public bool CanAddPassive => PassiveCount < MaxPassives;

    public IReadOnlyList<string> OrderedPassives => _passiveOrder;

    public bool HasPassive(string id)        => _passiveLevels.ContainsKey(id);
    public int  GetPassiveLevel(string id)   => _passiveLevels.TryGetValue(id, out int lv) ? lv : 0;
    public bool IsPassiveMaxLevel(string id) => GetPassiveLevel(id) >= MaxPassiveLevel;

    // ── Run-scoped stat multipliers ───────────────────────────────────────────

    public float PickupRadiusMultiplier { get; private set; } = 1f;
    public float GoldPickupMultiplier   { get; private set; } = 1f;
    public float EnemySpawnPressure     { get; private set; } = 1f;

    public void AddPickupRadiusPct(float pct)  => PickupRadiusMultiplier  += pct;
    public void AddGoldPickupPct(float pct)    => GoldPickupMultiplier    += pct;
    public void AddEnemySpawnPressure(float p) => EnemySpawnPressure      += p;

    // ── Passive tracking ──────────────────────────────────────────────────────

    public void UnlockPassive(string id)
    {
        if (_passiveLevels.ContainsKey(id)) return;
        _passiveLevels[id] = 1;
        _passiveOrder.Add(id);
    }

    public void UpgradePassive(string id)
    {
        if (_passiveLevels.TryGetValue(id, out int lv) && lv < MaxPassiveLevel)
            _passiveLevels[id] = lv + 1;
    }

    public void ResetForNewRun()
    {
        _passiveLevels.Clear();
        _passiveOrder.Clear();
        _chestRewards.Clear();
        PickupRadiusMultiplier = 1f;
        GoldPickupMultiplier   = 1f;
        EnemySpawnPressure     = 1f;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("[RunLoadoutSystem]");
        Instance = go.AddComponent<RunLoadoutSystem>();
        DontDestroyOnLoad(go);
    }
}
