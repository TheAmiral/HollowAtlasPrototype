using System;
using System.Collections.Generic;
using UnityEngine;

// ─────────────────────────────────────────────────────────────────────────────
// Enums
// ─────────────────────────────────────────────────────────────────────────────

public enum CardRarity
{
    Common,     // Ortak    — gri/mavi
    Rare,       // Nadir    — mor
    Legendary,  // Efsanevi — altın
    Curse,      // Lanet    — kırmızı (olumsuz efekt ama büyük ödül)
}

public enum CardGod
{
    Nyx,       // Gece     — koyu mor     #7B2FBE
    Thanatos,  // Ölüm     — kan kırmızı  #C62828
    Atlas,     // Güç      — altın        #C9943A
    Hermes,    // Hız      — elektrik     #00BCD4
    Khaos,     // Kaos     — kaotik       #AB47BC
}

// ─────────────────────────────────────────────────────────────────────────────
// Card Data
// ─────────────────────────────────────────────────────────────────────────────

[Serializable]
public class LevelUpCard
{
    public string  id;
    public string  title;
    public string  description;
    public string  effectPreview;   // kısa "+4 Hasar  +0.25 Alan" gibi
    public string  godIcon;         // unicode sembol (☽ ⚔ ◈ ⚡ ∞)
    public string  godName;
    public CardGod god;
    public CardRarity rarity;

    // Efekti uygulayan delegate — LevelUpCardSystem çağırır
    public Action<GameObject> Apply;

    public LevelUpCard(string id, string title, string desc, string preview,
                       string icon, string godName, CardGod god, CardRarity rarity,
                       Action<GameObject> apply)
    {
        this.id           = id;
        this.title        = title;
        this.description  = desc;
        this.effectPreview = preview;
        this.godIcon      = icon;
        this.godName      = godName;
        this.god          = god;
        this.rarity       = rarity;
        this.Apply        = apply;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Kart Havuzu — tüm kartlar burada tanımlı
// ─────────────────────────────────────────────────────────────────────────────

public static class CardPool
{
    // ── Görsel sabitler ──────────────────────────────────────────────────────
    public static Color GodColor(CardGod g) => g switch
    {
        CardGod.Nyx      => new Color(0.482f, 0.184f, 0.745f, 1f),  // #7B2FBE
        CardGod.Thanatos => new Color(0.776f, 0.157f, 0.157f, 1f),  // #C62828
        CardGod.Atlas    => new Color(0.788f, 0.580f, 0.227f, 1f),  // #C9943A
        CardGod.Hermes   => new Color(0.000f, 0.737f, 0.831f, 1f),  // #00BCD4
        CardGod.Khaos    => new Color(0.671f, 0.278f, 0.737f, 1f),  // #AB47BC
        _                => Color.white,
    };

    public static Color RarityColor(CardRarity r) => r switch
    {
        CardRarity.Common    => new Color(0.608f, 0.757f, 0.922f, 1f), // açık mavi
        CardRarity.Rare      => new Color(0.671f, 0.278f, 0.737f, 1f), // mor
        CardRarity.Legendary => new Color(0.988f, 0.816f, 0.000f, 1f), // altın
        CardRarity.Curse     => new Color(0.900f, 0.100f, 0.100f, 1f), // kırmızı
        _                    => Color.white,
    };

    public static string RarityLabel(CardRarity r) => r switch
    {
        CardRarity.Common    => "ORTAK",
        CardRarity.Rare      => "NADİR",
        CardRarity.Legendary => "EFSANEVİ",
        CardRarity.Curse     => "LANET",
        _                    => "",
    };

    // ── Tüm kart listesi ─────────────────────────────────────────────────────
    private static List<LevelUpCard> _all;
    public static List<LevelUpCard> All => _all ??= BuildPool();

    static List<LevelUpCard> BuildPool()
    {
        return new List<LevelUpCard>
        {
            // ═══════════════════════════════════════
            // NYX — Gece / Karanlık Güç
            // ═══════════════════════════════════════
            new("nyx_shadow_surge", "Gece Darbesi",
                "Nyx'in karanlık lütfu aura'na işler. Etraf hasarı artar, menzil genişler.",
                "+4 Aura Hasar  +0.3 Menzil",
                "☽", "Nyx", CardGod.Nyx, CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) { aura.damage += 4; aura.radius += 0.3f; }
                }),

            new("nyx_veil", "Gölge Örtüsü",
                "Karanlık seni sarar. Dash sonrasında bir anlığına düşmanlar seni göremez.",
                "+0.08s Dash İnvulnerability  +5 Dash Hasar",
                "☽", "Nyx", CardGod.Nyx, CardRarity.Common,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) { mv.dashDamage += 5; mv.dashDuration += 0.04f; }
                }),

