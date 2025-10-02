using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform muzzle;                 // optional; if null uses this.transform
    public Vector2 spawnOffset = Vector2.zero;

    [Header("Firing")]
    public float projectileSpeed = 16f;      // units per second
    public int projectileDamage = 5;
    public bool useFacingFromSprite = true;  // left/right decided by SpriteRenderer.flipX (or localScale.x)
    public SpriteRenderer spriteToRead;      // optional; auto-found if null

    [Header("Orientation")]
    [Tooltip("Extra Z rotation applied to the spawned projectile.\n-90 is common when the projectile art faces 'up' by default.")]
    public float rotationOffsetDeg = -90f;

    [Header("Input")]
    public int mouseButton = 0;              // 0 = left click
    public bool holdToFire = false;          // set true to auto-fire while held

    [Header("Cooldown")]
    [Tooltip("Minimum time between shots (seconds).")]
    public float fireCooldown = 0.25f;

    private float _cooldownTimer = 0f;

    void Awake()
    {
        if (!spriteToRead)
            spriteToRead = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // tick cooldown
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        bool wantsToShoot = holdToFire
            ? Input.GetMouseButton(mouseButton)
            : Input.GetMouseButtonDown(mouseButton);

        if (wantsToShoot && _cooldownTimer <= 0f)
        {
            FireOnce();
            _cooldownTimer = Mathf.Max(0f, fireCooldown);
        }
    }

    void FireOnce()
    {
        if (!projectilePrefab) return;

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
        Quaternion rot = Quaternion.Euler(0f, 0f, baseAngle + rotationOffsetDeg);

        // Spawn
        GameObject go = Instantiate(projectilePrefab, spawnPos, rot);

        // Velocity via Rigidbody2D
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = dir * projectileSpeed;

        // Try to set damage on any component with 'public int damage' or 'void SetDamage(int)'
        foreach (var c in go.GetComponents<Component>())
        {
            if (c == null) continue;
            var t = c.GetType();

            var f = t.GetField("damage");
            if (f != null && f.FieldType == typeof(int))
            {
                f.SetValue(c, projectileDamage);
                break;
            }

            var m = t.GetMethod("SetDamage", new System.Type[] { typeof(int) });
            if (m != null)
            {
                m.Invoke(c, new object[] { projectileDamage });
                break;
            }
        }

        // Optional: flip projectile sprite visually
        var projSR = go.GetComponentInChildren<SpriteRenderer>();
        if (projSR != null) projSR.flipX = (dirSign < 0);
    }
}
