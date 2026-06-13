// ─────────────────────────────────────────────────────────────────────────────
//  CardOfferGenerator.cs  (YENİDEN YAZILDI — VS Sistemi)
//  Assets/Scripts/Systems/Cards/CardOfferGenerator.cs
//
//  Her level-up'ta VS mantığına göre 3 kart teklif eder:
//
//  Slot 1 → Sahip olunan silahlardan upgrade teklifi
//           (veya en ucuz yeni silah, eğer hiç upgrade yoksa)
//  Slot 2 → Pasif kart (Sustain / Mobility / Damage)
//  Slot 3 → Yeni silah teklifi (sahip olunmayanlardan)
//           veya ikinci upgrade / ikinci pasif
// ─────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public static class CardOfferGenerator
{
    static readonly int[] BaseWeights = { 60, 28, 10, 2, 0 }; // C/R/E/L/Chaos

    public static List<CardDefinition> Generate(int count, int playerLevel,
                                                HashSet<string> excludeIds = null)
    {
        var inv = WeaponInventory.Instance;
        var result = new List<CardDefinition>(count);
        var usedIds = excludeIds != null ? new HashSet<string>(excludeIds) : new HashSet<string>();

        // ── Silah upgrade kartlarını hazırla ──────────────────────────────────
        var weaponUpgrades = inv != null
            ? CardDatabase.GetWeaponCards(inv)
            : new List<CardDefinition>();

        // Elinde olmayan silahlar
        var newWeapons = new List<CardDefinition>();
        // Elindeki silahlara upgrade
        var ownedUpgrades = new List<CardDefinition>();

        foreach (var wCard in weaponUpgrades)
        {
            if (wCard == null) continue;
            if (usedIds.Contains(wCard.id)) continue;

            if (inv != null && inv.HasWeapon(wCard.weaponType))
                ownedUpgrades.Add(wCard);
            else
                newWeapons.Add(wCard);
        }

        // Pasif kartlar
        var passives = GetPassiveCandidates(playerLevel, usedIds);
        var khaos    = GetKhaosCandidates(playerLevel, usedIds);

        // ── 3 Slot doldur ─────────────────────────────────────────────────────

        // Slot 1: Mevcut silah upgrade (VS önceliği)
        if (ownedUpgrades.Count > 0)
        {
            var pick = ownedUpgrades[Random.Range(0, ownedUpgrades.Count)];
            result.Add(pick);
            usedIds.Add(pick.id);
        }
        else if (newWeapons.Count > 0)
        {
            // Henüz upgrade yoksa yeni silah teklif et
            var pick = newWeapons[Random.Range(0, newWeapons.Count)];
            result.Add(pick);
            usedIds.Add(pick.id);
            newWeapons.Remove(pick);
        }
        else if (passives.Count > 0)
        {
            // Hiç silah seçeneği yoksa pasif
            var pick = PickWeightedPassive(passives, playerLevel);
            if (pick != null) { result.Add(pick); usedIds.Add(pick.id); passives.Remove(pick); }
        }

        // Slot 2: Pasif kart
        if (result.Count < count && passives.Count > 0)
        {
            var pick = PickWeightedPassive(passives, playerLevel);
            if (pick != null) { result.Add(pick); usedIds.Add(pick.id); passives.Remove(pick); }
        }

        // Slot 3: Yeni silah VEYA ikinci pasif VEYA khaos
        if (result.Count < count)
        {
            bool offerKhaos = playerLevel >= 3 && Random.value < 0.15f;

            if (offerKhaos && khaos.Count > 0)
            {
                var pick = khaos[Random.Range(0, khaos.Count)];
                result.Add(pick);
                usedIds.Add(pick.id);
            }
            else if (newWeapons.Count > 0)
            {
                var pick = newWeapons[Random.Range(0, newWeapons.Count)];
                result.Add(pick);
                usedIds.Add(pick.id);
            }
            else if (passives.Count > 0)
            {
                var pick = PickWeightedPassive(passives, playerLevel);
                if (pick != null) { result.Add(pick); usedIds.Add(pick.id); }
            }
            else if (ownedUpgrades.Count > 0)
            {
                // Bonus upgrade slot
                foreach (var up in ownedUpgrades)
                {
                    if (!usedIds.Contains(up.id))
                    {
                        result.Add(up);
                        usedIds.Add(up.id);
                        break;
                    }
                }
            }
        }

        // count'a ulaşamadıysak pasif ile doldur
        while (result.Count < count && passives.Count > 0)
        {
            var pick = PickWeightedPassive(passives, playerLevel);
            if (pick == null) break;
            if (!usedIds.Contains(pick.id))
            {
                result.Add(pick);
                usedIds.Add(pick.id);
            }
            passives.Remove(pick);
        }

        return result;
    }

    // ── Pasif aday listesi ────────────────────────────────────────────────────

    static List<CardDefinition> GetPassiveCandidates(int playerLevel, HashSet<string> usedIds)
    {
        var candidates = new List<CardDefinition>();
        foreach (var card in CardDatabase.All)
        {
            if (card == null) continue;
            if (card.cardType != CardType.Passive) continue;
            if (usedIds.Contains(card.id)) continue;
            candidates.Add(card);
        }
        return candidates;
    }

    static List<CardDefinition> GetKhaosCandidates(int playerLevel, HashSet<string> usedIds)
    {
        var candidates = new List<CardDefinition>();
        foreach (var card in CardDatabase.All)
        {
            if (card == null) continue;
            if (card.cardType != CardType.Chaos) continue;
            if (usedIds.Contains(card.id)) continue;
            candidates.Add(card);
        }
        return candidates;
    }

    // ── Ağırlıklı pasif seçimi ────────────────────────────────────────────────

    static CardDefinition PickWeightedPassive(List<CardDefinition> pool, int playerLevel)
    {
        if (pool.Count == 0) return null;

        // Level arttıkça nadir kartlar açılır
        var weighted = new List<(CardDefinition card, int w)>();
        foreach (var c in pool)
        {
            int w = c.rarity switch
            {
                CardRarity.Common    => 60,
                CardRarity.Rare      => playerLevel >= 3 ? 30 : 15,
                CardRarity.Epic      => playerLevel >= 6 ? 15 : 5,
                CardRarity.Legendary => playerLevel >= 9 ? 8  : 2,
                _                    => 5,
            };
            weighted.Add((c, w));
        }

        int total = 0;
        foreach (var (_, w) in weighted) total += w;
        if (total <= 0) return pool[Random.Range(0, pool.Count)];

        int roll = Random.Range(0, total);
        int cursor = 0;
        foreach (var (card, w) in weighted)
        {
            cursor += w;
            if (roll < cursor) return card;
        }
        return pool[pool.Count - 1];
    }
}
