using UnityEngine;

public class TargetScanner2D : MonoBehaviour
{
    [Header("Scan")]
    public float range = 12f;
    public LayerMask enemyMask;
    public float interval = 0.1f;

    [Header("Front Bias (optional)")]
    public Rigidbody2D ownerRb;
    [Range(0f, 1f)] public float frontBias = 0.0f; // 0 = off
    public float frontAngle = 60f;                 // degrees for bias cone

    [Header("Buffer")]
    [SerializeField] private int bufferSize = 64;

    public Transform CurrentTarget { get; private set; }

    private Collider2D[] _buf;
    private float _timer;

    void Awake()
    {
        if (ownerRb == null) ownerRb = GetComponent<Rigidbody2D>();
        _buf = new Collider2D[Mathf.Max(8, bufferSize)];
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < interval) return;
        _timer = 0f;

        Scan();
    }

    public void Scan()
    {
        int n = Physics2D.OverlapCircleNonAlloc(transform.position, range, _buf, enemyMask);
        Transform best = null;
        float bestScore = float.MaxValue;

        Vector2 fwd = (ownerRb != null && ownerRb.velocity.sqrMagnitude > 0.01f)
                        ? ownerRb.velocity.normalized
                        : Vector2.right;

        for (int i = 0; i < n; i++)
        {
            var c = _buf[i];
            if (c == null) continue;

            Vector2 to = (Vector2)c.transform.position - (Vector2)transform.position;
            float dist2 = to.sqrMagnitude;
            if (dist2 < 0.0001f) dist2 = 0.0001f;

            float score = dist2;

            if (frontBias > 0f && to.sqrMagnitude > 0.0001f)
            {
                float ang = Vector2.Angle(fwd, to);
                float norm = Mathf.Clamp01(ang / Mathf.Max(1f, frontAngle));
                score *= Mathf.Lerp(1f, 0.6f, (1f - norm) * frontBias); // prefer inside the cone
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = c.transform;
            }
        }

        CurrentTarget = best;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);

        if (ownerRb != null && frontBias > 0f)
        {
            Vector2 fwd = ownerRb.velocity.sqrMagnitude > 0.01f ? ownerRb.velocity.normalized : Vector2.right;
            Vector3 a = Quaternion.Euler(0, 0, +frontAngle * 0.5f) * (Vector3)fwd;
            Vector3 b = Quaternion.Euler(0, 0, -frontAngle * 0.5f) * (Vector3)fwd;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.15f);
            Gizmos.DrawLine(transform.position, transform.position + a * range);
            Gizmos.DrawLine(transform.position, transform.position + b * range);
        }
    }
#endif
}
