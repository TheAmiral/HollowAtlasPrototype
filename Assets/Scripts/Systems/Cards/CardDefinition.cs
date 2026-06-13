// ─────────────────────────────────────────────────────────────────────────────
//  CardDefinition.cs  (YENİDEN YAZILDI)
//  Assets/Scripts/Systems/Cards/CardDefinition.cs
//
//  VS sistemine geçiş: Silah upgrade kartları artık WeaponType ve
//  weaponLevel taşır. Pasif kartlar eskisi gibi Apply Action kullanır.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using UnityEngine;

// Kartın temel tipi
public enum CardType
{
    WeaponUpgrade,  // Silah aç veya seviye atla (VS: weapon card)
    Passive,        // Stat boost (VS: passive item)
    Chaos,          // Rastgele / özel
}

// Visual renk kategorisi — UI renklendirme için
public enum CardVisualCategory
{
    Sustain,
    Damage,
    Mobility,
    BossReward,
    Combo,
    Chaos,
    WeaponKatana,   // YENİ — turuncu/altın
    WeaponKunai,    // YENİ — cyan
    WeaponSphere,   // YENİ — mor/altın
}

public class CardDefinition
{
    public string   id;
    public string   title;
    public string   description;
    public string   effectPreview;

    public CardType           cardType;
    public CardClass          cardClass;        // geriye dönük uyumluluk
    public CardRarity         rarity;
    public CardVisualCategory visualCategory;
    public CardTag            tags;

    // Silah kartları için
    public WeaponType weaponType  = WeaponType.None;
    public int        weaponLevel = 0;           // hangi seviyeye atlanıyor (1=ilk açılış)

    // Pasif / Chaos kartlar için
    public Action<GameObject> Apply;

    public bool IsWeaponCard  => cardType == CardType.WeaponUpgrade;
    public bool IsPassiveCard => cardType == CardType.Passive;
    public bool IsChaosCard   => cardType == CardType.Chaos;

    public bool IsRandomLike =>
        cardType == CardType.Chaos ||
        (tags & CardTag.Random) != 0;

    public CardDefinition CreateCopy() => new CardDefinition
    {
        id             = id,
        title          = title,
        description    = description,
        effectPreview  = effectPreview,
        cardType       = cardType,
        cardClass      = cardClass,
        rarity         = rarity,
        visualCategory = visualCategory,
        tags           = tags,
        weaponType     = weaponType,
        weaponLevel    = weaponLevel,
        Apply          = Apply,
    };
}
