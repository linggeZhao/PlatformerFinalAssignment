using UnityEngine;

public enum PlayerDirection
{
    left, right
}

public enum PlayerState
{
    idle, walking, jumping, dead
}

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    private PlayerDirection currentDirection = PlayerDirection.right;
    public PlayerState currentState = PlayerState.idle;
    public PlayerState previousState = PlayerState.idle;

    [Header("Horizontal")]
    public float maxSpeed = 5f;
    public float accelerationTime = 0.25f;
    public float decelerationTime = 0.15f;

    [Header("Vertical")]
    public float apexHeight = 3f;
    public float apexTime = 0.5f;

    [Header("Ground Checking")]
    public float groundCheckOffset = 0.5f;
    public Vector2 groundCheckSize = new(0.4f, 0.1f);
    public LayerMask groundCheckMask;

    private float accelerationRate;
    private float decelerationRate;

    private float gravity;
    private float initialJumpSpeed;

    private bool isGrounded = false;
    public bool isDead = false;

    private Vector2 velocity;

    public float dashSpeed = 15f; // Define dash speed and assign a value
    public float dashDuration = 0.2f; // Define dash duration and assign a value
    private bool isDashing = false; // Define a boolean to determine the dash state
    private float dashTimeLeft = 0f; //Define the remaining dash time

    public float minJumpHeight = 1f; // Define the minimum jump height and assign a value of 1
    public float maxJumpHeight = 3f; // Define the maximum jump height and assign it to 3
    public float jumpControlTime = 0.2f; // Define jump duration
    private bool isJumping = false; // Define a boolean to determine the jump state
    private float jumpTimeCounter = 0f; //Define the remaining jump time counter

    public float wallCheckDistance = 0.5f; // Define the distance for wall detection and assign a value of 0.5
    public LayerMask wallLayer; // Defines the Layer used to detect walls (after that choose "Wall" in the list)
    public float wallJumpForceX = 5f; // Define the horizontal wall jumping force and assign it to 5.
    public float wallJumpForceY = 7f; // Define the vertical wall jumping strength and assign it to 7.
    private bool isTouchingWall = false; // Define a boolean to determine the player touch the wall or not
    private bool isWallJumping = false; // Define a boolean to determine the player is wall-jumping or not

    public void Start()
    {
        body.gravityScale = 0;

        accelerationRate = maxSpeed / accelerationTime;
        decelerationRate = maxSpeed / decelerationTime;

        gravity = -2 * apexHeight / (apexTime * apexTime);
        initialJumpSpeed = 2 * apexHeight / apexTime;
    }

    public void Update()
    {
        previousState = currentState;

        CheckForGround();
        CheckForWall();

        Vector2 playerInput = new Vector2();
        playerInput.x = Input.GetAxisRaw("Horizontal");

        if (isDead)
        {
            currentState = PlayerState.dead;
        }

        switch(currentState)
        {
            case PlayerState.dead:
                // do nothing - we dead.
                break;
            case PlayerState.idle:
                if (!isGrounded) currentState = PlayerState.jumping;
                else if (velocity.x != 0) currentState = PlayerState.walking;
                break;
            case PlayerState.walking:
                if (!isGrounded) currentState = PlayerState.jumping;
                else if (velocity.x == 0) currentState = PlayerState.idle;
                break;
            case PlayerState.jumping:
                if (isGrounded)
                {
                    if (velocity.x != 0) currentState = PlayerState.walking;
                    else currentState = PlayerState.idle;
                }
                break;
        }

        MovementUpdate(playerInput);

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && currentState != PlayerState.jumping)
        {
            StartDash();
        }

        if (isDashing)
        {
            DashUpdate();
        }
        else
        {
            JumpUpdate();
            if (!isGrounded && !isJumping)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else if (isGrounded && !isJumping)
            {
                velocity.y = 0;
            }
        }
        
        if (isTouchingWall && !isGrounded && Input.GetButtonDown("Jump"))
        {
            StartWallJump();
        }

        if (!isWallJumping)
        {
            JumpUpdate(); 
        }

        if (!isGrounded && !isJumping && !isWallJumping)
        {
            velocity.y += Physics2D.gravity.y * Time.deltaTime; // Applied Gravity
        }

        body.velocity = velocity;
        Debug.Log("body.velocity: " + body.velocity + ", velocity: " + velocity);
    }

    private void CheckForWall()
    {
        Vector2 position = transform.position;

        // Send rays to the left and to the right, respectively, to detect the wall
        bool wallLeft = Physics2D.Raycast(position, Vector2.left, wallCheckDistance, wallLayer);
        bool wallRight = Physics2D.Raycast(position, Vector2.right, wallCheckDistance, wallLayer);

        isTouchingWall = wallLeft || wallRight;
        Debug.Log($"Touching wall: {isTouchingWall}");
    }

    private void StartWallJump()
    {
        isWallJumping = true; // Setting the wall jump status

        // Set jump direction
        float jumpDirection = isTouchingWall && Input.GetAxisRaw("Horizontal") > 0 ? -1 : 1;

        // Setting the speed of wall jumping
        velocity.x = wallJumpForceX * jumpDirection; // Horizontal velocity away from the wall
        velocity.y = wallJumpForceY;                // Vertical jump speed

        Debug.Log($"Wall Jump! Velocity: {velocity}");

        // Delayed end of wall jump state
        Invoke(nameof(EndWallJump), 0.2f);
    }

    private void EndWallJump()
    {
        isWallJumping = false; // End wall jump status
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;

        // Setting the dash speed (dash to right direction or left)
        if (currentDirection == PlayerDirection.right)
            velocity.x = dashSpeed;
        else
            velocity.x = -dashSpeed;
    }

    private void DashUpdate()
    {
        dashTimeLeft -= Time.deltaTime;

        if (dashTimeLeft <= 0)
        {
            isDashing = false;

            // Decelerate to normal speed after dash finished.
            if (currentState == PlayerState.walking)
            {
                velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            }
            else
            {
                velocity.x = 0; // If the player stops moving, velocity should be 0. 
            }
        }
    }

    private void MovementUpdate(Vector2 playerInput)
    {
        if (isDashing)
            return; //return/skip this part if player is on the dash.

        if (playerInput.x < 0)
            currentDirection = PlayerDirection.left;
        else if (playerInput.x > 0)
            currentDirection = PlayerDirection.right;

        if (playerInput.x != 0)
        {
            velocity.x += accelerationRate * playerInput.x * Time.deltaTime;
            velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
        }
        else
        {
            if (velocity.x > 0)
            {
                velocity.x -= decelerationRate * Time.deltaTime;
                velocity.x = Mathf.Max(velocity.x, 0);
            }
            else if (velocity.x < 0)
            {
                velocity.x += decelerationRate * Time.deltaTime;
                velocity.x = Mathf.Min(velocity.x, 0);
            }
        }
    }

    private void JumpUpdate()
    {
        if (isGrounded && Input.GetButton("Jump"))
        {
            StartJump();
        }
        if (isJumping)
        {
            // If the player releases the jump button or the jump time runs out, ending the jump
            if (Input.GetButtonUp("Jump") || jumpTimeCounter <= 0)
            {
                EndJump();
            }
            else
            {
                // Players hold down the jump button continuously to reduce the jump time counter
                jumpTimeCounter -= Time.deltaTime;
                velocity.y = Mathf.Lerp(velocity.y, Mathf.Sqrt(2 * Mathf.Abs(gravity) * maxJumpHeight),1 - (jumpTimeCounter / jumpControlTime));
            }
        }
    }

    private void StartJump()
    {
        isJumping = true;
        jumpTimeCounter = jumpControlTime; // reset timer
    }

    private void EndJump()
    {
        isJumping = false;
    }

    private void CheckForGround()
    {
        isGrounded = Physics2D.OverlapBox(
            transform.position + Vector3.down * groundCheckOffset,
            groundCheckSize,
            0,
            groundCheckMask);
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position + Vector3.down * groundCheckOffset, groundCheckSize);
    }

    public bool IsWalking()
    {
        return velocity.x != 0;
    }
    public bool IsGrounded()
    {
        return isGrounded;
    }

    public PlayerDirection GetFacingDirection()
    {
        return currentDirection;
    }
}
