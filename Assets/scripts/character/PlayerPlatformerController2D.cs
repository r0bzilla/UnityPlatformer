using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>  
/// This class offers a script for controlling player movement of a 2D platformer game.
/// It offers customizable walk and run behavior, wall-jumps, mid-air-jumps, fall
/// acceleration when moving down in mid-air, and higher jumps when holding down the jump button.
/// </summary>
public class PlayerPlatformerController2D : MonoBehaviour
{
    private const string AIRBORNE_ANIM_PARAM = "isAirborne";
    private const string FACE_RIGHT_ANIM_PARAM = "isFacingRight";
    private const string VELOCITY_X_ANIM_PARAM = "velocityX";
    private const string VELOCITY_Y_ANIM_PARAM = "velocityY";

    [Header("Player Parts")]
    [SerializeField]
    [Tooltip("The RigidBody of the player.")]
    private Rigidbody2D playerBody;

    [SerializeField]
    [Tooltip("A collider script that must be attached to a BoxCollider2D.")]
    private QuadCollider2D sideColliders;

    [SerializeField]
    [Tooltip("The Animators of the player.")]
    private Animator[] playerAnimators;

    [Header("Input")]
    [SerializeField]
    [Tooltip("The name of the jump-button, set up in Project Settings > Input.")]
    private string jumpButton = "Fire2";

    [SerializeField]
    [Tooltip("The name of the run-button, set up in Project Settings > Input.")]
    private string runButton = "Fire1";

    [Header("Walk & Run")]
    [Tooltip("How fast the player accelerates when moving left or right.")]
    public float acceleration = 0.9f;
    [Tooltip("How fast the player decelerates when not walking, while on the ground.")]
    public float deceleration = 0.8f;
    [Tooltip("How fast the player decelerates when not walking, while in the air.")]
    public float airborneDeceleration = 0.2f;
    [Tooltip("The top speed the player can walk when moving left or right.")]
    public float maxWalkSpeed = 9.0f;
    [Tooltip("The top speed the player can run when moving left or right. If 0, running is disabled.")]
    public float maxRunSpeed = 16f;

    [Header("Jumps")]
    [Tooltip("The minimum jump height when letting go of the jump-button immediately.")]
    public float jumpHeight = 12.0f;
    [Tooltip("While pressing the jump-button, this parameter determines the number of frames that the player can extend the jump. Affects the maximum jump height along with the 'Gravity Factor Jump Extension'.")]
    public int jumpExtension = 16;
    [Tooltip("While pressing the jump-button, this factor is multiplied to the player gravity. Affects the maximum jump height along with the 'Jump Extension'.")]
    public float gravityFactorJumpExtension = 0.1f;
    [Tooltip("The number of additional jumps allowed while airborne.")]
    public int airborneJumps = 0;
    [Tooltip("A factor that is multiplied to the airborne jump height.")]
    public float airborneJumpFactor = 0.9f;
    [Tooltip("While pressing the down-button during falls, this factor is multiplied to the player gravity.")]
    public float gravityFactorFastFall = 2f;

    [Header("Wall Jumps")]
    [Tooltip("Enables wall jumping. When turning away from a wall that has been touched while airborne, you can briefly jump to execute a wall jump.")]
    public bool allowWallJumps = false;
    [Tooltip("The minimum wall jump height when letting go of the jump-button immediately.")]
    public float wallJumpHeight = 8.0f;
    [Tooltip("The horizontal distance of the wall jump.")]
    public float wallJumpHorizontal = 12.0f;
    [Tooltip("The number of frames after turning away from the wall, during which the wall jump can be executed.")]
    private int wallJumpGracePeriod = 8;

    public bool isTouchingLeftWall { get { return sideColliders.isTouchingLeft; } }
    public bool isTouchingRightWall { get { return sideColliders.isTouchingRight; } }
    public bool isTouchingCeiling { get { return sideColliders.isTouchingTop; } }
    public bool isAirborne { get { return !sideColliders.isTouchingBottom; } }
    public bool isFacingRight { get; private set; }

    private float xAxis;
    private float yAxis;
    private float normalGravityScale;
    private int currentAirborneJumps;
    private int currentJumpExtension;
    private int currentWallJumpGracePeriod;
    private bool wasAirBorne;
    private bool isRunning;


