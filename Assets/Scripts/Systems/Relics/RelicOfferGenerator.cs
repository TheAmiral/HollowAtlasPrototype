using System.Collections.Generic;
using UnityEngine;

public static class RelicOfferGenerator
{
    // Weights indexed by RelicRarity cast to int: Common=55, Rare=30, Epic=12, Legendary=2, Cursed=1
    // TODO: scale weights by biome depth when that system exists
    static readonly int[] RarityWeights = { 55, 30, 12, 2, 1 };

    public static List<RelicDefinition> Generate(int count)
    {
        var inventory  = RelicInventory.Instance;
        var pool       = new List<RelicDefinition>(RelicDatabase.All);
        var result     = new List<RelicDefinition>();
        var usedIds    = new HashSet<string>();

        pool.RemoveAll(r => r.isUnique && inventory != null && inventory.HasRelic(r.id));

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            var rarity     = PickRarity();
            var candidates = pool.FindAll(r => r.rarity == rarity && !usedIds.Contains(r.id));
            if (candidates.Count == 0)
                candidates = pool.FindAll(r => !usedIds.Contains(r.id));
            if (candidates.Count == 0)
                break;

            var picked = candidates[Random.Range(0, candidates.Count)];
            result.Add(picked);
            usedIds.Add(picked.id);
            pool.Remove(picked);
        }

        return result;
    }

    static RelicRarity PickRarity()
    {
        int total = 0;
        foreach (var w in RarityWeights) total += w;
        int roll = Random.Range(0, total);
        int acc  = 0;
        for (int i = 0; i < RarityWeights.Length; i++)
        {
            acc += RarityWeights[i];
            if (roll < acc) return (RelicRarity)i;
        }
        return RelicRarity.Common;
    }
}
