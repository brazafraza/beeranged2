using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Inventory Binding")]
    public InventorySystem inventory;
    public bool autoBindInventory = true;

    [Header("Active Abilities (runtime)")]
    public PrimaryAbilitySO activePrimaryAbility;
    public SecondaryAbilitySO activeSecondaryAbility;


    [Header("Projectile - Primary")]
    [Tooltip("Projectile prefab for the PRIMARY attack (Left Click).")]
    public GameObject projectilePrefab;
    public Transform muzzle;                 // optional; if null uses this.transform
    public Vector2 spawnOffset = Vector2.zero;

    [Header("Primary Firing")]
    [Tooltip("Speed of the primary projectile.")]
    public float projectileSpeed = 16f;      // units per second
    [Tooltip("Damage dealt by the primary projectile.")]
    public int projectileDamage = 5;
    public bool useFacingFromSprite = true;  // left/right decided by SpriteRenderer.flipX (or localScale.x)
    public SpriteRenderer spriteToRead;      // optional; auto-found if null

    [Header("Primary Orientation")]
    [Tooltip("Extra Z rotation applied to the spawned primary projectile.\n-90 is common when the projectile art faces 'up' by default.")]
    public float rotationOffsetDeg = -90f;

    [Header("Primary Input")]
    [Tooltip("Mouse button index for PRIMARY attack. 0 = Left Click.")]
    public int mouseButton = 0;              // 0 = left click
    public bool holdToFire = false;          // set true to auto-fire while held

    [Header("Primary Cooldown")]
    [Tooltip("Minimum time between primary shots (seconds).")]
    public float fireCooldown = 0.25f;

    [Header("Secondary Attack")]
    [Tooltip("Projectile prefab for the SECONDARY attack (Right Click). If null, falls back to primary projectile.")]
    public GameObject secondaryProjectilePrefab;
    [Tooltip("Speed of the secondary projectile.")]
    public float secondaryProjectileSpeed = 12f;
    [Tooltip("Damage dealt by the secondary projectile.")]
    public int secondaryProjectileDamage = 10;
    [Tooltip("Cooldown between secondary shots (seconds).")]
    public float secondaryCooldown = 1.0f;
    [Tooltip("Mouse button index for SECONDARY attack. 1 = Right Click.")]
    public int secondaryMouseButton = 1;       // 1 = right click
    [Tooltip("Extra Z rotation applied to the spawned secondary projectile.\nDefaults to same as primary.")]
    public float secondaryRotationOffsetDeg = -90f;

    [Header("Defaults (when no item equipped / not in 3-slot mode)")]
    public GameObject defaultPrimaryProjectile;
    public GameObject defaultSecondaryProjectile;

    private float _primaryCooldownTimer = 0f;
    private float _secondaryCooldownTimer = 0f;

    void Awake()
    {
        if (!spriteToRead)
            spriteToRead = GetComponentInChildren<SpriteRenderer>();

        // Capture defaults if not set
        ////if (!defaultPrimaryProjectile && projectilePrefab)
        ////    defaultPrimaryProjectile = projectilePrefab;
        ////if (!defaultSecondaryProjectile && secondaryProjectilePrefab)
        ////    defaultSecondaryProjectile = secondaryProjectilePrefab;

        if (autoBindInventory && !inventory)
            inventory = FindAnyObjectByType<InventorySystem>();
    }

    void OnEnable()
    {
        if (inventory)
            inventory.OnChanged += RefreshAbilitiesFromInventory;

        RefreshAbilitiesFromInventory();
    }

    void OnDisable()
    {
        if (inventory)
            inventory.OnChanged -= RefreshAbilitiesFromInventory;
    }

    void Update()
    {
        // tick cooldowns
        if (_primaryCooldownTimer > 0f)
            _primaryCooldownTimer -= Time.deltaTime;
        if (_secondaryCooldownTimer > 0f)
            _secondaryCooldownTimer -= Time.deltaTime;

        // ---------- PRIMARY (Left Click) ----------
        bool wantsPrimary = holdToFire
            ? Input.GetMouseButton(mouseButton)
            : Input.GetMouseButtonDown(mouseButton);

        if (wantsPrimary && _primaryCooldownTimer <= 0f)
        {
            FirePrimary();
            _primaryCooldownTimer = Mathf.Max(0f, fireCooldown);
        }

        // ---------- SECONDARY (Right Click) ----------
        bool wantsSecondary = Input.GetMouseButtonDown(secondaryMouseButton);

        if (wantsSecondary && _secondaryCooldownTimer <= 0f)
        {
            FireSecondary();
            _secondaryCooldownTimer = Mathf.Max(0f, secondaryCooldown);
        }
    }

    // ===== INV BINDING: Use slot 0 & 1 when in 3-slot mode =====
    void RefreshAbilitiesFromInventory()
    {
        // No inventory or not using 3-slot mode? Don't bind abilities
        if (inventory == null || inventory.gameplayMode != GameplayMode.ThreeAbilitySystem)
        {
            activePrimaryAbility = null;
            activeSecondaryAbility = null;
            return;
        }

        ItemSO primaryItem = inventory.GetActiveItemAt(0); // PRIMARY slot
        ItemSO secondaryItem = inventory.GetActiveItemAt(1); // SECONDARY slot

        activePrimaryAbility = primaryItem ? primaryItem.primaryAbility : null;
        activeSecondaryAbility = secondaryItem ? secondaryItem.secondaryAbility : null;
    }


    // ===== PRIMARY ATTACK =====
    void FirePrimary()
    {
        if (activePrimaryAbility != null)
        {
            activePrimaryAbility.UsePrimary(this);
        }
        else
        {
            // Fallback to default select attack / class - should i make this a seperate item?
        }

    }

    // ===== SECONDARY ATTACK =====
    void FireSecondary()
    {
        if (activeSecondaryAbility != null)
        {
            activeSecondaryAbility.UseSecondary(this);
        }
        else
        {
            // Fallback to default select attack / class - should i make this a seperate item?
            GameObject prefab = secondaryProjectilePrefab ? secondaryProjectilePrefab : projectilePrefab;
            if (!prefab) return;

            SpawnProjectile(
                prefab: prefab,
                speed: secondaryProjectileSpeed,
                damage: secondaryProjectileDamage,
                rotationOffset: secondaryRotationOffsetDeg);



        }

        // ===== SHARED SPAWN LOGIC =====
        void SpawnProjectile(GameObject prefab, float speed, int damage, float rotationOffset)
        {
            // Spawn position
            Vector3 spawnPos = (muzzle ? muzzle.position : transform.position) + (Vector3)spawnOffset;

            // Fixed horizontal direction
            int dirSign = 1; // +X by default
            if (useFacingFromSprite)
            {
                if (spriteToRead) dirSign = spriteToRead.flipX ? -1 : 1;
                else dirSign = transform.localScale.x < 0 ? -1 : 1;
            }
            Vector2 dir = (dirSign > 0) ? Vector2.right : Vector2.left;

            // Rotation: 0° for right, 180° for left, then apply offset (e.g. -90°)
            float baseAngle = (dirSign > 0) ? 0f : 180f;
            Quaternion rot = Quaternion.Euler(0f, 0f, baseAngle + rotationOffset);

            // Spawn
            GameObject go = Instantiate(prefab, spawnPos, rot);

            // Velocity via Rigidbody2D
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb) rb.velocity = dir * speed;

            // Try to set damage on any component with 'public int damage' or 'void SetDamage(int)'
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var t = c.GetType();

                var f = t.GetField("damage");
                if (f != null && f.FieldType == typeof(int))
                {
                    f.SetValue(c, damage);
                    break;
                }

                var m = t.GetMethod("SetDamage", new System.Type[] { typeof(int) });
                if (m != null)
                {
                    m.Invoke(c, new object[] { damage });
                    break;
                }
            }

            // Optional: flip projectile sprite visually
            var projSR = go.GetComponentInChildren<SpriteRenderer>();
            if (projSR != null) projSR.flipX = (dirSign < 0);
        }
    }
}
