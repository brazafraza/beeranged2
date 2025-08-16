using UnityEngine;

public class XPGem : MonoBehaviour
{
    public int xpValue = 1;

    [Header("Pooling (optional)")]
    public ObjectPool pool;
    public string poolKey; // e.g., "XP_S" / "XP_M" / "XP_L"

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        LevelSystem ls = FindObjectOfType<LevelSystem>();
        if (ls != null) ls.AddXP(xpValue);

        if (pool != null && !string.IsNullOrEmpty(poolKey))
            pool.Despawn(poolKey, gameObject);
        else
            Destroy(gameObject);
    }
}
