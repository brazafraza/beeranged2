using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MergedItem", menuName = "Inventory/Merged Item (Runtime Only)")]
public class MergedItemSO : ItemSO
{
    [Tooltip("Items that this merged item is composed of (applies their stat modifiers).")]
    public List<ItemSO> sources = new List<ItemSO>();

    // We ignore base modifiesStats/statType on the merged SO and apply each source's logic.
    public override void ApplyStatModifier(PlayerStats stats)
    {
        if (sources == null) return;
        for (int i = 0; i < sources.Count; i++)
        {
            var s = sources[i];
            if (s != null)
                s.ApplyStatModifier(stats);
        }
    }
}
