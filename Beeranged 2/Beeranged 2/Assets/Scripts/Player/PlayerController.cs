using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public Rigidbody2D rb;
    private PlayerStats stats;

    [Header("Ground & Wall Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer; // Only ground (no walls)
    public LayerMask wallLayer;

    private bool isGrounded;
    private bool wasGroundedLastFrame;

    [Header("Jump Recovery")]
    public Transform lastJumpPoint;
    public Transform fallbackPoint;
    public float jumpOffsetX = 0.6f;

    private int _facingDirection = 1; // 1 = right, -1 = left

    [Header("Visual Flip")]
    public Transform spriteRoot;
    public SpriteRenderer spriteRenderer;
    public bool useSpriteFlip = true;
    public bool invertFlip = false;

    [Header("Sprites")]
    public Sprite idleSprite;
    public Sprite attackSprite;

    [Header("Walk Animation")]
    public List<Sprite> walkFrames = new List<Sprite>();
    public float walkAnimFPS = 6f;
    private float walkAnimTimer;
    private int currentWalkFrame;

    [Header("Jump Animation")]
    public List<Sprite> jumpFrames = new List<Sprite>();
    public float jumpAnimFPS = 6f;
    private float jumpAnimTimer;
    private int currentJumpFrame;
    private bool isJumpAnimating;

    private float _initialSpriteScaleX = 1f;
    private float moveInput;
    private bool isAttacking;

    private SpriteRenderer sr;
    private GameManager gm;

    void Awake()
    {
        stats = GetComponent<PlayerStats>(); // NEW

        if (stats != null)
        {
            SyncStatsFromPlayerStats(); // Get initial values
        }

        gm = FindAnyObjectByType<GameManager>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        sr = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

        if (spriteRoot != null)
        {
            float x = spriteRoot.localScale.x;
            _initialSpriteScaleX = Mathf.Approximately(x, 0f) ? 1f : Mathf.Abs(x);
        }

        // Create lastJumpPoint if missing
        if (lastJumpPoint == null)
        {
            GameObject jumpPointObj = new GameObject("LastJumpPoint");
            lastJumpPoint = jumpPointObj.transform;

            if (gm != null)
                gm.tpDest = jumpPointObj.transform;

            lastJumpPoint.position = fallbackPoint != null
                ? fallbackPoint.position
                : transform.position;
        }
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // Wall-stick fix
        if ((IsTouchingWall(Vector2.left) && moveInput < 0) || (IsTouchingWall(Vector2.right) && moveInput > 0))
        {
            moveInput = 0f;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        bool justLeftGround = wasGroundedLastFrame && !isGrounded;
        bool justLanded = !wasGroundedLastFrame && isGrounded;

        if (justLeftGround)
        {
            StartJumpAnimation();

            // Set facing direction at the time of jump
            if (moveInput != 0)
                _facingDirection = moveInput > 0 ? -1 : 1;

            if (lastJumpPoint != null)
            {
                Vector2 offset = new Vector2(jumpOffsetX * _facingDirection, 0f);
                lastJumpPoint.position = (Vector2)transform.position + offset;
                Debug.Log("Set LastJumpPoint with offset: " + lastJumpPoint.position);
            }
        }

        if (justLanded)
        {
            StopJumpAnimation();
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        HandleVisualFlip();
        HandleSpriteChange();

        wasGroundedLastFrame = isGrounded;
    }

    void FixedUpdate()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    public void SyncStatsFromPlayerStats()
    {
        if (stats == null) return;

        moveSpeed = stats.moveSpeed;
        jumpForce = stats.jumpForce;
    }

    void HandleVisualFlip()
    {
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            bool movingLeft = moveInput < 0f;
            if (invertFlip) movingLeft = !movingLeft;

            if (useSpriteFlip && sr != null)
            {
                sr.flipX = movingLeft;
            }
            else if (spriteRoot != null)
            {
                var s = spriteRoot.localScale;
                s.x = _initialSpriteScaleX * (movingLeft ? -1f : 1f);
                spriteRoot.localScale = s;
            }
        }
    }

    void HandleSpriteChange()
    {
        if (isAttacking && attackSprite != null)
        {
            sr.sprite = attackSprite;
            return;
        }

        if (isJumpAnimating && jumpFrames.Count > 0)
        {
            float frameDuration = 1f / Mathf.Max(1f, jumpAnimFPS);

            if (currentJumpFrame < jumpFrames.Count - 1)
            {
                jumpAnimTimer += Time.deltaTime;

                if (jumpAnimTimer >= frameDuration)
                {
                    jumpAnimTimer = 0f;
                    currentJumpFrame++;
                    if (currentJumpFrame >= jumpFrames.Count)
                        currentJumpFrame = jumpFrames.Count - 1;
                }
            }

            sr.sprite = jumpFrames[currentJumpFrame];
            return;
        }

        if (Mathf.Abs(moveInput) > 0.01f && walkFrames.Count > 0)
        {
            walkAnimTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, walkAnimFPS);

            if (walkAnimTimer >= frameDuration)
            {
                walkAnimTimer = 0f;
                currentWalkFrame = (currentWalkFrame + 1) % walkFrames.Count;
            }

            sr.sprite = walkFrames[currentWalkFrame];
        }
        else
        {
            walkAnimTimer = 0f;
            currentWalkFrame = 0;
            if (idleSprite != null)
                sr.sprite = idleSprite;
        }
    }

    void StartJumpAnimation()
    {
        isJumpAnimating = true;
        currentJumpFrame = 0;
        jumpAnimTimer = 0f;
    }

    void StopJumpAnimation()
    {
        isJumpAnimating = false;
    }

    public void SetAttacking(bool value)
    {
        isAttacking = value;
    }

    bool IsTouchingWall(Vector2 dir)
    {
        return Physics2D.Raycast(transform.position, dir, 0.1f, wallLayer);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * 0.1f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 0.1f);

        if (lastJumpPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(lastJumpPoint.position, 0.2f);
        }
    }
}
