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

    [Header("Separation Blend")]

    public float separationWeight = 1.0f; // how much to bend by separation
    public float maxSpeed = 3f;          // desired chase speed
    public float accel = 12f;            // how fast we lerp to desired

    private int _hp;

    void OnEnable()
    {
        _hp = maxHP;
      

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

        // Base chase direction
        Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        Vector2 desired = toPlayer * maxSpeed;

        // Clamp to max speed and ease in (feels smoother, avoids jitter)
        desired = Vector2.ClampMagnitude(desired, maxSpeed);
        rb.velocity = Vector2.Lerp(rb.velocity, desired, Mathf.Clamp01(accel * Time.fixedDeltaTime));
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

          
        }
    }
}
