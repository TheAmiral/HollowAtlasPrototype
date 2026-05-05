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

    static List<CardDefinition> Build()
    {
        var pool = new List<CardDefinition>
        {
            // ── Hermes ───────────────────────────────────────────────────────

            Card("ruzgar_adimi", "Rüzgar Adımı",
                "Hermes'in rüzgarı adımlarını hızlandırır.",
                "+0.7 Hareket Hızı",
                CardClass.Hermes, CardRarity.Common,
                CardTag.Movement,
                player => { Mv(player, mv => mv.moveSpeed += 0.7f); }),

            Card("hayalet_kosusu", "Hayalet Koşusu",
                "Dash cooldown azalır ve dash hızın artar.",
                "-0.15 Dash CD  +3 Dash Hızı",
                CardClass.Hermes, CardRarity.Rare,
                CardTag.Movement,
                player => { Mv(player, mv => { mv.dashCooldown = Mathf.Max(0.25f, mv.dashCooldown - 0.15f); mv.dashSpeed += 3f; }); }),

            Card("uzun_adim", "Uzun Adım",
                "Dash mesafesi ve hızı artar.",
                "+0.06 Dash Süresi  +2 Dash Hızı",
                CardClass.Hermes, CardRarity.Rare,
                CardTag.Movement,
                player => { Mv(player, mv => { mv.dashDuration += 0.06f; mv.dashSpeed += 2f; }); }),

            Card("kirilan_zaman", "Kırılan Zaman",
                "Hızın ve dash kabiliyetin ciddi şekilde artar.",
                "+0.9 Hız  +2 Dash Hızı  -0.1 Dash CD",
                CardClass.Hermes, CardRarity.Epic,
                CardTag.Movement,
                player => { Mv(player, mv => { mv.moveSpeed += 0.9f; mv.dashSpeed += 2f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.1f); }); }),

            Card("civa_refleks", "Cıva Refleks",
                "Çok daha hızlı hareket eder ve dash'i çok daha sık kullanırsın.",
                "+1.0 Hız  -0.2 Dash CD  +4 Dash Hızı",
                CardClass.Hermes, CardRarity.Legendary,
                CardTag.Movement,
                player => { Mv(player, mv => { mv.moveSpeed += 1.0f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f); mv.dashSpeed += 4f; }); }),

            Card("kayma_akisi", "Kayma Akışı",
                "Hareket hızın, dash süresi ve cooldown'u birlikte iyileşir.",
                "+1.2 Hız  +0.15 Dash Süresi  -0.12 Dash CD",
                CardClass.Hermes, CardRarity.Epic,
                CardTag.Movement,
                player => { Mv(player, mv => { mv.moveSpeed += 1.2f; mv.dashDuration += 0.15f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.12f); }); }),

            // ── Nyx ──────────────────────────────────────────────────────────

            Card("gece_fisiltisi", "Gece Fısıltısı",
                "Karanlık enerji auranı hafifçe güçlendirir.",
                "+3 Aura Hasar  +0.1 Menzil",
                CardClass.Nyx, CardRarity.Common,
                CardTag.Aura,
                player => { Aura(player, a => { a.damage += 3; a.radius += 0.1f; }); }),

            Card("gece_darbesi", "Gece Darbesi",
                "Karanlık enerji auranı güçlendirir.",
                "+4 Aura Hasar  +0.3 Menzil",
                CardClass.Nyx, CardRarity.Rare,
                CardTag.Aura,
                player => { Aura(player, a => { a.damage += 4; a.radius += 0.3f; }); }),

            Card("golge_ortus", "Gölge Örtüsü",
                "Dash sonrasında gölge izi bırakır.",
                "+5 Dash Hasar  +0.04 Dash Süresi",
                CardClass.Nyx, CardRarity.Rare,
                CardTag.Aura | CardTag.Movement,
                player => { Mv(player, mv => { mv.dashDamage += 5; mv.dashDuration += 0.04f; }); }),

            Card("golge_dash", "Gölge Dash",
                "Dash daha sık kullanılabilir ve daha fazla hasar verir.",
                "+12 Dash Hasar  -0.2 Dash CD",
                CardClass.Nyx, CardRarity.Epic,
                CardTag.Aura | CardTag.Damage,
                player => { Mv(player, mv => { mv.dashDamage += 12; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f); }); }),

            Card("umbra_kapisi", "Umbra Kapısı",
                "Karanlığın zirvesi aura menzilini büyük ölçüde genişletir.",
                "+6 Aura Hasar  +0.55 Menzil",
                CardClass.Nyx, CardRarity.Epic,
                CardTag.Aura,
                player => { Aura(player, a => { a.damage += 6; a.radius += 0.55f; }); }),

            Card("karanlik_tutulma", "Karanlık Tutulma",
                "Aura hasarın, menzilin ve saldırı frekansın büyük ölçüde güçlenir.",
                "+8 Aura Hasar  +0.5 Menzil  Tick Hızı +%15",
                CardClass.Nyx, CardRarity.Legendary,
                CardTag.Aura,
                player => { Aura(player, a => { a.damage += 8; a.radius += 0.5f; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.85f); }); }),

            // ── Atlas ─────────────────────────────────────────────────────────

            Card("demir_beden", "Demir Beden",
                "Maksimum canın artar ve az miktarda iyileşirsin.",
                "+25 Maks. Can  +15 İyileşme",
                CardClass.Atlas, CardRarity.Common,
                CardTag.Defense,
                player => { Hp(player, hp => { hp.IncreaseMaxHealth(25); hp.Heal(15); }); }),

            Card("titan_kalkani", "Titan Kalkanı",
                "Maksimum canın belirgin şekilde artar.",
                "+40 Maks. Can  +20 İyileşme",
                CardClass.Atlas, CardRarity.Rare,
                CardTag.Defense,
                player => { Hp(player, hp => { hp.IncreaseMaxHealth(40); hp.Heal(20); }); }),

            Card("koruyucu_sur", "Koruyucu Sur",
                "Maksimum canın belirgin şekilde artar ve hafifçe iyileşirsin.",
                "+30 Maks. Can  +10 İyileşme",
                CardClass.Atlas, CardRarity.Rare,
                CardTag.Defense,
                player => { Hp(player, hp => { hp.IncreaseMaxHealth(30); hp.Heal(10); }); }),

            Card("atlas_yankisi", "Atlas Yankısı",
                "Aura menzilin ve hasarın güçlü biçimde artar.",
                "+7 Aura Hasar  +0.45 Menzil",
                CardClass.Atlas, CardRarity.Epic,
                CardTag.Aura | CardTag.Defense,
                player => { Aura(player, a => { a.damage += 7; a.radius += 0.45f; }); }),

            Card("kolossus", "Kolossus",
                "Çok daha dayanıklı olursun ama hareketinde ağırlık hissedersin.",
                "+80 Maks. Can  Hız -0.4",
                CardClass.Atlas, CardRarity.Legendary,
                CardTag.Defense | CardTag.RiskReward,
                player =>
                {
                    Hp(player, hp => { hp.IncreaseMaxHealth(80); hp.Heal(40); });
                    Mv(player, mv => mv.moveSpeed = Mathf.Max(2f, mv.moveSpeed - 0.4f));
                }),

            Card("atlas_laneti", "Atlas'ın Laneti",
                "Tüm savaş gücün büyük ölçüde artar.",
                "+10 Aura Hasar  +0.4 Menzil  +8 Dash Hasar",
                CardClass.Atlas, CardRarity.Legendary,
                CardTag.Aura | CardTag.Damage,
                player =>
                {
                    Aura(player, a => { a.damage += 10; a.radius += 0.4f; });
                    Mv(player, mv => mv.dashDamage += 8);
                }),

            Card("titan_yuku", "Titan Yükü",
                "Canın ve aura gücün artar ama hareket hızın azalır.",
                "+35 Maks. Can  +7 Aura Hasar  -0.8 Hız",
                CardClass.Atlas, CardRarity.Chaos,
                CardTag.Defense | CardTag.Aura | CardTag.RiskReward,
                player =>
                {
                    Hp(player, hp => { hp.IncreaseMaxHealth(35); hp.Heal(20); });
                    Aura(player, a => a.damage += 7);
                    Mv(player, mv => mv.moveSpeed = Mathf.Max(1.5f, mv.moveSpeed - 0.8f));
                }),

            Card("kirilmaz_irade", "Kırılmaz İrade",
                "Atlas'ın iradesi bedeni çelikten sağlam kılar.",
                "+55 Maks. Can  +30 İyileşme",
                CardClass.Atlas, CardRarity.Epic,
                CardTag.Defense,
                player => { Hp(player, hp => { hp.IncreaseMaxHealth(55); hp.Heal(30); }); }),

            // ── Ares ──────────────────────────────────────────────────────────

            Card("kanli_hasat", "Kanlı Hasat",
                "Savaş auran daha ölümcül hale gelir.",
                "+6 Aura Hasar",
                CardClass.Ares, CardRarity.Common,
                CardTag.Damage | CardTag.Aura,
                player => { Aura(player, a => a.damage += 6); }),

            Card("son_vurus", "Son Vuruş",
                "Dash saldırın daha ölümcül olur.",
                "+10 Dash Hasar  +0.15 Dash Radius",
                CardClass.Ares, CardRarity.Rare,
                CardTag.Damage,
                player => { Mv(player, mv => { mv.dashDamage += 10; mv.dashHitRadius += 0.15f; }); }),

            Card("kizil_kesik", "Kızıl Kesik",
                "Aura daha hızlı tetiklenir ve daha yüksek hasar verir.",
                "+5 Aura Hasar  Tick Hızı +%15",
                CardClass.Ares, CardRarity.Epic,
                CardTag.Damage | CardTag.Aura,
                player => { Aura(player, a => { a.damage += 5; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.85f); }); }),

            Card("olum_cilginligi", "Ölüm Çılgınlığı",
                "Tüm hasar kaynaklarını güçlendirir.",
                "+12 Aura Hasar  +12 Dash Hasar",
                CardClass.Ares, CardRarity.Legendary,
                CardTag.Damage | CardTag.Aura,
                player =>
                {
                    Aura(player, a => a.damage += 12);
                    Mv(player, mv => mv.dashDamage += 12);
                }),

            Card("savas_acligi", "Savaş Açlığı",
                "Her düşman canını kılarken saldırı gücün kabarır.",
                "+8 Aura Hasar  +6 Dash Hasar  Tick Hızı +%10",
                CardClass.Ares, CardRarity.Epic,
                CardTag.Damage | CardTag.Aura,
                player =>
                {
                    Aura(player, a => { a.damage += 8; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.90f); });
                    Mv(player, mv => mv.dashDamage += 6);
                }),

            Card("lanetli_guc", "Lanetli Güç",
                "Hasarın ciddi şekilde artar ama maksimum canın düşer.",
                "+14 Aura Hasar  +10 Dash Hasar  -20 Maks. Can",
                CardClass.Ares, CardRarity.Chaos,
                CardTag.Damage | CardTag.Aura | CardTag.RiskReward,
                player =>
                {
                    Hp(player, hp => hp.IncreaseMaxHealth(-20));
                    Aura(player, a => a.damage += 14);
                    Mv(player, mv => mv.dashDamage += 10);
                }),

            Card("bicici", "Biçici",
                "Tüm hasar kaynaklarını öldürücü seviyeye taşır.",
                "+10 Aura Hasar  +8 Dash Hasar  Tick Hızı +%20",
                CardClass.Ares, CardRarity.Legendary,
                CardTag.Damage | CardTag.Aura,
                player =>
                {
                    Aura(player, a => { a.damage += 10; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.80f); });
                    Mv(player, mv => mv.dashDamage += 8);
                }),

            // ── Artemis ───────────────────────────────────────────────────────

            Card("avci_gozu", "Avcı Gözü",
                "Artemis'in keskin gözü hasarını doğrulaştırır.",
                "+4 Aura Hasar  +0.2 Menzil",
                CardClass.Artemis, CardRarity.Common,
                CardTag.Damage | CardTag.Critical,
                player => { Aura(player, a => { a.damage += 4; a.radius += 0.2f; }); }),

            Card("zayif_nokta", "Zayıf Nokta",
                "Düşmanların zayıf noktasını bulan darbeler daha fazla hasar verir.",
                "+7 Aura Hasar  +5 Dash Hasar",
                CardClass.Artemis, CardRarity.Rare,
                CardTag.Damage | CardTag.Critical,
                player =>
                {
                    Aura(player, a => a.damage += 7);
                    Mv(player, mv => mv.dashDamage += 5);
                }),

            Card("olum_isareti", "Ölüm İşareti",
                "Hedeflediğin düşman kaçış yolu bulamaz.",
                "+9 Aura Hasar  +8 Dash Hasar  +0.3 Menzil",
                CardClass.Artemis, CardRarity.Epic,
                CardTag.Damage | CardTag.Critical | CardTag.Unique,
                player =>
                {
                    Aura(player, a => { a.damage += 9; a.radius += 0.3f; });
                    Mv(player, mv => mv.dashDamage += 8);
                }),

            Card("avci_ritmi", "Avcı Ritmi",
                "Her vuruş bir sonrakini hazırlar.",
                "Tick Hızı +%20  +5 Aura Hasar",
                CardClass.Artemis, CardRarity.Epic,
                CardTag.Damage | CardTag.Critical,
                player => { Aura(player, a => { a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.80f); a.damage += 5; }); }),

            Card("efsanevi_nisan", "Efsanevi Nişan",
                "Artemis'in gücü en yüksek seviyede.",
                "+12 Aura Hasar  +10 Dash Hasar  +0.4 Menzil",
                CardClass.Artemis, CardRarity.Legendary,
                CardTag.Damage | CardTag.Critical | CardTag.Unique,
                player =>
                {
                    Aura(player, a => { a.damage += 12; a.radius += 0.4f; });
                    Mv(player, mv => mv.dashDamage += 10);
                }),

            // ── Hephaistos ────────────────────────────────────────────────────

            Card("kivilcim_patlamasi", "Kıvılcım Patlaması",
                "Hephaistos'un kıvılcımı auranı tutuşturur.",
                "+5 Aura Hasar  +0.15 Menzil",
                CardClass.Hephaistos, CardRarity.Common,
                CardTag.Weapon | CardTag.Aura,
                player => { Aura(player, a => { a.damage += 5; a.radius += 0.15f; }); }),

            Card("zirh_kiran", "Zırh Kıran",
                "Saldırıların düşman direncini aşıyor.",
                "+8 Aura Hasar  +6 Dash Hasar",
                CardClass.Hephaistos, CardRarity.Rare,
                CardTag.Weapon | CardTag.Damage,
                player =>
                {
                    Aura(player, a => a.damage += 8);
                    Mv(player, mv => mv.dashDamage += 6);
                }),

            Card("dovulmus_silah", "Dövülmüş Silah",
                "Hephaistos'un demirhanesi silahını mükemmelleştirir.",
                "+10 Aura Hasar  +8 Dash Hasar  +0.2 Menzil",
                CardClass.Hephaistos, CardRarity.Epic,
                CardTag.Weapon | CardTag.Damage | CardTag.Aura,
                player =>
                {
                    Aura(player, a => { a.damage += 10; a.radius += 0.2f; });
                    Mv(player, mv => mv.dashDamage += 8);
                }),

            Card("patlama_formulu", "Patlama Formülü",
                "Aura titreşim frekansı kritik seviyeye yükselir.",
                "Tick Hızı +%25  +6 Aura Hasar",
                CardClass.Hephaistos, CardRarity.Epic,
                CardTag.Weapon | CardTag.Aura,
                player => { Aura(player, a => { a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.75f); a.damage += 6; }); }),

            Card("tanri_demiri", "Tanrı Demiri",
                "Hephaistos'un en gizli formülü silahını tanrısal kılar.",
                "+15 Aura Hasar  +12 Dash Hasar  Tick Hızı +%15",
                CardClass.Hephaistos, CardRarity.Legendary,
                CardTag.Weapon | CardTag.Damage | CardTag.Aura,
                player =>
                {
                    Aura(player, a => { a.damage += 15; a.tickInterval = Mathf.Max(0.2f, a.tickInterval * 0.85f); });
                    Mv(player, mv => mv.dashDamage += 12);
                }),

            // ── Khaos ─────────────────────────────────────────────────────────

            Card("kaotik_denge", "Kaotik Denge",
                "Tüm temel statlardan küçük miktarda güç kazanırsın.",
                "+2 Hasar  +0.15 Menzil  +0.3 Hız  +10 Can",
                CardClass.Khaos, CardRarity.Rare,
                CardTag.Random | CardTag.ShowsResultFeedback,
                player =>
                {
                    Aura(player, a => { a.damage += 2; a.radius += 0.15f; });
                    Mv(player, mv => mv.moveSpeed += 0.3f);
                    Hp(player, hp => { hp.IncreaseMaxHealth(10); hp.Heal(5); });
                }),

            Card("khaos_akisi", "Khaos Akışı",
                "Kaotik enerji tüm temel gücü dengeli artırır.",
                "+3 Aura Hasar  +0.2 Menzil  +0.25 Hız",
                CardClass.Khaos, CardRarity.Rare,
                CardTag.Random | CardTag.ShowsResultFeedback,
                player =>
                {
                    Aura(player, a => { a.damage += 3; a.radius += 0.2f; });
                    Mv(player, mv => mv.moveSpeed += 0.25f);
                }),

            Card("kaotik_patlama", "Kaotik Patlama",
                "Khaos rastgele ama büyük bir güç verir.",
                "Rastgele güçlü efekt",
                CardClass.Khaos, CardRarity.Legendary,
                CardTag.Random | CardTag.ShowsResultFeedback,
                player =>
                {
                    int roll = UnityEngine.Random.Range(0, 3);
                    if (roll == 0) { Aura(player, a => { a.damage += 14; a.radius += 0.6f; }); }
                    else if (roll == 1) { Hp(player, hp => { hp.IncreaseMaxHealth(60); hp.Heal(60); }); }
                    else { Mv(player, mv => { mv.moveSpeed += 1.2f; mv.dashSpeed += 5f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.25f); }); }
                }),

            Card("khaos_aynasi", "Khaos Aynası",
                "Khaos tüm olasılıkları değerlendirir ve güçlü bir yol seçer.",
                "Rastgele güçlü kombinasyon",
                CardClass.Khaos, CardRarity.Chaos,
                CardTag.Random | CardTag.ShowsResultFeedback,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    var mv   = player.GetComponent<PlayerMovement>();
                    var hp   = player.GetComponent<PlayerHealth>();
                    switch (UnityEngine.Random.Range(0, 4))
                    {
                        case 0:
                            if (aura != null) { aura.damage += 10; aura.radius += 0.3f; }
                            if (mv   != null) mv.dashDamage += 8;
                            break;
                        case 1:
                            if (mv != null) { mv.moveSpeed += 1.2f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f); mv.dashSpeed += 4f; }
                            break;
                        case 2:
                            if (hp   != null) { hp.IncreaseMaxHealth(55); hp.Heal(35); }
                            if (aura != null) aura.damage += 5;
                            break;
                        case 3:
                            if (aura != null) { aura.damage += 7; aura.radius += 0.25f; }
                            if (mv   != null) mv.moveSpeed += 0.7f;
                            if (hp   != null) hp.Heal(20);
                            break;
                    }
                }),

            Card("kirilganlik_lutfu", "Kırılganlık Lütfu",
                "Muazzam saldırı gücü kazanırsın ama bedenin zayıflar.",
                "+18 Aura Hasar  +14 Dash Hasar  -30 Maks. Can",
                CardClass.Khaos, CardRarity.Chaos,
                CardTag.Random | CardTag.RiskReward | CardTag.ShowsResultFeedback,
                player =>
                {
                    Hp(player, hp => hp.IncreaseMaxHealth(-30));
                    Aura(player, a => a.damage += 18);
                    Mv(player, mv => mv.dashDamage += 14);
                })
        };

        return pool;
    }

    // ── Builder helper ────────────────────────────────────────────────────────

    static CardDefinition Card(
        string id, string title, string desc, string preview,
        CardClass cls, CardRarity rarity, CardTag tags,
        Action<GameObject> apply)
    {
        return new CardDefinition
        {
            id            = id,
            title         = title,
            description   = desc,
            effectPreview = preview,
            cardClass     = cls,
            rarity        = rarity,
            visualCategory = ResolveVisualCategory(id, cls, rarity, tags),
            tags          = tags,
            Apply         = apply
        };
    }

    // ── Convenience component accessors ──────────────────────────────────────

    static CardVisualCategory ResolveVisualCategory(string id, CardClass cls, CardRarity rarity, CardTag tags)
    {
        if (cls == CardClass.Khaos ||
            rarity == CardRarity.Chaos ||
            Has(tags, CardTag.Random) ||
            Has(tags, CardTag.RiskReward))
            return CardVisualCategory.Chaos;

        return id switch
        {
            "demir_beden" or
            "titan_kalkani" or
            "koruyucu_sur" or
            "kirilmaz_irade" => CardVisualCategory.Sustain,

            "ruzgar_adimi" or
            "hayalet_kosusu" or
            "uzun_adim" or
            "kirilan_zaman" or
            "civa_refleks" or
            "kayma_akisi" => CardVisualCategory.Mobility,

            "gece_fisiltisi" or
            "gece_darbesi" or
            "golge_ortus" or
            "golge_dash" or
            "umbra_kapisi" or
            "karanlik_tutulma" or
            "atlas_yankisi" or
            "atlas_laneti" or
            "dovulmus_silah" => CardVisualCategory.Combo,

            _ => ResolveVisualCategoryFromTags(tags)
        };
    }

    static CardVisualCategory ResolveVisualCategoryFromTags(CardTag tags)
    {
        bool sustain = Has(tags, CardTag.Defense);
        bool mobility = Has(tags, CardTag.Movement);
        bool damage = Has(tags, CardTag.Damage) ||
                      Has(tags, CardTag.Aura) ||
                      Has(tags, CardTag.Critical) ||
                      Has(tags, CardTag.Weapon);

        if ((sustain && (damage || mobility)) || (damage && mobility))
            return CardVisualCategory.Combo;
        if (sustain)
            return CardVisualCategory.Sustain;
        if (mobility)
            return CardVisualCategory.Mobility;
        if (damage)
            return CardVisualCategory.Damage;

        return CardVisualCategory.Damage;
    }

    static bool Has(CardTag tags, CardTag tag) => (tags & tag) != 0;

    static void Aura(GameObject p, Action<AutoAttackAura> f)
    {
        if (p == null) return;
        var c = p.GetComponent<AutoAttackAura>();
        if (c != null) f(c);
    }

    static void Mv(GameObject p, Action<PlayerMovement> f)
    {
        if (p == null) return;
        var c = p.GetComponent<PlayerMovement>();
        if (c != null) f(c);
    }

    static void Hp(GameObject p, Action<PlayerHealth> f)
    {
        if (p == null) return;
        var c = p.GetComponent<PlayerHealth>();
        if (c != null) f(c);
    }
}
