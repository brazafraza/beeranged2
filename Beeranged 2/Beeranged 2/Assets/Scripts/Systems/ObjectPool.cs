using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [System.Serializable]
    public class PoolItem
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 16;
    }

    public List<PoolItem> items = new List<PoolItem>();
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

    void Awake()
    {
        for (int i = 0; i < items.Count; i++)
        {
            PoolItem it = items[i];
            Queue<GameObject> q = new Queue<GameObject>();
            for (int j = 0; j < it.initialSize; j++)
            {
                GameObject obj = Instantiate(it.prefab, transform);
                obj.SetActive(false);
                q.Enqueue(obj);
            }
            pools[it.key] = q;
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot)
    {
        if (!pools.ContainsKey(key)) return null;
        Queue<GameObject> q = pools[key];
        GameObject obj;
        if (q.Count > 0)
        {
            obj = q.Dequeue();
        }
        else
        {
            // find item to instantiate
            PoolItem src = null;
            for (int i = 0; i < items.Count; i++)
                if (items[i].key == key) { src = items[i]; break; }
            if (src == null) return null;
            obj = Instantiate(src.prefab, transform);
        }
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        return obj;
    }

    public void Despawn(string key, GameObject obj)
    {
        if (!pools.ContainsKey(key)) { obj.SetActive(false); return; }
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[key].Enqueue(obj);
    }
}
