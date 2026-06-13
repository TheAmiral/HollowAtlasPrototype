// ─────────────────────────────────────────────────────────────────────────────
//  CardDatabase.cs  (YENİDEN YAZILDI — VS Sistemi)
//  Assets/Scripts/Systems/Cards/CardDatabase.cs
//
//  3 katman:
//    1. SİLAH KARTLARI   — WeaponInventory üzerinden level atlar
//    2. PASİF KARTLAR    — Direkt stat boost (hız, hasar, can)
//    3. KHAOS KARTLARI   — Rastgele / riskli efektler
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardDatabase
{
    static List<CardDefinition> _all;
    public static IReadOnlyList<CardDefinition> All => _all ??= Build();

    public static CardDefinition GetById(string id)
    {
        foreach (var c in All)
            if (c.id == id) return c;
        return null;
    }

    // ── Silah kartlarını çalışma zamanında üret (WeaponInventory'ye göre)
    //    CardOfferGenerator bu listeyi filtreler.
    public static List<CardDefinition> GetWeaponCards(WeaponInventory inv)
    {
        var result = new List<CardDefinition>();
        if (inv == null) return result;

        AddWeaponCards(result, inv, WeaponType.KatanaAura,  "katana");
        AddWeaponCards(result, inv, WeaponType.RuhKunai,    "kunai");
        AddWeaponCards(result, inv, WeaponType.AtlasSphere, "sphere");
        return result;
    }

    static void AddWeaponCards(List<CardDefinition> list, WeaponInventory inv,
                               WeaponType wt, string prefix)
    {
        int currentLevel = inv.GetLevel(wt);
        bool has = inv.HasWeapon(wt);
        bool maxed = inv.IsMaxLevel(wt);

        if (!has)
        {
            // Silah açılış kartı
            list.Add(WeaponUnlockCard(wt));
        }
        else if (!maxed)
        {
            // Seviye atlama kartı
            list.Add(WeaponLevelCard(wt, currentLevel + 1));
        }
        // Maxed silahlar listeye girmez
    }

    // ── Build: tüm pasif + khaos kartlar ────────────────────────────────────

    static List<CardDefinition> Build()
    {
        var pool = new List<CardDefinition>();

        // ──────────────────────────────────────────────
        //  PASİF KARTLAR — Stat boost (VS: passives)
        // ──────────────────────────────────────────────

        // HAREKETLİLİK
        pool.Add(Passive("hiz_artisi_1", "Rüzgar Adımı",
            "Hareket hızın artar.",
            "+0.7 Hız",
            CardRarity.Common, CardVisualCategory.Mobility,
            CardTag.Movement,
            p => Mv(p, mv => mv.moveSpeed += 0.7f)));

        pool.Add(Passive("hiz_artisi_2", "Fırtına Adımı",
            "Hareket hızın belirgin şekilde artar.",
            "+1.2 Hız",
            CardRarity.Rare, CardVisualCategory.Mobility,
            CardTag.Movement,
            p => Mv(p, mv => mv.moveSpeed += 1.2f)));

        pool.Add(Passive("hiz_artisi_3", "Cıva Ayaklar",
            "Hareket hızın çok artar.",
            "+1.8 Hız",
            CardRarity.Epic, CardVisualCategory.Mobility,
            CardTag.Movement,
            p => Mv(p, mv => mv.moveSpeed += 1.8f)));

        pool.Add(Passive("dash_cd_1", "Hızlı Refleks",
            "Dash cooldown azalır.",
            "-0.15 Dash CD",
            CardRarity.Common, CardVisualCategory.Mobility,
            CardTag.Movement,
            p => Mv(p, mv => mv.dashCooldown = Mathf.Max(0.25f, mv.dashCooldown - 0.15f))));

        pool.Add(Passive("dash_cd_2", "Gölge Refleks",
            "Dash çok daha sık kullanılabilir.",
            "-0.25 Dash CD",
            CardRarity.Rare, CardVisualCategory.Mobility,
            CardTag.Movement,
            p => Mv(p, mv => mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.25f))));

        // CAN / SAVUNMA
        pool.Add(Passive("can_artisi_1", "Demir Ten",
            "Maksimum canın artar.",
            "+25 Maks. Can",
            CardRarity.Common, CardVisualCategory.Sustain,
            CardTag.Defense,
            p => Hp(p, hp => { hp.IncreaseMaxHealth(25); hp.Heal(15); })));

        pool.Add(Passive("can_artisi_2", "Titan Derisi",
            "Maksimum canın belirgin şekilde artar.",
            "+45 Maks. Can",
            CardRarity.Rare, CardVisualCategory.Sustain,
            CardTag.Defense,
            p => Hp(p, hp => { hp.IncreaseMaxHealth(45); hp.Heal(25); })));

        pool.Add(Passive("can_artisi_3", "Atlas Bedeni",
            "Maksimum canın büyük ölçüde artar.",
            "+70 Maks. Can",
            CardRarity.Epic, CardVisualCategory.Sustain,
            CardTag.Defense,
            p => Hp(p, hp => { hp.IncreaseMaxHealth(70); hp.Heal(40); })));

        pool.Add(Passive("iyilesme_1", "Hayat Damarı",
            "Hafifçe iyileşirsin.",
            "+30 İyileşme",
            CardRarity.Common, CardVisualCategory.Sustain,
            CardTag.Defense,
            p => Hp(p, hp => hp.Heal(30))));

        pool.Add(Passive("iyilesme_2", "Yaşam Kaynağı",
            "İyi miktarda iyileşirsin.",
            "+55 İyileşme",
            CardRarity.Rare, CardVisualCategory.Sustain,
            CardTag.Defense,
            p => Hp(p, hp => hp.Heal(55))));

        // HASAR GENELİ
        pool.Add(Passive("dash_hasar_1", "Keskin Dash",
            "Dash hasarın artar.",
            "+10 Dash Hasar",
            CardRarity.Common, CardVisualCategory.Damage,
            CardTag.Damage,
            p => Mv(p, mv => mv.dashDamage += 10)));

        pool.Add(Passive("dash_hasar_2", "Ölüm Darbesi",
            "Dash hasarın belirgin şekilde artar.",
            "+18 Dash Hasar",
            CardRarity.Rare, CardVisualCategory.Damage,
            CardTag.Damage,
            p => Mv(p, mv => mv.dashDamage += 18)));

        pool.Add(Passive("dash_hasar_3", "Katil Refleks",
            "Dash hasarın çok artar.",
            "+28 Dash Hasar",
            CardRarity.Epic, CardVisualCategory.Damage,
            CardTag.Damage,
            p => Mv(p, mv => mv.dashDamage += 28)));

        // KARMA / RİSK-ÖDÜL
        pool.Add(Passive("kor_hiz", "Kör Hız",
            "Hız artar ama aura daralır.",
            "+1.0 Hız  -0.35 Aura",
            CardRarity.Rare, CardVisualCategory.Combo,
            CardTag.Movement | CardTag.RiskReward,
            p => {
                Mv(p, mv => mv.moveSpeed += 1.0f);
                Aura(p, a => a.radius = Mathf.Max(0.5f, a.radius - 0.35f));
            }));

        pool.Add(Passive("lanetli_guc", "Lanetli Güç",
            "Hasar artar ama maksimum can düşer.",
            "+14 Dash Hasar  -20 Can",
            CardRarity.Rare, CardVisualCategory.Combo,
            CardTag.Damage | CardTag.RiskReward,
            p => {
                Mv(p, mv => mv.dashDamage += 14);
                Hp(p, hp => hp.IncreaseMaxHealth(-20));
            }));

        pool.Add(Passive("kolossus", "Kolossus",
            "Çok dayanıklı olursun ama yavaşlarsın.",
            "+80 Can  -0.4 Hız",
            CardRarity.Epic, CardVisualCategory.Combo,
            CardTag.Defense | CardTag.RiskReward,
            p => {
                Hp(p, hp => { hp.IncreaseMaxHealth(80); hp.Heal(40); });
                Mv(p, mv => mv.moveSpeed = Mathf.Max(2f, mv.moveSpeed - 0.4f));
            }));

        // ──────────────────────────────────────────────
        //  KHAOS KARTLARI — Rastgele / özel efektler
        // ──────────────────────────────────────────────

        pool.Add(Khaos("kaotik_denge", "Kaotik Denge",
            "Tüm statlardan küçük güç kazanırsın.",
            "+2 Hasar  +0.15 Menzil  +0.3 Hız  +10 Can",
            CardRarity.Rare,
            p => {
                Aura(p, a => { a.damage += 2; a.radius += 0.15f; });
                Mv(p, mv => mv.moveSpeed += 0.3f);
                Hp(p, hp => { hp.IncreaseMaxHealth(10); hp.Heal(5); });
            }));

        pool.Add(Khaos("kaotik_patlama", "Kaotik Patlama",
            "Khaos rastgele ama büyük bir güç verir.",
            "Rastgele güçlü efekt",
            CardRarity.Legendary,
            p => {
                int roll = UnityEngine.Random.Range(0, 3);
                if (roll == 0) { Aura(p, a => { a.damage += 14; a.radius += 0.6f; }); }
                else if (roll == 1) { Hp(p, hp => { hp.IncreaseMaxHealth(60); hp.Heal(60); }); }
                else { Mv(p, mv => { mv.moveSpeed += 1.2f; mv.dashSpeed += 5f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.25f); }); }
            }));

        pool.Add(Khaos("khaos_carki", "Khaos Çarkı",
            "Güçlü bir rastgele stat demeti.",
            "Rastgele büyük kazanç",
            CardRarity.Legendary,
            p => {
                int roll = UnityEngine.Random.Range(0, 4);
                if      (roll == 0) { Aura(p, a => { a.damage += 12; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.85f); }); }
                else if (roll == 1) { Mv(p, mv => { mv.moveSpeed += 1.0f; mv.dashDamage += 10; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.15f); }); }
                else if (roll == 2) { Hp(p, hp => { hp.IncreaseMaxHealth(70); hp.Heal(50); }); Aura(p, a => a.radius += 0.4f); }
                else               { Aura(p, a => { a.damage += 8; a.radius += 0.3f; }); Mv(p, mv => mv.dashDamage += 8); Hp(p, hp => hp.Heal(25)); }
            }));

        pool.Add(Khaos("kirilganlik_lutfu", "Kırılganlık Lütfu",
            "Muazzam saldırı gücü ama beden zayıflar.",
            "+18 Hasar  +14 Dash  -30 Can",
            CardRarity.Chaos,
            p => {
                Hp(p, hp => hp.IncreaseMaxHealth(-30));
                Aura(p, a => a.damage += 18);
                Mv(p, mv => mv.dashDamage += 14);
            }));

        pool.Add(Khaos("atlas_kumarhanesi", "Atlas'ın Kumarhanesi",
            "3/4 ihtimalle büyük kazanç, 1/4 ihtimalle kayıp.",
            "Yüksek kazanç veya küçük bedel",
            CardRarity.Chaos,
            p => {
                if (UnityEngine.Random.Range(0, 4) < 3)
                {
                    Aura(p, a => { a.damage += 10; a.radius += 0.3f; });
                    Hp(p, hp => { hp.IncreaseMaxHealth(25); hp.Heal(15); });
                }
                else
                {
                    Hp(p, hp => hp.IncreaseMaxHealth(-20));
                    Mv(p, mv => mv.moveSpeed = Mathf.Max(1.5f, mv.moveSpeed - 0.3f));
                }
            }));

        return pool;
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  SİLAH KART FABRİKASI — WeaponInventory çağırdığında üretilir
    // ──────────────────────────────────────────────────────────────────────────

    static CardDefinition WeaponUnlockCard(WeaponType wt) => wt switch
    {
        WeaponType.KatanaAura => new CardDefinition
        {
            id = "unlock_katana", title = "Katana Aura",
            description    = "Düşmanları çevreleyen enerji alanı açılır.",
            effectPreview  = "Aura Lv1 açılır",
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Ares,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponKatana,
            tags           = CardTag.Weapon | CardTag.Aura,
            weaponType     = WeaponType.KatanaAura,
            weaponLevel    = 1,
        },
        WeaponType.RuhKunai => new CardDefinition
        {
            id = "unlock_kunai", title = "Ruh Kunai",
            description    = "En yakın düşmanlara otomatik kunai fırlatır.",
            effectPreview  = "Ruh Kunai Lv1 açılır",
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Artemis,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponKunai,
            tags           = CardTag.Weapon | CardTag.Damage,
            weaponType     = WeaponType.RuhKunai,
            weaponLevel    = 1,
        },
        WeaponType.AtlasSphere => new CardDefinition
        {
            id = "unlock_sphere", title = "Atlas Küresi",
            description    = "Etrafında dönen enerji küreleri düşmanlara hasar verir.",
            effectPreview  = "Atlas Küresi Lv1 açılır",
            cardType       = CardType.WeaponUpgrade,
            cardClass      = CardClass.Atlas,
            rarity         = CardRarity.Common,
            visualCategory = CardVisualCategory.WeaponSphere,
            tags           = CardTag.Weapon | CardTag.Aura,
            weaponType     = WeaponType.AtlasSphere,
            weaponLevel    = 1,
        },
        _ => null
    };

    static CardDefinition WeaponLevelCard(WeaponType wt, int targetLevel)
    {
        string levelTag = $"Lv{targetLevel}";
        string rarity = targetLevel switch { <= 3 => "Common", <= 5 => "Rare", <= 7 => "Epic", _ => "Legendary" };
        CardRarity r = targetLevel switch { <= 3 => CardRarity.Common, <= 5 => CardRarity.Rare, <= 7 => CardRarity.Epic, _ => CardRarity.Legendary };

        return wt switch
        {
            WeaponType.KatanaAura => new CardDefinition
            {
                id = $"katana_lv{targetLevel}", title = $"Katana Aura {levelTag}",
                description    = KatanaDesc(targetLevel),
                effectPreview  = KatanaPreview(targetLevel),
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Ares,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponKatana,
                tags           = CardTag.Weapon | CardTag.Aura,
                weaponType     = WeaponType.KatanaAura,
                weaponLevel    = targetLevel,
            },
            WeaponType.RuhKunai => new CardDefinition
            {
                id = $"kunai_lv{targetLevel}", title = $"Ruh Kunai {levelTag}",
                description    = KunaiDesc(targetLevel),
                effectPreview  = KunaiPreview(targetLevel),
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Artemis,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponKunai,
                tags           = CardTag.Weapon | CardTag.Damage,
                weaponType     = WeaponType.RuhKunai,
                weaponLevel    = targetLevel,
            },
            WeaponType.AtlasSphere => new CardDefinition
            {
                id = $"sphere_lv{targetLevel}", title = $"Atlas Küresi {levelTag}",
                description    = SphereDesc(targetLevel),
                effectPreview  = SpherePreview(targetLevel),
                cardType       = CardType.WeaponUpgrade,
                cardClass      = CardClass.Atlas,
                rarity         = r,
                visualCategory = CardVisualCategory.WeaponSphere,
                tags           = CardTag.Weapon | CardTag.Aura,
                weaponType     = WeaponType.AtlasSphere,
                weaponLevel    = targetLevel,
            },
            _ => null
        };
    }

    // ── Silah açıklamaları ────────────────────────────────────────────────────

    static string KatanaDesc(int lv) => lv switch
    {
        2 => "Aura hasarı ve menzili artar.", 3 => "Aura daha hızlı titreşir.",
        4 => "Hasar ve menzil güçlenir.",     5 => "Titreşim frekansı artar.",
        6 => "Aura büyür ve güçlenir.",       7 => "Frekans kritik seviyeye ulaşır.",
        8 => "Katana Aura maksimum güce erişir.", _ => "Silah güçlenir."
    };
    static string KatanaPreview(int lv) => lv switch
    {
        2 => "+4 Hasar  +0.2 Menzil",  3 => "+5 Hasar  Tick +10%",
        4 => "+6 Hasar  +0.25 Menzil", 5 => "+8 Hasar  Tick +12%",
        6 => "+10 Hasar +0.3 Menzil",  7 => "+12 Hasar Tick +12%",
        8 => "+15 Hasar +0.4 Menzil  Tick MAX", _ => "Güçlenir"
    };

    static string KunaiDesc(int lv) => lv switch
    {
        2 => "Kunai hasarı artar.",              3 => "Kunai daha sık fırlatılır.",
        4 => "İkinci kunai açılır, hasar artar.", 5 => "Hasar güçlenir.",
        6 => "Atış hızı artar.",                  7 => "Üçüncü kunai açılır.",
        8 => "Ruh Kunai maksimum güce erişir.",   _ => "Silah güçlenir."
    };
    static string KunaiPreview(int lv) => lv switch
    {
        2 => "+5 Hasar",        3 => "-0.1 Atış CD",
        4 => "2 Kunai  +5 Hasar", 5 => "+8 Hasar",
        6 => "-0.1 Atış CD",    7 => "3 Kunai  +8 Hasar",
        8 => "+12 Hasar  Hız MAX", _ => "Güçlenir"
    };

    static string SphereDesc(int lv) => lv switch
    {
        2 => "Küre hasarı artar.",            3 => "Küre daha hızlı döner.",
        4 => "Hasar ve yörünge genişler.",    5 => "İkinci küre açılır.",
        6 => "Dönüş hızı ve hasar artar.",   7 => "Hasar büyük ölçüde artar.",
        8 => "Üçüncü küre açılır, maksimum güç.", _ => "Silah güçlenir."
    };
    static string SpherePreview(int lv) => lv switch
    {
        2 => "+4 Hasar",              3 => "+30°/s Dönüş",
        4 => "+6 Hasar  +0.2 Yörünge", 5 => "2 Küre  +4 Hasar",
        6 => "+30°/s  +6 Hasar",      7 => "+8 Hasar",
        8 => "3 Küre  +8 Hasar  +20°/s", _ => "Güçlenir"
    };

    // ── Builder helper'ları ───────────────────────────────────────────────────

    static CardDefinition Passive(
        string id, string title, string desc, string preview,
        CardRarity rarity, CardVisualCategory vc, CardTag tags,
        Action<GameObject> apply)
    {
        return new CardDefinition
        {
            id = id, title = title, description = desc, effectPreview = preview,
            cardType = CardType.Passive,
            cardClass = CardClass.Atlas, // geriye dönük uyumluluk
            rarity = rarity, visualCategory = vc, tags = tags,
            Apply = apply,
        };
    }

    static CardDefinition Khaos(
        string id, string title, string desc, string preview,
        CardRarity rarity, Action<GameObject> apply)
    {
        return new CardDefinition
        {
            id = id, title = title, description = desc, effectPreview = preview,
            cardType = CardType.Chaos,
            cardClass = CardClass.Khaos,
            rarity = rarity,
            visualCategory = CardVisualCategory.Chaos,
            tags = CardTag.Random | CardTag.ShowsResultFeedback,
            Apply = apply,
        };
    }

    // ── Bileşen erişim yardımcıları ──────────────────────────────────────────

    static void Aura(GameObject p, Action<AutoAttackAura> f)
    { var c = p?.GetComponent<AutoAttackAura>(); if (c != null) f(c); }

    static void Mv(GameObject p, Action<PlayerMovement> f)
    { var c = p?.GetComponent<PlayerMovement>(); if (c != null) f(c); }

    static void Hp(GameObject p, Action<PlayerHealth> f)
    { var c = p?.GetComponent<PlayerHealth>(); if (c != null) f(c); }
}
