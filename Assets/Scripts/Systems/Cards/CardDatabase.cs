using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardDatabase
{
    // All() artık boş — CardOfferGenerator GetWeaponCards / GetPassiveCards kullanır.
    static readonly List<CardDefinition> _empty = new();
    public static IReadOnlyList<CardDefinition> All => _empty;

    public static CardDefinition GetById(string id)
    {
        // Dinamik kart sisteminde id'ye göre arama gerekmez; geriye dönük uyumluluk için korundu.
        return null;
    }

    // ── Pasif isim ve görsel yardımcıları ────────────────────────────────────

    public static string GetPassiveName(string id) => id switch
    {
        "kan_yemini"     => "Kan Yemini",
        "ruzgar_adimi"   => "Rüzgar Adımı",
        "demir_beden"    => "Demir Beden",
        "zaman_kirigi"   => "Zaman Kırığı",
        "buyuyen_yanki"  => "Büyüyen Yankı",
        "ruh_miknatis"   => "Ruh Mıknatısı",
        "atlas_bereketi" => "Atlas Bereketi",
        "lanetli_servet" => "Lanetli Servet",
        _                => id
    };

    public static string GetWeaponName(WeaponType wt) => wt switch
    {
        WeaponType.KatanaAura  => "Katana Aura",
        WeaponType.RuhKunai    => "Ruh Kunai",
        WeaponType.AtlasSphere => "Atlas Küresi",
        _                      => "?"
    };

    static CardVisualCategory GetPassiveVisualCategory(string id) => id switch
    {
        "kan_yemini"     => CardVisualCategory.Sustain,
        "ruzgar_adimi"   => CardVisualCategory.Mobility,
        "demir_beden"    => CardVisualCategory.Sustain,
        "zaman_kirigi"   => CardVisualCategory.Damage,
        "buyuyen_yanki"  => CardVisualCategory.Damage,
        "ruh_miknatis"   => CardVisualCategory.Mobility,
        "atlas_bereketi" => CardVisualCategory.Combo,
        "lanetli_servet" => CardVisualCategory.Chaos,
        _                => CardVisualCategory.Damage
    };

    static CardClass GetPassiveCardClass(string id) => id switch
    {
        "kan_yemini"     => CardClass.Ares,
        "ruzgar_adimi"   => CardClass.Hermes,
        "demir_beden"    => CardClass.Atlas,
        "zaman_kirigi"   => CardClass.Hephaistos,
        "buyuyen_yanki"  => CardClass.Nyx,
        "ruh_miknatis"   => CardClass.Artemis,
        "atlas_bereketi" => CardClass.Atlas,
        "lanetli_servet" => CardClass.Khaos,
        _                => CardClass.Atlas
    };

    static CardRarity GetPassiveRarity(int targetLevel) => targetLevel switch
    {
        1    => CardRarity.Common,
        2    => CardRarity.Common,
        3    => CardRarity.Rare,
        4    => CardRarity.Epic,
        _    => CardRarity.Legendary
    };

    // ── Silah kartları — WeaponInventory durumuna göre çalışma zamanında üretilir ──

    public static List<CardDefinition> GetWeaponCards(WeaponInventory inv)
    {
        var result = new List<CardDefinition>();
        if (inv == null) return result;

        var rls = RunLoadoutSystem.Instance;

        AddWeaponCards(result, inv, rls, WeaponType.KatanaAura);
        AddWeaponCards(result, inv, rls, WeaponType.RuhKunai);
        AddWeaponCards(result, inv, rls, WeaponType.AtlasSphere);
        return result;
    }

    static void AddWeaponCards(List<CardDefinition> list, WeaponInventory inv, RunLoadoutSystem rls, WeaponType wt)
    {
        int  currLv = inv.GetLevel(wt);
        bool has    = inv.HasWeapon(wt);
        bool maxed  = inv.IsMaxLevel(wt);

        if (!has && (rls == null || rls.CanAddWeapon))
            list.Add(WeaponUnlockCard(wt));
        else if (has && !maxed)
            list.Add(WeaponLevelCard(wt, currLv + 1, currLv));
    }

    static CardDefinition WeaponUnlockCard(WeaponType wt) => wt switch
    {
        WeaponType.KatanaAura => new CardDefinition
        {
            id = "unlock_katana", title = "Katana Aura",
            description    = "Düşmanları çevreleyen enerji alanı açılır.",
            effectPreview  = "Aura Lv 1 açılır",
            cardKind       = CardKind.WeaponUnlock,
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Hephaistos,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponKatana,
            tags           = CardTag.Weapon | CardTag.Aura,
            weaponType     = WeaponType.KatanaAura,
            weaponLevel    = 1,
            currentLevel   = 0,
            maxLevel       = 8,
        },
        WeaponType.RuhKunai => new CardDefinition
        {
            id = "unlock_kunai", title = "Ruh Kunai",
            description    = "En yakın düşmanlara otomatik kunai fırlatır.",
            effectPreview  = "Ruh Kunai Lv 1 açılır",
            cardKind       = CardKind.WeaponUnlock,
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Artemis,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponKunai,
            tags           = CardTag.Weapon | CardTag.Damage,
            weaponType     = WeaponType.RuhKunai,
            weaponLevel    = 1,
            currentLevel   = 0,
            maxLevel       = 8,
        },
        WeaponType.AtlasSphere => new CardDefinition
        {
            id = "unlock_sphere", title = "Atlas Küresi",
            description    = "Etrafında dönen enerji küreleri düşmanlara hasar verir.",
            effectPreview  = "Atlas Küresi Lv 1 açılır",
            cardKind       = CardKind.WeaponUnlock,
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Atlas,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponSphere,
            tags           = CardTag.Weapon | CardTag.Aura,
            weaponType     = WeaponType.AtlasSphere,
            weaponLevel    = 1,
            currentLevel   = 0,
            maxLevel       = 8,
        },
        _ => null
    };

    static CardDefinition WeaponLevelCard(WeaponType wt, int targetLevel, int currentLevel)
    {
        CardRarity r = targetLevel switch
        {
            <= 3 => CardRarity.Common,
            <= 5 => CardRarity.Rare,
            <= 7 => CardRarity.Epic,
            _    => CardRarity.Legendary
        };
        string tag = $"Lv {targetLevel}";

        return wt switch
        {
            WeaponType.KatanaAura => new CardDefinition
            {
                id = $"katana_lv{targetLevel}", title = "Katana Aura",
                description    = KatanaDesc(targetLevel),
                effectPreview  = KatanaPreview(targetLevel),
                cardKind       = CardKind.WeaponUpgrade,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Hephaistos,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponKatana,
                tags           = CardTag.Weapon | CardTag.Aura,
                weaponType     = WeaponType.KatanaAura,
                weaponLevel    = targetLevel,
                currentLevel   = currentLevel,
                maxLevel       = 8,
            },
            WeaponType.RuhKunai => new CardDefinition
            {
                id = $"kunai_lv{targetLevel}", title = "Ruh Kunai",
                description    = KunaiDesc(targetLevel),
                effectPreview  = KunaiPreview(targetLevel),
                cardKind       = CardKind.WeaponUpgrade,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Artemis,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponKunai,
                tags           = CardTag.Weapon | CardTag.Damage,
                weaponType     = WeaponType.RuhKunai,
                weaponLevel    = targetLevel,
                currentLevel   = currentLevel,
                maxLevel       = 8,
            },
            WeaponType.AtlasSphere => new CardDefinition
            {
                id = $"sphere_lv{targetLevel}", title = "Atlas Küresi",
                description    = SphereDesc(targetLevel),
                effectPreview  = SpherePreview(targetLevel),
                cardKind       = CardKind.WeaponUpgrade,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Atlas,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponSphere,
                tags           = CardTag.Weapon | CardTag.Aura,
                weaponType     = WeaponType.AtlasSphere,
                weaponLevel    = targetLevel,
                currentLevel   = currentLevel,
                maxLevel       = 8,
            },
            _ => null
        };
    }

    // ── Silah açıklama metinleri ──────────────────────────────────────────────

    static string KatanaDesc(int lv) => lv switch
    {
        2 => "Aura hasarı ve menzili artar.",        3 => "Aura daha hızlı titreşir.",
        4 => "Hasar ve menzil güçlenir.",             5 => "Titreşim frekansı artar.",
        6 => "Aura büyür ve güçlenir.",               7 => "Frekans kritik seviyeye ulaşır.",
        8 => "Katana Aura maksimum güce erişir.",     _ => "Silah güçlenir."
    };
    static string KatanaPreview(int lv) => lv switch
    {
        2 => "+4 Hasar  +0.2 Alan",           3 => "+5 Hasar  Saldırı Hızı +%10",
        4 => "+6 Hasar  +0.25 Alan",          5 => "+8 Hasar  Saldırı Hızı +%11",
        6 => "+10 Hasar  +0.3 Alan",          7 => "+12 Hasar  Saldırı Hızı +%12",
        8 => "+15 Hasar  +0.4 Alan  Saldırı Hızı MAX", _ => "Güçlenir"
    };
    static string KunaiDesc(int lv) => lv switch
    {
        2 => "Kunai hasarı artar.",                  3 => "Kunai daha sık fırlatılır.",
        4 => "İkinci kunai açılır, hasar artar.",     5 => "Hasar güçlenir.",
        6 => "Atış hızı artar.",                      7 => "Üçüncü kunai açılır.",
        8 => "Ruh Kunai maksimum güce erişir.",      _ => "Silah güçlenir."
    };
    static string KunaiPreview(int lv) => lv switch
    {
        2 => "+5 Hasar",              3 => "Atış Hızı +%12",
        4 => "2 Kunai  +5 Hasar",    5 => "+8 Hasar",
        6 => "Atış Hızı +%14",       7 => "3 Kunai  +8 Hasar",
        8 => "+12 Hasar  Hız MAX",   _ => "Güçlenir"
    };
    static string SphereDesc(int lv) => lv switch
    {
        2 => "Küre hasarı artar.",           3 => "Küre daha hızlı döner.",
        4 => "Hasar ve yörünge genişler.",   5 => "İkinci küre açılır.",
        6 => "Dönüş hızı ve hasar artar.",   7 => "Hasar büyük ölçüde artar.",
        8 => "Üçüncü küre açılır, maksimum güç.", _ => "Silah güçlenir."
    };
    static string SpherePreview(int lv) => lv switch
    {
        2 => "+4 Hasar",               3 => "+30°/s Dönüş",
        4 => "+6 Hasar  +0.2 Yörünge", 5 => "2 Küre  +4 Hasar",
        6 => "+30°/s  +6 Hasar",       7 => "+8 Hasar",
        8 => "3 Küre  +8 Hasar  +20°/s", _ => "Güçlenir"
    };

    // ── Pasif kartları ────────────────────────────────────────────────────────

    public static List<CardDefinition> GetPassiveCards(RunLoadoutSystem rls)
    {
        var result = new List<CardDefinition>();
        if (rls == null) return result;

        AddPassiveCards(result, rls, "kan_yemini");
        AddPassiveCards(result, rls, "ruzgar_adimi");
        AddPassiveCards(result, rls, "demir_beden");
        AddPassiveCards(result, rls, "zaman_kirigi");
        AddPassiveCards(result, rls, "buyuyen_yanki");
        AddPassiveCards(result, rls, "ruh_miknatis");
        AddPassiveCards(result, rls, "atlas_bereketi");
        AddPassiveCards(result, rls, "lanetli_servet");

        return result;
    }

    static void AddPassiveCards(List<CardDefinition> list, RunLoadoutSystem rls, string passiveId)
    {
        bool has    = rls.HasPassive(passiveId);
        bool maxed  = rls.IsPassiveMaxLevel(passiveId);
        int  currLv = rls.GetPassiveLevel(passiveId);

        if (!has && rls.CanAddPassive)
            list.Add(BuildPassiveCard(passiveId, 1, 0));
        else if (has && !maxed)
            list.Add(BuildPassiveCard(passiveId, currLv + 1, currLv));
    }

    static CardDefinition BuildPassiveCard(string passiveId, int targetLevel, int currentLevel)
    {
        bool isUnlock = currentLevel == 0;
        return new CardDefinition
        {
            id             = $"passive_{passiveId}_lv{targetLevel}",
            title          = GetPassiveName(passiveId),
            description    = GetPassiveDesc(passiveId, targetLevel),
            effectPreview  = GetPassivePreview(passiveId, targetLevel),
            cardKind       = isUnlock ? CardKind.PassiveUnlock : CardKind.PassiveUpgrade,
            cardType       = CardType.Passive,
            cardClass      = GetPassiveCardClass(passiveId),
            rarity         = GetPassiveRarity(targetLevel),
            visualCategory = GetPassiveVisualCategory(passiveId),
            tags           = CardTag.None,
            passiveId      = passiveId,
            passiveLevel   = targetLevel,
            currentLevel   = currentLevel,
            maxLevel       = RunLoadoutSystem.MaxPassiveLevel,
            Apply          = GetPassiveApply(passiveId, targetLevel),
        };
    }

    // ── Pasif açıklamaları ────────────────────────────────────────────────────

    static string GetPassiveDesc(string id, int lv) => id switch
    {
        "kan_yemini"     => "Tüm saldırıların daha fazla hasar verir.",
        "ruzgar_adimi"   => "Hareket hızın artar.",
        "demir_beden"    => "Maksimum canın artar.",
        "zaman_kirigi"   => "Saldırıların daha sık tetiklenir.",
        "buyuyen_yanki"  => "Saldırı alanın genişler.",
        "ruh_miknatis"   => "XP ve altın toplama menzilin artar.",
        "atlas_bereketi" => "Daha fazla XP kazanırsın.",
        "lanetli_servet" => "Daha fazla altın kazanırsın ama düşman baskısı artar.",
        _                => "Güçlenir."
    };

    static string GetPassivePreview(string id, int lv) => id switch
    {
        "kan_yemini" => lv switch
        {
            1 => "+20 Can  +10 İyileşme  +3 Hasar",
            2 => "+25 Can  +12 İyileşme  +4 Hasar",
            3 => "+30 Can  +15 İyileşme  +5 Hasar",
            4 => "+35 Can  +20 İyileşme  +6 Hasar",
            _ => "+40 Can  +25 İyileşme  +8 Hasar"
        },
        "ruzgar_adimi" => lv switch
        {
            1 => "+0.5 Hız",
            2 => "+0.4 Hız  Dash Bekleme ↓",
            3 => "+0.4 Hız",
            4 => "+0.5 Hız  Dash Bekleme ↓",
            _ => "+0.6 Hız  +2 Dash Hızı"
        },
        "demir_beden" => lv switch
        {
            1 => "+35 Maks. Can",
            2 => "+40 Maks. Can",
            3 => "+45 Maks. Can  +15 İyileşme",
            4 => "+50 Maks. Can",
            _ => "+60 Maks. Can  +25 İyileşme"
        },
        "zaman_kirigi" => lv switch
        {
            1 => "Saldırı Hızı +%10",
            2 => "Saldırı Hızı +%10",
            3 => "Saldırı Hızı +%12  Dash Bekleme ↓",
            4 => "Saldırı Hızı +%12",
            _ => "Saldırı Hızı +%15"
        },
        "buyuyen_yanki" => lv switch
        {
            1 => "+5 Aura Hasar  +0.1 Alan",
            2 => "+6 Aura Hasar  +0.1 Alan",
            3 => "+7 Aura Hasar",
            4 => "+8 Aura Hasar  +0.15 Alan",
            _ => "+10 Aura Hasar  +0.2 Alan"
        },
        "ruh_miknatis"   => "Toplama Menzili +%25",
        "atlas_bereketi" => "XP Kazancı +%8",
        "lanetli_servet" => "Altın +%15  /  Risk +%5",
        _ => "Güçlenir."
    };

    // ── Pasif Apply lambdaları (level başına delta) ───────────────────────────

    static Action<GameObject> GetPassiveApply(string id, int targetLevel) => id switch
    {
        "kan_yemini"     => KanYeminiApply(targetLevel),
        "ruzgar_adimi"   => RuzgarAdimiApply(targetLevel),
        "demir_beden"    => DemirBedenApply(targetLevel),
        "zaman_kirigi"   => ZamanKirigiApply(targetLevel),
        "buyuyen_yanki"  => BuyuyenYankiApply(targetLevel),
        "ruh_miknatis"   => RuhMiknatisApply(targetLevel),
        "atlas_bereketi" => AtlasBereketiApply(targetLevel),
        "lanetli_servet" => LanetliServetApply(targetLevel),
        _                => _ => { }
    };

    static Action<GameObject> KanYeminiApply(int lv) => lv switch
    {
        1 => p => { Hp(p, hp => { hp.IncreaseMaxHealth(20); hp.Heal(10); }); Aura(p, a => a.damage += 3); },
        2 => p => { Hp(p, hp => { hp.IncreaseMaxHealth(25); hp.Heal(12); }); Aura(p, a => a.damage += 4); },
        3 => p => { Hp(p, hp => { hp.IncreaseMaxHealth(30); hp.Heal(15); }); Aura(p, a => a.damage += 5); },
        4 => p => { Hp(p, hp => { hp.IncreaseMaxHealth(35); hp.Heal(20); }); Aura(p, a => a.damage += 6); },
        _ => p => { Hp(p, hp => { hp.IncreaseMaxHealth(40); hp.Heal(25); }); Aura(p, a => a.damage += 8); }
    };

    static Action<GameObject> RuzgarAdimiApply(int lv) => lv switch
    {
        1 => p => Mv(p, mv => mv.moveSpeed += 0.5f),
        2 => p => Mv(p, mv => { mv.moveSpeed += 0.4f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.08f); }),
        3 => p => Mv(p, mv => mv.moveSpeed += 0.4f),
        4 => p => Mv(p, mv => { mv.moveSpeed += 0.5f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.08f); }),
        _ => p => Mv(p, mv => { mv.moveSpeed += 0.6f; mv.dashSpeed += 2f; })
    };

    static Action<GameObject> DemirBedenApply(int lv) => lv switch
    {
        1 => p => Hp(p, hp => hp.IncreaseMaxHealth(35)),
        2 => p => Hp(p, hp => hp.IncreaseMaxHealth(40)),
        3 => p => Hp(p, hp => { hp.IncreaseMaxHealth(45); hp.Heal(15); }),
        4 => p => Hp(p, hp => hp.IncreaseMaxHealth(50)),
        _ => p => Hp(p, hp => { hp.IncreaseMaxHealth(60); hp.Heal(25); })
    };

    static Action<GameObject> ZamanKirigiApply(int lv) => lv switch
    {
        1 => p => Aura(p, a => a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.90f)),
        2 => p => Aura(p, a => a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.90f)),
        3 => p => {
            Aura(p, a => a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.88f));
            Mv(p, mv => mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.05f));
        },
        4 => p => Aura(p, a => a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.88f)),
        _ => p => Aura(p, a => a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.85f))
    };

    static Action<GameObject> BuyuyenYankiApply(int lv) => lv switch
    {
        1 => p => Aura(p, a => { a.damage += 5;  a.radius += 0.10f; }),
        2 => p => Aura(p, a => { a.damage += 6;  a.radius += 0.10f; }),
        3 => p => Aura(p, a =>   a.damage += 7),
        4 => p => Aura(p, a => { a.damage += 8;  a.radius += 0.15f; }),
        _ => p => Aura(p, a => { a.damage += 10; a.radius += 0.20f; })
    };

    static Action<GameObject> RuhMiknatisApply(int lv) =>
        // Her seviye toplama menzilini %25 artırır
        _ => RunLoadoutSystem.Instance?.AddPickupRadiusPct(0.25f);

    static Action<GameObject> AtlasBereketiApply(int lv) =>
        // Her seviye XP kazancını %8 artırır
        p => { var ls = p.GetComponent<PlayerLevelSystem>(); ls?.AddXpMultiplier(0.08f); };

    static Action<GameObject> LanetliServetApply(int lv) =>
        // Her seviye altın kazancını %15 ve düşman baskısını %5 artırır
        // TODO: EnemySpawnPressure ileride spawn yoğunluğuna bağlanacak
        _ => {
            RunLoadoutSystem.Instance?.AddGoldPickupPct(0.15f);
            RunLoadoutSystem.Instance?.AddEnemySpawnPressure(0.05f);
        };

    // ── Yardımcı metotlar ─────────────────────────────────────────────────────

    static void Aura(GameObject p, Action<AutoAttackAura> f)
    { if (p == null) return; var c = p.GetComponent<AutoAttackAura>(); if (c != null) f(c); }

    static void Mv(GameObject p, Action<PlayerMovement> f)
    { if (p == null) return; var c = p.GetComponent<PlayerMovement>(); if (c != null) f(c); }

    static void Hp(GameObject p, Action<PlayerHealth> f)
    { if (p == null) return; var c = p.GetComponent<PlayerHealth>(); if (c != null) f(c); }

    static void Gold(GameObject p, Action<GoldWallet> f)
    { if (p == null) return; var c = p.GetComponent<GoldWallet>(); if (c != null) f(c); }

    static void Sphere(GameObject p, Action<AtlasSphere> f)
    { if (p == null) return; var c = p.GetComponent<AtlasSphere>(); if (c != null) f(c); }

    // ── Chest-only kartlar ────────────────────────────────────────────────────

    public static List<CardDefinition> GetChestCards() => new List<CardDefinition>
    {
        ChestCard("firavun_tozu",    "Firavun Tozu",
            "Kadim toz her dash'e keskinlik katar.",
            "+8 Dash Hasarı  +0.1 Çarpma Alanı",
            CardKind.ChestRelic, CardVisualCategory.ChestRelic,
            CardClass.Ares, CardRarity.Rare,
            p => Mv(p, mv => { mv.dashDamage += 8; mv.dashHitRadius += 0.1f; })),

        ChestCard("altin_damar",     "Altın Damar",
            "Topladığın her altın biraz daha fazla değer taşır.",
            "Altın Kazancı +%25",
            CardKind.ChestEconomy, CardVisualCategory.ChestEconomy,
            CardClass.Khaos, CardRarity.Common,
            _ => RunLoadoutSystem.Instance?.AddGoldPickupPct(0.25f)),

        ChestCard("atlas_parcasi",   "Atlas Parçası",
            "Atlas'ın kırık kabuğu — cana can katar.",
            "+25 Maks. Can  +10 İyileşme",
            CardKind.ChestSurvival, CardVisualCategory.ChestSurvival,
            CardClass.Atlas, CardRarity.Common,
            p => Hp(p, hp => { hp.IncreaseMaxHealth(25); hp.Heal(10); })),

        ChestCard("muhafiz_muhru",   "Muhafızın Mührü",
            "Kadim mühür Atlas'ın gücünü hissettirir.",
            "+5 Aura Hasarı  +0.15 Alan  +5 Küre Hasarı",
            CardKind.ChestAtlas, CardVisualCategory.ChestAtlas,
            CardClass.Atlas, CardRarity.Epic,
            p => { Aura(p, a => { a.damage += 5; a.radius += 0.15f; }); Sphere(p, s => s.damage += 5); }),

        ChestCard("golge_adim",      "Gölge Adım",
            "Gölgenin hızını giyinirsin.",
            "+0.3 Hız  Dash Bekleme ↓  +4 Dash Hasarı",
            CardKind.ChestSeal, CardVisualCategory.ChestSeal,
            CardClass.Hermes, CardRarity.Rare,
            p => Mv(p, mv => { mv.moveSpeed += 0.3f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.1f); mv.dashDamage += 4; })),

        ChestCard("atlas_cekirdegi", "Atlas Çekirdeği",
            "Çekirdeğin rezonansı Atlas Küresi'ni besler.",
            "+8 Küre Hasarı  +0.2 Yörünge  +20°/s Dönüş",
            CardKind.ChestAtlas, CardVisualCategory.ChestAtlas,
            CardClass.Atlas, CardRarity.Rare,
            p => Sphere(p, s => { s.damage += 8; s.orbitRadius += 0.2f; s.orbitSpeed += 20f; })),

        ChestCard("tas_deri",        "Taş Deri",
            "Derini taş gibi sertleştirir.",
            "+50 Maks. Can  +20 İyileşme",
            CardKind.ChestSurvival, CardVisualCategory.ChestSurvival,
            CardClass.Atlas, CardRarity.Rare,
            p => Hp(p, hp => { hp.IncreaseMaxHealth(50); hp.Heal(20); })),

        ChestCard("yasam_runu",      "Yaşam Rünü",
            "Rünün enerjisi yaraları onarır.",
            "+15 İyileşme  +30 Maks. Can",
            CardKind.ChestSurvival, CardVisualCategory.ChestSurvival,
            CardClass.Artemis, CardRarity.Common,
            p => Hp(p, hp => { hp.Heal(15); hp.IncreaseMaxHealth(30); })),

        ChestCard("atlas_pusulasi",  "Atlas Pusulası",
            "Hem altın hem bilgeliği rehberlik eder.",
            "Altın +%15  XP Kazancı +%8",
            CardKind.ChestEconomy, CardVisualCategory.ChestEconomy,
            CardClass.Atlas, CardRarity.Rare,
            p => {
                RunLoadoutSystem.Instance?.AddGoldPickupPct(0.15f);
                p?.GetComponent<PlayerLevelSystem>()?.AddXpMultiplier(0.08f);
            }),
    };

    static CardDefinition ChestCard(string id, string title, string desc, string preview,
        CardKind kind, CardVisualCategory visual, CardClass cls, CardRarity rarity,
        Action<GameObject> apply) => new CardDefinition
    {
        id             = id,
        title          = title,
        description    = desc,
        effectPreview  = preview,
        iconId         = "icon_" + id,
        cardKind       = kind,
        cardType       = CardType.Passive,
        cardClass      = cls,
        rarity         = rarity,
        visualCategory = visual,
        tags           = CardTag.None,
        Apply          = apply,
    };

    // ── Silah Uyanışı kartları ────────────────────────────────────────────────

    public static List<CardDefinition> GetWeaponAwakenings(WeaponInventory inv, RunLoadoutSystem rls)
    {
        var result = new List<CardDefinition>();
        if (inv == null || rls == null) return result;

        // Kanlı Hilal: Katana Lv8 + (kan_yemini veya buyuyen_yanki)
        if (inv.GetLevel(WeaponType.KatanaAura) >= 8 && !inv.IsWeaponAwakened(WeaponType.KatanaAura) &&
            (rls.HasPassive("kan_yemini") || rls.HasPassive("buyuyen_yanki")))
            result.Add(new CardDefinition
            {
                id             = "awakening_kanli_hilal",
                title          = "Kanlı Hilal",
                description    = "Kan ve enerji birleşerek kesici bir hilal oluşturur.",
                effectPreview  = "+20 Aura Hasarı  +0.5 Alan  Saldırı Hızı +%10",
                iconId         = "icon_kanli_hilal",
                cardKind       = CardKind.WeaponAwakening,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Ares,
                rarity         = CardRarity.Legendary,
                visualCategory = CardVisualCategory.WeaponAwakening,
                tags           = CardTag.Weapon | CardTag.Unique,
                weaponType     = WeaponType.KatanaAura,
                Apply = p => {
                    var a = p?.GetComponent<AutoAttackAura>(); if (a == null) return;
                    a.damage += 20;
                    a.radius += 0.5f;
                    a.tickInterval = Mathf.Max(0.15f, a.tickInterval * 0.90f);
                },
            });

        // Ruh Fırtınası: Kunai Lv8 + (zaman_kirigi veya ruh_miknatis)
        if (inv.GetLevel(WeaponType.RuhKunai) >= 8 && !inv.IsWeaponAwakened(WeaponType.RuhKunai) &&
            (rls.HasPassive("zaman_kirigi") || rls.HasPassive("ruh_miknatis")))
            result.Add(new CardDefinition
            {
                id             = "awakening_ruh_firtinasi",
                title          = "Ruh Fırtınası",
                description    = "Kunai'ler rüzgar gibi çoğalır ve düşmanları deler.",
                effectPreview  = "Pierce +2  +1 Kunai  Atış Hızı +%10",
                iconId         = "icon_ruh_firtinasi",
                cardKind       = CardKind.WeaponAwakening,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Artemis,
                rarity         = CardRarity.Legendary,
                visualCategory = CardVisualCategory.WeaponAwakening,
                tags           = CardTag.Weapon | CardTag.Unique,
                weaponType     = WeaponType.RuhKunai,
                Apply = p => {
                    var k = p?.GetComponent<RuhKunai>(); if (k == null) return;
                    k.pierceCount     += 2;
                    k.projectileCount += 1;
                    k.fireInterval     = Mathf.Max(0.2f, k.fireInterval * 0.90f);
                },
            });

        // Atlas Halosu: AtlasSphere Lv8 + (buyuyen_yanki veya demir_beden)
        if (inv.GetLevel(WeaponType.AtlasSphere) >= 8 && !inv.IsWeaponAwakened(WeaponType.AtlasSphere) &&
            (rls.HasPassive("buyuyen_yanki") || rls.HasPassive("demir_beden")))
            result.Add(new CardDefinition
            {
                id             = "awakening_atlas_halosu",
                title          = "Atlas Halosu",
                description    = "Küreler haleye dönüşür; güce güç katar.",
                effectPreview  = "+1 Küre  +15 Hasar  +0.3 Yörünge",
                iconId         = "icon_atlas_halosu",
                cardKind       = CardKind.WeaponAwakening,
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Atlas,
                rarity         = CardRarity.Legendary,
                visualCategory = CardVisualCategory.WeaponAwakening,
                tags           = CardTag.Weapon | CardTag.Unique,
                weaponType     = WeaponType.AtlasSphere,
                Apply = p => {
                    var s = p?.GetComponent<AtlasSphere>(); if (s == null) return;
                    s.sphereCount += 1;
                    s.damage      += 15;
                    s.orbitRadius += 0.3f;
                },
            });

        return result;
    }
}
