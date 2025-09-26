using System;
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

    public event Action OnChanged;

    private PlayerStats ps;

    void Awake()
    {
        ps = GetComponent<PlayerStats>();
    }

    void RaiseChanged()
    {
        OnChanged?.Invoke();
    }

    // --- Adding loot ---
    public bool AddItem(ItemSO item)
    {
        if (item == null) return false;

        // All items (stack)
        var all = allItems.Find(x => x.item == item);
        if (all != null) all.count++;
        else allItems.Add(new StackedItem { item = item, count = 1 });

        // If already active, increment active stack too
        var act = activeItems.Find(x => x.item == item);
        if (act != null)
        {
            act.count++;
            ps?.RecalculateStats();
            RaiseChanged();
            return true;
        }

        // Otherwise, if there’s room, auto-activate first time
        if (activeItems.Count < MAX_ACTIVE_ITEMS)
        {
            activeItems.Add(new StackedItem { item = item, count = 1 });
            if (item.abilityPrefab) Instantiate(item.abilityPrefab, transform);
            ps?.RecalculateStats();
            RaiseChanged();
            return true;
        }

        // No room to auto-activate; remains inactive only
        ps?.RecalculateStats();
        RaiseChanged();
        return true;
    }

    // --- Helpers for UI ---

    public void SwapActive(int a, int b)
    {
        if (a < 0 || b < 0 || a >= activeItems.Count || b >= activeItems.Count) return;
        (activeItems[a], activeItems[b]) = (activeItems[b], activeItems[a]);
        ps?.RecalculateStats();
        RaiseChanged();
    }

    public void DeactivateSlot(int index)
    {
        if (index < 0 || index >= activeItems.Count) return;
        activeItems.RemoveAt(index);
        ps?.RecalculateStats();
        RaiseChanged();
    }

    /// <summary>
    /// Put 'item' into active slot 'index'. If replace==true, will replace whatever is there.
    /// desiredCount: how many stacks to make active (defaults to 1). Clamped by remainder (all - active).
    /// </summary>
    public bool SetActiveAt(int index, ItemSO item, int desiredCount = 1, bool replace = true)
    {
        if (item == null) return false;

        int remainder = GetInactiveCount(item);
        if (remainder <= 0) return false;

        int makeActive = Mathf.Clamp(desiredCount, 1, remainder);

        if (index >= 0 && index < activeItems.Count)
        {
            if (!replace) return false;
            activeItems[index] = new StackedItem { item = item, count = makeActive };
        }
        else
        {
            if (activeItems.Count >= MAX_ACTIVE_ITEMS) return false;
            activeItems.Add(new StackedItem { item = item, count = makeActive });
        }

        MergeDuplicateActives(item);

        if (item.abilityPrefab && !HasAbilityInstance(item))
        {
            Instantiate(item.abilityPrefab, transform);
        }

        ps?.RecalculateStats();
        RaiseChanged();
        return true;
    }

    void MergeDuplicateActives(ItemSO item)
    {
        int first = -1;
        int total = 0;
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i].item == item)
            {
                total += activeItems[i].count;
                if (first == -1) first = i;
            }
        }
        if (first != -1)
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                if (i == first) continue;
                if (activeItems[i].item == item) activeItems.RemoveAt(i);
            }
            activeItems[first].count = Mathf.Min(total, GetAllCount(item));
        }
    }

    bool HasAbilityInstance(ItemSO item)
    {
        return false;
    }

    public int GetAllCount(ItemSO item)
    {
        int c = 0;
        foreach (var s in allItems) if (s.item == item) c += s.count;
        return c;
    }

    public int GetActiveCount(ItemSO item)
    {
        int c = 0;
        foreach (var s in activeItems) if (s.item == item) c += s.count;
        return c;
    }

    public int GetInactiveCount(ItemSO item)
    {
        return Mathf.Max(0, GetAllCount(item) - GetActiveCount(item));
    }
}
