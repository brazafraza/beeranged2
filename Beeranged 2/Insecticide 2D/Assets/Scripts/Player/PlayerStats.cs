using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{

    [Header("Health")]
    public int baseMaxHP = 100;
    public float invulnOnHit = 0.15f;
    public Slider hpBar;
    public TextMeshProUGUI hpTextTMP;

    [Header("Core Stats (Base Values)")]
    public float baseMoveSpeed = 6f;
    public float baseJumpForce = 6f;
    public int baseDamage = 5;
    public float basePickupRadius = 1.5f;

    [Header("Current (Calculated) Stats")]
    public int maxHP;
    public float moveSpeed;
    public float jumpForce;
    public int damage;
    public float pickupRadius;
    public float damageMultiplier = 1f;

    [Header("Feedback")]
    public SpriteRenderer flashSprite;
    public float flashDuration = 0.05f;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    private float _invulnTimer;
    private InventorySystem inventory;

    void Awake()
    {
        inventory = GetComponent<InventorySystem>();

        RecalculateStats(); // Init stats

        CurrentHP = maxHP;
        UpdateHPUI();
    }

    void Update()
    {
        if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || _invulnTimer > 0f) return;

        int dmg = Mathf.Max(0, amount);
        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        _invulnTimer = invulnOnHit;

        if (flashSprite != null) StartCoroutine(FlashCR());

        UpdateHPUI();

        if (CurrentHP == 0)
        {
            OnDied?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0) return;

        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        UpdateHPUI();
    }

    public int GetDamageOutput()
    {
        return Mathf.RoundToInt(damage * Mathf.Max(0.1f, damageMultiplier));
    }

    public void RecalculateStats()
    {
        // Start from base
        moveSpeed = baseMoveSpeed;
        jumpForce = baseJumpForce;
        damage = baseDamage;
        maxHP = baseMaxHP;
        pickupRadius = basePickupRadius;
        damageMultiplier = 1f;

        // Add modifiers from all items
        if (inventory != null)
        {
            foreach (var stack in inventory.activeItems)
            {
                if (stack == null || stack.item == null || stack.count <= 0)
                    continue;

                // Diminishing-returns multiplier based on this stack’s count
                float mult = stack.item.GetStackMultiplier(stack.count);

                // Apply the item’s effect once, scaled by the multiplier
                stack.item.ApplyStatModifier(this, mult);
            }
        }


        // Clamp HP
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);
        UpdateHPUI();

        // Push updated stats to controller
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.SyncStatsFromPlayerStats();
        }
    }


    public void SetCurrentHPRaw(int newHP, bool clampToMax = true, bool suppressFeedback = true)
    {
        int target = clampToMax ? Mathf.Clamp(newHP, 0, maxHP) : newHP;
        CurrentHP = target;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    private void UpdateHPUI()
    {
        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (hpBar != null)
        {
            hpBar.maxValue = maxHP;
            hpBar.value = CurrentHP;
        }

        if (hpTextTMP != null)
        {
            hpTextTMP.text = $"HP: {CurrentHP} / {maxHP}";
        }
    }

    private System.Collections.IEnumerator FlashCR()
    {
        Color orig = flashSprite.color;
        flashSprite.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        flashSprite.color = orig;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (hpBar != null)
            {
                hpBar.maxValue = baseMaxHP;
                hpBar.value = baseMaxHP;
            }

            if (hpTextTMP != null)
            {
                hpTextTMP.text = $"HP: {baseMaxHP} / {baseMaxHP}";
            }
        }
    }
#endif
}
