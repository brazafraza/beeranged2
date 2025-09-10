using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    public enum PlayerStatType
    {
        MoveSpeed,
        Damage,
        MaxHP,
        PickupRadius,
        AttackSpeed,
        JumpForce,
        Other
    }

    public string itemName;
    public string description;
    public Sprite icon;

    public bool modifiesStats;
    public PlayerStatType statType;
    public float modifierAmount = 1f;

    public GameObject abilityPrefab;

    // Used by PlayerStats.RecalculateStats()
    public virtual void ApplyStatModifier(PlayerStats stats)
    {
        if (!modifiesStats) return;

        switch (statType)
        {
            case PlayerStatType.MoveSpeed:
                stats.moveSpeed += modifierAmount;
                break;
            case PlayerStatType.Damage:
                stats.damage += Mathf.RoundToInt(modifierAmount);
                break;
            case PlayerStatType.MaxHP:
                stats.maxHP += Mathf.RoundToInt(modifierAmount);
                break;
            case PlayerStatType.PickupRadius:
                stats.pickupRadius += modifierAmount;
                break;
            case PlayerStatType.AttackSpeed:
                stats.damageMultiplier += modifierAmount;
                break;
            case PlayerStatType.Other:
                //get specific here;
                break;
            case PlayerStatType.JumpForce:
                stats.jumpForce += modifierAmount;
                break;
        }
    }
}
