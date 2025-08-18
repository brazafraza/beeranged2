using UnityEngine;

/// <summary>
/// Computes a separation steer vector away from nearby enemies.
/// Does NOT modify velocity; Enemy reads Steer and blends it into its own movement.
/// </summary>
[DefaultExecutionOrder(-50)]
public class EnemySeperation2D : MonoBehaviour
{
    [Header("Who to repel")]
    public LayerMask enemyMask;          // ONLY the Enemy layer

    [Header("Separation")]
    public float radius = 1.0f;          // neighbor radius
    public float strength = 1.0f;        // base steer magnitude
    public float tickInterval = 0.1f;    // compute every X seconds
    public int bufferSize = 16;

    public Vector2 Steer { get; private set; }  // smoothed outward steer

    Collider2D[] _buf;
    float _timer;

    void Awake()
    {
        _buf = new Collider2D[Mathf.Max(8, bufferSize)];
    }

    void FixedUpdate()
    {
        _timer += Time.fixedDeltaTime;
        if (_timer < tickInterval) return;
        _timer = 0f;

        Vector2 self = (Vector2)transform.position;
        Vector2 steer = Vector2.zero;
        int count = 0;

        int n = Physics2D.OverlapCircleNonAlloc(self, radius, _buf, enemyMask);
        for (int i = 0; i < n; i++)
        {
            var c = _buf[i];
            if (!c || c.transform == transform) continue; // skip self

            Vector2 to = self - (Vector2)c.transform.position;
            float d = to.magnitude;
            if (d <= 0.0001f || d > radius) continue;

            float push = (radius - d) / radius;           // stronger when closer
            steer += to / Mathf.Max(0.0001f, d) * push;   // unit away * push
            count++;
        }

        if (count > 0) steer /= count;

        // scale to desired magnitude but keep small when empty
        if (steer.sqrMagnitude > 1e-6f)
            steer = steer.normalized * strength;

        // light smoothing to avoid jitter
        Steer = Vector2.Lerp(Steer, steer, 0.5f);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}