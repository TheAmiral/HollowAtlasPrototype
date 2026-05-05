using System;
using System.Collections.Generic;
using UnityEngine;

public enum CardVisualCategory
{
    Sustain,
    Damage,
    Mobility,
    BossReward,
    Combo,
    Chaos
}

public class CardDefinition
{
    public string           id;
    public string           title;
    public string           description;
    public string           effectPreview;
    public CardClass        cardClass;
    public CardRarity       rarity;
    public CardVisualCategory visualCategory;
    public CardTag          tags;
    public List<CardEffect> effects = new();
    public Action<GameObject> Apply;

    public bool IsRandomLike =>
        cardClass == CardClass.Khaos ||
        (tags & CardTag.Random) != 0 ||
        (tags & CardTag.ShowsResultFeedback) != 0;

    public CardDefinition CreateCopy() => new CardDefinition
    {
        id            = id,
        title         = title,
        description   = description,
        effectPreview = effectPreview,
        cardClass     = cardClass,
        rarity        = rarity,
        visualCategory = visualCategory,
        tags          = tags,
        effects       = effects,
        Apply         = Apply
    };
}
