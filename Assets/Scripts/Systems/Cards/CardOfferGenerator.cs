using System.Collections.Generic;
using UnityEngine;

public static class CardOfferGenerator
{
    public static List<CardDefinition> Generate(int count, int playerLevel, HashSet<string> excludeIds = null)
    {
        var result  = new List<CardDefinition>(count);
        var usedIds = excludeIds != null
            ? new HashSet<string>(excludeIds)
            : new HashSet<string>();

        var inv = WeaponInventory.Instance;
        var rls = RunLoadoutSystem.Instance;

        // Mevcut duruma göre uygun kartları topla
        var weaponCards  = CardDatabase.GetWeaponCards(inv);
        var passiveCards = CardDatabase.GetPassiveCards(rls);

        // Max level ve daha önce sunulan kartları filtrele
        weaponCards.RemoveAll(c => c == null || usedIds.Contains(c.id));
        passiveCards.RemoveAll(c => c == null || usedIds.Contains(c.id));

        // Havuzu birleştir ve karıştır
        var pool = new List<CardDefinition>(weaponCards.Count + passiveCards.Count);
        pool.AddRange(weaponCards);
        pool.AddRange(passiveCards);
        Shuffle(pool);

        // count kadar benzersiz kart seç
        foreach (var card in pool)
        {
            if (result.Count >= count) break;
            if (usedIds.Contains(card.id)) continue;
            result.Add(card);
            usedIds.Add(card.id);
        }

        return result;
    }

    /// <summary>
    /// Sandık ödül teklifi üretir.
    /// Uygun uyanış varsa 3 karttan en az 1 tanesi SİLAH UYANIŞI olur.
    /// Normal level-up pasifleri bu havuza girmez.
    /// </summary>
    public static List<CardDefinition> GenerateChestOffer(int count = 3)
    {
        var inv    = WeaponInventory.Instance;
        var rls    = RunLoadoutSystem.Instance;
        var result = new List<CardDefinition>(count);
        var usedIds = new HashSet<string>();

        // Uygun uyanışları kontrol et — varsa 1 tane ekle
        var awakenings = CardDatabase.GetWeaponAwakenings(inv, rls);
        Shuffle(awakenings);
        if (awakenings.Count > 0)
        {
            result.Add(awakenings[0]);
            usedIds.Add(awakenings[0].id);
        }

        // Kalan slotları chest-only kartlarla doldur
        // Daha önce alınmış chest reward'ları havuzdan çıkar
        var chestCards = CardDatabase.GetChestCards();
        if (rls != null)
            chestCards.RemoveAll(c => rls.HasChestReward(c.id));
        Shuffle(chestCards);
        foreach (var card in chestCards)
        {
            if (result.Count >= count) break;
            if (usedIds.Contains(card.id)) continue;
            result.Add(card);
            usedIds.Add(card.id);
        }

        return result;
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}
