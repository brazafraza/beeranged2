using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public GameObject player;
    public Rigidbody2D rb;
    public float playerSpeed = 5f;

    private float horizontal;
    private float vertical;

    private Vector2 input;

    [Header("Keybinds")]
    public KeyCode upKey = KeyCode.W;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode downKey = KeyCode.S;
    public KeyCode rightKey = KeyCode.D;

    private void Start()
    {
        player = gameObject;
        rb = player.GetComponent<Rigidbody2D>();

      
    }

    private void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        input.Normalize();

        if (Input.GetKeyDown(upKey) || Input.GetKeyDown(leftKey) || Input.GetKeyDown(rightKey) || Input.GetKeyDown(downKey))
        {
         
           // Debug.Log("PlayerMovement: Player is moving!");
        }


    }

    private void FixedUpdate()
    {
        rb.velocity = input * playerSpeed;
    }

    

}
