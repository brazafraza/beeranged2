using UnityEngine;

public class SplitAfterDistance : MonoBehaviour
{
    [Header("Split Settings")]
    [Tooltip("Prefab for the child bullets spawned when splitting.")]
    public GameObject childProjectilePrefab;

    [Tooltip("How far this projectile travels before splitting.")]
    public float splitDistance = 4f;

    [Tooltip("Angle in degrees for the side bullets.")]
    public float spreadAngleDeg = 15f;

    [Tooltip("Speed multiplier for child bullets relative to this projectile's speed.")]
    public float childSpeedMultiplier = 1f;

    [Tooltip("Damage dealt by child bullets.")]
    public int damage = 5;

    private Vector2 _startPos;
    private bool _hasSplit;
    private Rigidbody2D _rb;

    void Awake()
    {
        _startPos = transform.position;
        _rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (_hasSplit) return;

        float travelled = Vector2.Distance(_startPos, transform.position);
        if (travelled >= splitDistance)
        {
            DoSplit();
        }
    }

    void DoSplit()
    {
        _hasSplit = true;

        if (!childProjectilePrefab)
        {
            Destroy(gameObject);
            return;
        }

        // Base direction and speed from this projectile
        Vector2 baseDir;
        float baseSpeed = 10f;

        if (_rb != null && _rb.velocity.sqrMagnitude > 0.0001f)
        {
            baseDir = _rb.velocity.normalized;
            baseSpeed = _rb.velocity.magnitude;
        }
        else
        {
            baseDir = transform.up;
        }

        float childSpeed = baseSpeed * childSpeedMultiplier;

        // Center, left, right
        SpawnChild(baseDir, childSpeed);
        SpawnChild(Rotate(baseDir, spreadAngleDeg), childSpeed);
        SpawnChild(Rotate(baseDir, -spreadAngleDeg), childSpeed);

        // Remove the original
        Destroy(gameObject);
    }

    void SpawnChild(Vector2 dir, float speed)
    {
        GameObject child = Instantiate(childProjectilePrefab, transform.position, Quaternion.identity);

        var bullet = child.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Launch(dir, speed, damage);
        }
        else
        {
            var rb = child.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = dir.normalized * speed;
        }
    }

    Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
