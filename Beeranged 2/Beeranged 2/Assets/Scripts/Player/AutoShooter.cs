using System.Collections;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    public enum AimMode
    {
        ClosestEnemy,
        MovementDirection,
        MouseDirection,
        MouseDirectionOnClick // ?? NEW MODE
    }

    

    [Header("Firing")]
    public float fireRate = 2f;
    public float bulletSpeed = 14f;
    public Transform muzzle;

    [Header("Aim Mode")]
    public AimMode aimMode = AimMode.MouseDirectionOnClick;

    public bool enableHoming;

    [Header("Closest Enemy Settings")]
    public float closestEnemyRange = 12f;
    public LayerMask enemyMask;
    public TargetScanner2D scanner;
    public bool showRangeGizmo = false;

    [Header("Movement Aim Settings")]
    public Rigidbody2D playerRb;
    public float moveAimDeadzone = 0.05f;
    [Header("Muzzle Offset")]
    public float muzzleYOffset = 0f;

    [Header("Multi-shot")]
    [Min(1)] public int projectileCount = 1;
    public float spreadDegrees = 0f;

    [Header("Damage")]
    public PlayerStats playerStats;
    public float weaponDamageMultiplier = 1f;
    public int weaponDamageFlat = 0;
    public int damage = 0;
    public int fallbackDamage = 5;


    private Vector2 _lastAimDir = Vector2.right;

    void Awake()
    {
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!playerRb) playerRb = GetComponent<Rigidbody2D>();
        if (!scanner) scanner = GetComponent<TargetScanner2D>();
        if (scanner != null)
        {
            if (aimMode == AimMode.ClosestEnemy)
            {
                enableHoming = true;
            }
            scanner.range = closestEnemyRange;
            scanner.enemyMask = enemyMask;
            scanner.ownerRb = playerRb;
        }
    }

    void Start()
    {
        if (aimMode != AimMode.MouseDirectionOnClick)
            StartCoroutine(FireLoop());
    }

    void Update()
    {
        if (aimMode == AimMode.MouseDirectionOnClick)
        {
            if (Input.GetMouseButtonDown(0)) // left click
            {
                TryShoot();
            }
        }
    }

    IEnumerator FireLoop()
    {
        while (true)
        {
            float delay = 1f / Mathf.Max(0.1f, fireRate);
            TryShoot();
            yield return new WaitForSeconds(delay);
        }
    }

    void TryShoot()
    {
        Vector3 spawnPos = muzzle ? muzzle.position : transform.position;
        spawnPos.y += muzzleYOffset;
        Vector2 fireDir;

        if (aimMode == AimMode.ClosestEnemy)
        {
            Transform t = scanner ? scanner.CurrentTarget : FindNearest(spawnPos);
            if (t != null)
            {
                fireDir = (Vector2)t.position - (Vector2)spawnPos;
                if (fireDir.sqrMagnitude > 0.0001f) _lastAimDir = fireDir.normalized;
            }
            else
            {
                Vector2 move = playerRb ? playerRb.velocity : Vector2.zero;
                if (move.magnitude > moveAimDeadzone) _lastAimDir = move.normalized;
                if (_lastAimDir.sqrMagnitude < 0.0001f) return;
                fireDir = _lastAimDir;
            }
        }
        else if (aimMode == AimMode.MouseDirection || aimMode == AimMode.MouseDirectionOnClick)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            fireDir = mouseWorld - transform.position;

            // ? Clamp to LEFT or RIGHT only
            fireDir = (fireDir.x < 0) ? Vector2.left : Vector2.right;

            _lastAimDir = fireDir;
        }
        else // MovementDirection
        {
            Vector2 move = playerRb ? playerRb.velocity : Vector2.zero;
            if (move.magnitude > moveAimDeadzone) _lastAimDir = move.normalized;
            if (_lastAimDir.sqrMagnitude < 0.0001f) return;
            fireDir = _lastAimDir;

            // ? Clamp to horizontal only
            fireDir = (fireDir.x < 0) ? Vector2.left : Vector2.right;
            _lastAimDir = fireDir;
        }

        // ? Flip sprite based on fireDir.x
        var player = GetComponent<PlayerController>();
        if (player != null)
        {
            player.SetAttacking(true);
            Invoke(nameof(ResetAttackState), 0.2f);

            // Flip based on shooting direction
            if (player.spriteRenderer != null)
            {
                bool movingLeft = (_lastAimDir.x < 0);
                if (player.invertFlip) movingLeft = !movingLeft;
                if (player.useSpriteFlip)
                {
                    player.spriteRenderer.flipX = movingLeft;
                }
                else if (player.spriteRoot != null)
                {
                    Vector3 s = player.spriteRoot.localScale;
                    s.x = Mathf.Abs(s.x) * (movingLeft ? -1f : 1f);
                    player.spriteRoot.localScale = s;
                }
            }
        }

        int baseDmg = playerStats ? playerStats.GetDamageOutput() : fallbackDamage;
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * Mathf.Max(0.1f, weaponDamageMultiplier)) + weaponDamageFlat + damage);

        FireBurst(spawnPos, fireDir.normalized, finalDmg);
    }


    void ResetAttackState()
    {
        var player = GetComponent<PlayerController>();
        if (player != null)
            player.SetAttacking(false);
    }

    void FireBurst(Vector3 spawnPos, Vector2 baseDir, int damage)
    {
       
        int count = Mathf.Max(1, projectileCount);
        float totalSpread = Mathf.Max(0f, spreadDegrees);
        float step = (count > 1) ? totalSpread / (count - 1) : 0f;
        float start = -totalSpread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = start + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;

            
           
        }
    }

    Transform FindNearest(Vector3 origin)
    {
        Collider2D nearest = null;
        float best = float.MaxValue;
        var hits = Physics2D.OverlapCircleAll(origin, closestEnemyRange, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            float d = (hits[i].transform.position - origin).sqrMagnitude;
            if (d < best) { best = d; nearest = hits[i]; }
        }
        return nearest ? nearest.transform : null;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showRangeGizmo) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, closestEnemyRange);
    }
#endif
}
