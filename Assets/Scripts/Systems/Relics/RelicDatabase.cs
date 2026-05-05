using System.Collections.Generic;

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
            "Boss ödül seçiminde ekstra +1 seçenek kazandırır.",
            RelicRarity.Epic, RelicCategory.Boss,
            RelicTag.BossReward | RelicTag.Unique,
            isUnique: true),

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
    };

    static RelicDefinition Relic(
        string id, string title, string desc,
        RelicRarity rarity, RelicCategory category,
        RelicTag tags,
        bool isUnique = false, int maxStacks = 1)
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
            maxStacks   = maxStacks
        };
    }
}
