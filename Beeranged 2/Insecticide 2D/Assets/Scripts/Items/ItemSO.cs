using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]


public class ItemSO : ScriptableObject
{
    public enum PlayerStatType { MoveSpeed, Damage, MaxHP, PickupRadius, AttackSpeed, JumpForce, Other }
    public enum ItemClass { Winged, Predator, Metamorph, Swarm, Dancer }
    public enum Rarity { Scraps, Pseudo, Gene, Strand, Merged }

    public enum MovementAbilityType
    {
        None,
        Dash,
        BlinkForward,
        BlinkUp,
        Hover
    }


    [Header("Identity")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemClass itemClass;

    public Rarity rarity = Rarity.Scraps; // <-- used by weighted shop rolls

    [Header("Effects (Stats)")]
    public bool modifiesStats;
    public PlayerStatType statType;
    public float modifierAmount = 1f;

    [Header("Legacy Ability")]
    [Tooltip("Used by the old 5-item system, or as a generic fallback if slot-specific prefabs are not set.")]
    public GameObject abilityPrefab;

    [Header("Slot Abilities (3-slot mode)")]
    [Tooltip("Ability used when this item sits in the PRIMARY slot (slot 0).")]
    public PrimaryAbilitySO primaryAbility;

    [Tooltip("Ability used when this item sits in the SECONDARY slot (slot 1).")]
    public SecondaryAbilitySO secondaryAbility;

    [Tooltip("Ability used when this item sits in the MOVEMENT slot (slot 2).")]
    public MovementAbilitySO movementAbility;


  

    [Header("Movement Ability")]
    [Tooltip("What movement ability this item grants when placed in the movement slot (slot 2).")]
    public MovementAbilityType movementAbilityType = MovementAbilityType.None;

    [Header("Movement Overrides")]
    [Tooltip("If true and this item is in the movement slot, it overrides dash stats on the PlayerController.")]
    public bool overridesMovementStats;
    public float movementDashSpeed = 16f;
    public float movementDashDuration = 0.15f;
    public float movementDashCooldown = 0.75f;

    [Header("Other")]
    public int stability = 1;

    [Header("Stack Scaling")]
    public float maxStackBonus = 0.20f;   // e.g. 20% max
    public float stackFalloff = 0.70f;   // tweak curve

    public float GetStackMultiplier(int count)
    {
        if (count <= 1) return 1f;
        int extra = count - 1;
        float bonus = maxStackBonus * (1f - Mathf.Exp(-stackFalloff * extra));
        return 1f + bonus;
    }

    // Used by PlayerStats.RecalculateStats()
    public virtual void ApplyStatModifier(PlayerStats stats, float stackMultiplier)
    {
        if (!modifiesStats) return;
        float amount = modifierAmount * stackMultiplier;

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
    public virtual void ApplyStatModifier(PlayerStats stats)
    {
        ApplyStatModifier(stats, 1f);
    }
}
