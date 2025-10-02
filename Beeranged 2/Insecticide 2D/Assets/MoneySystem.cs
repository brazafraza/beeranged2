using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneySystem : MonoBehaviour
{
    [Header("Balance")]
    [Tooltip("Current money the player has.")]
    public int balance = 0;

    [Tooltip("Max value for the optional slider (and optional clamp for balance). Set to 0 to disable clamping.")]
    public int walletCap = 9999;

    [Tooltip("Prefix shown in the money text (e.g., $, £, ¥).")]
    public string currencySymbol = "$";

    [Header("UI (optional)")]
    public TextMeshProUGUI moneyText;   // e.g., " $123 "
   

    [Header("Dev / Testing")]
    public KeyCode devAddKey = KeyCode.F1;
    public int devAddAmount = 5;

    // Fired whenever balance changes: provides new balance
    public event Action<int> OnBalanceChanged;

    void Start()
    {
        OnBalanceChanged += UpdateMoneyUI;
        // Initial sync
        UpdateMoneyUI(balance);
    }

    void Update()
    {
        // Dev cheat to add money
        if (Input.GetKeyUp(devAddKey))
        {
            AddMoney(devAddAmount);
        }
    }

    /// <summary>Adds money. Clamps to walletCap if > 0.</summary>
    public void AddMoney(int amount)
    {
        if (amount == 0) return;

        long newBal = (long)balance + amount; // avoid int overflow paranoia
        if (walletCap > 0) newBal = Mathf.Clamp((int)newBal, 0, walletCap);
        else newBal = Mathf.Max(0, (int)newBal);

        if (balance != (int)newBal)
        {
            balance = (int)newBal;
            OnBalanceChanged?.Invoke(balance);
        }
    }

    /// <summary>Attempts to spend money. Returns true if successful.</summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0) return true; // nothing to spend
        if (balance < amount) return false;

        balance -= amount;
        if (walletCap > 0) balance = Mathf.Clamp(balance, 0, walletCap);

        OnBalanceChanged?.Invoke(balance);
        return true;
    }

    /// <summary>Sets a new wallet cap (optional). Pass 0 to disable clamping. Updates slider if present.</summary>
    public void SetWalletCap(int newCap, bool clampBalance = true)
    {
        walletCap = Mathf.Max(0, newCap);
        if (clampBalance && walletCap > 0) balance = Mathf.Clamp(balance, 0, walletCap);
        OnBalanceChanged?.Invoke(balance);
    }

    /// <summary>0..1 fill toward walletCap (0 if cap is 0).</summary>
    public float GetFill()
    {
        if (walletCap <= 0) return 0f;
        return Mathf.Clamp01(walletCap == 0 ? 0f : (float)balance / walletCap);
    }

    private void UpdateMoneyUI(int newBalance)
    {
        if (moneyText != null)
            moneyText.text = $"{currencySymbol}{newBalance}";

       
           
           
        
    }
}
