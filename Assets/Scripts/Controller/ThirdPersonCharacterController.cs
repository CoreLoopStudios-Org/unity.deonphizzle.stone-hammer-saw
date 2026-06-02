using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 9.81f;

    [Header("Camera Reference")]
    [Tooltip("Reference to the main camera transform. If left null, Camera.main will be used.")]
    public Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError("[ThirdPersonCharacterController] Main Camera not found in the scene! Please assign it manually.");
        }
    }

    private void Update()
    {
        // 1. Read movement inputs (WASD / Joysticks)
        float horizontal = 0f;
        float vertical = 0f;
        bool isRunning = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical = -1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1f;

            isRunning = Keyboard.current.leftShiftKey.isPressed;
        }
        else
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }
#else
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        isRunning = Input.GetKey(KeyCode.LeftShift);
#endif

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        Vector3 moveDirection = Vector3.zero;

        if (inputDir.magnitude > 0.05f && cameraTransform != null)
        {
            // Project camera forward/right directions onto XZ plane
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // Calculate directional target move vector relative to camera perspective
            moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            // Rotate character to face movement direction smoothly
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 3. Apply gravity to vertical velocity
        if (controller.isGrounded)
        {
            // Keep grounded status stable
            verticalVelocity = -0.5f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // 4. Combine movement speed and gravity
        Vector3 velocity = moveDirection * (inputDir.magnitude * currentSpeed);
        velocity.y = verticalVelocity;

        // Apply movement through Unity CharacterController
        controller.Move(velocity * Time.deltaTime);

        // 5. Bind parameters to the Animator Controller (if present)
        if (animator != null)
        {
            // Speed float parameter for blending between Idle (0), Walk (1), and Run (2)
            float speedParam = 0f;
            if (inputDir.magnitude > 0.05f)
            {
                speedParam = isRunning ? 2f : 1f;
            }
            
            // Damp speed parameter update for smooth animation state blending
            animator.SetFloat("Speed", speedParam, 0.15f, Time.deltaTime);
        }
    }
}
