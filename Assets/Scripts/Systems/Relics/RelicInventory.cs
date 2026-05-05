using System.Collections.Generic;
using UnityEngine;

public class RelicInventory : MonoBehaviour
{
    public static RelicInventory Instance { get; private set; }

    readonly Dictionary<string, int> _stacks = new();

    public float GoldMultiplier           { get; private set; } = 1f;
    public bool  HasPortalResonanceBoost  { get; private set; }
    public bool  HasExtraBossRewardChoice { get; private set; }
    public bool  HasEmergencyHeal         { get; private set; }
    public bool  HasLevelUpQualityBoost   { get; private set; }
    public bool  EmergencyHealUsed        { get; set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public static RelicInventory EnsureInstance()
    {
        if (Instance == null)
            new GameObject("RelicInventory").AddComponent<RelicInventory>();
        return Instance;
    }

    public void Reset()
    {
        _stacks.Clear();
        GoldMultiplier           = 1f;
        HasPortalResonanceBoost  = false;
        HasExtraBossRewardChoice = false;
        HasEmergencyHeal         = false;
        HasLevelUpQualityBoost   = false;
        EmergencyHealUsed        = false;
    }

    public bool HasRelic(string id) => _stacks.ContainsKey(id);

    public int GetStack(string id) => _stacks.TryGetValue(id, out int s) ? s : 0;

    public void AddRelic(RelicDefinition relic)
    {
        if (relic == null) return;
        if (relic.isUnique && HasRelic(relic.id)) return;
        if (_stacks.ContainsKey(relic.id))
            _stacks[relic.id]++;
        else
            _stacks[relic.id] = 1;
        ApplyPassiveFlags(relic);
    }

    void ApplyPassiveFlags(RelicDefinition relic)
    {
        switch (relic.id)
        {
            case "altin_damar":
                // Each stack adds 25% gold bonus
                GoldMultiplier = 1f + 0.25f * GetStack(relic.id);
                break;
            case "solmus_ankh":
                HasEmergencyHeal = true;
                break;
            case "kirik_muhur":
                HasExtraBossRewardChoice = true;
                break;
            case "atlas_kivilcimi":
                HasLevelUpQualityBoost = true;
                break;
            case "catlak_pusula":
                HasPortalResonanceBoost = true;
                break;
            // TODO: "kanli_harita" — integrate with elite enemy gold drop
            // TODO: "kan_bedeli" — integrate with damage dealt/received modifiers
            // TODO: "mezar_sogugu" — integrate with on-kill AoE slow/damage VFX
        }
    }
}
