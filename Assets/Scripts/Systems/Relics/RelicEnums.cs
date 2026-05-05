using System;

public enum RelicRarity
{
    Common    = 0,
    Rare      = 1,
    Epic      = 2,
    Legendary = 3,
    Cursed    = 4
}

public enum RelicCategory
{
    Survival,
    Economy,
    Boss,
    Portal,
    Combat,
    RiskReward,
    Utility
}

[Flags]
public enum RelicTag
{
    None            = 0,
    Healing         = 1 << 0,
    Gold            = 1 << 1,
    Elite           = 1 << 2,
    BossReward      = 1 << 3,
    PortalResonance = 1 << 4,
    RiskReward      = 1 << 5,
    OneTime         = 1 << 6,
    Unique          = 1 << 7,
    Combo           = 1 << 8
}
