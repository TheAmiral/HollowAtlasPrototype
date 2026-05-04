using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardRarity
{
    Unknown = 0,
    Common = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Cursed = 5
}

public enum CardGod
{
    Nyx,
    Thanatos,
    Atlas,
    Hermes,
    Khaos
}

[Flags]
public enum CardEffectTag
{
    None = 0,
    AuraDamage = 1 << 0,
    AuraRange = 1 << 1,
    AuraSpeed = 1 << 2,
    DashDamage = 1 << 3,
    DashMobility = 1 << 4,
    Movement = 1 << 5,
    Health = 1 << 6,
    Heal = 1 << 7,
    Random = 1 << 8,
    Gold = 1 << 9,
    Cursed = 1 << 10
}

public struct CardRarityPresentation
{
    public string Label { get; private set; }
    public Color BadgeColor { get; private set; }
    public Color TextColor { get; private set; }

    public CardRarityPresentation(string label, Color badgeColor, Color textColor)
    {
        Label = label;
        BadgeColor = badgeColor;
        TextColor = textColor;
    }
}

public struct CardRarityProfile
{
    public int PowerScore { get; private set; }
    public CardEffectTag Tags { get; private set; }
    public bool HasTradeoff { get; private set; }
    public bool IsBuildDefining { get; private set; }

    public CardRarityProfile(
        int powerScore,
        CardEffectTag tags,
        bool hasTradeoff = false,
        bool isBuildDefining = false)
    {
        PowerScore = powerScore;
        Tags = tags;
        HasTradeoff = hasTradeoff;
        IsBuildDefining = isBuildDefining;
    }
}

[Serializable]
public class LevelUpCard
{
    public string id;
    public string title;
    public string description;
    public string effectPreview;
    public string godIcon;
    public string godName;
    public CardGod god;
    public CardRarity rarity { get; private set; }
    public CardRarity DesignRarity => rarity;
    public int powerScore { get; private set; }
    public CardEffectTag effectTags { get; private set; }
    public bool hasTradeoff { get; private set; }
    public bool isBuildDefining { get; private set; }

    public Action<GameObject> Apply;

    public LevelUpCard(
        string id,
        string title,
        string desc,
        string preview,
        string icon,
        string godName,
        CardGod god,
        CardRarity rarity,
        Action<GameObject> apply,
        int powerScore = -1,
        CardEffectTag effectTags = CardEffectTag.None,
        bool hasTradeoff = false,
        bool isBuildDefining = false)
    {
        this.id = id;
        this.title = title;
        this.description = desc;
        this.effectPreview = preview;
        this.godIcon = icon;
        this.godName = godName;
        this.god = god;
        this.rarity = rarity;
        this.Apply = apply;
        this.powerScore = powerScore;
        this.effectTags = effectTags;
        this.hasTradeoff = hasTradeoff;
        this.isBuildDefining = isBuildDefining;

        if (!CardPool.IsValidRarity(rarity))
            CardRarityResolver.LogWarning($"[LevelUpCard] Missing rarity for card: {title}");
    }

    public void SetRarityProfile(CardRarityProfile profile)
    {
        powerScore = profile.PowerScore;
        effectTags = profile.Tags;
        hasTradeoff = profile.HasTradeoff;
        isBuildDefining = profile.IsBuildDefining;
    }

    public CardRarity GetExpectedRarity() => CardRarityResolver.GetExpectedRarity(this);

    public CardRarity GetDisplayRarity() => CardRarityResolver.GetDisplayRarity(this);

    public CardRarity GetResolvedRarity() => GetDisplayRarity();

    public LevelUpCard CreateRuntimeCopy()
    {
        return new LevelUpCard(
            id,
            title,
            description,
            effectPreview,
            godIcon,
            godName,
            god,
            rarity,
            Apply,
            powerScore,
            effectTags,
            hasTradeoff,
            isBuildDefining
        );
    }
}

public static class CardRarityResolver
{
    public static bool DiagnosticsEnabled
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    public static void LogWarning(string message)
    {
        if (DiagnosticsEnabled)
            Debug.LogWarning(message);
    }

    public static void Log(string message)
    {
        if (DiagnosticsEnabled)
            Debug.Log(message);
    }

