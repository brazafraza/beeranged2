using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public Rigidbody2D rb;
    public VirtualJoystick joystick;

    [Header("Visual Flip (no root scaling)")]
    public Transform spriteRoot;             // child that contains visuals (if not using SpriteRenderer.flipX)
    public SpriteRenderer spriteRenderer;    // preferred: flipX only mirrors the sprite, not the transform
    public bool useSpriteFlip = true;        // true = use SpriteRenderer.flipX, false = flip spriteRoot.localScale.x
    public bool invertFlip = false;          // tick if your art faces left by default (swaps left/right)
    public float flipDeadzone = 0.05f;       // ignore tiny jitters

    [Header("Aiming (scan nearest)")]
    public float aimScanInterval = 0.1f;
    public float aimRange = 12f;
    public LayerMask enemyMask;

    private Vector2 _move;
    private Vector2 _aimDir = Vector2.right;
    private float _initialSpriteScaleX = 1f;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody2D>();

        // Cache initial X scale magnitude for spriteRoot flipping
        if (spriteRoot != null)
        {
            float x = spriteRoot.localScale.x;
            _initialSpriteScaleX = Mathf.Approximately(x, 0f) ? 1f : Mathf.Abs(x);
        }
    }

    void Start()
    {
        StartCoroutine(AimScanLoop());
    }

    void Update()
    {
        Vector2 j = joystick != null ? joystick.Read() : Vector2.zero;
        _move = j.normalized * moveSpeed;
    }

    void FixedUpdate()
    {
        rb.velocity = _move;

        // Optional: rotate the body toward aim
        if (_aimDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(_aimDir.y, _aimDir.x) * Mathf.Rad2Deg;
            rb.MoveRotation(angle);
        }
    }

    void LateUpdate()
    {
        // Decide facing from velocity first; fallback to input X if needed
        float vx = rb != null ? rb.velocity.x : 0f;
        if (Mathf.Abs(vx) < flipDeadzone && _move.sqrMagnitude > 0.0001f)
            vx = _move.x;

        if (Mathf.Abs(vx) < flipDeadzone)
            return; // no strong intent; keep current facing

        bool movingLeft = vx < 0f;
        if (invertFlip) movingLeft = !movingLeft;

        if (useSpriteFlip && spriteRenderer != null)
        {
            // Cleanest: flips only the rendered pixels; world-space UI won’t mirror
            spriteRenderer.flipX = movingLeft;
        }
        else if (spriteRoot != null)
        {
            // Flip only the visuals child scale; root stays (1,1,1)
            var s = spriteRoot.localScale;
            s.x = _initialSpriteScaleX * (movingLeft ? -1f : 1f);
            spriteRoot.localScale = s;
        }
        // If neither spriteRenderer nor spriteRoot is assigned, nothing to flip (safe no-op).
    }

    private IEnumerator AimScanLoop()
    {
        WaitForSeconds wait = new WaitForSeconds(aimScanInterval);
        while (true)
        {
            Collider2D nearest = null;
            float best = 99999f;
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aimRange, enemyMask);
            for (int i = 0; i < hits.Length; i++)
            {
                float d = (hits[i].transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = hits[i]; }
            }
            if (nearest != null)
            {
                Vector2 dir = (nearest.transform.position - transform.position);
                if (dir.sqrMagnitude > 0.0001f) _aimDir = dir.normalized;
            }
            yield return wait;
        }
    }
}
