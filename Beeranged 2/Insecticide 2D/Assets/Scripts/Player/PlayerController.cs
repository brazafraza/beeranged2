using System.Collections.Generic;
using UnityEngine;
using static ItemSO;

public class PlayerController : MonoBehaviour
{
    [Header("Inventory Binding (Movement Slot)")]
    public InventorySystem inventory;
    public bool autoBindInventory = true;

    [Header("Movement Ability (runtime)")]
    public MovementAbilitySO activeMovementAbility;

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

    [Header("Movement Ability (Dash)")]
    public KeyCode movementAbilityKey = KeyCode.LeftShift;
    public float dashSpeed = 16f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.75f;
    public bool dashOverrideVerticalVelocity = true;

    [Header("Movement Ability Type")]
    [Tooltip("What movement ability is currently active (driven by slot 2 item in 3-item mode).")]
    public MovementAbilityType currentMovementAbility = MovementAbilityType.Dash;


    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private int dashDirection; // -1 left, +1 right

    [Header("Dash Defaults (for reset)")]
    public float defaultDashSpeed;
    public float defaultDashDuration;
    public float defaultDashCooldown;


    [Header("Pause")]
    public bool freezePhysicsOnPause = true;   // freeze body on any pause
    public bool keepMomentumAfterPause = false; // restore velocity on resume?
    private Vector2 _savedVel;
    private float _savedAngVel;
    private float _savedGravity;
    private bool _frozenForPause;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();

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

        // Capture dash defaults if not set
        if (defaultDashSpeed <= 0f) defaultDashSpeed = dashSpeed;
        if (defaultDashDuration <= 0f) defaultDashDuration = dashDuration;
        if (defaultDashCooldown <= 0f) defaultDashCooldown = dashCooldown;

