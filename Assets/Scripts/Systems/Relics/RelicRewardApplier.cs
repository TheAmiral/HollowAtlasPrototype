using UnityEngine;

public static class RelicRewardApplier
{
    public static void Apply(RelicDefinition relic, GameObject player)
    {
        if (relic == null) return;
        RelicInventory.EnsureInstance().AddRelic(relic);
    }
}
