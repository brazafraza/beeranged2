using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]

public class StackedItem
{
    public ItemSO item;
    public int count = 1;
}

// Gameplay mode for active slots
public enum GameplayMode
{
    FiveItemSystem,      // old: up to 5 stacked items
    ThreeAbilitySystem   // new: 3 ability slots (Primary / Secondary / Movement)
}

public class InventorySystem : MonoBehaviour
{
    public const int MAX_ACTIVE_ITEMS = 5;
 



    [Header("Gameplay Mode")]
    public GameplayMode gameplayMode = GameplayMode.FiveItemSystem;

    /// <summary>
    /// How many active slots should be used based on the selected gameplay mode.
    /// FiveItemSystem: 5 slots
    /// ThreeAbilitySystem: 3 slots (use as Primary / Secondary / Movement).
    /// </summary>
    public int CurrentMaxActiveSlots
    {
        get
        {
            switch (gameplayMode)
            {
                case GameplayMode.ThreeAbilitySystem:
                    return 3;

                case GameplayMode.FiveItemSystem:
                default:
                    return MAX_ACTIVE_ITEMS; // 5
            }
        }
    }

    [Header("All Items")]
    public List<StackedItem> allItems = new List<StackedItem>();

    [Header("Active Items")]
    public List<StackedItem> activeItems = new List<StackedItem>();

    public event Action OnChanged;

    private PlayerStats ps;

    [Header("Ability UI")]
    public Image primaryAbilityIcon;
    public Image secondaryAbilityIcon;
    public Image movementAbilityIcon;
    void Awake()
    {
        ps = GetComponent<PlayerStats>();
    }

    void RaiseChanged()
    {
        UpdateAbilityUI();
        OnChanged?.Invoke();
    }

    // --- Adding loot ---
    public bool AddItem(ItemSO item)
    {
        if (item == null) return false;

        // Every pickup is its own entry (no stacking)
        allItems.Add(new StackedItem { item = item, count = 1 });

        // Auto-activate if there is room in active bar
        if (activeItems.Count < MAX_ACTIVE_ITEMS)
        {
            activeItems.Add(new StackedItem { item = item, count = 1 });

            if (item.abilityPrefab)
                Instantiate(item.abilityPrefab, transform);

            ps?.RecalculateStats();
            RaiseChanged();
            return true;
        }

        // No room to auto-activate; item is just inactive in inventory
        ps?.RecalculateStats();
        RaiseChanged();
        return true;
    }


    // --- Helpers for UI & abilities ---
    public bool MoveOneActiveToInactive(int fromIndex)
    {
        if (fromIndex < 0 || fromIndex >= activeItems.Count) return false;
        var src = activeItems[fromIndex];
        if (src == null || src.item == null || src.count <= 0) return false;

        // Just decrement active; 'allItems' already holds total count.
        src.count -= 1;
        if (src.count <= 0)
        {
            activeItems[fromIndex] = new StackedItem { item = null, count = 0 };
        }

        ps?.RecalculateStats();
        OnChanged?.Invoke();
        return true;
    }

    public bool MoveOneActiveToSlot(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= activeItems.Count) return false;
        if (toIndex < 0) return false;

        var src = activeItems[fromIndex];
        if (src == null || src.item == null || src.count <= 0) return false;

        // Ensure list is large enough to hold dest index
        while (activeItems.Count <= toIndex)
        {
            activeItems.Add(new StackedItem { item = null, count = 0 });
        }

        var dst = activeItems[toIndex];

        // If dest slot empty: create new stack of 1
        if (dst == null || dst.item == null || dst.count <= 0)
        {
            activeItems[toIndex] = new StackedItem { item = src.item, count = 1 };
        }
        else if (dst.item == src.item)
        {
            // Same item type: increment dest stack
            dst.count += 1;
        }
        else
        {
            // Different item already there: you can decide to fail or swap.
            // For "drag 1 copy here", I'd usually FAIL and let UI prevent this scenario.
            return false;
        }

        // Decrement source
        src.count -= 1;
        if (src.count <= 0)
        {
            // Clear source slot
            activeItems[fromIndex] = new StackedItem { item = null, count = 0 };
        }

        // Recalc stats & notify UI
        ps?.RecalculateStats();
        OnChanged?.Invoke();
        return true;
    }

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

        // Do we actually have at least one inactive copy of this item?
        int remainder = GetInactiveCount(item);
        if (remainder <= 0) return false;

        // In the non-stacking system, we always activate exactly ONE copy
        int makeActive = 1;

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

        // No more merging duplicates – duplicates should stay as separate entries
        // MergeDuplicateActives(item);  // <- remove or comment this out

        if (item.abilityPrefab && !HasAbilityInstance(item))
            Instantiate(item.abilityPrefab, transform);

        ps?.RecalculateStats();
        RaiseChanged();
        return true;
    }

    void UpdateAbilityUI()
    {
        // We can still update in both modes, but it mainly makes sense in ThreeAbilitySystem
        UpdateAbilityIcon(primaryAbilityIcon, GetActiveItemAt(0));
        UpdateAbilityIcon(secondaryAbilityIcon, GetActiveItemAt(1));
        UpdateAbilityIcon(movementAbilityIcon, GetActiveItemAt(2));
    }

    void UpdateAbilityIcon(Image img, ItemSO item)
    {
        if (!img) return;

        if (item != null && item.icon != null)
        {
            img.sprite = item.icon;
            // Make sure it's visible
            var c = img.color;
            c.a = 1f;
            img.color = c;
        }
        else
        {
            // No item: clear sprite and hide it (alpha 0)
            img.sprite = null;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
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
        // (You can implement tracking here if you want to avoid multiple instances.)
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

    /// <summary>
    /// Returns the ItemSO currently in the given active slot index, or null if empty/out of range.
    /// </summary>
    public ItemSO GetActiveItemAt(int index)
    {
        if (index < 0 || index >= activeItems.Count) return null;
        return activeItems[index]?.item;
    }
}
