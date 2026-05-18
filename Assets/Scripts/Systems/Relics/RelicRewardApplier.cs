using UnityEngine;

public static class RelicRewardApplier
{
    public static void Apply(RelicDefinition relic, GameObject player)
    {
        if (relic == null) return;
        relic.Apply?.Invoke(player);
        RelicInventory.EnsureInstance().AddRelic(relic);
    }
}
