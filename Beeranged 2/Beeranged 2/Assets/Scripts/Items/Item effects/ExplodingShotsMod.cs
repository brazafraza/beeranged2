using UnityEngine;

public class ExplodingShotsMod : MonoBehaviour
{
    [Header("Damage & Radius")]
    public LayerMask enemyMask;
    public float baseRadius = 1.2f;
    public float radiusPerStack = 0.25f;
    public float damageMultiplier = 0.4f; // 40% of bullet damage per explosion, adjust to taste

    private int _stacks = 0;

    public void SetStacks(int stacks) => _stacks = Mathf.Max(0, stacks);

    void OnEnable()
    {
        Bullet.OnBulletHit += OnBulletHit;
    }

    void OnDisable()
    {
        Bullet.OnBulletHit -= OnBulletHit;
    }

    private void OnBulletHit(Vector2 pos, int bulletDamage)
    {
        if (!isActiveAndEnabled || _stacks <= 0) return;

        float radius = baseRadius + radiusPerStack * _stacks;
        int aoeDamage = Mathf.Max(1, Mathf.RoundToInt(bulletDamage * Mathf.Max(0f, damageMultiplier)));

        var hits = Physics2D.OverlapCircleAll(pos, radius, enemyMask);
        for (int i = 0; i < hits.Length; i++)
        {
            // Hit anything damageable
            IDamageable d;
            if (hits[i].TryGetComponent(out d))
            {
                d.TakeDamage(aoeDamage);
                continue;
            }
            // Fallback: your Enemy script
            Enemy e = hits[i].GetComponent<Enemy>();
            if (e == null) e = hits[i].GetComponentInParent<Enemy>();
            if (e != null) e.Hit(aoeDamage);
        }

        // TODO (optional): spawn pooled VFX here
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        float r = baseRadius + radiusPerStack * _stacks;
        Gizmos.DrawWireSphere(transform.position, r);
    }
#endif
}
