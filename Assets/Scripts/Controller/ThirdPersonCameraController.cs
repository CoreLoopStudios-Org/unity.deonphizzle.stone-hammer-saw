using UnityEngine;
using UnityEngine.EventSystems;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    [Tooltip("The target transform the camera should orbit around (usually the player's pivot).")]
    public Transform target;
    
    [Tooltip("Offset height above the target pivot point.")]
    public float targetOffsetHeight = 1.5f;

    [Header("Distance Settings")]
    [Tooltip("The distance between the camera and target.")]
    public float distance = 4.0f;
    [Tooltip("Minimum follow distance (zoom).")]
    public float minDistance = 2.0f;
    [Tooltip("Maximum follow distance (zoom).")]
    public float maxDistance = 10.0f;

    [Header("Speed and Sensitivity")]
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
    public float zoomSpeed = 2.0f;

    [Header("Angle Constraints")]
    [Tooltip("Lowest angle the camera can tilt vertically (degrees).")]
    public float yMinLimit = -20f;
    [Tooltip("Highest angle the camera can tilt vertically (degrees).")]
    public float yMaxLimit = 60f;

    [Header("Follow Settings")]
    [Tooltip("How fast the camera returns behind the player's back.")]
    public float autoAlignSpeed = 3f;
    [Tooltip("Seconds of inactivity before auto-aligning behind the player.")]
    public float autoAlignDelay = 1.5f;

    private float lastDragTime = 0f;
    private bool isDragging = false;

    [Header("Smoothing")]
    public float smoothTime = 0.12f;

    [Header("Current Rotation Angles")]
    [Tooltip("Horizontal rotation angle (yaw) in degrees.")]
    public float x = 0.0f;
    [Tooltip("Vertical rotation angle (pitch/tilt) in degrees.")]
    public float y = 0.0f;

    [Header("Initial Angle Override")]
    [Tooltip("If true, the camera will start at the custom X and Y angles specified above instead of copying the scene transform's rotation.")]
    public bool overrideStartRotation = false;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;

    private void Start()
    {
        if (!overrideStartRotation)
        {
            Vector3 angles = transform.eulerAngles;
            x = angles.y;
            y = angles.x;
        }

        currentX = x;
        currentY = y;

        // Optionally lock cursor to window
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Detect Screen Drag (Touch or Mouse drag) for 360 Camera Rotation
        float deltaX = 0f;
        float deltaY = 0f;
        float scrollInput = 0f;
        bool inputDetected = false;

        // Mouse inputs for PC / Editor testing
        if (Input.GetMouseButton(0))
        {
            // Verify we aren't clicking on a UI element (like the virtual joystick)
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                deltaX = Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                deltaY = Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                inputDetected = true;
            }
        }
        // Mobile touch inputs
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Moved)
            {
                // Verify touch is not over a UI element (like the joystick)
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    deltaX = touch.deltaPosition.x * xSpeed * 0.005f;
                    deltaY = touch.deltaPosition.y * ySpeed * 0.005f;
                    inputDetected = true;
                }
            }
        }

        // Scroll zoom inputs
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            scrollInput = Mouse.current.scroll.ReadValue().y * 0.001f;
        }
        else
        {
            scrollInput = Input.GetAxis("Mouse ScrollWheel");
        }
#else
        scrollInput = Input.GetAxis("Mouse ScrollWheel");
#endif

        // Apply manual orbital rotation if dragging
        if (inputDetected)
        {
            x += deltaX;
            y -= deltaY;
            lastDragTime = Time.time;
            isDragging = true;
        }
        else
        {
            isDragging = false;
        }

        // 2. Smoothly Auto-Align Camera behind player during locomotion
        if (!isDragging && (Time.time - lastDragTime > autoAlignDelay))
        {
            // Retrieve target's CharacterController to verify movement
            CharacterController targetController = target.GetComponent<CharacterController>();
            Vector3 velocity = targetController != null ? targetController.velocity : Vector3.zero;
            
            // Check if player is moving on XZ plane
            if (new Vector3(velocity.x, 0f, velocity.z).magnitude > 0.1f)
            {
                // Align camera rotation angle (x) with player's forward direction
                float targetAngle = target.eulerAngles.y;
                x = Mathf.LerpAngle(x, targetAngle, Time.deltaTime * autoAlignSpeed);
            }
        }

        // Clamp vertical angle
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // Interpolate rotation angles for smooth transition
        currentX = Mathf.SmoothDampAngle(currentX, x, ref xVelocity, smoothTime);
        currentY = Mathf.SmoothDampAngle(currentY, y, ref yVelocity, smoothTime);

        // Convert angles to rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Handle distance zoom using mouse scroll wheel / pinch zoom
        distance = Mathf.Clamp(distance - scrollInput * zoomSpeed, minDistance, maxDistance);

        // Calculate target pivot position
        Vector3 targetPivot = target.position + Vector3.up * targetOffsetHeight;

        // Calculate camera position based on rotation and target pivot
        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
        Vector3 position = rotation * negDistance + targetPivot;

        // Set camera position and rotation
        transform.rotation = rotation;
        transform.position = position;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}