        if (autoBindInventory && !inventory)
            inventory = FindAnyObjectByType<InventorySystem>();
    }

    void OnEnable()
    {
        PauseManager.OnPaused += ApplyPauseFreeze;
        PauseManager.OnResumed += RemovePauseFreeze;

        if (inventory == null && autoBindInventory)
            inventory = FindAnyObjectByType<InventorySystem>();

        if (inventory != null)
            inventory.OnChanged += RefreshMovementFromInventory;

        RefreshMovementFromInventory();
    }

    void OnDisable()
    {
        PauseManager.OnPaused -= ApplyPauseFreeze;
        PauseManager.OnResumed -= RemovePauseFreeze;

        if (inventory != null)
            inventory.OnChanged -= RefreshMovementFromInventory;
    }


    void Update()
    {
        if (PauseManager.IsPaused)
        {
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        // Update dash timers
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }

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
                _facingDirection = moveInput > 0 ? 1 : -1;

            if (lastJumpPoint != null)
            {
                Vector2 offset = new Vector2(jumpOffsetX * _facingDirection, 0f);
                lastJumpPoint.position = (Vector2)transform.position + offset;
            }
        }

        if (justLanded)
        {
            StopJumpAnimation();
        }

        if (Input.GetButtonDown("Jump") && isGrounded && !isDashing)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }

        // ---------- MOVEMENT ABILITY (generic) ----------
        if (Input.GetKeyDown(movementAbilityKey))
        {
            if (activeMovementAbility != null)
                activeMovementAbility.UseMovement(this);
           // else
                 // Optional default dash, or do nothing
        }



        HandleVisualFlip();
        HandleSpriteChange();

        wasGroundedLastFrame = isGrounded;
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            float vx = dashDirection * dashSpeed;
            float vy = dashOverrideVerticalVelocity ? 0f : rb.velocity.y;
            rb.velocity = new Vector2(vx, vy);
            return;
        }

        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }

    // ===== Inventory binding for movement ability (slot 2) =====
    void RefreshMovementFromInventory()
    {
        if (inventory == null || inventory.gameplayMode != GameplayMode.ThreeAbilitySystem)
        {
            activeMovementAbility = null;
            return;
        }

        ItemSO movementItem = inventory.GetActiveItemAt(2); // MOVEMENT slot
        activeMovementAbility = movementItem ? movementItem.movementAbility : null;
    }


    // ===== MOVEMENT ABILITY DISPATCH =====
    void UseMovementAbility()
    {
        switch (currentMovementAbility)
        {
            case MovementAbilityType.Dash:
                TryStartDash();
                break;

            case MovementAbilityType.BlinkForward:
                DoBlinkForward();
                break;

            case MovementAbilityType.BlinkUp:
                DoBlinkUp();
                break;

            case MovementAbilityType.Hover:
                ToggleHover();
                break;

            case MovementAbilityType.None:
            default:
                // // Fallback to default movement abil / class - should i make this a seperate item?
                break;
        }
    }

    // --- Dash (existing behaviour, just refactored) ---
    public void StartDash(float speed, float duration, float cooldown)
    {
        dashSpeed = speed;
        dashDuration = duration;
        dashCooldown = cooldown;

        if (dashCooldownTimer > 0f && dashCooldownTimer > dashCooldown)
            dashCooldownTimer = dashCooldown;

        TryStartDash();
    }

    public void StartDash(float speed, float duration)
    {
        StartDash(speed, duration, dashCooldown);
    }


    void TryStartDash()
    {
        if (isDashing) return;
        if (dashCooldownTimer > 0f) return;

        // Decide dash direction: prefer movement input, otherwise facing
        int dir = 0;
        if (Mathf.Abs(moveInput) > 0.01f)
            dir = moveInput > 0 ? 1 : -1;
        else
            dir = (_facingDirection != 0) ? (int)Mathf.Sign(_facingDirection) : 1;

        dashDirection = dir;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    // --- Blink forward: instant teleport in facing direction ---
    [Header("Blink Settings")]
    public float blinkDistance = 4f;

    void DoBlinkForward()
    {
        // Basic version: move the player by blinkDistance in facing direction.
        int dir = (_facingDirection != 0) ? (int)Mathf.Sign(_facingDirection) : 1;
        Vector2 offset = new Vector2(blinkDistance * dir, 0f);
        transform.position = (Vector2)transform.position + offset;

        // You can later add raycasts here to prevent blinking inside walls.
    }

    // --- Blink up: instant vertical teleport ---
    public float blinkUpDistance = 3f;

    void DoBlinkUp()
    {
        Vector2 offset = new Vector2(0f, blinkUpDistance);
        transform.position = (Vector2)transform.position + offset;
    }

    // --- Hover: toggle a slow-fall mode ---
    [Header("Hover Settings")]
    public bool isHovering = false;
    public float hoverGravityScale = 0.2f;
    private float _normalGravityScale;


    // ADD ANY NEW ABILITIES HERE
    void ToggleHover()
    {
        if (!isHovering)
        {
            _normalGravityScale = rb.gravityScale;
            rb.gravityScale = hoverGravityScale;
            isHovering = true;
        }
        else
        {
            rb.gravityScale = _normalGravityScale;
            isHovering = false;
        }
    }

    void ApplyPauseFreeze()
    {
        if (!freezePhysicsOnPause || _frozenForPause || rb == null) return;

        // Save current motion
        _savedVel = rb.velocity;
        _savedAngVel = rb.angularVelocity;
        _savedGravity = rb.gravityScale;

        // Stop movement instantly and freeze physics
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;          // <- hard freeze for Rigidbody2D
        _frozenForPause = true;

        // Optional: stop sprite anims too
        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.speed = 0f;
    }

    void RemovePauseFreeze()
    {
        if (!_frozenForPause || rb == null) return;

        // Unfreeze physics
        rb.simulated = true;
        rb.gravityScale = _savedGravity;

        // Restore or clear momentum
        if (keepMomentumAfterPause)
        {
            rb.velocity = _savedVel;
            rb.angularVelocity = _savedAngVel;
        }
        else
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        _frozenForPause = false;

        // Resume sprite anims
        var anim = GetComponentInChildren<Animator>();
        if (anim) anim.speed = 1f;
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
