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

    [Header("Camera Reference & Settings")]
    [Tooltip("Reference to the main camera transform. If left null, Camera.main will be used.")]
    public Transform cameraTransform;
    
    [Tooltip("Speed OF Mouse movement.")]
    public float mouseSensitivity = 2.0f;
    [Tooltip("Distance from camera to the main camera.")]
    public float cameraDistance = 4.0f;     
    [Tooltip("Height of the camera from the character's position.")]
    public float cameraHeight = 1.5f;       
    public float minPitch = -20f;           
    public float maxPitch = 60f;            

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0f;

    // ক্যামেরার রোটেশন ধরে রাখার ভ্যারিয়েবল
    private float pitch = 0f;
    private float yaw = 0f;

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

        // গেম চালুর সাথে সাথে মাউস কার্সর হাইড করে স্ক্রিনের মাঝে লক করে দেবে
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

        // 5. Bind parameters to the Animator Controller
        if (animator != null)
        {
            float speedParam = 0f;
            if (inputDir.magnitude > 0.05f)
            {
                speedParam = isRunning ? 1f : 0.5f;
            }
            
            animator.SetFloat("Speed", speedParam, 0.15f, Time.deltaTime);
        }
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // মাউস ইনপুট নেওয়া
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 

        // ক্যামেরার নতুন রোটেশন এবং পজিশন ক্যালকুলেট করা
        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight;
        Quaternion camRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 camPosition = targetPosition - (camRotation * Vector3.forward * cameraDistance);

        // ক্যামেরাকে পজিশনে বসানো
        cameraTransform.position = camPosition;
        cameraTransform.rotation = camRotation;
    }
}