using UnityEngine;

public struct CardClassTheme
{
    public Color main;
    public Color dark;
    public Color glow;
    public Color text;
    public Color secondaryGlow;
}

public struct CardVisualTheme
{
    public Color main;
    public Color dark;
    public Color glow;
    public Color text;
    public Color secondaryGlow;
}

public struct CardRarityTheme
{
    public Color  accent;
    public Color  glow;
    public float  glowAlpha;
    public string displayName;
}

public static class CardThemeLibrary
{
    public static CardVisualTheme GetVisualTheme(CardVisualCategory c) => c switch
    {
        CardVisualCategory.Sustain => new CardVisualTheme
        {
            main = Hex("2FBF71"), dark = Hex("0C2E1D"), glow = Hex("7DFFAE"),
            text = Hex("E8FFF0"), secondaryGlow = Hex("7DFFAE")
        },
        CardVisualCategory.Damage => new CardVisualTheme
        {
            main = Hex("C43737"), dark = Hex("2A0909"), glow = Hex("FF7B7B"),
            text = Hex("FFEAEA"), secondaryGlow = Hex("FF7B7B")
        },
        CardVisualCategory.Mobility => new CardVisualTheme
        {
            main = Hex("2F7DFF"), dark = Hex("0A1838"), glow = Hex("86B8FF"),
            text = Hex("EEF5FF"), secondaryGlow = Hex("86B8FF")
        },
        CardVisualCategory.BossReward => new CardVisualTheme
        {
            main = Hex("D6A62E"), dark = Hex("332304"), glow = Hex("FFE07A"),
            text = Hex("FFF6DA"), secondaryGlow = Hex("FFE07A")
        },
        CardVisualCategory.Combo => new CardVisualTheme
        {
            main = Hex("8758E8"), dark = Hex("1C1036"), glow = Hex("C4A2FF"),
            text = Hex("F3ECFF"), secondaryGlow = Hex("C4A2FF")
        },
        CardVisualCategory.Chaos => new CardVisualTheme
        {
            main = Hex("C04BFF"), dark = Hex("250530"), glow = Hex("FF7CF5"),
            text = Hex("FFE9FF"), secondaryGlow = Hex("59E1FF")
        },
        CardVisualCategory.WeaponKatana => new CardVisualTheme
        {
            main = Hex("D86A1C"), dark = Hex("2D1204"), glow = Hex("FFB05A"),
            text = Hex("FFE8D0"), secondaryGlow = Hex("FFB05A")
        },
        CardVisualCategory.WeaponKunai => new CardVisualTheme
        {
            main = Hex("10B8C8"), dark = Hex("063B46"), glow = Hex("6DF6FF"),
            text = Hex("D8FFFF"), secondaryGlow = Hex("6DF6FF")
        },
        CardVisualCategory.WeaponSphere => new CardVisualTheme
        {
            main = Hex("B8873A"), dark = Hex("2C1B08"), glow = Hex("FFD56A"),
            text = Hex("FFF1C2"), secondaryGlow = Hex("FFD56A")
        },
        CardVisualCategory.WeaponAwakening => new CardVisualTheme
        {
            main = Hex("E8B800"), dark = Hex("3D2800"), glow = Hex("FFE97A"),
            text = Hex("FFF8D0"), secondaryGlow = Hex("FF8A00")
        },
        CardVisualCategory.ChestRelic => new CardVisualTheme
        {
            main = Hex("C43737"), dark = Hex("2A0909"), glow = Hex("FF7B7B"),
            text = Hex("FFEAEA"), secondaryGlow = Hex("FF7B7B")
        },
        CardVisualCategory.ChestSeal => new CardVisualTheme
        {
            main = Hex("7B52C8"), dark = Hex("1A0D3A"), glow = Hex("B99AFF"),
            text = Hex("EEE3FF"), secondaryGlow = Hex("B99AFF")
        },
        CardVisualCategory.ChestEconomy => new CardVisualTheme
        {
            main = Hex("C89A30"), dark = Hex("2D1F00"), glow = Hex("FFD96A"),
            text = Hex("FFF3CC"), secondaryGlow = Hex("FFD96A")
        },
        CardVisualCategory.ChestSurvival => new CardVisualTheme
        {
            main = Hex("2FBF71"), dark = Hex("0C2E1D"), glow = Hex("7DFFAE"),
            text = Hex("E8FFF0"), secondaryGlow = Hex("7DFFAE")
        },
        CardVisualCategory.ChestAtlas => new CardVisualTheme
        {
            main = Hex("3A7FC1"), dark = Hex("0A1C36"), glow = Hex("7CC4FF"),
            text = Hex("D6EEFF"), secondaryGlow = Hex("7CC4FF")
        },
        _ => new CardVisualTheme
        {
            main = Color.white, dark = Color.black, glow = Color.white,
            text = Color.white, secondaryGlow = Color.white
        }
    };

