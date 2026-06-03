using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonCharacterController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3.0f;
    public float runSpeed = 6.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 9.81f;

    [Header("Camera Reference & Settings")]
    public Transform cameraTransform;
    public float touchSensitivity = 1.5f; // মোবাইলে টাচ ঘোরানোর স্পিড
    public float cameraDistance = 4.0f;     
    public float cameraHeight = 1.5f;       
    public float minPitch = -20f;           
    public float maxPitch = 60f;            
    
    [Header("Camera Manual Control")]
    [Tooltip("Inspector থেকে ক্যামেরার X Rotation (Pitch) ম্যানুয়ালি কন্ট্রোল করার জন্য")]
    public float cameraRotationX_Offset = 0f; // নতুন ভ্যারিয়েবল

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity = 0f;

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

        // পিসির কার্সর লক রিমুভ করা হয়েছে, কারণ এটি মোবাইল গেম
        yaw = transform.eulerAngles.y; 
    }

    private void Update()
    {
        // আমাদের বানানো নতুন জয়স্টিক থেকে ইনপুট নেওয়া
        float horizontal = SimpleMobileJoystick.InputDirection.x;
        float vertical = SimpleMobileJoystick.InputDirection.y;
        
        // জয়স্টিক অর্ধেকের বেশি টানলে ক্যারেক্টার দৌড়াবে
        bool isRunning = SimpleMobileJoystick.InputDirection.magnitude > 0.7f; 

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
            verticalVelocity = -0.5f;
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

        // স্ক্রিনের ডানদিকে টাচ বা ক্লিক করে ঘুরালে ক্যামেরা ঘুরবে
        if (Input.GetMouseButton(0))
        {
            // স্ক্রিনের বামদিকে (যেখানে জয়স্টিক আছে) টাচ করলে ক্যামেরা ঘুরবে না
            if (Input.mousePosition.x > Screen.width / 2.5f)
            {
                float mouseX = Input.GetAxis("Mouse X") * touchSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * touchSensitivity;

                yaw += mouseX;
                pitch -= mouseY;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch); 
            }
        }

        Vector3 targetPosition = transform.position + Vector3.up * cameraHeight;
        
        // এখানে X Offset যুক্ত করা হয়েছে যাতে Inspector থেকে মডিফাই করা যায়
        Quaternion camRotation = Quaternion.Euler(pitch + cameraRotationX_Offset, yaw, 0f); 
        
        Vector3 camPosition = targetPosition - (camRotation * Vector3.forward * cameraDistance);

        cameraTransform.position = camPosition;
        cameraTransform.rotation = camRotation;
    }
}