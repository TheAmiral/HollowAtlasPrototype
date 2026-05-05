using System.Collections.Generic;
using UnityEngine;

public static class CardOfferGenerator
{
    // Common, Rare, Epic, Legendary, Chaos
    static readonly int[] BaseWeights = { 60, 28, 10, 2, 0 };

    public static List<CardDefinition> Generate(int count, int playerLevel, HashSet<string> excludeIds = null)
    {
        var result = new List<CardDefinition>(count);
        var usedIds = excludeIds != null
            ? new HashSet<string>(excludeIds)
            : new HashSet<string>();

        var usedClasses = new HashSet<CardClass>();

        for (int i = 0; i < count; i++)
        {
            CardRarity targetRarity = RollRarity(playerLevel);

            CardDefinition pick = PickCandidate(targetRarity, usedIds, usedClasses, preferNewClass: true);

            if (pick == null)
                pick = PickCandidate(targetRarity, usedIds, usedClasses, preferNewClass: false);

            if (pick == null)
                pick = PickAny(usedIds);

            if (pick == null)
                break;

            result.Add(pick);
            usedIds.Add(pick.id);
            usedClasses.Add(pick.cardClass);
        }

        return result;
    }

    static CardDefinition PickCandidate(
        CardRarity rarity,
        HashSet<string> usedIds,
        HashSet<CardClass> usedClasses,
        bool preferNewClass)
    {
        var candidates = new List<CardDefinition>();

        foreach (var card in CardDatabase.All)
        {
            if (card == null)
                continue;

            if (usedIds.Contains(card.id))
                continue;

            if (card.rarity != rarity)
                continue;

            if (preferNewClass && usedClasses.Contains(card.cardClass))
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    static CardDefinition PickAny(HashSet<string> usedIds)
    {
        var candidates = new List<CardDefinition>();

        foreach (var card in CardDatabase.All)
        {
            if (card == null)
                continue;

            if (usedIds.Contains(card.id))
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    static CardRarity RollRarity(int playerLevel)
    {
        int common = BaseWeights[0];
        int rare = BaseWeights[1];
        int epic = BaseWeights[2];
        int legendary = BaseWeights[3];
        int chaos = BaseWeights[4];

        // Level arttıkça Rare/Epic/Legendary şansı hafif artsın.
        if (playerLevel >= 4)
        {
            common -= 5;
            rare += 3;
            epic += 2;
        }

        if (playerLevel >= 7)
        {
            common -= 5;
            rare += 2;
            epic += 2;
            legendary += 1;
        }

        if (playerLevel >= 10)
        {
            common -= 5;
            epic += 3;
            legendary += 2;
        }

        // Khaos çok düşük oranlı özel teklif.
        if (playerLevel >= 3)
            chaos = 3;

        common = Mathf.Max(0, common);
        rare = Mathf.Max(0, rare);
        epic = Mathf.Max(0, epic);
        legendary = Mathf.Max(0, legendary);
        chaos = Mathf.Max(0, chaos);

        int total = common + rare + epic + legendary + chaos;
        if (total <= 0)
            return CardRarity.Common;

        int roll = Random.Range(0, total);
        int cursor = common;

        if (roll < cursor)
            return CardRarity.Common;

        cursor += rare;
        if (roll < cursor)
            return CardRarity.Rare;

        cursor += epic;
        if (roll < cursor)
            return CardRarity.Epic;

        cursor += legendary;
        if (roll < cursor)
            return CardRarity.Legendary;

        return CardRarity.Chaos;
    }
}