    public static string GetVisualCornerLabel(CardVisualCategory c) => c switch
    {
        CardVisualCategory.BossReward      => "BOSS",
        CardVisualCategory.Combo           => "KOMBO",
        CardVisualCategory.Chaos           => "KHAOS",
        CardVisualCategory.WeaponKatana    => "SİLAH",
        CardVisualCategory.WeaponKunai     => "SİLAH",
        CardVisualCategory.WeaponSphere    => "SİLAH",
        CardVisualCategory.WeaponAwakening => "UYANIŞ",
        CardVisualCategory.ChestRelic      => "KALINTI",
        CardVisualCategory.ChestSeal       => "MÜHÜR",
        CardVisualCategory.ChestEconomy    => "EKONOMİ",
        CardVisualCategory.ChestSurvival   => "KALKAN",
        CardVisualCategory.ChestAtlas      => "ATLAS",
        _ => null
    };

    // ── Class themes ─────────────────────────────────────────────────────────

    public static CardClassTheme GetClassTheme(CardClass c) => c switch
    {
        CardClass.Hermes     => new CardClassTheme
        {
            main          = Hex("10B8C8"),
            dark          = Hex("063B46"),
            glow          = Hex("6DF6FF"),
            text          = Hex("D8FFFF"),
            secondaryGlow = Hex("6DF6FF")
        },
        CardClass.Nyx        => new CardClassTheme
        {
            main          = Hex("6C2BB8"),
            dark          = Hex("1B0A2F"),
            glow          = Hex("B482FF"),
            text          = Hex("F0D8FF"),
            secondaryGlow = Hex("B482FF")
        },
        CardClass.Atlas      => new CardClassTheme
        {
            main          = Hex("B8873A"),
            dark          = Hex("2C1B08"),
            glow          = Hex("FFD56A"),
            text          = Hex("FFF1C2"),
            secondaryGlow = Hex("FFD56A")
        },
        CardClass.Ares       => new CardClassTheme
        {
            main          = Hex("B82035"),
            dark          = Hex("2A050B"),
            glow          = Hex("FF5A6A"),
            text          = Hex("FFE0E3"),
            secondaryGlow = Hex("FF5A6A")
        },
        CardClass.Artemis    => new CardClassTheme
        {
            main          = Hex("1FA66A"),
            dark          = Hex("062619"),
            glow          = Hex("7CFFB2"),
            text          = Hex("DFFFF0"),
            secondaryGlow = Hex("7CFFB2")
        },
        CardClass.Hephaistos => new CardClassTheme
        {
            main          = Hex("D86A1C"),
            dark          = Hex("2D1204"),
            glow          = Hex("FFB05A"),
            text          = Hex("FFE8D0"),
            secondaryGlow = Hex("FFB05A")
        },
        CardClass.Khaos      => new CardClassTheme
        {
            main          = Hex("A13DFF"),
            dark          = Hex("21002F"),
            glow          = Hex("FF4FD8"),
            text          = Hex("FFE6FF"),
            secondaryGlow = Hex("40E0FF")
        },
        _ => new CardClassTheme
        {
            main = Color.white, dark = Color.black, glow = Color.white,
            text = Color.white, secondaryGlow = Color.white
        }
    };

