using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float expireTime = 3f;
    public float damage = 1f;

    private Enemy enemyScript;

    void Start()
    {
        Invoke("DestroySelf", expireTime);
    }
  
    private void DestroySelf()
    {
        Destroy(gameObject);
    }

    private void DamageEnemy(float damage) 
    {
        enemyScript.TakeDamage(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            GameObject enemy = other.gameObject;
            enemyScript = enemy.GetComponent<Enemy>();

            if(enemyScript!= null)
            {
                DamageEnemy(damage);
            }

        }
    }

}