    /// <summary>
    /// Initializes the script, setting some animator parameters.
    /// </summary>
    void Start()
    {
        this.isFacingRight = true;
        this.normalGravityScale = playerBody.gravityScale;

        SetAnimatorsBool(FACE_RIGHT_ANIM_PARAM, isFacingRight);
        SetAnimatorsBool(AIRBORNE_ANIM_PARAM, isAirborne);
    }


    /// <summary>
    /// Sets a boolean parameter of all Animators.
    /// </summary>
    /// <param name="paramName">The name of the animator parameter</param>
    /// <param name="value">The value that is to be applied</param>
    private void SetAnimatorsBool(string paramName, bool value)
    {
        int i = playerAnimators.Length;
        while (i != 0)
        {
            i--;
            playerAnimators[i].SetBool(paramName, value);
        }
    }


    /// <summary>
    /// Sets a float parameter of all Animators.
    /// </summary>
    /// <param name="paramName">The name of the animator parameter</param>
    /// <param name="value">The value that is to be applied</param>
    private void SetAnimatorsFloat(string paramName, float value)
    {
        int i = playerAnimators.Length;
        while (i != 0)
        {
            i--;
            playerAnimators[i].SetFloat(paramName, value);
        }
    }


    /// <summary>
    /// Update that is called on each frame.
    /// Handles jumping, the run button, and the wall colliders.
    /// </summary>
    protected void Update()
    {
        // after leaving a wall hug, you have a couple of frames to do a wall jump
        if (isTouchingLeftWall || isTouchingRightWall)
            currentWallJumpGracePeriod = wallJumpGracePeriod;
        else if (currentWallJumpGracePeriod > 0)
            currentWallJumpGracePeriod--;

        // jumping
        if (Input.GetButtonDown(jumpButton))
            Jump();
        else if (Input.GetButton(jumpButton))
            ExtendJump();
        else if (Input.GetButtonUp(jumpButton))
            RestoreGravityScale();

        // running
        isRunning = maxRunSpeed != 0 && Input.GetButton(runButton);
    }


    /// <summary>
    /// Update that is called in a fixed interval.
    /// Handles walking, running, landing, turning around, and Animator updates.
    /// </summary>
    protected void FixedUpdate()
    {
        this.xAxis = Input.GetAxisRaw("Horizontal");
        this.yAxis = Input.GetAxisRaw("Vertical");

        // horizontal movement
        SetLookDirection();
        Walk();

        Vector2 velocity = playerBody.velocity;

        // vertical movement
        if (yAxis < 0f)
            SpeedUpFall();
        else if (velocity.y <= 0f)
            RestoreGravityScale();

        // update animator
        SetAnimatorsBool(AIRBORNE_ANIM_PARAM, isAirborne);
        SetAnimatorsFloat(VELOCITY_X_ANIM_PARAM, velocity.x);
        SetAnimatorsFloat(VELOCITY_Y_ANIM_PARAM, velocity.y);

        // update landing
        if (wasAirBorne && !isAirborne)
            Land();

        wasAirBorne = isAirborne;
    }


    /// <summary>
    /// Flips the player around if it is steered to the opposite horizontal direction.
    /// </summary>
    private void SetLookDirection()
    {
        if (xAxis == 0f)
            return;

        if (xAxis > 0f != isFacingRight)
            Flip();
    }


    /// <summary>
    /// Flips the player around by updating a boolean parameter in the Animators.
    /// </summary>
    private void Flip()
    {
        isFacingRight = !isFacingRight;
        SetAnimatorsBool(FACE_RIGHT_ANIM_PARAM, isFacingRight);
    }

