using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StackedItem
{
    public ItemSO item;
    public int count = 1;
}

public class InventorySystem : MonoBehaviour
{
    public const int MAX_ACTIVE_ITEMS = 5;

    [Header("All Items (With Stack Counts)")]
    public List<StackedItem> allItems = new List<StackedItem>();

    [Header("Active Items (Max 5 Unique Types)")]
    public List<StackedItem> activeItems = new List<StackedItem>();

    private PlayerStats ps;

    void Awake()
    {
        ps = GetComponent<PlayerStats>();
    }

    public bool AddItem(ItemSO item)
    {
        // Add to allItems
        StackedItem allItem = allItems.Find(x => x.item == item);
        if (allItem != null) allItem.count++;
        else allItems.Add(new StackedItem { item = item, count = 1 });

        // Add or stack in activeItems
        StackedItem activeItem = activeItems.Find(x => x.item == item);
        if (activeItem != null)
        {
            activeItem.count++;
        }
        else
        {
            if (activeItems.Count >= MAX_ACTIVE_ITEMS)
            {
                Debug.Log("Inventory full — can't add active item.");
                return false;
            }

            activeItems.Add(new StackedItem { item = item, count = 1 });

            if (item.abilityPrefab != null)
            {
                Instantiate(item.abilityPrefab, transform);
            }
        }

        Debug.Log($"Added/Stacked: {item.itemName}");
        ps.RecalculateStats();
        return true;
    }
}
