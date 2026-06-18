using UnityEngine;
using UnityEngine.EventSystems; 

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 9.81f;
    public float jumpHeight = 1.2f; 

    [Header("Camera Reference & Settings")]
    public Transform cameraTransform;
    public float touchSensitivity = 0.2f; 
    public float cameraDistance = 4.0f;     
    public float cameraHeight = 1.5f;       
    public float minPitch = -20f;           
    public float maxPitch = 60f;            
    
    [Header("Camera Manual Control")]
    public float cameraRotationX_Offset = 0f;

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0f;

    private float pitch = 0f;
    private float yaw = 0f;

    private Vector2 lastTouchPosition;
    private bool isDraggingCamera = false;
    
    private bool jumpPressed = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        yaw = transform.eulerAngles.y; 
    }

    private void Update()
    {
        float horizontal = SimpleMobileJoystick.InputDirection.x;
        float vertical = SimpleMobileJoystick.InputDirection.y;
        bool isRunning = SimpleMobileJoystick.InputDirection.magnitude > 0.7f; 

        if (horizontal == 0 && vertical == 0)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");
            isRunning = Input.GetKey(KeyCode.LeftShift);
        }

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical);
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveDirection = Vector3.zero;

        if (inputDir.magnitude > 0.05f && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f; 
            }

            if (jumpPressed || Input.GetKeyDown(KeyCode.Space))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity); 
                jumpPressed = false; 
                
                if (animator != null)
                {
                    animator.SetTrigger("Jump"); 
                }
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * (inputDir.magnitude * currentSpeed);
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        if (animator != null)
        {
            float speedParam = inputDir.magnitude > 0.05f ? (isRunning ? 1f : 0.5f) : 0f;
            animator.SetFloat("Speed", speedParam, 0.15f, Time.deltaTime);
            
            // পিসিতে টেস্ট করার জন্য মাউসের লেফট ক্লিক চাপলেও অ্যাটাক করবে
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < Screen.width / 2.5f)
            {
               // OnAttackButtonClicked();
            }
        }

        HandleCameraInput();
    }

    public void OnJumpButtonClicked()
    {
        if (controller.isGrounded)
        {
            jumpPressed = true;
        }
    }

    public void OnAttackButtonClicked()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack"); 
        }
    }

    private void HandleCameraInput()
    {
        Vector2 currentTouchPos = Vector2.zero;
        bool inputDetected = false;

        // --- মোবাইলের টাচ কন্ট্রোল ---
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                // নতুন লজিক: যদি টাচটি কোনো UI (যেমন জয়স্টিকের ব্যাকগ্রাউন্ড) এর ওপর পড়ে, তবে ক্যামেরা ঘুরবে না
                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    continue; // এই টাচটি ইগনোর করে লুপের পরের কাজে চলে যাবে
                }

                if (touch.position.x > Screen.width / 2.5f)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        lastTouchPosition = touch.position;
                        isDraggingCamera = true;
                    }
                    else if (touch.phase == TouchPhase.Moved && isDraggingCamera)
                    {
                        currentTouchPos = touch.position;
                        inputDetected = true;
                    }
                    else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        isDraggingCamera = false;
                    }
                    break; 
                }
            }
        }
        // --- পিসির মাউস কন্ট্রোল ---
        else if (Input.GetMouseButton(0))
        {
            // নতুন লজিক: মাউস ক্লিক যদি কোনো UI এর ওপর থাকে, তবে ক্যামেরা ঘুরবে না
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.mousePosition.x > Screen.width / 2.5f)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        lastTouchPosition = Input.mousePosition;
                        isDraggingCamera = true;
                    }
                    else if (isDraggingCamera)
                    {
                        currentTouchPos = Input.mousePosition;
                        inputDetected = true;
                    }
                }
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDraggingCamera = false;
        }

        if (inputDetected && isDraggingCamera)
        {
            Vector2 delta = currentTouchPos - lastTouchPosition;
            yaw += delta.x * touchSensitivity;
            pitch -= delta.y * touchSensitivity; 
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 
            lastTouchPosition = currentTouchPos; 
        }
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight;
        Quaternion camRotation = Quaternion.Euler(pitch + cameraRotationX_Offset, yaw, 0f); 
        Vector3 camPosition = targetPosition - (camRotation * Vector3.forward * cameraDistance);

        cameraTransform.position = camPosition;
        cameraTransform.rotation = camRotation;
    }
}