    public static CardRarity GetExpectedRarity(LevelUpCard card)
    {
        if (card == null)
            return CardRarity.Unknown;

        if (card.powerScore < 0)
        {
            LogWarning($"[LevelUpCard] Missing powerScore for card: {card.title}");
            return CardRarity.Unknown;
        }

        if ((card.effectTags & CardEffectTag.Cursed) != 0)
            return CardRarity.Cursed;

        int score = Mathf.Clamp(card.powerScore, 0, 100);
        CardRarity expected;

        if (score >= 75)
            expected = CardRarity.Legendary;
        else if (score >= 50)
            expected = CardRarity.Epic;
        else if (score >= 25)
            expected = CardRarity.Rare;
        else
            expected = CardRarity.Common;

        if ((card.hasTradeoff || card.isBuildDefining) && (int)expected < (int)CardRarity.Epic)
            expected = CardRarity.Epic;

        return expected;
    }

    public static void LogRarityAuditWarning(LevelUpCard card, CardRarity expected, string reason)
    {
        if (card == null)
        {
            LogWarning($"[LevelUpCard][RARITY AUDIT WARNING] Missing card data. Expected={expected} Reason={reason}");
            return;
        }

        LogWarning(
            $"[LevelUpCard][RARITY AUDIT WARNING] Id={card.id} Title={card.title} Assigned={card.DesignRarity} Expected={expected} Score={card.powerScore} Reason={reason}"
        );
    }

    public static CardRarity GetDisplayRarity(LevelUpCard card)
    {
        if (card == null)
            return CardRarity.Unknown;

        if (!CardPool.IsValidRarity(card.DesignRarity))
        {
            LogWarning($"[LevelUpCard] Missing rarity for card: {card.title}");
            return CardRarity.Unknown;
        }

        CardRarity expected = GetExpectedRarity(card);

        if (CardPool.IsValidRarity(expected) && card.DesignRarity != expected)
        {
            LogRarityAuditWarning(card, expected, "Assigned rarity differs from audit profile.");
        }

        return card.DesignRarity;
    }

    public static CardRarity GetResolvedRarity(LevelUpCard card) => GetDisplayRarity(card);
}

public static class CardPool
{
    public static Color GodColor(CardGod g) => g switch
    {
        CardGod.Nyx => new Color(0.482f, 0.184f, 0.745f, 1f),
        CardGod.Thanatos => new Color(0.776f, 0.157f, 0.157f, 1f),
        CardGod.Atlas => new Color(0.788f, 0.580f, 0.227f, 1f),
        CardGod.Hermes => new Color(0.000f, 0.737f, 0.831f, 1f),
        CardGod.Khaos => new Color(0.671f, 0.278f, 0.737f, 1f),
        _ => Color.white
    };

    public static bool IsValidRarity(CardRarity r) =>
        r == CardRarity.Common ||
        r == CardRarity.Rare ||
        r == CardRarity.Epic ||
        r == CardRarity.Legendary ||
        r == CardRarity.Cursed;

    public static CardRarityPresentation GetRarityPresentation(CardRarity r)
    {
        if (!IsValidRarity(r))
        {
            CardRarityResolver.LogWarning($"[LevelUpCard] Missing rarity presentation for rarity: {r}");
            return new CardRarityPresentation(
                "BİLİNMİYOR",
                new Color(0.20f, 0.18f, 0.22f, 0.98f),
                new Color(1.00f, 0.60f, 0.60f, 1f)
            );
        }

        switch (r)
        {
            case CardRarity.Common:
                return new CardRarityPresentation(
                    "YAYGIN",
                    new Color(0.21f, 0.21f, 0.27f, 0.98f),
                    new Color(0.86f, 0.86f, 0.92f, 1f)
                );

            case CardRarity.Rare:
                return new CardRarityPresentation(
                    "NADİR",
                    new Color(0.05f, 0.34f, 0.58f, 0.98f),
                    new Color(0.74f, 0.94f, 1.00f, 1f)
                );

            case CardRarity.Epic:
                return new CardRarityPresentation(
                    "ENDER",
                    new Color(0.34f, 0.14f, 0.56f, 0.98f),
                    new Color(0.90f, 0.78f, 1.00f, 1f)
                );

            case CardRarity.Legendary:
                return new CardRarityPresentation(
                    "DESTANSI",
                    new Color(0.62f, 0.34f, 0.04f, 0.98f),
                    new Color(1.00f, 0.90f, 0.48f, 1f)
                );

            case CardRarity.Cursed:
                return new CardRarityPresentation(
                    "LANETLİ",
                    new Color(0.54f, 0.05f, 0.08f, 0.98f),
                    new Color(1.00f, 0.72f, 0.72f, 1f)
                );

            default:
                throw new ArgumentOutOfRangeException(nameof(r), r, "Unknown card rarity.");
        }
    }

