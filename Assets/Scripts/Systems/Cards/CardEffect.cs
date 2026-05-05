public class CardEffect
{
    public CardEffectKind kind;
    public CardStatType   stat;
    public float          value;
    public string         labelOverride;
    public float          min;
    public float          max;

    public static CardEffect Flat(CardStatType stat, float value, string label = null)
        => new CardEffect { kind = CardEffectKind.FlatStat, stat = stat, value = value, labelOverride = label };

    public static CardEffect Percent(CardStatType stat, float value, string label = null)
        => new CardEffect { kind = CardEffectKind.PercentStat, stat = stat, value = value, labelOverride = label };
}
