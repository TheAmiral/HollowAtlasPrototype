using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Cursed
}

public enum CardGod
{
    Nyx,
    Thanatos,
    Atlas,
    Hermes,
    Khaos
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
    public CardRarity rarity;

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
        Action<GameObject> apply)
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
    }
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

    public static Color RarityColor(CardRarity r) => r switch
    {
        CardRarity.Common => new Color(0.608f, 0.757f, 0.922f, 1f),
        CardRarity.Rare => new Color(0.260f, 0.520f, 1.000f, 1f),
        CardRarity.Epic => new Color(0.671f, 0.278f, 0.737f, 1f),
        CardRarity.Legendary => new Color(0.988f, 0.816f, 0.000f, 1f),
        CardRarity.Cursed => new Color(0.900f, 0.100f, 0.100f, 1f),
        _ => Color.white
    };

    public static string RarityLabel(CardRarity r) => r switch
    {
        CardRarity.Common => "COMMON",
        CardRarity.Rare => "RARE",
        CardRarity.Epic => "EPIC",
        CardRarity.Legendary => "LEGENDARY",
        CardRarity.Cursed => "CURSED",
        _ => ""
    };

    private static List<LevelUpCard> _all;
    public static List<LevelUpCard> All => _all ??= BuildPool();

    static List<LevelUpCard> BuildPool()
    {
        return new List<LevelUpCard>
        {
            new LevelUpCard(
                "nyx_veil",
                "Gölge Örtüsü",
                "Dash sonrasında kısa süreli gölge izi bırakır. Dash hasarı ve dash süresi hafif artar.",
                "+5 Dash Hasar  +0.04 Dash Süresi",
                "☽",
                "Nyx",
                CardGod.Nyx,
                CardRarity.Common,
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
                CardRarity.Common,
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
                CardRarity.Common,
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
                CardRarity.Common,
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
                CardRarity.Epic,
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
                "✖",
                "Lanet",
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
                "✖",
                "Lanet",
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
            )
        };
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

            var candidates = All.FindAll(card => card.rarity == target && !usedIds.Contains(card.id));

            if (candidates.Count == 0)
                candidates = All.FindAll(card => !usedIds.Contains(card.id));

            if (candidates.Count == 0)
                break;

            LevelUpCard pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            result.Add(pick);
            usedIds.Add(pick.id);
        }

        return result;
    }

    static void GetRarityChances(
        int playerLevel,
        out float common,
        out float rare,
        out float epic,
        out float legendary,
        out float cursed)
    {
        if (playerLevel <= 3)
        {
            common = 0.75f;
            rare = 0.25f;
            epic = 0f;
            legendary = 0f;
            cursed = 0f;
            return;
        }

        if (playerLevel <= 7)
        {
            common = 0.55f;
            rare = 0.35f;
            epic = 0.10f;
            legendary = 0f;
            cursed = 0f;
            return;
        }

        common = 0.40f;
        rare = 0.35f;
        epic = 0.18f;
        legendary = 0.05f;
        cursed = 0.02f;
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