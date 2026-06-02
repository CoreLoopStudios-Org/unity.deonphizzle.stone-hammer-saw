using UnityEngine;

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

    [Header("Smoothing")]
    public float smoothTime = 0.12f;

    private float x = 0.0f;
    private float y = 0.0f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float xVelocity = 0.0f;
    private float yVelocity = 0.0f;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        currentX = x;
        currentY = y;

        // Optionally lock cursor to window
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Get mouse movement inputs
        float mouseDeltaX = 0f;
        float mouseDeltaY = 0f;
        float scrollInput = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseDeltaX = delta.x * 0.05f; // Scale down input delta for new input system
            mouseDeltaY = delta.y * 0.05f;
            scrollInput = Mouse.current.scroll.ReadValue().y * 0.001f;
        }
        else
        {
            mouseDeltaX = Input.GetAxis("Mouse X");
            mouseDeltaY = Input.GetAxis("Mouse Y");
            scrollInput = Input.GetAxis("Mouse ScrollWheel");
        }
#else
        mouseDeltaX = Input.GetAxis("Mouse X");
        mouseDeltaY = Input.GetAxis("Mouse Y");
        scrollInput = Input.GetAxis("Mouse ScrollWheel");
#endif

        x += mouseDeltaX * xSpeed * 0.02f;
        y -= mouseDeltaY * ySpeed * 0.02f;

        // Clamp vertical angle
        y = ClampAngle(y, yMinLimit, yMaxLimit);

        // Interpolate rotation angles for smooth transition
        currentX = Mathf.SmoothDampAngle(currentX, x, ref xVelocity, smoothTime);
        currentY = Mathf.SmoothDampAngle(currentY, y, ref yVelocity, smoothTime);

        // Convert angles to rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Handle distance zoom using mouse scroll wheel
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
