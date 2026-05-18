using System;
using UnityEngine;

public class RelicDefinition
{
    public string        id;
    public string        title;
    public string        description;
    public RelicRarity   rarity;
    public RelicCategory category;
    public RelicTag      tags;
    public bool          isUnique;
    public int           maxStacks;
    // Direct stat effect applied to player on pickup.
    // Null is valid for passive/flag relics whose effect is handled by RelicInventory.
    public Action<GameObject> Apply;
}
