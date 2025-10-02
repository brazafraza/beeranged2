using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    [Header("Shop UI")]
    public LevelUpPanel levelUpPanel;

    [Header("Available Items")]
    public List<ItemSO> allAvailableItems;

    [Header("Money")]
    public MoneySystem money;                   // auto-found if null
    public KeyCode openShopKey = KeyCode.Alpha1;

    [Header("Rarity Weights (runtime-tweakable)")]
    public float weightScraps = 75f;   // common
    public float weightPseudo = 18f;   // uncommon
    public float weightGene = 5f;    // rare
    public float weightStrand = 2f;    // ultra rare

    [Header("Rarity Prices (runtime-tweakable)")]
    public int priceScraps = 25;
    public int pricePseudo = 60;
    public int priceGene = 150;
    public int priceStrand = 350;

    [Header("Options")]
    public bool preventDuplicatesPerRoll = true; // avoid same item twice this roll

    // runtime state
    private InventorySystem inventory;
    private bool shopOpen;
    private ItemSO[] currentChoices;
    private int[] currentPrices;

    public bool IsShopOpen => shopOpen;

    void Start()
    {
        if (!money) money = FindAnyObjectByType<MoneySystem>();
        inventory = FindAnyObjectByType<InventorySystem>();

        if (!levelUpPanel)
            Debug.LogWarning("UpgradeManager: LevelUpPanel is not assigned.");
        if (!inventory)
            Debug.LogWarning("UpgradeManager: InventorySystem not found in scene.");
        if (!money)
            Debug.LogWarning("UpgradeManager: MoneySystem not found in scene.");
    }

    void Update()
    {
        // Optional hotkey opening (terminal also calls OpenShop/CloseShop)
        if (Input.GetKeyDown(openShopKey))
        {
            if (shopOpen) CloseShop();
            else OpenShop();
        }
        // NOTE: No 'R' handling here — Terminal handles close+pause so prompt stays correct.
    }

    // ===== Shop flow =====
    public void OpenShop()
    {
        if (!levelUpPanel) return;

        currentChoices = PickWeightedItems(3);

        currentPrices = new int[currentChoices.Length];
        for (int i = 0; i < currentChoices.Length; i++)
            currentPrices[i] = GetRarityPrice(currentChoices[i]?.rarity ?? ItemSO.Rarity.Scraps);

        // Keep the panel open on click if player can't afford
        levelUpPanel.SetAutoHide(false);

        levelUpPanel.Show(currentChoices, OnUpgradeChosen);
        shopOpen = true;

        SetOptionText(0);
        SetOptionText(1);
        SetOptionText(2);
    }

    public void CloseShop()
    {
        if (!levelUpPanel) return;
        levelUpPanel.Hide();
        shopOpen = false;

        currentChoices = null;
        currentPrices = null;
    }

    void OnUpgradeChosen(ItemSO chosenItem)
    {
        if (!shopOpen || chosenItem == null) return;
        if (!inventory || !money) { CloseShop(); return; }

        int idx = IndexOf(currentChoices, chosenItem);
        if (idx < 0) return;

        int price = currentPrices[idx];

        // Try to buy
        if (money.SpendMoney(price))
        {
            if (inventory.AddItem(chosenItem))
            {
                Debug.Log($"Purchased: {chosenItem.itemName} for ${price}");
                CloseShop(); // success closes
            }
            else
            {
                // Refund if we couldn't add the item
                money.AddMoney(price);
                Debug.Log("Inventory full — refunded.");
                // Panel stays open
            }
        }
        else
        {
            // Not enough money -> click does nothing, panel stays open
        }
    }

    // ===== Weighted rarity selection =====
    ItemSO[] PickWeightedItems(int count)
    {
        var pool = new List<ItemSO>();
        foreach (var it in allAvailableItems)
        {
            if (it == null) continue;
            if (it.rarity == ItemSO.Rarity.Merged) continue; // exclude
            pool.Add(it);
        }

        var picks = new List<ItemSO>(count);

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0) break;

            var pick = WeightedPick(pool);
            if (pick == null) break;

            picks.Add(pick);
            if (preventDuplicatesPerRoll) pool.Remove(pick);
        }

        return picks.ToArray();
    }

    ItemSO WeightedPick(List<ItemSO> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;

        float total = 0f;
        for (int i = 0; i < candidates.Count; i++)
            total += GetRarityWeight(candidates[i].rarity);

        if (total <= 0f) return candidates[Random.Range(0, candidates.Count)]; // fallback

        float r = Random.value * total;
        float acc = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            acc += GetRarityWeight(candidates[i].rarity);
            if (r <= acc) return candidates[i];
        }
        return candidates[candidates.Count - 1]; // safety
    }

    float GetRarityWeight(ItemSO.Rarity r)
    {
        switch (r)
        {
            case ItemSO.Rarity.Scraps: return Mathf.Max(0f, weightScraps);
            case ItemSO.Rarity.Pseudo: return Mathf.Max(0f, weightPseudo);
            case ItemSO.Rarity.Gene: return Mathf.Max(0f, weightGene);
            case ItemSO.Rarity.Strand: return Mathf.Max(0f, weightStrand);
            case ItemSO.Rarity.Merged: return 0f;
            default: return 0f;
        }
    }

    int GetRarityPrice(ItemSO.Rarity r)
    {
        switch (r)
        {
            case ItemSO.Rarity.Scraps: return Mathf.Max(0, priceScraps);
            case ItemSO.Rarity.Pseudo: return Mathf.Max(0, pricePseudo);
            case ItemSO.Rarity.Gene: return Mathf.Max(0, priceGene);
            case ItemSO.Rarity.Strand: return Mathf.Max(0, priceStrand);
            case ItemSO.Rarity.Merged: return 0;
            default: return priceScraps;
        }
    }

    // ===== UI helpers =====
    int IndexOf(ItemSO[] arr, ItemSO item)
    {
        if (arr == null || item == null) return -1;
        for (int i = 0; i < arr.Length; i++)
            if (arr[i] == item) return i;
        return -1;
    }

    void SetOptionText(int i)
    {
        if (currentChoices == null || i >= currentChoices.Length) return;
        var item = currentChoices[i];
        int price = (currentPrices != null && i < currentPrices.Length) ? currentPrices[i] : 0;

        switch (i)
        {
            case 0:
                if (levelUpPanel.optionName1) levelUpPanel.optionName1.text = item ? item.itemName : "--";
                if (levelUpPanel.optionDesc1) levelUpPanel.optionDesc1.text =
                    item ? $"{item.description}\n<color=#FFD700>Cost: ${price}</color>" : "";
                if (levelUpPanel.optionIcon1) levelUpPanel.optionIcon1.sprite = item ? item.icon : null;
                break;

            case 1:
                if (levelUpPanel.optionName2) levelUpPanel.optionName2.text = item ? item.itemName : "--";
                if (levelUpPanel.optionDesc2) levelUpPanel.optionDesc2.text =
                    item ? $"{item.description}\n<color=#FFD700>Cost: ${price}</color>" : "";
                if (levelUpPanel.optionIcon2) levelUpPanel.optionIcon2.sprite = item ? item.icon : null;
                break;

            case 2:
                if (levelUpPanel.optionName3) levelUpPanel.optionName3.text = item ? item.itemName : "--";
                if (levelUpPanel.optionDesc3) levelUpPanel.optionDesc3.text =
                    item ? $"{item.description}\n<color=#FFD700>Cost: ${price}</color>" : "";
                if (levelUpPanel.optionIcon3) levelUpPanel.optionIcon3.sprite = item ? item.icon : null;
                break;
        }
    }
}
