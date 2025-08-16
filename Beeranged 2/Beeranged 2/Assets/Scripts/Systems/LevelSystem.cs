using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSystem : MonoBehaviour
{
    [Header("Refs")]
    public PlayerInventory inventory;  // drag your PlayerInventory
    public ItemDatabase database;      // drag your ItemDatabase
    public LevelUpPanel levelUpPanel;  // drag the panel
    //public TextMeshProUGUI levelText;  // optional
    public Slider xpBar;               // optional

    [Header("XP Settings")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int xpToNext = 10;

    [Header("Rarity Weights")]
    public int wCommon = 60;
    public int wUncommon = 30;
    public int wRare = 10;
    public int wEpic = 4;
    public int wLegendary = 1;

    private bool _panelOpen = false;

    void Start()
    {
        if (xpToNext < 1) xpToNext = 10;
        UpdateUI();
    }

    public void AddXP(int amount)
    {
        if (amount <= 0) return;
        currentXP += amount;

        while (currentXP >= xpToNext)
        {
            currentXP -= xpToNext;
            currentLevel += 1;
            xpToNext = GetXPRequirementForLevel(currentLevel);
            UpdateUI();

            OpenLevelUp();
            if (_panelOpen) break; // wait for choice
        }
        UpdateUI();
    }

    private int GetXPRequirementForLevel(int level)
    {
        int baseReq = 10;
        int perLevel = 5;
        float scale = 1f + 0.05f * (level - 1);
        int req = Mathf.RoundToInt((baseReq + perLevel * (level - 1)) * scale);
        return Mathf.Max(10, req);
    }

    private void OpenLevelUp()
    {
        if (_panelOpen || database == null || inventory == null || levelUpPanel == null) return;

        ItemSO[] options = RollOptions(3);
        _panelOpen = true;
        Time.timeScale = 0f;

        levelUpPanel.Show(options, OnItemChosen);
    }

    private void OnItemChosen(ItemSO item)
    {
        if (item != null) inventory.Add(item);

        _panelOpen = false;
        levelUpPanel.Hide();
        Time.timeScale = 1f;
        UpdateUI();
    }

    private ItemSO[] RollOptions(int count)
    {
        var result = new List<ItemSO>(count);
        if (database == null || database.items == null || database.items.Count == 0)
            return result.ToArray();

        // Build pool of items not at max stack
        var pool = new List<ItemSO>();
        for (int i = 0; i < database.items.Count; i++)
        {
            var it = database.items[i];
            if (it == null) continue;
            if (inventory == null || inventory.CanAdd(it)) pool.Add(it);
        }
        if (pool.Count == 0) return result.ToArray();

        // Pick distinct items with rarity weights
        for (int k = 0; k < count; k++)
        {
            if (pool.Count == 0) break;

            int idx = WeightedIndex(pool);
            if (idx < 0 || idx >= pool.Count) break;

            result.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
        return result.ToArray();
    }

    private int WeightedIndex(List<ItemSO> pool)
    {
        int total = 0;
        for (int i = 0; i < pool.Count; i++) total += WeightFor(pool[i].rarity);
        if (total <= 0) return Random.Range(0, pool.Count);

        int roll = Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            acc += WeightFor(pool[i].rarity);
            if (roll < acc) return i;
        }
        return pool.Count - 1;
    }

    private int WeightFor(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Common: return wCommon;
            case ItemRarity.Uncommon: return wUncommon;
            case ItemRarity.Rare: return wRare;
            case ItemRarity.Epic: return wEpic;
            case ItemRarity.Legendary: return wLegendary;
            default: return 1;
        }
    }

    private void UpdateUI()
    {
        //if (levelText) levelText.text = $"Lv {currentLevel}";
        if (xpBar)
        {
            xpBar.minValue = 0;
            xpBar.maxValue = xpToNext;
            xpBar.value = Mathf.Clamp(currentXP, 0, xpToNext);
        }
    }
}
