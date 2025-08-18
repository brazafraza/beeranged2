using System;
using System.Numerics;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation2D : MonoBehaviour
{
    [Header("Who to repel")]
    public LayerMask enemyMask;       // set to Enemy layer only

    [Header("Separation")]
    public float radius = 1.0f;       // neighbor radius
    public float steerWeight = 0.5f;  // how strongly we bend direction
    public float tickInterval = 0.1f; // compute every X seconds
    public int bufferSize = 16;

    private Rigidbody2D _rb;
    private Collider2D[] _buf;
    private float _t;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _buf = new Collider2D[Mathf.Max(8, bufferSize)];
    }

    void FixedUpdate()
    {
        _t += Time.fixedDeltaTime;
        if (_t < tickInterval) return;
        _t = 0f;

        int n = Physics2D.OverlapCircleNonAlloc(transform.position, radius, _buf, enemyMask);

        Vector2 self = _rb.position;
        Vector2 steer = Vector2.zero;
        int count = 0;

        for (int i = 0; i < n; i++)
        {
            var c = _buf[i];
            if (!c || c.attachedRigidbody == _rb) continue; // skip self

            Vector2 to = self - (Vector2)c.transform.position;
            float d = to.magnitude;
            if (d < 0.0001f || d > radius) continue;

            float push = (radius - d) / radius;            // stronger when closer
            steer += to / Mathf.Max(0.0001f, d) * push;    // unit away * push
            count++;
        }

        if (count == 0) return;

        steer /= count;                         // average
        if (steer.sqrMagnitude < 1e-6f) return;

        // Bend current velocity direction by steer, but KEEP the same speed
        Vector2 v = _rb.velocity;
        if (v.sqrMagnitude > 1e-4f)
        {
            float speed = v.magnitude;
            Vector2 newV = (v + steer * steerWeight).normalized * speed;
            _rb.velocity = newV;
        }
        else
        {
            // If we're basically stopped, a tiny nudge
            _rb.velocity += steer * (steerWeight * 0.5f);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}