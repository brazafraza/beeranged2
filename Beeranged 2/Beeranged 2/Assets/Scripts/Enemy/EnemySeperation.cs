using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemySeparation2D : MonoBehaviour
{
    public LayerMask enemyMask;       // only the Enemy layer
    public float radius = 1.0f;       // separation radius
    public float strength = 2.0f;     // steering strength
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
        Vector2 steer = Vector2.zero;
        Vector2 self = _rb.position;

        for (int i = 0; i < n; i++)
        {
            var c = _buf[i];
            if (!c || c.attachedRigidbody == _rb) continue; // skip self

            Vector2 to = self - (Vector2)c.transform.position;
            float d = to.magnitude;
            if (d < 0.0001f) continue;

            float push = Mathf.Clamp01((radius - d) / radius);
            steer += to / d * push;
        }

        if (steer.sqrMagnitude > 0.0001f)
        {
            // light steering, don't exceed a small cap
            steer = steer.normalized * Mathf.Min(strength, steer.magnitude * strength);
            _rb.velocity += steer * Time.fixedDeltaTime * 10f; // scale to taste
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
