using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    public static event Action<Vector2, int> OnBulletHit;

    [Header("Refs")]
    public Rigidbody2D rb;

    [Header("Lifetime")]
    public float life = 2f;

    [Header("Orientation")]
    public float rotationOffsetDeg = 0f;           // -90 if sprite points RIGHT
    public bool faceVelocityContinuously = true;

    [Header("Pierce")]
    [Tooltip("How many EXTRA enemies this bullet can hit after the first.\n0 = destroy on first hit, 1 = hit two enemies total, etc.")]
    public int pierce = 0;

    private int _damage;
    private ObjectPool _pool;
    private string _key;
    private float _t;

    public void Launch(
        Vector2 dir, float speed, int damage, ObjectPool pool, string key,
        bool enableHoming = false, Transform target = null, float homingStrengthOverride = -1f)
    {
        _damage = damage;
        _pool = pool;
        _key = key;
        _t = 0f;

        Vector2 n = (dir.sqrMagnitude > 0.000001f) ? dir.normalized : Vector2.right;

        if (rb != null) rb.velocity = n * speed;

        transform.up = n;
        if (Mathf.Abs(rotationOffsetDeg) > 0.001f)
            transform.Rotate(0f, 0f, rotationOffsetDeg, Space.Self);

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (faceVelocityContinuously && rb != null && rb.velocity.sqrMagnitude > 0.0001f)
        {
            transform.up = rb.velocity.normalized;
            if (Mathf.Abs(rotationOffsetDeg) > 0.001f)
                transform.Rotate(0f, 0f, rotationOffsetDeg, Space.Self);
        }

        _t += Time.deltaTime;
        if (_t >= life) Despawn();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Try generic
        IDamageable dmg;
        if (other.TryGetComponent(out dmg))
        {
            dmg.TakeDamage(_damage);
            OnBulletHit?.Invoke(transform.position, _damage);
            HandlePierceOrDespawn();
            return;
        }

        // Fallback: your existing Enemy class
        Enemy e = other.GetComponent<Enemy>();
        if (e == null) e = other.GetComponentInParent<Enemy>();
        if (e != null)
        {
            e.Hit(_damage);
            OnBulletHit?.Invoke(transform.position, _damage);
            HandlePierceOrDespawn();
        }
    }

    void HandlePierceOrDespawn()
    {
        if (pierce > 0)
        {
            pierce--;
            return;
        }
        Despawn();
    }

    void Despawn()
    {
        if (_pool != null && !string.IsNullOrEmpty(_key))
            _pool.Despawn(_key, gameObject);
        else
            gameObject.SetActive(false);
    }

    void OnDisable()
    {
        if (rb != null) rb.velocity = Vector2.zero;
        // pierce remains as prefab default; set per-shot from the shooter if needed.
    }
}