            new("nyx_eclipse", "Karanlık Tutulma",
                "Nyx tüm gücünü sana bağışlar. Aura hasarı fırlıyor, menzil büyük artış.",
                "+8 Aura Hasar  +0.5 Menzil  Tick Hızı +%15",
                "☽", "Nyx", CardGod.Nyx, CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) { aura.damage += 8; aura.radius += 0.5f; aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval * 0.85f); }
                }),

            // ═══════════════════════════════════════
            // THANATOS — Ölüm / Hasar
            // ═══════════════════════════════════════
            new("than_harvest", "Kanlı Hasat",
                "Her düşman canıyla seni besler. Öldürünce küçük ama sürekli iyileşme.",
                "+6 Aura Hasar",
                "⚔", "Thanatos", CardGod.Thanatos, CardRarity.Common,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) aura.damage += 6;
                }),

            new("than_final", "Son Vuruş",
                "Thanatos elini omzuna koyar. Öldürme darbesi daha da güçlenir.",
                "+10 Dash Hasar  +0.15 Dash Menzili",
                "⚔", "Thanatos", CardGod.Thanatos, CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) { mv.dashDamage += 10; mv.dashHitRadius += 0.15f; }
                }),

            new("than_frenzy", "Ölüm Çılgınlığı",
                "HP yüzde yirmi beşin altına düştüğünde Thanatos seni kuşatır. Tüm hasar ikiye katlanır.",
                "Efsanevi Pasif — Düşük HP'de ×2 Hasar",
                "⚔", "Thanatos", CardGod.Thanatos, CardRarity.Legendary,
                player =>
                {
                    // Gelecek: ThanathosFrenzyPassive component eklenecek.
                    // Şimdilik büyük stat artışı uygula.
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) { aura.damage += 12; }
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.dashDamage += 12;
                }),

            // ═══════════════════════════════════════
            // ATLAS — Güç / Dayanıklılık
            // ═══════════════════════════════════════
            new("atlas_iron", "Demir Beden",
                "Atlas'ın omuzları seninkine dönüşür. Maksimum canın büyük ölçüde artar.",
                "+25 Maks. Can  +15 Anlık İyileşme",
                "◈", "Atlas", CardGod.Atlas, CardRarity.Common,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(25); hp.Heal(15); }
                }),

            new("atlas_bulwark", "Titan Kalkanı",
                "Düşman saldırıları Atlas'ın güçlü vücuduna çarpar. Maksimum canın çok artar.",
                "+40 Maks. Can  +20 Anlık İyileşme",
                "◈", "Atlas", CardGod.Atlas, CardRarity.Rare,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(40); hp.Heal(20); }
                }),

            new("atlas_colossus", "Kolossus",
                "Atlas'ın tüm yükünü taşırsın. Canın iki katına çıkar ama hareketinde ağırlık hissedersin.",
                "+80 Maks. Can  Hız −0.4",
                "◈", "Atlas", CardGod.Atlas, CardRarity.Rare,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(80); hp.Heal(40); }
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.moveSpeed = Mathf.Max(2f, mv.moveSpeed - 0.4f);
                }),

            // ═══════════════════════════════════════
            // HERMES — Hız / Hareket
            // ═══════════════════════════════════════
            new("hermes_wind", "Rüzgar Adımı",
                "Hermes'in sandalet kanatları ayaklarına takılır. Hareket hızın belirgin şekilde artar.",
                "+0.7 Hareket Hızı",
                "⚡", "Hermes", CardGod.Hermes, CardRarity.Common,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.moveSpeed += 0.7f;
                }),

            new("hermes_ghost", "Hayalet Koşusu",
                "Dash cooldown'ın azalır, dash hızın fırlar. Savaş alanını rüzgar gibi geçersin.",
                "Dash CD −0.15s  Dash Hız +3",
                "⚡", "Hermes", CardGod.Hermes, CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) { mv.dashCooldown = Mathf.Max(0.25f, mv.dashCooldown - 0.15f); mv.dashSpeed += 3f; }
                }),

            new("hermes_quicksilver", "Cıva Refleks",
                "Hermes'in en büyük hediyesi. Hem çok daha hızlısın hem dash'in çok daha sık kullanılabilir.",
                "+1.0 Hız  Dash CD −0.2s  Dash Hız +4",
                "⚡", "Hermes", CardGod.Hermes, CardRarity.Legendary,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) { mv.moveSpeed += 1.0f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f); mv.dashSpeed += 4f; }
                }),

            // ═══════════════════════════════════════
            // KHAOS — Kaos / Rastgele
            // ═══════════════════════════════════════
            new("khaos_wild", "Kaotik Patlama",
                "Khaos'un sınırsız gücü seni sarar. Ne vereceği belli olmaz, ama her zaman büyük olur.",
                "Rastgele güçlü efekt",
                "∞", "Khaos", CardGod.Khaos, CardRarity.Legendary,
                player =>
                {
                    // Rastgele 3 stat'tan birini büyük artış
                    int roll = UnityEngine.Random.Range(0, 3);
                    if (roll == 0) { var a = player.GetComponent<AutoAttackAura>(); if(a!=null){a.damage+=14;a.radius+=0.6f;} }
                    if (roll == 1) { var h = player.GetComponent<PlayerHealth>(); if(h!=null){h.IncreaseMaxHealth(60);h.Heal(60);} }
                    if (roll == 2) { var m = player.GetComponent<PlayerMovement>(); if(m!=null){m.moveSpeed+=1.2f;m.dashSpeed+=5f;m.dashCooldown=Mathf.Max(0.2f,m.dashCooldown-0.25f);} }
                }),

            new("khaos_balance", "Kaotik Denge",
                "Khaos hepsini biraz verir. Küçük ama kapsamlı güçlenme — tüm statlar artar.",
                "+2 Hasar  +0.15 Menzil  +0.3 Hız  +10 Can",
                "∞", "Khaos", CardGod.Khaos, CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) { aura.damage += 2; aura.radius += 0.15f; }
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.moveSpeed += 0.3f;
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(10); hp.Heal(5); }
                }),

            // ═══════════════════════════════════════
            // LANET Kartlar
            // ═══════════════════════════════════════
            new("curse_brittle", "Kan Kırılganlığı",
                "Thanatos bir bedel ister. Can azalır ama saldırı gücün büyük artış kazanır.",
                "Maks. Can −20  Aura Hasar +10  Dash Hasar +8",
                "✖", "Lanет", CardGod.Thanatos, CardRarity.Curse,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(-20); }
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) aura.damage += 10;
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.dashDamage += 8;
                }),

            new("curse_slow", "Titan Yükü",
                "Atlas ağırlığını bırakır. Hız büyük düşer ama can ve aura güçlenir.",
                "Hız −0.8  Aura Hasar +7  Maks. Can +35",
                "✖", "Lanet", CardGod.Atlas, CardRarity.Curse,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null) mv.moveSpeed = Mathf.Max(1.5f, mv.moveSpeed - 0.8f);
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null) aura.damage += 7;
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null) { hp.IncreaseMaxHealth(35); hp.Heal(20); }
                }),
        };
    }

    // ── Rastgele kart seçimi (rarity ağırlıklı) ────────────────────────────
    public static List<LevelUpCard> PickRandom(int count, int playerLevel)
    {
        // Seviye arttıkça nadir kart olasılığı yükselir
        float rareChance      = Mathf.Clamp01(0.20f + playerLevel * 0.015f);
        float legendaryChance = Mathf.Clamp01(0.05f + playerLevel * 0.008f);
        float curseChance     = 0.08f;

        var pool = new List<LevelUpCard>(All);
        var result = new List<LevelUpCard>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            // Hedef rarity belirle
            float roll = UnityEngine.Random.value;
            CardRarity target;
            if (roll < curseChance)                          target = CardRarity.Curse;
            else if (roll < curseChance + legendaryChance)   target = CardRarity.Legendary;
            else if (roll < curseChance + legendaryChance + rareChance) target = CardRarity.Rare;
            else                                              target = CardRarity.Common;

            // O rarity'de kart bul
            var candidates = pool.FindAll(c => c.rarity == target && !usedIds.Contains(c.id));

            // Yoksa herhangi bir kartı al
            if (candidates.Count == 0)
                candidates = pool.FindAll(c => !usedIds.Contains(c.id));

            if (candidates.Count == 0)
                break;

            var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(pick);
            usedIds.Add(pick.id);
        }

        return result;
    }
}