    private static List<LevelUpCard> _all;
    public static List<LevelUpCard> All => _all ??= BuildPool();

    static List<LevelUpCard> BuildPool()
    {
        var pool = new List<LevelUpCard>
        {
            new LevelUpCard(
                "nyx_veil",
                "Gölge Örtüsü",
                "Dash sonrasında kısa süreli gölge izi bırakır. Dash hasarı ve dash süresi hafif artar.",
                "+5 Dash Hasar  +0.04 Dash Süresi",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.dashDamage += 5;
                        mv.dashDuration += 0.04f;
                    }
                }
            ),

            new LevelUpCard(
                "than_harvest",
                "Kanlı Hasat",
                "Katana auran daha ölümcül hale gelir.",
                "+6 Aura Hasar",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                        aura.damage += 6;
                }
            ),

            new LevelUpCard(
                "atlas_iron",
                "Demir Beden",
                "Maksimum canın artar ve az miktarda iyileşirsin.",
                "+25 Maks. Can  +15 İyileşme",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Rare,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(25);
                        hp.Heal(15);
                    }
                }
            ),

            new LevelUpCard(
                "hermes_wind",
                "Rüzgar Adımı",
                "Hareket hızın artar.",
                "+0.7 Hareket Hızı",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Common,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.moveSpeed += 0.7f;
                }
            ),

            new LevelUpCard(
                "khaos_balance",
                "Kaotik Denge",
                "Tüm temel statlardan küçük miktarda güç kazanırsın.",
                "+2 Hasar  +0.15 Menzil  +0.3 Hız  +10 Can",
                "∞",
                "Khaos",
                CardGod.Khaos,
                CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 2;
                        aura.radius += 0.15f;
                    }

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.moveSpeed += 0.3f;

                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(10);
                        hp.Heal(5);
                    }
                }
            ),

            new LevelUpCard(
                "nyx_shadow_surge",
                "Gece Darbesi",
                "Karanlık enerji auranı güçlendirir. Etraf hasarın ve menzilin artar.",
                "+4 Aura Hasar  +0.3 Menzil",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 4;
                        aura.radius += 0.3f;
                    }
                }
            ),

            new LevelUpCard(
                "than_final",
                "Son Vuruş",
                "Dash saldırın daha ölümcül olur.",
                "+10 Dash Hasar  +0.15 Dash Hit Radius",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.dashDamage += 10;
                        mv.dashHitRadius += 0.15f;
                    }
                }
            ),

            new LevelUpCard(
                "atlas_bulwark",
                "Titan Kalkanı",
                "Maksimum canın belirgin şekilde artar.",
                "+40 Maks. Can  +20 İyileşme",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Rare,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(40);
                        hp.Heal(20);
                    }
                }
            ),

            new LevelUpCard(
                "hermes_ghost",
                "Hayalet Koşusu",
                "Dash cooldown azalır ve dash hızın artar.",
                "-0.15 Dash CD  +3 Dash Hızı",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.dashCooldown = Mathf.Max(0.25f, mv.dashCooldown - 0.15f);
                        mv.dashSpeed += 3f;
                    }
                }
            ),

            new LevelUpCard(
                "atlas_echo",
                "Atlas Yankısı",
                "Aura menzilin ve hasarın güçlü biçimde artar.",
                "+7 Aura Hasar  +0.45 Menzil",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Epic,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 7;
                        aura.radius += 0.45f;
                    }
                }
            ),

            new LevelUpCard(
                "red_cut",
                "Kızıl Kesik",
                "Katana auran daha hızlı tetiklenir ve daha yüksek hasar verir.",
                "+5 Aura Hasar  Tick Hızı +%15",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Epic,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 5;
                        aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval * 0.85f);
                    }
                }
            ),

            new LevelUpCard(
                "shadow_dash",
                "Gölge Dash",
                "Dash daha sık kullanılabilir ve daha fazla hasar verir.",
                "+12 Dash Hasar  -0.2 Dash CD",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Epic,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.dashDamage += 12;
                        mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f);
                    }
                }
            ),

            new LevelUpCard(
                "broken_time",
                "Kırık Zaman",
                "Hızın ve dash kabiliyetin ciddi şekilde artar.",
                "+0.9 Hız  +2 Dash Hızı  -0.1 Dash CD",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Epic,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.moveSpeed += 0.9f;
                        mv.dashSpeed += 2f;
                        mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.1f);
                    }
                }
            ),

            new LevelUpCard(
                "atlas_colossus",
                "Kolossus",
                "Çok daha dayanıklı olursun ama hareketinde ağırlık hissedersin.",
                "+80 Maks. Can  Hız -0.4",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Legendary,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(80);
                        hp.Heal(40);
                    }

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.moveSpeed = Mathf.Max(2f, mv.moveSpeed - 0.4f);
                }
            ),

            new LevelUpCard(
                "nyx_eclipse",
                "Karanlık Tutulma",
                "Aura hasarın, menzilin ve saldırı frekansın büyük ölçüde güçlenir.",
                "+8 Aura Hasar  +0.5 Menzil  Tick Hızı +%15",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 8;
                        aura.radius += 0.5f;
                        aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval * 0.85f);
                    }
                }
            ),

            new LevelUpCard(
                "than_frenzy",
                "Ölüm Çılgınlığı",
                "Thanatos’un gücü tüm hasar kaynaklarını güçlendirir.",
                "+12 Aura Hasar  +12 Dash Hasar",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                        aura.damage += 12;

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.dashDamage += 12;
                }
            ),

            new LevelUpCard(
                "hermes_quicksilver",
                "Cıva Refleks",
                "Çok daha hızlı hareket eder ve dash’i çok daha sık kullanırsın.",
                "+1.0 Hız  -0.2 Dash CD  +4 Dash Hızı",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Legendary,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.moveSpeed += 1.0f;
                        mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f);
                        mv.dashSpeed += 4f;
                    }
                }
            ),

            new LevelUpCard(
                "khaos_wild",
                "Kaotik Patlama",
                "Khaos rastgele ama büyük bir güç verir.",
                "Rastgele güçlü efekt",
                "∞",
                "Khaos",
                CardGod.Khaos,
                CardRarity.Legendary,
                player =>
                {
                    int roll = UnityEngine.Random.Range(0, 3);

                    if (roll == 0)
                    {
                        var aura = player.GetComponent<AutoAttackAura>();
                        if (aura != null)
                        {
                            aura.damage += 14;
                            aura.radius += 0.6f;
                        }
                    }
                    else if (roll == 1)
                    {
                        var hp = player.GetComponent<PlayerHealth>();
                        if (hp != null)
                        {
                            hp.IncreaseMaxHealth(60);
                            hp.Heal(60);
                        }
                    }
                    else
                    {
                        var mv = player.GetComponent<PlayerMovement>();
                        if (mv != null)
                        {
                            mv.moveSpeed += 1.2f;
                            mv.dashSpeed += 5f;
                            mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.25f);
                        }
                    }
                }
            ),

            new LevelUpCard(
                "atlas_curse",
                "Atlas’ın Laneti",
                "Tüm savaş gücün büyük ölçüde artar.",
                "+10 Aura Hasar  +0.4 Menzil  +8 Dash Hasar",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 10;
                        aura.radius += 0.4f;
                    }

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.dashDamage += 8;
                }
            ),

            new LevelUpCard(
                "cursed_power",
                "Lanetli Güç",
                "Hasarın ciddi şekilde artar ama maksimum canın düşer.",
                "+14 Aura Hasar  +10 Dash Hasar  -20 Maks. Can",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Cursed,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                        hp.IncreaseMaxHealth(-20);

                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                        aura.damage += 14;

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.dashDamage += 10;
                }
            ),

            new LevelUpCard(
                "cursed_titan_weight",
                "Titan Yükü",
                "Canın ve aura gücün artar ama hareket hızın azalır.",
                "+35 Maks. Can  +7 Aura Hasar  -0.8 Hız",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Cursed,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(35);
                        hp.Heal(20);
                    }

                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                        aura.damage += 7;

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.moveSpeed = Mathf.Max(1.5f, mv.moveSpeed - 0.8f);
                }
            ),

            // ── 10 yeni kart ─────────────────────────────────────────────────

            new LevelUpCard(
                "nyx_whisper",
                "Gece Fısıltısı",
                "Karanlık enerji auranı hafifçe güçlendirir.",
                "+3 Aura Hasar  +0.1 Menzil",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Common,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 3;
                        aura.radius += 0.1f;
                    }
                }
            ),

            new LevelUpCard(
                "hermes_stride",
                "Uzun Adım",
                "Dash mesafesi ve hızı artar.",
                "+0.06 Dash Süresi  +2 Dash Hızı",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Rare,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.dashDuration += 0.06f;
                        mv.dashSpeed += 2f;
                    }
                }
            ),

            new LevelUpCard(
                "atlas_ward",
                "Koruyucu Sur",
                "Maksimum canın belirgin şekilde artar ve hafifçe iyileşirsin.",
                "+30 Maks. Can  +10 İyileşme",
                "◈",
                "Atlas",
                CardGod.Atlas,
                CardRarity.Rare,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                    {
                        hp.IncreaseMaxHealth(30);
                        hp.Heal(10);
                    }
                }
            ),

            new LevelUpCard(
                "than_bleed",
                "Kanayan Yara",
                "Aura çok daha hızlı hasar verir.",
                "Tick Hızı +%20  +3 Aura Hasar",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Epic,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval * 0.80f);
                        aura.damage += 3;
                    }
                }
            ),

            new LevelUpCard(
                "khaos_drift",
                "Khaos Akışı",
                "Kaotik enerji tüm temel gücü dengeli artırır.",
                "+3 Aura Hasar  +0.2 Menzil  +0.25 Hız",
                "∞",
                "Khaos",
                CardGod.Khaos,
                CardRarity.Rare,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 3;
                        aura.radius += 0.2f;
                    }

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.moveSpeed += 0.25f;
                }
            ),

            new LevelUpCard(
                "nyx_umbra",
                "Umbra Kapısı",
                "Karanlığın zirvesi aura menzilini büyük ölçüde genişletir.",
                "+6 Aura Hasar  +0.55 Menzil",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Epic,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 6;
                        aura.radius += 0.55f;
                    }
                }
            ),

            new LevelUpCard(
                "hermes_slipstream",
                "Kayma Akışı",
                "Hareket hızın, dash süresi ve cooldown'u birlikte iyileşir.",
                "+1.2 Hız  +0.15 Dash Süresi  -0.12 Dash CD",
                "⚡",
                "Hermes",
                CardGod.Hermes,
                CardRarity.Epic,
                player =>
                {
                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                    {
                        mv.moveSpeed += 1.2f;
                        mv.dashDuration += 0.15f;
                        mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.12f);
                    }
                }
            ),

            new LevelUpCard(
                "khaos_mirror",
                "Khaos Aynası",
                "Khaos tüm olasılıkları değerlendirir ve güçlü bir yol seçer.",
                "Rastgele güçlü kombinasyon",
                "∞",
                "Khaos",
                CardGod.Khaos,
                CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    var mv = player.GetComponent<PlayerMovement>();
                    var hp = player.GetComponent<PlayerHealth>();
                    int roll = UnityEngine.Random.Range(0, 4);

                    switch (roll)
                    {
                        case 0:
                            if (aura != null) { aura.damage += 10; aura.radius += 0.3f; }
                            if (mv != null) mv.dashDamage += 8;
                            break;
                        case 1:
                            if (mv != null) { mv.moveSpeed += 1.2f; mv.dashCooldown = Mathf.Max(0.2f, mv.dashCooldown - 0.2f); mv.dashSpeed += 4f; }
                            break;
                        case 2:
                            if (hp != null) { hp.IncreaseMaxHealth(55); hp.Heal(35); }
                            if (aura != null) aura.damage += 5;
                            break;
                        case 3:
                            if (aura != null) { aura.damage += 7; aura.radius += 0.25f; }
                            if (mv != null) mv.moveSpeed += 0.7f;
                            if (hp != null) hp.Heal(20);
                            break;
                    }
                }
            ),

            new LevelUpCard(
                "than_reaper",
                "Biçici",
                "Thanatos'un biçici formu tüm hasar kaynaklarını öldürücü seviyeye taşır.",
                "+10 Aura Hasar  +8 Dash Hasar  Tick Hızı +%20",
                "⚔",
                "Thanatos",
                CardGod.Thanatos,
                CardRarity.Legendary,
                player =>
                {
                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                    {
                        aura.damage += 10;
                        aura.tickInterval = Mathf.Max(0.2f, aura.tickInterval * 0.80f);
                    }

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.dashDamage += 8;
                }
            ),

            new LevelUpCard(
                "cursed_frailty",
                "Kırılganlık Lütfu",
                "Muazzam saldırı gücü kazanırsın ama bedenin zayıflar.",
                "+18 Aura Hasar  +14 Dash Hasar  -30 Maks. Can",
                "∞",
                "Khaos",
                CardGod.Khaos,
                CardRarity.Cursed,
                player =>
                {
                    var hp = player.GetComponent<PlayerHealth>();
                    if (hp != null)
                        hp.IncreaseMaxHealth(-30);

                    var aura = player.GetComponent<AutoAttackAura>();
                    if (aura != null)
                        aura.damage += 18;

                    var mv = player.GetComponent<PlayerMovement>();
                    if (mv != null)
                        mv.dashDamage += 14;
                }
            )
        };

        ApplyRarityProfiles(pool);
        AuditPool(pool);
        return pool;
    }

    static void ApplyRarityProfiles(List<LevelUpCard> cards)
    {
        Dictionary<string, CardRarityProfile> profiles = BuildRarityProfiles();

        foreach (var card in cards)
        {
            if (card == null)
                continue;

            if (profiles.TryGetValue(card.id, out CardRarityProfile profile))
                card.SetRarityProfile(profile);
            else
                CardRarityResolver.LogWarning($"[LevelUpCard] Missing rarity profile for card: {card.title}");
        }
    }

    static Dictionary<string, CardRarityProfile> BuildRarityProfiles()
    {
        return new Dictionary<string, CardRarityProfile>
        {
            { "nyx_veil", Profile(35, CardEffectTag.DashDamage | CardEffectTag.DashMobility) },
            { "than_harvest", Profile(28, CardEffectTag.AuraDamage) },
            { "atlas_iron", Profile(30, CardEffectTag.Health | CardEffectTag.Heal) },
            { "hermes_wind", Profile(12, CardEffectTag.Movement) },
            { "khaos_balance", Profile(36, CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.Movement | CardEffectTag.Health | CardEffectTag.Heal) },
            { "nyx_shadow_surge", Profile(34, CardEffectTag.AuraDamage | CardEffectTag.AuraRange) },
            { "than_final", Profile(38, CardEffectTag.DashDamage) },
            { "atlas_bulwark", Profile(35, CardEffectTag.Health | CardEffectTag.Heal) },
            { "hermes_ghost", Profile(42, CardEffectTag.DashMobility) },
            { "atlas_echo", Profile(58, CardEffectTag.AuraDamage | CardEffectTag.AuraRange) },
            { "red_cut", Profile(60, CardEffectTag.AuraDamage | CardEffectTag.AuraSpeed) },
            { "shadow_dash", Profile(62, CardEffectTag.DashDamage | CardEffectTag.DashMobility) },
            { "broken_time", Profile(58, CardEffectTag.Movement | CardEffectTag.DashMobility) },
            { "atlas_colossus", Profile(78, CardEffectTag.Health | CardEffectTag.Heal, true, true) },
            { "nyx_eclipse", Profile(82, CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.AuraSpeed, false, true) },
            { "than_frenzy", Profile(80, CardEffectTag.AuraDamage | CardEffectTag.DashDamage, false, true) },
            { "hermes_quicksilver", Profile(80, CardEffectTag.Movement | CardEffectTag.DashMobility, false, true) },
            { "khaos_wild", Profile(86, CardEffectTag.Random | CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.Health | CardEffectTag.Heal | CardEffectTag.Movement | CardEffectTag.DashMobility, false, true) },
            { "atlas_curse", Profile(78, CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.DashDamage, false, true) },
            { "cursed_power", Profile(85, CardEffectTag.Cursed | CardEffectTag.AuraDamage | CardEffectTag.DashDamage | CardEffectTag.Health, true, true) },
            { "cursed_titan_weight", Profile(70, CardEffectTag.Cursed | CardEffectTag.Health | CardEffectTag.Heal | CardEffectTag.AuraDamage | CardEffectTag.Movement, true) },
            { "nyx_whisper", Profile(16, CardEffectTag.AuraDamage | CardEffectTag.AuraRange) },
            { "hermes_stride", Profile(32, CardEffectTag.DashMobility) },
            { "atlas_ward", Profile(30, CardEffectTag.Health | CardEffectTag.Heal) },
            { "than_bleed", Profile(55, CardEffectTag.AuraDamage | CardEffectTag.AuraSpeed) },
            { "khaos_drift", Profile(40, CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.Movement) },
            { "nyx_umbra", Profile(58, CardEffectTag.AuraDamage | CardEffectTag.AuraRange) },
            { "hermes_slipstream", Profile(66, CardEffectTag.Movement | CardEffectTag.DashMobility) },
            { "khaos_mirror", Profile(82, CardEffectTag.Random | CardEffectTag.AuraDamage | CardEffectTag.AuraRange | CardEffectTag.DashDamage | CardEffectTag.Movement | CardEffectTag.DashMobility | CardEffectTag.Health | CardEffectTag.Heal, false, true) },
            { "than_reaper", Profile(84, CardEffectTag.AuraDamage | CardEffectTag.AuraSpeed | CardEffectTag.DashDamage, false, true) },
            { "cursed_frailty", Profile(92, CardEffectTag.Cursed | CardEffectTag.AuraDamage | CardEffectTag.DashDamage | CardEffectTag.Health, true, true) }
        };
    }

    static CardRarityProfile Profile(
        int powerScore,
        CardEffectTag tags,
        bool hasTradeoff = false,
        bool isBuildDefining = false)
    {
        return new CardRarityProfile(powerScore, tags, hasTradeoff, isBuildDefining);
    }

    public static List<LevelUpCard> PickRandom(int count, int playerLevel)
    {
        GetRarityChances(
            playerLevel,
            out float commonChance,
            out float rareChance,
            out float epicChance,
            out float legendaryChance,
            out float cursedChance
        );

        var result = new List<LevelUpCard>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            CardRarity target = RollRarity(
                commonChance,
                rareChance,
                epicChance,
                legendaryChance,
                cursedChance
            );

            var candidates = All.FindAll(card => card.GetDisplayRarity() == target && !usedIds.Contains(card.id));

            if (candidates.Count == 0)
                candidates = All.FindAll(card => !usedIds.Contains(card.id));

            if (candidates.Count == 0)
                break;

            LevelUpCard pick = candidates[UnityEngine.Random.Range(0, candidates.Count)].CreateRuntimeCopy();
            result.Add(pick);
            usedIds.Add(pick.id);
        }

        return result;
    }

    static void AuditPool(List<LevelUpCard> cards)
    {
        var ids = new HashSet<string>();

        foreach (var card in cards)
        {
            if (card == null)
            {
                CardRarityResolver.LogWarning("CardPool contains a null card definition.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(card.id))
                CardRarityResolver.LogWarning($"LevelUpCard '{card.title}' has an empty id.");
            else if (!ids.Add(card.id))
                CardRarityResolver.LogWarning($"Duplicate LevelUpCard id detected: '{card.id}'.");

            if (!IsValidRarity(card.rarity))
            {
                CardRarityResolver.LogWarning($"[LevelUpCard] Missing rarity for card: {card.title}");
                continue;
            }

            CardRarity expected = card.GetExpectedRarity();
            if (IsValidRarity(expected) && expected != card.DesignRarity)
            {
                CardRarityResolver.LogRarityAuditWarning(card, expected, "Pool audit mismatch.");
            }
        }
    }

    static void GetRarityChances(
        int playerLevel,
        out float common,
        out float rare,
        out float epic,
        out float legendary,
        out float cursed)
    {
        if (playerLevel <= 2)
        {
            common = 0.80f;
            rare = 0.20f;
            epic = 0f;
            legendary = 0f;
            cursed = 0f;
            return;
        }

        if (playerLevel <= 5)
        {
            common = 0.65f;
            rare = 0.30f;
            epic = 0.05f;
            legendary = 0f;
            cursed = 0f;
            return;
        }

        common = 0.45f;
        rare = 0.40f;
        epic = 0.13f;
        legendary = 0.02f;
        cursed = 0f;
    }

    static CardRarity RollRarity(
        float common,
        float rare,
        float epic,
        float legendary,
        float cursed)
    {
        float roll = UnityEngine.Random.value;
        float cursor = common;

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

        return CardRarity.Cursed;
    }
}
