using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("Health")]
    public int maxHP = 100;
    public float invulnOnHit = 0.15f;         // short i-frames after getting hit
    public Slider hpBar;                       // optional UI; leave null if not used
    public TextMeshProUGUI hpTextTMP;          // optional TMP label: "HP: X / Y"

    [Header("Core Stats")]
    public float moveSpeed = 6f;
    public int baseDamage = 5;
    public float damageMultiplier = 1f;
    public float pickupRadius = 1.5f;

    [Header("Feedback (optional)")]
    public SpriteRenderer flashSprite;         // assign if you want a quick white flash
    public float flashDuration = 0.05f;

    public event Action<int, int> OnHealthChanged; // (current, max)
    public event Action OnDied;

    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    private float _invulnTimer;

    void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        CurrentHP = Mathf.Clamp(CurrentHP <= 0 ? maxHP : CurrentHP, 0, maxHP);
        UpdateHPUI();
    }

    void Update()
    {
        if (_invulnTimer > 0f) _invulnTimer -= Time.deltaTime;
    }

    /// <summary>Enemies (or hazards) call this to hurt the player.</summary>
    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        if (_invulnTimer > 0f) return;

        int dmg = Mathf.Max(0, amount);
        if (dmg == 0) return;

        CurrentHP = Mathf.Max(0, CurrentHP - dmg);
        _invulnTimer = invulnOnHit;

        // optional quick flash
        if (flashSprite != null) StartCoroutine(FlashCR());

        UpdateHPUI();

        if (CurrentHP == 0)
        {
            OnDied?.Invoke();
            // TODO: disable input / show game over, etc.
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        if (amount <= 0) return;

        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        UpdateHPUI();
    }

    public int GetDamageOutput()
    {
        // Central place to compute outgoing damage
        return Mathf.RoundToInt(baseDamage * Mathf.Max(0.1f, damageMultiplier));
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
            // Keep it simple/clear for mobile
            hpTextTMP.text = $"HP: {CurrentHP} / {maxHP}";
        }
    }
    // Sets CurrentHP directly (for inventory/maxHP changes) without flashes/i-frames.
    // Call this instead of TakeDamage/Heal when adjusting after stat recalculation.
    public void SetCurrentHPRaw(int newHP, bool clampToMax = true, bool suppressFeedback = true)
    {
        int target = clampToMax ? Mathf.Clamp(newHP, 0, maxHP) : newHP;
        CurrentHP = target;

        // If you ever add death checks here, keep them gated by suppressFeedback if needed.
        // e.g., if (!suppressFeedback && CurrentHP <= 0) { ... }

        // Ensure UI refresh
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
        // If you also use a Slider/TMP directly, your existing UpdateHPUI will cover it;
        // call it here if it's public. If it's private, the event + your UI listeners are enough.
    }

    private System.Collections.IEnumerator FlashCR()
    {
        Color orig = flashSprite.color;
        flashSprite.color = Color.white;
        yield return new WaitForSeconds(flashDuration);
        flashSprite.color = orig;
    }

#if UNITY_EDITOR
    // Keep UI in sync when tweaking in Inspector
    void OnValidate()
    {
        if (maxHP < 1) maxHP = 1;
        if (Application.isPlaying) return;

        // Preview values in editor if assigned
        if (hpBar != null)
        {
            hpBar.maxValue = maxHP;
            hpBar.value = Mathf.Clamp(CurrentHP == 0 ? maxHP : CurrentHP, 0, maxHP);
        }
        if (hpTextTMP != null)
        {
            int preview = Mathf.Clamp(CurrentHP == 0 ? maxHP : CurrentHP, 0, maxHP);
            hpTextTMP.text = $"HP: {preview} / {maxHP}";
        }
    }
#endif
}
