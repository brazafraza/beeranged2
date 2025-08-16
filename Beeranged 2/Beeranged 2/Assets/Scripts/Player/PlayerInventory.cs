using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [System.Serializable]
    public class StackEntry
    {
        public ItemSO item;
        public int count;
    }

    [SerializeField] private GameClock clock;        // optional; auto-found if null

    [Header("HP Ratio Preserve + Time Bonus")]
    [Range(0f, 1f)] public float healBonusBase = 0.10f;      // 10% of the MaxHP increase
    [Range(0f, 1f)] public float healBonusPerMinute = 0.02f;  // +2% per minute
    [Range(0f, 1f)] public float healBonusMax = 0.30f;        // cap at 30%

    [Header("References")]
    public PlayerStats playerStats;   // drag player
    public AutoShooter autoShooter;   // drag shooter

    [Header("Unique Behaviours (enable/tune here)")]
    public PollenAura pollenAura;             // optional behaviour; toggled by items
    public ExplodingShotsMod explodingShots;  // optional, if you added it

    [Header("Inventory (runtime)")]
    public List<StackEntry> stacks = new List<StackEntry>();

    public event System.Action OnInventoryChanged;

    // --- Baseline snapshot (captured on Awake) ---
    private int _baseMaxHP;
    private int _baseBaseDamage;
    private float _baseDamageMultiplier;
    private float _baseMoveSpeed;
    private float _basePickupRadius;

    private float _baseFireRate;
    private float _baseBulletSpeed;
    private float _baseClosestEnemyRange;
    private float _baseWeaponDamageMultiplier;
    private int _baseWeaponDamageFlat;
    private int _baseShooterLegacyDamage;

    void Awake()
    {
        if (!clock) clock = FindObjectOfType<GameClock>();
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!autoShooter) autoShooter = GetComponent<AutoShooter>();

        // Snapshot base stats
        if (playerStats)
        {
            _baseMaxHP = Mathf.Max(1, playerStats.maxHP);
            _baseBaseDamage = playerStats.baseDamage;
            _baseDamageMultiplier = playerStats.damageMultiplier;
            _baseMoveSpeed = playerStats.moveSpeed;
            _basePickupRadius = playerStats.pickupRadius;
        }
        if (autoShooter)
        {
            _baseFireRate = autoShooter.fireRate;
            _baseBulletSpeed = autoShooter.bulletSpeed;
            _baseClosestEnemyRange = autoShooter.closestEnemyRange;
            _baseWeaponDamageMultiplier = autoShooter.weaponDamageMultiplier;
            _baseWeaponDamageFlat = autoShooter.weaponDamageFlat;
            _baseShooterLegacyDamage = autoShooter.damage;
        }

        RecalculateAll();
    }

    public int GetCount(ItemSO item)
    {
        for (int i = 0; i < stacks.Count; i++)
            if (stacks[i].item == item) return stacks[i].count;
        return 0;
    }

    public bool CanAdd(ItemSO item)
    {
        int c = GetCount(item);
        return c < Mathf.Max(1, item.maxStack);
    }

    public void Add(ItemSO item)
    {
        if (item == null) return;

        // One-shot effects on pick (before stacking)
        for (int i = 0; i < item.effects.Count; i++)
        {
            var eff = item.effects[i];
            if (eff.type == ItemEffectType.HealPercentOnPick && playerStats && playerStats.IsAlive)
            {
                int heal = Mathf.RoundToInt(playerStats.maxHP * Mathf.Clamp01(eff.value / 100f));
                playerStats.Heal(heal);
            }
        }

        // Stack or create
        for (int i = 0; i < stacks.Count; i++)
        {
            if (stacks[i].item == item)
            {
                if (stacks[i].count < item.maxStack)
                    stacks[i].count += 1;
                RecalculateAll();
                return;
            }
        }
        stacks.Add(new StackEntry { item = item, count = 1 });
        RecalculateAll();
    }

    public void RecalculateAll()
    {
        // Capture current health BEFORE we reset/apply items (needed for ratio-preserve)
        int oldMax = playerStats ? playerStats.maxHP : 0;
        int oldHP = playerStats ? playerStats.CurrentHP : 0;

        // 1) Reset to base
        if (playerStats)
        {
            playerStats.maxHP = _baseMaxHP;
            playerStats.baseDamage = _baseBaseDamage;
            playerStats.damageMultiplier = _baseDamageMultiplier;
            playerStats.moveSpeed = _baseMoveSpeed;
            playerStats.pickupRadius = _basePickupRadius;
        }
        if (autoShooter)
        {
            autoShooter.fireRate = _baseFireRate;
            autoShooter.bulletSpeed = _baseBulletSpeed;
            autoShooter.closestEnemyRange = _baseClosestEnemyRange;
            autoShooter.weaponDamageMultiplier = _baseWeaponDamageMultiplier;
            autoShooter.weaponDamageFlat = _baseWeaponDamageFlat;
            autoShooter.damage = _baseShooterLegacyDamage;
        }

        // Behaviours default off; will be toggled on by items
        if (pollenAura) { pollenAura.enabled = false; pollenAura.SetStacks(0); }
        if (explodingShots) { explodingShots.enabled = false; explodingShots.SetStacks(0); }

        // 2) Apply all stacks
        for (int i = 0; i < stacks.Count; i++)
        {
            var entry = stacks[i];
            if (!entry.item || entry.count <= 0) continue;

            for (int j = 0; j < entry.item.effects.Count; j++)
            {
                var eff = entry.item.effects[j];
                float val = eff.value * entry.count;

                switch (eff.type)
                {
                    // --- Stat mods ---
                    case ItemEffectType.DamageFlat:
                        if (playerStats) playerStats.baseDamage += Mathf.RoundToInt(val);
                        break;

                    case ItemEffectType.DamageMultiplierPercent:
                        if (playerStats) playerStats.damageMultiplier *= Mathf.Max(0.01f, 1f + val / 100f);
                        break;

                    case ItemEffectType.FireRateFlat:
                        if (autoShooter) autoShooter.fireRate += val;
                        break;

                    case ItemEffectType.BulletSpeedFlat:
                        if (autoShooter) autoShooter.bulletSpeed += val;
                        break;

                    case ItemEffectType.ClosestEnemyRangeFlat:
                        if (autoShooter) autoShooter.closestEnemyRange += val;
                        break;

                    case ItemEffectType.PickupRadiusFlat:
                        if (playerStats) playerStats.pickupRadius += val;
                        break;

                    case ItemEffectType.MoveSpeedFlat:
                        if (playerStats) playerStats.moveSpeed += val;
                        break;

                    case ItemEffectType.MaxHPFlat:
                        if (playerStats) playerStats.maxHP += Mathf.RoundToInt(val);
                        break;

                    // --- Behaviours ---
                    case ItemEffectType.Enable_PollenAura:
                        if (pollenAura)
                        {
                            pollenAura.enabled = true;
                            pollenAura.SetStacks(entry.count);
                        }
                        break;

                    case ItemEffectType.Enable_ExplodingShots:
                        if (explodingShots)
                        {
                            explodingShots.enabled = true;
                            explodingShots.SetStacks(entry.count);
                        }
                        break;

                    case ItemEffectType.Enable_RoyalGuardShields:
                        // TODO: future behaviour enable
                        break;
                }
            }
        }

        // 3) HP ratio-preserve + time-scaled heal (after maxHP is final)
        if (playerStats)
        {
            int newMax = Mathf.Max(1, playerStats.maxHP);

            // Preserve the same health ratio
            float ratio = (oldMax > 0) ? (float)oldHP / oldMax : 1f;
            int hpFromRatio = Mathf.Clamp(Mathf.RoundToInt(newMax * ratio), 0, newMax);
            int hpTarget = hpFromRatio;

            // If MaxHP increased, grant a small time-based bonus heal
            if (newMax > oldMax)
            {
                float minutes = (clock != null) ? (clock.ElapsedSeconds / 60f) : 0f;
                float bonusPct = Mathf.Clamp01(healBonusBase + healBonusPerMinute * minutes);
                bonusPct = Mathf.Min(bonusPct, healBonusMax);

                int maxIncrease = newMax - oldMax;
                int healBonus = Mathf.RoundToInt(maxIncrease * bonusPct);
                hpTarget = Mathf.Min(newMax, hpFromRatio + healBonus);
            }

            // Apply target HP without triggering hit effects
            playerStats.SetCurrentHPRaw(hpTarget, clampToMax: true, suppressFeedback: true);
        }

        OnInventoryChanged?.Invoke();
    }
}
