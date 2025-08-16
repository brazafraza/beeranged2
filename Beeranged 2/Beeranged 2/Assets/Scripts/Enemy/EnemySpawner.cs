using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        [Tooltip("ObjectPool key for this enemy prefab (must exist in ObjectPool).")]
        public string key = "Enemy";
        [Tooltip("Relative chance. 0 disables this entry.")]
        public float weight = 1f;
        [Tooltip("Batch size per spawn tick.")]
        public int minPerSpawn = 1;
        public int maxPerSpawn = 1;
        [Tooltip("Won’t spawn before this minute mark.")]
        public float startMinute = 0f;
    }

    [Header("Pool & Refs")]
    public ObjectPool pool;
    public Transform player;
    public Camera cam;
    [Tooltip("If true, tries to auto-assign Pool/Player/Camera on Awake.")]
    public bool autoAssignRefs = true;

    [Header("Spawn Table (multi-type)")]
    public List<SpawnEntry> spawnTable = new List<SpawnEntry>();

    [Header("Fallback (single type)")]
    [Tooltip("Used only if SpawnTable is empty.")]
    public string enemyKey = "Enemy";

    [Header("Global Spawn Rate (spawns per second vs minutes)")]
    public AnimationCurve spawnRateOverTime; // X=minutes, Y=spawns/sec

    [Header("Spawn ring (outside camera view)")]
    public float padding = 2f;        // how far outside the view to spawn
    public float ringThickness = 4f;  // ring width

    [Header("Bias")]
    [Range(0f, 1f)] public float frontBias = 0.6f; // 0 = anywhere, 1 = mostly in front
    public float frontAngle = 50f;                 // degrees around move direction

    [Header("Debug")]
    public bool showRingGizmo = false;
    public bool logWarnings = true;

    float _time;
    float _budget;
    bool _loggedZeroRate;
    static readonly List<SpawnEntry> _buf = new List<SpawnEntry>(16);

    void Awake()
    {
        if (autoAssignRefs)
        {
            if (!pool) pool = FindObjectOfType<ObjectPool>();
            if (!player)
            {
                var pgo = GameObject.FindGameObjectWithTag("Player");
                if (pgo) player = pgo.transform;
            }
            if (!cam) cam = Camera.main;
        }

        // Provide a sane default curve if empty (0.5 spawns/s flat)
        if (spawnRateOverTime == null || spawnRateOverTime.length == 0)
        {
            spawnRateOverTime = new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(5f, 0.8f),
                new Keyframe(10f, 1.2f),
                new Keyframe(15f, 1.5f)
            );
        }
    }

    void Update()
    {
        if (!IsReady()) return;

        _time += Time.deltaTime;
        float minutes = _time / 60f;

        float rate = Mathf.Max(0f, spawnRateOverTime.Evaluate(minutes));
        if (rate <= 0f)
        {
            if (logWarnings && !_loggedZeroRate)
            {
                Debug.LogWarning("[EnemySpawner] Spawn rate is 0 at current minute. Check your AnimationCurve.");
                _loggedZeroRate = true;
            }
            return;
        }
        _loggedZeroRate = false;

        _budget += rate * Time.deltaTime;
        while (_budget >= 1f)
        {
            SpawnGroup(minutes);
            _budget -= 1f;
        }
    }

    bool IsReady()
    {
        if (!enabled) return false;
        if (!pool) { if (logWarnings) Debug.LogWarning("[EnemySpawner] Pool is missing."); return false; }
        if (!player) { if (logWarnings) Debug.LogWarning("[EnemySpawner] Player is missing."); return false; }
        if (!cam) { if (logWarnings) Debug.LogWarning("[EnemySpawner] Camera is missing."); return false; }
        if (Time.timeScale <= 0f) return false;
        return true;
    }

    void SpawnGroup(float minutes)
    {
        // Use table if present, else fallback to single key
        if (spawnTable != null && spawnTable.Count > 0)
        {
            _buf.Clear();
            float total = 0f;
            for (int i = 0; i < spawnTable.Count; i++)
            {
                var e = spawnTable[i];
                if (e == null) continue;
                if (e.weight <= 0f) continue;
                if (minutes < e.startMinute) continue;
                _buf.Add(e);
                total += e.weight;
            }

            if (_buf.Count == 0)
            {
                if (logWarnings)
                    Debug.LogWarning("[EnemySpawner] No eligible spawn entries at current minute. Check startMinute/weights.");
                return;
            }

            float roll = Random.value * total;
            SpawnEntry pick = _buf[0];
            float acc = 0f;
            for (int i = 0; i < _buf.Count; i++)
            {
                acc += _buf[i].weight;
                if (roll <= acc) { pick = _buf[i]; break; }
            }

            int batch = Random.Range(Mathf.Max(1, pick.minPerSpawn), Mathf.Max(pick.minPerSpawn, pick.maxPerSpawn) + 1);
            for (int i = 0; i < batch; i++) SpawnOne(pick.key);
        }
        else
        {
            // fallback: single key
            SpawnOne(enemyKey);
        }
    }

    void SpawnOne(string key)
    {
        // camera ring
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float viewRadius = Mathf.Sqrt(halfW * halfW + halfH * halfH);
        float minR = viewRadius + padding;
        float maxR = minR + ringThickness;

        // direction (bias toward player velocity)
        Vector2 dir = Random.insideUnitCircle.normalized;
        var rb2d = player.GetComponent<Rigidbody2D>();
        if (rb2d && rb2d.velocity.sqrMagnitude > 0.01f && Random.value < frontBias)
        {
            Vector2 fwd = rb2d.velocity.normalized;
            float baseAngle = Mathf.Atan2(fwd.y, fwd.x) * Mathf.Rad2Deg;
            float delta = Random.Range(-frontAngle * 0.5f, frontAngle * 0.5f);
            float ang = (baseAngle + delta) * Mathf.Deg2Rad;
            dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        }

        float r = Random.Range(minR, maxR);
        Vector3 pos = (Vector2)player.position + dir * r;

        var go = pool.Spawn(key, pos, Quaternion.identity);
        if (!go && logWarnings)
        {
            Debug.LogWarning($"[EnemySpawner] Pool returned null for key '{key}'. Check ObjectPool entries & keys.");
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showRingGizmo || !cam || !player) return;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        float viewRadius = Mathf.Sqrt(halfW * halfW + halfH * halfH);
        float minR = viewRadius + padding;
        float maxR = minR + ringThickness;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        for (int i = 0; i < 32; i++)
        {
            float t = i / 32f;
            float ang = t * Mathf.PI * 2f;
            Vector3 a = (Vector2)player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * minR;
            Vector3 b = (Vector2)player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * maxR;
            Gizmos.DrawLine(a, b);
        }
    }

    [ContextMenu("Spawn Test (Fallback Key)")]
    void SpawnTest()
    {
        if (!IsReady()) return;
        SpawnOne(enemyKey);
    }
#endif
}
