using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireForce = 1f;
    public float timeBetweenAttack = 2f;
    public bool readyToAttack = true;

    private void Start()
    {
        Attack(timeBetweenAttack);
    }

    void FixedUpdate()
    {

       

      
    }

    void Attack(float timeBetweenAttack)
    {
        if (readyToAttack)
        {
            
            readyToAttack = false;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            Quaternion mouseRot = Quaternion.LookRotation(Vector3.forward, mousePos);
           

            Vector3 direction = (mousePos - firePoint.position).normalized;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, mouseRot);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle + 30f);

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            rb.AddForce(direction * fireForce, ForceMode2D.Impulse);

           


            Invoke(nameof(ResetAttack), timeBetweenAttack);
            
        }
        
    }

    private void ResetAttack()
    {
        readyToAttack = true;
        Attack(timeBetweenAttack);
    }
}