    /// <summary>
    /// Handles horizontal movement of the player:
    /// When a horizontal button is pushed, the player accelerates to
    /// the corresponding direction until the maximum speed is reached.
    /// The player decelerates if the player speed is higher than the maximum speed, 
    /// or the movement buttons are let go, or the direction changes.
    /// The maximum horizontal speed changes when the run-button is held down.
    /// </summary>
    private void Walk()
    {
        float previousVelocityX = playerBody.velocity.x;
        float newVelocityX = 0f;

        if (isTouchingLeftWall && xAxis < 0f || isTouchingRightWall && xAxis > 0f)
            return;

        float finalAcceleration = acceleration * xAxis;
        float finalMaxSpeed = isRunning ? maxRunSpeed : maxWalkSpeed;
        float finalDeceleration = isAirborne ? airborneDeceleration : deceleration;

        float previousSpeed = Mathf.Abs(previousVelocityX);
        float previousDirection = Mathf.Sign(previousVelocityX);
        float newDirection = Mathf.Sign(xAxis);

        // if we do not move, decelerate
        if (xAxis == 0f)
        {
            if (previousSpeed != 0f)
            {
                newVelocityX = previousVelocityX - previousDirection * finalDeceleration;

                // stop if we decelerate too far
                if (Mathf.Sign(newVelocityX) != previousDirection)
                    newVelocityX = 0f;
            }
        }
        // if we walk against the current direction, decelerate and walk
        else if (newDirection != previousDirection && previousSpeed != 0f)
        {
            newVelocityX = previousVelocityX + newDirection * finalDeceleration + finalAcceleration;
        }
        // if we already move faster than maxSpeed, slow down
        else if (previousSpeed > finalMaxSpeed)
        {
            newVelocityX = previousVelocityX - previousDirection * finalDeceleration;

            // don't decelerate below the maximum speed
            if (Mathf.Abs(newVelocityX) < finalMaxSpeed)
                newVelocityX = previousDirection * finalMaxSpeed;
        }

        // accelerate if none of the above are true
        else
        {
            newVelocityX = previousVelocityX + finalAcceleration;

            // if we hit the maxSpeed limit, don't move faster
            if (Mathf.Abs(newVelocityX) > finalMaxSpeed)
                newVelocityX = newDirection * finalMaxSpeed;
        }


        Vector2 velocity = playerBody.velocity;
        velocity.x = newVelocityX;
        playerBody.velocity = velocity;
    }


    /// <summary>
    /// Depending on the conditions, either executes a regular-, wall-, or airborne jump.
    /// </summary>
    private void Jump()
    {
        if (isAirborne)
        {
            // wall jump
            if (allowWallJumps && currentWallJumpGracePeriod > 0
                && !(isTouchingLeftWall && xAxis <= 0f || isTouchingRightWall && xAxis >= 0f))
            {
                WallJump();
            }
            else
                AirborneJump();
        }
        else
            RegularJump();
    }


    /// <summary>
    /// Moves the player upwards and forwards, which should be away from the wall.
    /// </summary>
    private void WallJump()
    {
        currentWallJumpGracePeriod = 0;

        Vector2 velocity = playerBody.velocity;
        velocity.x = Mathf.Sign(xAxis) * wallJumpHorizontal;
        velocity.y = wallJumpHeight;
        playerBody.velocity = velocity;
        currentJumpExtension = jumpExtension;

    }


    /// <summary>
    /// Moves the player upwards.
    /// </summary>
    private void RegularJump()
    {
        Vector2 velocity = playerBody.velocity;
        velocity.y = jumpHeight;
        playerBody.velocity = velocity;
    }


    /// <summary>
    /// Moves the player upwards, if airborne jumps are available.
    /// </summary>
    private void AirborneJump()
    {
        if (currentAirborneJumps == 0)
            return;

        currentAirborneJumps--;

        Vector2 velocity = playerBody.velocity;
        velocity.y = jumpHeight * airborneJumpFactor;
        playerBody.velocity = velocity;
        currentJumpExtension = jumpExtension;
    }


    /// <summary>
    /// While jumping and 'currentJumpExtension' frames are not zero,
    /// the gravity is lowered, smoothly enabling a higher jump.
    /// Each frame during which this method is called, consumes a 'currentJumpExtension' frame.
    /// </summary>
    private void ExtendJump()
    {
        if (currentJumpExtension != 0 && playerBody.velocity.y > 0)
        {
            playerBody.gravityScale = normalGravityScale * gravityFactorJumpExtension;
            currentJumpExtension--;

            if (currentJumpExtension == 0f)
                RestoreGravityScale();
        }
    }


    /// <summary>
    /// Increases the player gravity, causing a faster fall.
    /// This method is called when the player presses the down-button while airborne.
    /// </summary>
    private void SpeedUpFall()
    {
        if (playerBody.velocity.y < 0)
            playerBody.gravityScale = Mathf.Abs(yAxis) * normalGravityScale * gravityFactorFastFall;
    }


    /// <summary>
    /// Restores the player gravity to the original value of the RigidBody2D.
    /// </summary>
    private void RestoreGravityScale()
    {
        playerBody.gravityScale = normalGravityScale;
    }


    /// <summary>
    /// This method is called when the player touches the ground.
    /// Restores the allowed number of mid-air-jumps and the jump extension frames.
    /// </summary>
    private void Land()
    {
        this.currentJumpExtension = jumpExtension;
        this.currentAirborneJumps = airborneJumps;
    }
}
