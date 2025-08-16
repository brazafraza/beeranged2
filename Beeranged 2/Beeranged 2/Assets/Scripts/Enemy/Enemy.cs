using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Core")]
    public Rigidbody2D rb;
    public float speed = 3f;
    public int maxHP = 10;

    [Header("Target")]
    private Transform _player;

    [Header("XP Drops")]
    public ObjectPool pool;             // assign your global pool
    public string xpSmallKey = "XP_S";
    public string xpMedKey = "XP_M";
    public string xpLargeKey = "XP_L";
    public int minDrops = 1;
    public int maxDrops = 3;
    [Range(0f, 1f)] public float smallWeight = 0.7f;
    [Range(0f, 1f)] public float medWeight = 0.25f;
    [Range(0f, 1f)] public float largeWeight = 0.05f;
    public float dropScatterRadius = 0.4f;

    [Header("UI")]
    public HealthBar2D healthBar;       // optional: child bar under enemy

    private int _hp;

    void OnEnable()
    {
        _hp = maxHP;
        pool = FindAnyObjectByType<ObjectPool>();

        if (healthBar) healthBar.SetInstant(1f);

        if (_player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
        }
    }

    void FixedUpdate()
    {
        if (_player == null) return;
        Vector2 dir = (_player.position - transform.position).normalized;
        rb.velocity = dir * speed;
    }

    public void Hit(int dmg)
    {
        if (dmg <= 0) return;

        _hp -= dmg;
        if (healthBar) healthBar.Set((float)_hp / Mathf.Max(1, maxHP));

        if (_hp <= 0)
        {
            DropXP();
            gameObject.SetActive(false);
        }
    }

    private void DropXP()
    {
        if (pool == null) return;

        int count = Random.Range(minDrops, maxDrops + 1);
        float wSum = Mathf.Max(0.0001f, smallWeight + medWeight + largeWeight);

        for (int i = 0; i < count; i++)
        {
            // roll size
            float r = Random.value * wSum;
            string key = xpSmallKey;
            if (r < smallWeight) key = xpSmallKey;
            else if (r < smallWeight + medWeight) key = xpMedKey;
            else key = xpLargeKey;

            // scatter spawn
            Vector2 off = Random.insideUnitCircle * dropScatterRadius;
            Vector3 pos = transform.position + (Vector3)off;

            pool.Spawn(key, pos, Quaternion.identity);
        }
    }
}
