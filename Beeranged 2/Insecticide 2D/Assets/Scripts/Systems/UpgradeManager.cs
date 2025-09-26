using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Upgrade UI")]
    public LevelUpPanel levelUpPanel;

    [Header("Available Items")]
    public List<ItemSO> allAvailableItems;

    private InventorySystem inventory;

    void Start()
    {
        inventory = FindObjectOfType<InventorySystem>();

        // Subscribe to XP level-up event
        XPSystem xp = FindAnyObjectByType<XPSystem>();
        if (xp != null)
        {
            xp.OnLevelUp += OnLevelUp;
        }
        else
        {
            Debug.LogWarning("XPSystem not found! UpgradeManager will not receive level-up events.");
        }
    }

    void OnLevelUp(int newLevel)
    {
        Debug.Log($"UpgradeManager: Level {newLevel} reached!");
        ShowUpgradeOptions();
    }

    public void ShowUpgradeOptions()
    {
        ItemSO[] choices = PickRandomItems(3);
        levelUpPanel.Show(choices, OnUpgradeChosen);
    }

    void OnUpgradeChosen(ItemSO chosenItem)
    {
        if (inventory.AddItem(chosenItem))
        {
            Debug.Log("Picked: " + chosenItem.itemName);
        }
        else
        {
            Debug.Log("Could not add item: Inventory full.");
        }
    }

    ItemSO[] PickRandomItems(int count)
    {
        List<ItemSO> shuffled = new List<ItemSO>(allAvailableItems);
        for (int i = 0; i < shuffled.Count; i++)
        {
            ItemSO temp = shuffled[i];
            int rand = UnityEngine.Random.Range(i, shuffled.Count);
            shuffled[i] = shuffled[rand];
            shuffled[rand] = temp;
        }

        return shuffled.GetRange(0, Mathf.Min(count, shuffled.Count)).ToArray();
    }
}
