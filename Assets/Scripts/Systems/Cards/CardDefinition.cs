using System;
using System.Collections.Generic;
using UnityEngine;

// Silah kartları için — committed CardDefinition'a eklendi
public enum CardType
{
    WeaponUpgrade,
    Passive,
    Chaos,
}

public enum CardVisualCategory
{
    Sustain,
    Damage,
    Mobility,
    BossReward,
    Combo,
    Chaos,
    WeaponKatana,
    WeaponKunai,
    WeaponSphere,
    WeaponAwakening,
    ChestRelic,
    ChestSeal,
    ChestEconomy,
    ChestSurvival,
    ChestAtlas,
}

public class CardDefinition
{
    public string           id;
    public string           title;
    public string           description;
    public string           effectPreview;
    public string           iconId;
    public CardClass        cardClass;
    public CardRarity       rarity;
    public CardVisualCategory visualCategory;
    public CardTag          tags;
    public List<CardEffect> effects = new();
    public Action<GameObject> Apply;

    // Silah kartları için ek alanlar (geriye dönük uyumluluk)
    public CardType   cardType   = CardType.Passive;
    public WeaponType weaponType = WeaponType.None;
    public int        weaponLevel = 0;

    // Yeni kart sınıflandırma sistemi
    public CardKind cardKind     = CardKind.PassiveUnlock;
    public string   passiveId;
    public int      passiveLevel = 0;  // bu kartın hedef pasif seviyesi
    public int      currentLevel = 0;  // uygulanmadan önceki mevcut seviye
    public int      maxLevel     = 0;  // görüntüleme için maksimum seviye

    public bool IsWeaponCard  => cardKind == CardKind.WeaponUnlock || cardKind == CardKind.WeaponUpgrade;
    public bool IsPassiveCard => cardKind == CardKind.PassiveUnlock || cardKind == CardKind.PassiveUpgrade;

    public bool IsRandomLike =>
        cardClass == CardClass.Khaos ||
        (tags & CardTag.Random) != 0 ||
        (tags & CardTag.ShowsResultFeedback) != 0;

    public CardDefinition CreateCopy() => new CardDefinition
    {
        id             = id,
        title          = title,
        description    = description,
        effectPreview  = effectPreview,
        iconId         = iconId,
        cardClass      = cardClass,
        rarity         = rarity,
        visualCategory = visualCategory,
        tags           = tags,
        effects        = effects,
        Apply          = Apply,
        cardType       = cardType,
        weaponType     = weaponType,
        weaponLevel    = weaponLevel,
        cardKind       = cardKind,
        passiveId      = passiveId,
        passiveLevel   = passiveLevel,
        currentLevel   = currentLevel,
        maxLevel       = maxLevel,
    };
}
