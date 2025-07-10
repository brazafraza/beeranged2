using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health;
    public float maxHealth = 100f;

    public bool takenDamage = false;
    GameObject enemy;

    public Color original;
    public Color damaged;
    public SpriteRenderer spriteRenderer;

    private void Start()
    {
        health = maxHealth;
        enemy = gameObject;
        spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        original = spriteRenderer.color;

    }
    private void Update()
    {
        if (health <= 0)
            Destroy(gameObject);

        if (takenDamage)
        {
           
            

            spriteRenderer.color = damaged;
            takenDamage = false;
            Invoke("ResetColour", 0.1f);
        }
    }

    public void TakeDamage(float damage)
    {
        health = health - damage;
        takenDamage = true;
        //Debug.Log("Enemy: Enemy Took Damage");
    }

    public void ResetColour()
    {
        spriteRenderer.color = original;
    }
}
