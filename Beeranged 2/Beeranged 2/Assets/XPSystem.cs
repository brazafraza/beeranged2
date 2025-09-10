using System;
using UnityEngine;
using UnityEngine.UI;

public class XPSystem : MonoBehaviour
{
    [Header("XP Settings")]
    public int currentLevel = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 100f;
    public float xpGrowthRate = 1.25f; // 125% growth per level
    public float devAmnt = 5f;         // Dev XP test input

    [Header("UI")]
    public Slider xpSlider;

    [Header("Upgrade Handling")]
    public UpgradeManager upgradeManager; // Set in inspector

    // Events
    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    void Start()
    {
        // Subscribe to our own XP change event
        OnXPChanged += UpdateXPUI;

        // Subscribe to our own level up event to trigger upgrades
        OnLevelUp += HandleLevelUp;

        // Ensure initial UI sync
        UpdateXPUI(currentXP, xpToNextLevel);
    }

    void Update()
    {
        // Dev cheat: Press Escape to add XP
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            AddXP(devAmnt);
        }
    }

    public void AddXP(float amount)
    {
        currentXP += amount;
        CheckForLevelUp();
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    void CheckForLevelUp()
    {
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            currentLevel++;
            xpToNextLevel *= xpGrowthRate;

            Debug.Log($"LEVEL UP! Now level {currentLevel}");

            OnLevelUp?.Invoke(currentLevel);
            OnXPChanged?.Invoke(currentXP, xpToNextLevel);
        }
    }

    void UpdateXPUI(float currentXP, float requiredXP)
    {
        if (xpSlider != null)
        {
            xpSlider.maxValue = requiredXP;
            xpSlider.value = currentXP;
        }
    }

    void HandleLevelUp(int newLevel)
    {
        if (upgradeManager != null)
        {
            upgradeManager.ShowUpgradeOptions();
        }
        else
        {
            Debug.LogWarning("UpgradeManager not assigned on XPSystem.");
        }
    }

    public float GetXPProgress()
    {
        return currentXP / xpToNextLevel;
    }
}
