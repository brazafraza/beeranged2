using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public Image icon;
        public TextMeshProUGUI countText;
    }

    [Header("Inventory Slots")]
    public List<InventorySlot> slots = new List<InventorySlot>();

    private InventorySystem inventory;

    void Start()
    {
        inventory = FindObjectOfType<InventorySystem>();
        UpdateUI();
    }

    public void UpdateUI()
    {
        List<StackedItem> items = inventory.activeItems;

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < items.Count)
            {
                var stacked = items[i];

                if (stacked.item != null && slots[i].icon != null)
                {
                    slots[i].icon.sprite = stacked.item.icon;
                    slots[i].icon.enabled = true;
                }

                if (slots[i].countText != null)
                {
                    slots[i].countText.text = stacked.count > 1 ? stacked.count.ToString() : "";
                }
            }
            else
            {
                // Empty slot
                if (slots[i].icon != null)
                {
                    slots[i].icon.sprite = null;
                    slots[i].icon.enabled = false;
                }

                if (slots[i].countText != null)
                {
                    slots[i].countText.text = "";
                }
            }
        }
    }
}
