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
        _ => new CardVisualTheme
        {
            main = Color.white, dark = Color.black, glow = Color.white,
            text = Color.white, secondaryGlow = Color.white
        }
    };

    public static string GetVisualCornerLabel(CardVisualCategory c) => c switch
    {
        CardVisualCategory.BossReward => "BOSS",
        CardVisualCategory.Combo      => "KOMBO",
        CardVisualCategory.Chaos      => "KHAOS",
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
        CardRarity.Common    => new CardRarityTheme { accent = Hex("BFC7D5"), glow = Hex("BFC7D5"), glowAlpha = 0.08f, displayName = "Yaygın"   },
        CardRarity.Rare      => new CardRarityTheme { accent = Hex("4DB8FF"), glow = Hex("4DB8FF"), glowAlpha = 0.18f, displayName = "Nadir"    },
        CardRarity.Epic      => new CardRarityTheme { accent = Hex("B45CFF"), glow = Hex("B45CFF"), glowAlpha = 0.26f, displayName = "Epik"     },
        CardRarity.Legendary => new CardRarityTheme { accent = Hex("FFD45A"), glow = Hex("FFD45A"), glowAlpha = 0.34f, displayName = "Efsanevi" },
        CardRarity.Chaos     => new CardRarityTheme { accent = Hex("FF4FD8"), glow = Hex("FF4FD8"), glowAlpha = 0.28f, displayName = "Khaos"    },
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
