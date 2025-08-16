using UnityEngine;

public class PollenAura : MonoBehaviour
{
    [Header("Tuning")]
    public LayerMask enemyMask;
    public float tickInterval = 0.25f;

    [Header("Scaling")]
    public float baseRadius = 1.6f;
    public float radiusPerStack = 0.3f;
    public float baseDPS = 4f;
    public float dpsPerStack = 2f;

    private int _stacks = 0;
    private float _timer;

    public void SetStacks(int stacks)
    {
        _stacks = Mathf.Max(0, stacks);
    }

    void OnEnable() { _timer = 0f; }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < tickInterval) return;
        _timer = 0f;

        float radius = baseRadius + radiusPerStack * _stacks;
        float dps = baseDPS + dpsPerStack * _stacks;
        int damagePerTick = Mathf.Max(1, Mathf.RoundToInt(dps * tickInterval));

        var hits = Physics2D.OverlapCircleAll(transform.position, radius, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            Enemy e = hits[i].GetComponent<Enemy>();
            if (e == null) e = hits[i].GetComponentInParent<Enemy>();
            if (e != null) e.Hit(damagePerTick);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0f, 0.3f);
        float r = baseRadius + radiusPerStack * _stacks;
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