    // ── Rarity themes ─────────────────────────────────────────────────────────

    public static CardRarityTheme GetRarityTheme(CardRarity r) => r switch
    {
        // Yaygın: parlak gri yerine koyu lacivert zemin + hafif cyan accent
        CardRarity.Common    => new CardRarityTheme { accent = Hex("8FC9DC"), glow = Hex("5FA9C6"), glowAlpha = 0.11f, displayName = "Yaygın"   },
        // Nadir: mor / violet
        CardRarity.Rare      => new CardRarityTheme { accent = Hex("A86CFF"), glow = Hex("9B5CFF"), glowAlpha = 0.20f, displayName = "Nadir"    },
        // Epik: pembe / magenta (border'da altın ile karışır)
        CardRarity.Epic      => new CardRarityTheme { accent = Hex("F46BD0"), glow = Hex("E85CC8"), glowAlpha = 0.28f, displayName = "Epik"     },
        // Efsanevi: altın glow — ama gövde sapsarı olmaz, sadece çerçeve/glow
        CardRarity.Legendary => new CardRarityTheme { accent = Hex("F4C24E"), glow = Hex("FFD45A"), glowAlpha = 0.34f, displayName = "Efsanevi" },
        CardRarity.Chaos     => new CardRarityTheme { accent = Hex("FF4FD8"), glow = Hex("FF4FD8"), glowAlpha = 0.30f, displayName = "Khaos"    },
        _ => new CardRarityTheme { accent = Color.grey, glow = Color.grey, glowAlpha = 0.05f, displayName = "?" }
    };

    // ── Display names ─────────────────────────────────────────────────────────

    public static string GetClassDisplayName(CardClass c) => c switch
    {
        CardClass.Hermes     => "Hermes",
        CardClass.Nyx        => "Nyx",
        CardClass.Atlas      => "Atlas",
        CardClass.Ares       => "Ares",
        CardClass.Artemis    => "Artemis",
        CardClass.Hephaistos => "Hephaistos",
        CardClass.Khaos      => "Khaos",
        _ => "?"
    };

    public static string GetClassIcon(CardClass c) => c switch
    {
        CardClass.Hermes     => "⚡",
        CardClass.Nyx        => "☽",
        CardClass.Atlas      => "◈",
        CardClass.Ares       => "⚔",
        CardClass.Artemis    => "◎",
        CardClass.Hephaistos => "⚙",
        CardClass.Khaos      => "∞",
        _ => "?"
    };

    // ── Overlay / panel colors ────────────────────────────────────────────────

    public static readonly Color OverlayBg    = Hex("08040E", 0.84f);
    public static readonly Color PanelBg      = Hex("100720", 0.97f);
    public static readonly Color PanelBorder  = Hex("4B2A72", 0.72f);
    public static readonly Color TitleGold    = Hex("FFD21A");
    public static readonly Color SubtitleTint = Hex("D8C8F0");

    // ── Premium kart paleti (polish) ────────────────────────────────────────────
    // Kart gövdesi: koyu mor / siyaha yakın indigo (rarity rengiyle boyanmaz)
    public static readonly Color CardBaseIndigo = Hex("0E0A1E", 0.985f);
    // Açıklama / effect panelleri için koyu yarı saydam zemin
    public static readonly Color CardPanelDark  = Hex("070512", 0.82f);
    // Çerçeve altın tonu (mor + altın premium his)
    public static readonly Color FrameGold      = Hex("E8C063");
    // Kırık beyaz gövde metni
    public static readonly Color BodyTextSoft   = Hex("F1ECFF");

    // ── Helpers ───────────────────────────────────────────────────────────────

    static Color Hex(string hex, float a = 1f)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
        {
            c.a = a;
            return c;
        }
        return Color.magenta;
    }

    public static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
