using System.Collections.Generic;
using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

public enum ItemEffectType
{
    // Stat mods
    DamageFlat,
    DamageMultiplierPercent,
    FireRateFlat,
    BulletSpeedFlat,
    ClosestEnemyRangeFlat,
    PickupRadiusFlat,
    MoveSpeedFlat,
    MaxHPFlat,

    // One-shot on pick
    HealPercentOnPick,

    // Unique behaviours (toggle-on; Inventory will enable matching components)
    Enable_PollenAura,
    Enable_ExplodingShots,     // placeholder for later
    Enable_RoyalGuardShields   // placeholder for later
}

[System.Serializable]
public struct ItemEffect
{
    public ItemEffectType type;
    public float value; // meaning depends on type (e.g., +2 dmg, +15% mult, +0.5 radius, etc.)
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Items/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                // optional unique id (leave blank if not needed)
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Rules")]
    [Range(1, 10)] public int maxStack = 5;

    [Header("Effects")]
    public List<ItemEffect> effects = new List<ItemEffect>();
}
