using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public enum PlayerStatType { MoveSpeed, Damage, MaxHP, PickupRadius, AttackSpeed, JumpForce, Other }
    public enum ItemClass { Winged, Predator, Metamorph, Swarm, Dancer }
    public enum Rarity { Scraps, Pseudo, Gene, Strand, Merged }

    [Header("Identity")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemClass itemClass;

    [Header("Rarity")]
    public Rarity rarity = Rarity.Scraps; // <-- used by weighted shop rolls

    [Header("Effects")]
    public bool modifiesStats;
    public PlayerStatType statType;
    public float modifierAmount = 1f;
    public GameObject abilityPrefab;

    [Header("Other")]
    public int stability = 1;

    // Used by PlayerStats.RecalculateStats()
    public virtual void ApplyStatModifier(PlayerStats stats)
    {
        if (!modifiesStats) return;

        switch (statType)
        {
            case PlayerStatType.MoveSpeed: stats.moveSpeed += modifierAmount; break;
            case PlayerStatType.Damage: stats.damage += Mathf.RoundToInt(modifierAmount); break;
            case PlayerStatType.MaxHP: stats.maxHP += Mathf.RoundToInt(modifierAmount); break;
            case PlayerStatType.PickupRadius: stats.pickupRadius += modifierAmount; break;
            case PlayerStatType.AttackSpeed: stats.damageMultiplier += modifierAmount; break;
            case PlayerStatType.JumpForce: stats.jumpForce += modifierAmount; break;
            case PlayerStatType.Other: /* custom hook */ break;
        }
    }
}
