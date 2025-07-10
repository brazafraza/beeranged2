using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [Header("Health")]
    public float health;
    public float maxHealth = 100f;

    [Header("Movement")]
    private GameObject player;
    public Transform playerPos;
    public float speed = 1;

    [Header("Attack")]
    public float damage = 1;
    public bool canAttack = true;
    public float attackCooldown = 2f;

    [Header("Color Refs")]
    public Color original;
    public Color damaged;
    public SpriteRenderer spriteRenderer;
    private bool takenDamage = false;

    //[Header("Refs")]
    private PlayerStats ps;
    private GameObject enemy;


    private void Start()
    {
        health = maxHealth;
        enemy = gameObject;
        spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        original = spriteRenderer.color;

        player = GameObject.Find("Bee");

        playerPos = player.transform;
        ps = FindObjectOfType<PlayerStats>();



    }
    private void Update()
    {

        playerPos = player.transform;

        if (health <= 0)
            Destroy(gameObject);

        if (playerPos != null )
        {
            //make enemy face and move towards player
            Vector2 direction = (playerPos.position - transform.position).normalized;
            float playerDir = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, playerDir - 180f);

            float z = transform.rotation.eulerAngles.z;

            if ( z > 90f && z < 270f)
                gameObject.transform.localScale = new Vector3(1f, -1f, 1f);                  
            else
                gameObject.transform.localScale = new Vector3(1f, 1f, 1f);

            


            Vector2 playerP = playerPos.position;
            Vector2 enemyPos = transform.position;
            Vector2 newPos = Vector2.MoveTowards(enemyPos, playerP, (speed * Time.deltaTime));
            transform.position = newPos;


            
        }

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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canAttack)
        {
            if (other.CompareTag("Player"))
            {
                ps.health = ps.health - damage;
                ps.UpdateUI();
                //Debug.Log($"Enemy: Enemy dealth {damage}to player!");
                StartCoroutine(AttackCooldown());
            }
        }    
            
    }

    IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    public void ResetColour()
    {
        spriteRenderer.color = original;
    }
}
