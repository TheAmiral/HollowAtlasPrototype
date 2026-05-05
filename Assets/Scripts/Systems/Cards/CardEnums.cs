using System;

public enum CardClass
{
    Hermes,
    Nyx,
    Atlas,
    Ares,
    Artemis,
    Hephaistos,
    Khaos
}

public enum CardRarity
{
    Common    = 0,
    Rare      = 1,
    Epic      = 2,
    Legendary = 3,
    Chaos     = 4
}

[Flags]
public enum CardTag
{
    None                = 0,
    Movement            = 1 << 0,
    Aura                = 1 << 1,
    Defense             = 1 << 2,
    Damage              = 1 << 3,
    Critical            = 1 << 4,
    Weapon              = 1 << 5,
    Random              = 1 << 6,
    RiskReward          = 1 << 7,
    Unique              = 1 << 8,
    ShowsResultFeedback = 1 << 9
}

public enum CardStatType
{
    MoveSpeed,
    DashCooldown,
    DashSpeed,
    DashDuration,
    MaxHealth,
    Healing,
    AuraDamage,
    AuraRange,
    AuraSpeed,
    DashDamage,
    DashHitRadius,
    CritChance,
    CritDamage,
    ArmorPierce
}

public enum CardEffectKind
{
    FlatStat,
    PercentStat,
    RandomStatBundle,
    Special
}
