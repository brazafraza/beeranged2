using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PatrolEnemy2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public bool startMovingRight = false;

    [Header("Detection (raycasts)")]
    public Transform wallCheck;                 // small empty at the front
    public Transform ledgeCheck;                // small empty near front feet
    public float wallCheckDistance = 0.2f;
    public float ledgeCheckDistance = 0.25f;
    public LayerMask groundLayer;               // floors & walls (or split with wallLayer if you want)

    [Header("Colliders")]
    [Tooltip("Non-trigger collider used to collide with world (floors/walls). Will be set to ignore the Player at runtime.")]
    public Collider2D worldCollider;            // NOT trigger
    [Tooltip("Trigger collider used to damage the player by overlap.")]
    public Collider2D damageTrigger;            // IS trigger

    [Header("Player Filtering")]
    public string playerTag = "Player";         // optional (faster than scanning components)

    [Header("Visual Flip (optional)")]
    public SpriteRenderer spriteRenderer;
    public Transform spriteRoot;
    public bool useSpriteFlip = true;
    public bool invertFlip = false;
    private float _initialSpriteScaleX = 1f;

    [Header("Contact Damage")]
    public int contactDamage = 10;
    public float contactDamageCooldown = 0.5f;  // seconds between hits per player

    private Rigidbody2D _rb;
    private int _dir; // -1 left, +1 right
    private Dictionary<PlayerStats, float> _lastHitTime = new Dictionary<PlayerStats, float>();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (!spriteRenderer) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Auto-pick colliders if not wired
        if (!worldCollider || worldCollider.isTrigger)
        {
            foreach (var c in GetComponentsInChildren<Collider2D>())
            {
                if (!c.isTrigger) { worldCollider = c; break; }
            }
        }
        if (!damageTrigger || !damageTrigger.isTrigger)
        {
            foreach (var c in GetComponentsInChildren<Collider2D>())
            {
                if (c.isTrigger) { damageTrigger = c; break; }
            }
        }

        // Warn if setup is incomplete
        if (!worldCollider)
            Debug.LogWarning($"{name}: No non-trigger worldCollider found/assigned.");
        if (!damageTrigger || !damageTrigger.isTrigger)
            Debug.LogWarning($"{name}: No trigger damageTrigger found/assigned.");

        _dir = startMovingRight ? 1 : -1;

        if (spriteRoot)
        {
            float x = spriteRoot.localScale.x;
            _initialSpriteScaleX = Mathf.Approximately(x, 0f) ? 1f : Mathf.Abs(x);
        }
    }

    void Update()
    {
        if (ShouldTurnAround())
            TurnAround();

        ApplyFlip();
    }

    void FixedUpdate()
    {
        _rb.velocity = new Vector2(_dir * moveSpeed, _rb.velocity.y);
    }

    bool ShouldTurnAround()
    {
        // Wall check
        Vector2 origin = wallCheck ? (Vector2)wallCheck.position : (Vector2)transform.position;
        Vector2 forward = new Vector2(_dir, 0f);
        bool hitWall = Physics2D.Raycast(origin, forward, wallCheckDistance, groundLayer);

        // Ledge check (no ground ahead?)
        Vector2 ledgeOrigin = ledgeCheck ? (Vector2)ledgeCheck.position : (Vector2)transform.position + new Vector2(_dir * 0.2f, 0f);
        bool groundAhead = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundLayer);

        return hitWall || !groundAhead;
    }

    void TurnAround() => _dir = -_dir;

    void ApplyFlip()
    {
        bool left = _dir < 0;
        if (invertFlip) left = !left;

        if (useSpriteFlip && spriteRenderer)
        {
            spriteRenderer.flipX = left;
        }
        else if (spriteRoot)
        {
            var s = spriteRoot.localScale;
            s.x = _initialSpriteScaleX * (left ? -1f : 1f);
            spriteRoot.localScale = s;
        }
    }

    // ---------------- pass-through logic ----------------
    // If worldCollider collides with the Player, immediately disable collision for that pair
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!worldCollider) return;
        if (IsPlayerCollider(col.collider))
            IgnorePlayerForWorldCollider(col.collider);
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (!worldCollider) return;
        if (IsPlayerCollider(col.collider))
            IgnorePlayerForWorldCollider(col.collider);
    }

    void IgnorePlayerForWorldCollider(Collider2D playerCol)
    {
        // Ignore only worldCollider ? player collider; keep damageTrigger free to overlap
        Physics2D.IgnoreCollision(worldCollider, playerCol, true);
    }

    bool IsPlayerCollider(Collider2D c)
    {
        if (!c) return false;
        if (!string.IsNullOrEmpty(playerTag) && c.CompareTag(playerTag)) return true;
        // Fallback: check for PlayerStats on the object or its parents
        return c.GetComponentInParent<PlayerStats>() != null;
    }

    // ---------------- damage via trigger overlap ----------------
    void OnTriggerEnter2D(Collider2D other) { TryDamage(other); }
    void OnTriggerStay2D(Collider2D other) { TryDamage(other); }

    void TryDamage(Collider2D other)
    {
        // Only damage if this event comes from the damageTrigger (avoid other triggers on the enemy)
        if (damageTrigger && other != null)
        {
            // Note: OnTrigger... is raised on the MonoBehaviour regardless of which collider,
            // so we sanity-check by ensuring 'damageTrigger' actually overlaps 'other' (bounds check is cheap).
            if (!damageTrigger.bounds.Intersects(other.bounds))
                return;
        }

        var ps = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        if (!ps) return;

        float now = Time.time;
        if (_lastHitTime.TryGetValue(ps, out float last) && (now - last < contactDamageCooldown))
            return;

        ps.TakeDamage(contactDamage);
        _lastHitTime[ps] = now;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        int dir = startMovingRight ? 1 : -1;
        Vector3 origin = wallCheck ? wallCheck.position : transform.position;
        Vector3 forward = new Vector3(dir * wallCheckDistance, 0f, 0f);
        Gizmos.color = Color.red; Gizmos.DrawLine(origin, origin + forward);

        Vector3 ledgeOrigin = ledgeCheck ? ledgeCheck.position : transform.position + new Vector3(dir * 0.2f, 0f, 0f);
        Gizmos.color = Color.yellow; Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeCheckDistance);
    }
#endif
}
