using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class FirstPersonDrifterController : MonoBehaviour
{
    // ==== Movement Settings ====
    [Header("Movement Settings")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 10.0f;
    public bool enableRunning = false;
    public float jumpSpeed = 4.0f;
    public float gravity = 10.0f;
    public float fallingDamageThreshold = 10.0f;

    [Tooltip("When true, diagonal movement is limited to avoid extra speed.")]
    public bool limitDiagonalSpeed = true;

    [Header("Sliding Settings")]
    public bool slideWhenOverSlopeLimit = false;
    public bool slideOnTaggedObjects = false;
    public float slideSpeed = 5.0f;

    [Header("Air Control & Bump Prevention")]
    public bool airControl = true;
    public float antiBumpFactor = 0.75f;
    public int antiBunnyHopFactor = 1;  // frames required to be grounded before a jump

    // ==== Look Settings ====
    [Header("Camera Look Settings")]
    [Tooltip("Drag your player camera (child) here.")]
    public Transform cameraTransform;
    public float sensitivityX = 10f;
    public float sensitivityY = 9f;
    public bool invertY = false;
    public int framesOfSmoothing = 5;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Head Bob Settings")]
    public bool enableHeadBob = true;
    public float headBobFrequency = 1.5f;  // Speed of the bobbing
    public float headBobAmplitude = 0.1f;  // How high/low the camera moves
    public float headBobRunningMultiplier = 1.5f;  // How much to multiply the head bobbing when running

    private Vector3 originalCameraLocalPosition;
    private float headBobTimer = 0f;

    // ==== Private References & Variables ====
    private PlayerInput playerInput;
    private CharacterController controller;
    private Transform myTransform;
    
    // Movement variables
    private Vector3 moveDirection = Vector3.zero;
    private bool grounded = false;
    private float speed;
    private RaycastHit hit;
    private float fallStartLevel;
    private bool falling;
    private float slideLimit;
    private float rayDistance;
    private Vector3 contactPoint;
    private bool playerControl = false;
    private int jumpTimer;

    // Look variables
    private float yaw;   // horizontal rotation offset (degrees)
    private float pitch; // vertical rotation offset (degrees)
    private List<float> yawHistory = new List<float>();
    private List<float> pitchHistory = new List<float>();
    private Quaternion originalPlayerRotation;
    private Quaternion originalCameraRotation;

    void Awake()
    {
        //mouse locking
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Retrieve required components
        playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<CharacterController>();
        myTransform = transform;
        originalPlayerRotation = transform.rotation;
        if (cameraTransform != null)
        {
            originalCameraRotation = cameraTransform.localRotation;
            // Initialize pitch from camera’s current local rotation
            pitch = cameraTransform.localEulerAngles.x;
            if (pitch > 180f)
                pitch -= 360f;
        }
        yaw = transform.eulerAngles.y;
        speed = walkSpeed;
        rayDistance = controller.height * 0.5f + controller.radius;
        slideLimit = controller.slopeLimit - 0.1f;
        jumpTimer = antiBunnyHopFactor;

        if (cameraTransform != null)
        {
            originalCameraLocalPosition = cameraTransform.localPosition;
        }
    }

    void Update()
    {
        HandleLook();
        BobHead();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Processes movement including walking, running, jumping, sliding, and gravity.
    /// </summary>
    void HandleMovement()
    {
        // Get movement input (Vector2: x = horizontal, y = vertical)
        Vector2 moveInput = playerInput.actions["Move"].ReadValue<Vector2>();
        float inputX = moveInput.x;
        float inputY = moveInput.y;
        float inputModifyFactor = (inputX != 0f && inputY != 0f && limitDiagonalSpeed) ? 0.7071f : 1f;

        if (grounded)
        {
            bool sliding = false;

            // Check slope beneath player
            if (Physics.Raycast(myTransform.position, -Vector3.up, out hit, rayDistance))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) > slideLimit)
                    sliding = true;
            }
            else if (Physics.Raycast(contactPoint + Vector3.up, -Vector3.up, out hit))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) > slideLimit)
                    sliding = true;
            }

            // Falling damage check when landing
            if (falling)
            {
                falling = false;
                if (myTransform.position.y < fallStartLevel - fallingDamageThreshold)
                    FallingDamageAlert(fallStartLevel - myTransform.position.y);
            }

            // Determine movement speed (running vs. walking)
            if (enableRunning)
            {
                // "Sprint" action is expected to be a button; if pressed, use runSpeed
                speed = playerInput.actions["Sprint"].ReadValue<float>() > 0f ? runSpeed : walkSpeed;
            }
            else
            {
                speed = walkSpeed;
            }

            // If sliding, compute a slide vector from the slope's normal
            if ((sliding && slideWhenOverSlopeLimit) || (slideOnTaggedObjects && hit.collider != null && hit.collider.CompareTag("Slide")))
            {
                Vector3 hitNormal = hit.normal;
                moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref moveDirection);
                moveDirection *= slideSpeed;
                playerControl = false;
            }
            else
            {
                // Use input for regular movement
                Vector3 desiredMove = new Vector3(inputX * inputModifyFactor, -antiBumpFactor, inputY * inputModifyFactor);
                moveDirection = myTransform.TransformDirection(desiredMove) * speed;
                playerControl = true;
            }

            // Jumping: allow jump if grounded for at least antiBunnyHopFactor frames
            // For button-type actions, ReadValue<float>() returns 1 when pressed and 0 otherwise.
            if (playerInput.actions["Jump"].ReadValue<float>() == 0f)
                jumpTimer++;
            else if (jumpTimer >= antiBunnyHopFactor)
            {
                moveDirection.y = jumpSpeed;
                jumpTimer = 0;
            }
        }
        else
        {
            // Record start of falling if not already falling
            if (!falling)
            {
                falling = true;
                fallStartLevel = myTransform.position.y;
            }

            // Allow air control if enabled
            if (airControl && playerControl)
            {
                Vector3 airMove = new Vector3(inputX * speed * inputModifyFactor, 0f, inputY * speed * inputModifyFactor);
                Vector3 transformedAirMove = myTransform.TransformDirection(airMove);
                moveDirection.x = transformedAirMove.x;
                moveDirection.z = transformedAirMove.z;
            }
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the controller and update grounded status
        CollisionFlags flags = controller.Move(moveDirection * Time.deltaTime);
        grounded = (flags & CollisionFlags.Below) != 0;
    }

    /// <summary>
    /// Handles camera look (yaw and pitch) with optional smoothing.
    /// The player's transform rotates horizontally (yaw) while the camera rotates vertically (pitch).
    /// </summary>
    void HandleLook()
    {
        // Get look input (Vector2: x = horizontal, y = vertical)
        Vector2 lookInput = playerInput.actions["Look"].ReadValue<Vector2>();

        // Update yaw and pitch using sensitivity and deltaTime
        yaw += lookInput.x * sensitivityX * Time.deltaTime;
        float invertMultiplier = invertY ? -1f : 1f;
        pitch += lookInput.y * sensitivityY * invertMultiplier * Time.deltaTime;

        // Store rotation history for smoothing
        yawHistory.Add(yaw);
        pitchHistory.Add(pitch);
        if (yawHistory.Count > framesOfSmoothing)
            yawHistory.RemoveAt(0);
        if (pitchHistory.Count > framesOfSmoothing)
            pitchHistory.RemoveAt(0);

        // Average the stored values
        float avgYaw = 0f, avgPitch = 0f;
        foreach (float y in yawHistory)
            avgYaw += y;
        foreach (float p in pitchHistory)
            avgPitch += p;
        avgYaw /= yawHistory.Count;
        avgPitch /= pitchHistory.Count;

        // Clamp vertical rotation
        avgPitch = Mathf.Clamp(avgPitch, minPitch, maxPitch);

        // Apply rotations:
        // - Rotate the player horizontally (yaw)
        transform.rotation = originalPlayerRotation * Quaternion.Euler(0f, avgYaw, 0f);
        // - Rotate the camera vertically (pitch)
        if (cameraTransform != null)
            cameraTransform.localRotation = originalCameraRotation * Quaternion.Euler(avgPitch, 0f, 0f);
    }

    void BobHead(){
        if (enableHeadBob && grounded && controller.velocity.magnitude > 0.1f)
        {
            var multiplier = playerInput.actions["Sprint"].ReadValue<float>() > 0f ? headBobRunningMultiplier : 1f;
            headBobTimer += Time.deltaTime * headBobFrequency*multiplier;
            float bobOffsetY = Mathf.Sin(headBobTimer) * headBobAmplitude*multiplier;
            cameraTransform.localPosition = originalCameraLocalPosition + new Vector3(0f, bobOffsetY, 0f);
        }
        else
        {
            headBobTimer = 0f;
            // Smoothly return to the original position when not moving
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, originalCameraLocalPosition, Time.deltaTime * headBobFrequency);
        }
    }

    /// <summary>
    /// Stores the collision contact point for sliding logic.
    /// </summary>
    /// <param name="hitInfo">Collision details.</param>
    void OnControllerColliderHit(ControllerColliderHit hitInfo)
    {
        contactPoint = hitInfo.point;
    }

    /// <summary>
    /// Called when falling damage should be applied.
    /// Insert your damage logic here (e.g., reduce health, play sound).
    /// </summary>
    /// <param name="fallDistance">Distance fallen.</param>
    void FallingDamageAlert(float fallDistance)
    {
        Debug.Log("Ouch! Fell " + fallDistance + " units!");
        // TODO: Implement falling damage logic (reduce health, etc.)
    }
}
