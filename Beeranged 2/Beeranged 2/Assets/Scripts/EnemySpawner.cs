using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public int spawnCount = 3;
    private BoxCollider2D spawnZone;
    public GameObject enemyPrefab;

    public float spawnDelay = 0.01f;


    void Start()
    {
        spawnZone = GetComponent<BoxCollider2D>();
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 spawnPos = GetRandomSpawnPoint();
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            yield return new WaitForSeconds(spawnDelay);
        }
    }


    Vector2 GetRandomSpawnPoint()
    {
        Bounds bounds = spawnZone.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector2(x, y);
    }
}
