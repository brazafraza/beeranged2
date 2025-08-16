using UnityEngine;

public class EnemyContactDamage : MonoBehaviour
{
    public int contactDamage = 5;
    public float tickInterval = 0.5f;   // damage every X seconds while touching

    // simple single-player cooldown
    private float _nextTickTime = 0f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < _nextTickTime) return;

        // Require the PlayerStats component on the thing we hit
        PlayerStats ps = other.GetComponent<PlayerStats>();
        if (ps != null && ps.IsAlive)
        {
            ps.TakeDamage(contactDamage);
            _nextTickTime = Time.time + tickInterval;
        }
    }
}
