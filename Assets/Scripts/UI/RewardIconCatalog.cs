using UnityEngine;

// Reward / build ikonları için tekil katalog.
// Runtime tarafı renkleri buradan okur; Editor generator aynı listeyi PNG sprite üretmek için kullanır.
public enum RewardIconShape
{
    Default,
    Katana,
    Kunai,
    Sphere,
    Heart,
    Boots,
    Shield,
    Hourglass,
    Echo,
    Magnet,
    Xp,
    Coin,
    Dust,
    AtlasShard,
    Seal,
    Footstep,
    Core,
    Stone,
    Rune,
    Compass,
    Crescent,
    Storm,
    Halo,
}

public readonly struct RewardIconSpec
{
    public readonly string IconId;
    public readonly string DisplayName;
    public readonly CardKind Kind;
    public readonly RewardIconShape Shape;
    public readonly Color MainColor;

    public RewardIconSpec(string iconId, string displayName, CardKind kind, RewardIconShape shape, Color mainColor)
    {
        IconId      = iconId;
        DisplayName = displayName;
        Kind        = kind;
        Shape       = shape;
        MainColor   = mainColor;
    }
}

public static class RewardIconCatalog
{
    public static readonly RewardIconSpec[] All =
    {
        // Silahlar
        new("icon_katana_aura",    "Katana Aura",    CardKind.WeaponUnlock,    RewardIconShape.Katana,    new Color(0.85f, 0.42f, 0.12f)),
        new("icon_ruh_kunai",      "Ruh Kunai",      CardKind.WeaponUnlock,    RewardIconShape.Kunai,     new Color(0.10f, 0.72f, 0.80f)),
        new("icon_atlas_kuresi",   "Atlas Küresi",   CardKind.WeaponUnlock,    RewardIconShape.Sphere,    new Color(0.72f, 0.55f, 0.22f)),

        // Pasifler
        new("icon_kan_yemini",     "Kan Yemini",     CardKind.PassiveUnlock,   RewardIconShape.Heart,     new Color(0.78f, 0.18f, 0.22f)),
        new("icon_ruzgar_adimi",   "Rüzgar Adımı",   CardKind.PassiveUnlock,   RewardIconShape.Boots,     new Color(0.20f, 0.62f, 0.85f)),
        new("icon_demir_beden",    "Demir Beden",    CardKind.PassiveUnlock,   RewardIconShape.Shield,    new Color(0.45f, 0.55f, 0.70f)),
        new("icon_zaman_kirigi",   "Zaman Kırığı",   CardKind.PassiveUnlock,   RewardIconShape.Hourglass, new Color(0.85f, 0.45f, 0.20f)),
        new("icon_buyuyen_yanki",  "Büyüyen Yankı",  CardKind.PassiveUnlock,   RewardIconShape.Echo,      new Color(0.55f, 0.30f, 0.78f)),
        new("icon_ruh_miknatis",   "Ruh Mıknatısı",  CardKind.PassiveUnlock,   RewardIconShape.Magnet,    new Color(0.25f, 0.78f, 0.55f)),
        new("icon_atlas_bereketi", "Atlas Bereketi", CardKind.PassiveUnlock,   RewardIconShape.Xp,        new Color(0.55f, 0.45f, 0.85f)),
        new("icon_lanetli_servet", "Lanetli Servet", CardKind.PassiveUnlock,   RewardIconShape.Coin,      new Color(0.70f, 0.25f, 0.62f)),

        // Chest / kalıntılar
        new("icon_firavun_tozu",   "Firavun Tozu",   CardKind.ChestRelic,      RewardIconShape.Dust,      new Color(0.85f, 0.25f, 0.25f)),
        new("icon_altin_damar",    "Altın Damar",    CardKind.ChestEconomy,    RewardIconShape.Coin,      new Color(0.95f, 0.80f, 0.12f)),
        new("icon_atlas_parcasi",  "Atlas Parçası",  CardKind.ChestAtlas,      RewardIconShape.AtlasShard,new Color(0.25f, 0.80f, 0.50f)),
        new("icon_muhafiz_muhru",  "Muhafız Mührü",  CardKind.ChestSeal,       RewardIconShape.Seal,      new Color(0.25f, 0.55f, 0.90f)),
        new("icon_golge_adim",     "Gölge Adım",     CardKind.ChestSurvival,   RewardIconShape.Footstep,  new Color(0.55f, 0.35f, 0.90f)),
        new("icon_atlas_cekirdegi","Atlas Çekirdeği",CardKind.ChestAtlas,      RewardIconShape.Core,      new Color(0.25f, 0.62f, 0.95f)),
        new("icon_tas_deri",       "Taş Deri",       CardKind.ChestSurvival,   RewardIconShape.Stone,     new Color(0.55f, 0.72f, 0.42f)),
        new("icon_yasam_runu",     "Yaşam Runu",     CardKind.ChestSurvival,   RewardIconShape.Rune,      new Color(0.35f, 0.92f, 0.55f)),
        new("icon_atlas_pusulasi", "Atlas Pusulası", CardKind.ChestAtlas,      RewardIconShape.Compass,   new Color(0.92f, 0.72f, 0.20f)),

        // Weapon awakening
        new("icon_kanli_hilal",    "Kanlı Hilal",    CardKind.WeaponAwakening, RewardIconShape.Crescent,  new Color(0.90f, 0.45f, 0.15f)),
        new("icon_ruh_firtinasi",  "Ruh Fırtınası",  CardKind.WeaponAwakening, RewardIconShape.Storm,     new Color(0.20f, 0.80f, 1.00f)),
        new("icon_atlas_halosu",   "Atlas Halosu",   CardKind.WeaponAwakening, RewardIconShape.Halo,      new Color(0.70f, 0.45f, 0.95f)),
    };

    public static bool TryGetSpec(string iconId, out RewardIconSpec spec)
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].IconId == iconId)
            {
                spec = All[i];
                return true;
            }
        }

        spec = default;
        return false;
    }

    public static RewardIconSpec GetSpec(string iconId, CardKind fallbackKind)
    {
        if (TryGetSpec(iconId, out var spec))
            return spec;

        return new RewardIconSpec(
            string.IsNullOrEmpty(iconId) ? "icon_unknown" : iconId,
            "Unknown Reward",
            fallbackKind,
            RewardIconShape.Default,
            new Color(0.60f, 0.55f, 0.80f));
    }

    public static Color GetColor(string iconId)
    {
        return TryGetSpec(iconId, out var spec)
            ? spec.MainColor
            : new Color(0.60f, 0.55f, 0.80f);
    }
}
