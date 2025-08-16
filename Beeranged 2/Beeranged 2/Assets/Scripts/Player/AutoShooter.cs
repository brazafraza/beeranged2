using System.Collections;
using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    public enum AimMode { ClosestEnemy, MovementDirection }

    [Header("Pooling")]
    public ObjectPool pool;
    public string bulletKey = "Bullet";

    [Header("Firing")]
    public float fireRate = 2f;
    public float bulletSpeed = 14f;
    public Transform muzzle;

    [Header("Aim Mode")]
    public AimMode aimMode = AimMode.ClosestEnemy;

    [Header("Closest Enemy Settings")]
    public float closestEnemyRange = 12f;
    public LayerMask enemyMask;
    public TargetScanner2D scanner;     // optional; if assigned, used instead of ad-hoc scans
    public bool showRangeGizmo = false;

    [Header("Movement Aim Settings")]
    public Rigidbody2D playerRb;
    public float moveAimDeadzone = 0.05f;

    [Header("Multi-shot")]
    [Min(1)] public int projectileCount = 1;
    public float spreadDegrees = 0f; // total spread across the volley

    [Header("Damage")]
    public PlayerStats playerStats;
    public float weaponDamageMultiplier = 1f;
    public int weaponDamageFlat = 0;
    public int damage = 0;                   // legacy/back-compat addition
    public int fallbackDamage = 5;

    private Vector2 _lastAimDir = Vector2.right;

    void Awake()
    {
        if (!playerStats) playerStats = GetComponent<PlayerStats>();
        if (!playerRb) playerRb = GetComponent<Rigidbody2D>();
        if (!scanner) scanner = GetComponent<TargetScanner2D>();
        if (scanner != null)
        {
            scanner.range = closestEnemyRange;
            scanner.enemyMask = enemyMask;
            scanner.ownerRb = playerRb;
        }
    }

    void Start() => StartCoroutine(FireLoop());

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

        // --- 1) Decide aim direction ---
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
        else // MovementDirection
        {
            Vector2 move = playerRb ? playerRb.velocity : Vector2.zero;
            if (move.magnitude > moveAimDeadzone) _lastAimDir = move.normalized;
            if (_lastAimDir.sqrMagnitude < 0.0001f) return;
            fireDir = _lastAimDir;
        }

        // --- 2) Damage calc ---
        int baseDmg = playerStats ? playerStats.GetDamageOutput() : fallbackDamage;
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt(baseDmg * Mathf.Max(0.1f, weaponDamageMultiplier)) + weaponDamageFlat + damage);

        // --- 3) Fire burst with spread ---
        FireBurst(spawnPos, fireDir.normalized, finalDmg);
    }

    void FireBurst(Vector3 spawnPos, Vector2 baseDir, int damage)
    {
        if (pool == null) return;

        int count = Mathf.Max(1, projectileCount);
        float totalSpread = Mathf.Max(0f, spreadDegrees);
        float step = (count > 1) ? totalSpread / (count - 1) : 0f;
        float start = -totalSpread * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float angle = start + step * i;
            Vector2 dir = Quaternion.Euler(0, 0, angle) * baseDir;

            GameObject obj = pool.Spawn(bulletKey, spawnPos, Quaternion.identity);
            if (!obj) continue;

            Bullet b = obj.GetComponent<Bullet>();
            if (b != null)
            {
                // If you add per-weapon pierce, set b.pierce here before Launch
                b.Launch(dir, bulletSpeed, damage, pool, bulletKey, false, null, -1f);
            }
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
