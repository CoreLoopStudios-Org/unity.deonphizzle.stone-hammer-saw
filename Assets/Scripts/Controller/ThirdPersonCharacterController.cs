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
    
    [Header("UI Block Settings")]
    public RectTransform touchBlockPanel; // Inspector-এ Controller Background প্যানেলটি টেনে দেওয়ার জন্য
    
    [Header("Camera Manual Control")]
    public float cameraRotationX_Offset = 0f;

    private bool startedDragOnUI = false; // টাচ UI-তে শুরু হয়েছে কিনা চেক করার ভেরিয়েবল

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

        // 1. If this is an NPC/Bot character, disable this controller script
        if (GetComponent<NPCSquadAI>() != null)
        {
            this.enabled = false;
            return;
        }

        // 2. If this is a networked player character, only keep it enabled if we have input authority
        var netObj = GetComponent<Fusion.NetworkObject>();
        if (netObj != null && netObj.IsValid)
        {
            if (!netObj.HasInputAuthority)
            {
                this.enabled = false;
                return;
            }
        }
    }

    private void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;
        bool isRunning = false;

        bool canMove = true;
        if (MobSquadGameManager.Instance != null)
        {
            if (!MobSquadGameManager.Instance.IsGameActiveSafe)
            {
                canMove = false;
            }
        }

        if (canMove)
        {
            horizontal = SimpleMobileJoystick.InputDirection.x;
            vertical = SimpleMobileJoystick.InputDirection.y;
            isRunning = SimpleMobileJoystick.InputDirection.magnitude > 0.7f; 

            if (horizontal == 0 && vertical == 0)
            {
                horizontal = Input.GetAxis("Horizontal");
                vertical = Input.GetAxis("Vertical");
                isRunning = Input.GetKey(KeyCode.LeftShift);
            }
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
        // --- মোবাইলের টাচ কন্ট্রোল ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); 

            if (touch.phase == TouchPhase.Began)
            {
                // প্যানেলের ভেতরে টাচ পড়েছে কিনা চেক করা
                bool isTouchInPanel = false;
                if (touchBlockPanel != null)
                {
                    isTouchInPanel = RectTransformUtility.RectangleContainsScreenPoint(touchBlockPanel, touch.position, null);
                }

                // লজিক: টাচ যদি অ্যাসাইন করা প্যানেলে, অন্য UI-তে, অথবা স্ক্রিনের বাম পাশে শুরু হয়
                if (isTouchInPanel || EventSystem.current.IsPointerOverGameObject(touch.fingerId) || touch.position.x < Screen.width / 2.5f)
                {
                    startedDragOnUI = true;  // UI তে টাচ শুরু হয়েছে, তাই ক্যামেরা ঘুরবে না
                    isDraggingCamera = false;
                }
                else
                {
                    startedDragOnUI = false;
                    isDraggingCamera = true;
                    lastTouchPosition = touch.position;
                }
            }
            else if (touch.phase == TouchPhase.Moved && isDraggingCamera && !startedDragOnUI)
            {
                // শুধু তখনই ক্যামেরা ঘুরবে যদি টাচ UI বা বাম পাশে শুরু না হয়ে থাকে
                Vector2 delta = touch.position - lastTouchPosition;
                yaw += delta.x * touchSensitivity;
                pitch -= delta.y * touchSensitivity; 
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 
                lastTouchPosition = touch.position; 
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDraggingCamera = false;
                startedDragOnUI = false;
            }
        }
        // --- পিসির মাউস কন্ট্রোল (টেস্ট করার জন্য) ---
        else 
        {
            if (Input.GetMouseButtonDown(0))
            {
                // প্যানেলের ভেতরে মাউস ক্লিক পড়েছে কিনা চেক করা
                bool isMouseInPanel = false;
                if (touchBlockPanel != null)
                {
                    isMouseInPanel = RectTransformUtility.RectangleContainsScreenPoint(touchBlockPanel, Input.mousePosition, null);
                }

                // মাউস ক্লিক যদি অ্যাসাইন করা প্যানেলে, অন্য UI-তে বা স্ক্রিনের বামে হয়
                if (isMouseInPanel || EventSystem.current.IsPointerOverGameObject() || Input.mousePosition.x < Screen.width / 2.5f)
                {
                    startedDragOnUI = true;
                    isDraggingCamera = false;
                }
                else
                {
                    startedDragOnUI = false;
                    isDraggingCamera = true;
                    lastTouchPosition = Input.mousePosition;
                }
            }
            else if (Input.GetMouseButton(0) && isDraggingCamera && !startedDragOnUI)
            {
                Vector2 currentTouchPos = Input.mousePosition;
                Vector2 delta = currentTouchPos - lastTouchPosition;
                yaw += delta.x * touchSensitivity;
                pitch -= delta.y * touchSensitivity; 
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 
                lastTouchPosition = currentTouchPos; 
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDraggingCamera = false;
                startedDragOnUI = false;
            }
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