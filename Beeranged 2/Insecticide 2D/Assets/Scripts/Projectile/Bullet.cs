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
    private float _t;

    public void Launch(
        Vector2 dir, float speed, int damage,
        bool enableHoming = false, Transform target = null, float homingStrengthOverride = -1f)
    {
        _damage = damage;
        _t = 0f;

        Vector2 n = (dir.sqrMagnitude > 0.000001f) ? dir.normalized : Vector2.right;

        if (rb != null)
            rb.velocity = n * speed;

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
        if (_t >= life)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
       

        // Fallback: specific Enemy class
        Enemy e = other.GetComponent<Enemy>();
        if (e == null) e = other.GetComponentInParent<Enemy>();
        if (e != null)
        {
            e.Hit(_damage);
            OnBulletHit?.Invoke(transform.position, _damage);
            HandlePierceOrDestroy();
        }
    }

    void HandlePierceOrDestroy()
    {
        if (pierce > 0)
        {
            pierce--;
            return;
        }

        Destroy(gameObject);
    }

    void OnDisable()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;
    }
}
