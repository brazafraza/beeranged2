using UnityEngine;

public class WorldItemPickup : MonoBehaviour
{
    public ItemSO itemSO;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            InventorySystem inventory = other.GetComponent<InventorySystem>();
            if (inventory != null && itemSO != null)
            {
                bool added = inventory.AddItem(itemSO);
                if (added)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Debug.LogWarning("Inventory full. Could not pick up item: " + itemSO.itemName);
                }
            }
        }
    }
}
