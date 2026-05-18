using System;
using System.Collections.Generic;
using UnityEngine;

public static class RelicDatabase
{
    static List<RelicDefinition> _all;
    public static IReadOnlyList<RelicDefinition> All => _all ??= Build();

    public static RelicDefinition GetById(string id)
    {
        foreach (var r in All)
            if (r.id == id) return r;
        return null;
    }

    static List<RelicDefinition> Build() => new List<RelicDefinition>
    {
        Relic("solmus_ankh", "Solmuş Ankh",
            "Can %30'un altına düştüğünde run başına 1 kez otomatik iyileşme hakkı verir.",
            RelicRarity.Rare, RelicCategory.Survival,
            RelicTag.Healing | RelicTag.OneTime | RelicTag.Unique,
            isUnique: true),

        Relic("kirik_muhur", "Kırık Mühür",
            "Eski bir mühürün kırıkları hâlâ güç saçıyor. +5 Aura Hasarı, +0.15 Menzil.",
            RelicRarity.Epic, RelicCategory.Combat,
            RelicTag.None,
            isUnique: true,
            apply: player =>
            {
                var a = player != null ? player.GetComponent<AutoAttackAura>() : null;
                if (a != null) { a.damage += 5; a.radius += 0.15f; }
            }),

        Relic("kanli_harita", "Kanlı Harita",
            "Elite düşmanlardan daha fazla gold kazanırsın.",
            RelicRarity.Rare, RelicCategory.RiskReward,
            RelicTag.Elite | RelicTag.Gold | RelicTag.RiskReward),

        Relic("atlas_kivilcimi", "Atlas Kıvılcımı",
            "Belirli aralıklarla level-up kart teklif kalitesini artırır.",
            RelicRarity.Epic, RelicCategory.Utility,
            RelicTag.Combo | RelicTag.Unique,
            isUnique: true),

        Relic("catlak_pusula", "Çatlak Pusula",
            "Boss sonrası portal yönlendirme ve rezonans hissini güçlendirir.",
            RelicRarity.Rare, RelicCategory.Portal,
            RelicTag.PortalResonance | RelicTag.Unique,
            isUnique: true),

        Relic("kan_bedeli", "Kan Bedeli",
            "Hasar verme kapasiten artar ama aldığın hasarı da hafif artırır.",
            RelicRarity.Rare, RelicCategory.Combat,
            RelicTag.RiskReward),

        Relic("altin_damar", "Altın Damar",
            "Gold pickup değerini artırır.",
            RelicRarity.Common, RelicCategory.Economy,
            RelicTag.Gold),

        Relic("mezar_sogugu", "Mezar Soğuğu",
            "Düşman öldüğünde çevresini etkileyen ölümcül bir soğuk yayılır.",
            RelicRarity.Epic, RelicCategory.Combat,
            RelicTag.Combo),

        Relic("firavun_tozu", "Firavun Tozu",
            "Kadim toz her dash'e can alıcı bir keskinlik katar. +8 Dash Hasarı, +0.1 Dash Hit Radius.",
            RelicRarity.Rare, RelicCategory.Combat,
            RelicTag.None,
            apply: player =>
            {
                var mv = player != null ? player.GetComponent<PlayerMovement>() : null;
                if (mv != null) { mv.dashDamage += 8; mv.dashHitRadius += 0.1f; }
            }),

        Relic("atlas_parcasi", "Atlas Parçası",
            "Atlas'ın kırık kabuğundan bir parça — cana can katar. +25 Maks. Can, +10 İyileşme.",
            RelicRarity.Common, RelicCategory.Survival,
            RelicTag.Healing,
            apply: player =>
            {
                var hp = player != null ? player.GetComponent<PlayerHealth>() : null;
                if (hp != null) { hp.IncreaseMaxHealth(25); hp.Heal(10); }
            }),

        Relic("yanki_tasi", "Yankı Taşı",
            "Taşın titreşimi adımları hafifletir ve refleksleri keskinleştirir. +0.35 Hız, -0.05 Dash CD.",
            RelicRarity.Rare, RelicCategory.Utility,
            RelicTag.None,
            apply: player =>
            {
                var mv = player != null ? player.GetComponent<PlayerMovement>() : null;
                if (mv != null) { mv.moveSpeed += 0.35f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.05f); }
            }),
    };

    static RelicDefinition Relic(
        string id, string title, string desc,
        RelicRarity rarity, RelicCategory category,
        RelicTag tags,
        bool isUnique = false, int maxStacks = 1,
        Action<GameObject> apply = null)
    {
        return new RelicDefinition
        {
            id          = id,
            title       = title,
            description = desc,
            rarity      = rarity,
            category    = category,
            tags        = tags,
            isUnique    = isUnique,
            maxStacks   = maxStacks,
            Apply       = apply
        };
    }
}
