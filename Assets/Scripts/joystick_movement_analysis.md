# Virtual Joystick & Orbit Camera System Implementation Plan

This document details the analysis and implementation steps required to integrate a mobile-friendly virtual Joystick UI, transition character movement from WASD to Joystick input, and configure the camera to auto-follow behind the player while supporting 360-degree touch drag (or mouse drag) orbits.

---

## 1. Technical Requirements & Architecture

We will implement a touch-based control system consisting of three parts:

```
[ Touch UI Canvas ] 
       │
       ▼
[ VirtualJoystick.cs ] ──(Vector2 Output)──► [ ThirdPersonCharacterController.cs ]
                                                          │
                                                (Player Orientation)
                                                          ▼
                                            [ ThirdPersonCameraController.cs ]
                                              - Auto-aligns behind player
                                              - Overridden by Touch/Click drag
```

1.  **Virtual Joystick UI:** A touch-input controller on the screen Canvas. It will read mouse-drag (desktop) and touch-drag (mobile) inputs and output a normalized 2D movement vector (`Vector2`).
2.  **Character Controller Integration:** Update [ThirdPersonCharacterController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCharacterController.cs) to read from the joystick if available, falling back to WASD on desktop.
3.  **Dynamic Orbit Camera:** Update [ThirdPersonCameraController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCameraController.cs) to implement:
    *   **Follow State:** The camera smoothly rotates to align directly behind the character during movement.
    *   **Manual Rotate State:** If the user drags on the screen (mouse drag or phone touch), auto-alignment is paused, allowing 360-degree manual orbit.
    *   **Reset Timeout:** If the user stops dragging and the player moves, the camera smoothly aligns back behind the player.

---

## 2. Planned Script Implementations

### 2.1 The `VirtualJoystick.cs` Script
This script will be attached to a Joystick Background UI element. It uses Unity's UI EventSystem handlers to track drag offsets:

```csharp
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static VirtualJoystick Instance { get; private set; }

    [Header("UI Elements")]
    public RectTransform container;
    public RectTransform handle;

    private Vector2 inputVector = Vector2.zero;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (container == null) container = GetComponent<RectTransform>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;

        // Calculate handle position relative to container bounds
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position))
        {
            float width = container.sizeDelta.x;
            float height = container.sizeDelta.y;

            // Map drag offset to a -1 to +1 range
            position.x = (position.x / width) * 2f;
            position.y = (position.y / height) * 2f;

            inputVector = new Vector2(position.x, position.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Update knob handle position visually
            handle.anchoredPosition = new Vector2(
                inputVector.x * (width / 2f), 
                inputVector.y * (height / 2f)
            );
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    public Vector2 GetInputDirection()
    {
        return inputVector;
    }
}
```

---

### 2.2 Updating `ThirdPersonCharacterController.cs`
We will replace the input-reading logic in [ThirdPersonCharacterController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCharacterController.cs) (around lines 41–68) with a check that queries the `VirtualJoystick`:

```diff
     private void Update()
     {
         // 1. Read movement inputs (Joystick with Keyboard fallback)
         float horizontal = 0f;
         float vertical = 0f;
         bool isRunning = false;
 
+        if (VirtualJoystick.Instance != null && VirtualJoystick.Instance.GetInputDirection() != Vector2.zero)
+        {
+            Vector2 joyInput = VirtualJoystick.Instance.GetInputDirection();
+            horizontal = joyInput.x;
+            vertical = joyInput.y;
+            isRunning = joyInput.magnitude > 0.85f; // Auto-run if joystick pushed far enough
+        }
+        else
+        {
 #if ENABLE_INPUT_SYSTEM
             if (Keyboard.current != null)
             {
                 if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical = 1f;
                 if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical = -1f;
                 if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal = -1f;
                 if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal = 1f;
                 isRunning = Keyboard.current.leftShiftKey.isPressed;
             }
 #else
             horizontal = Input.GetAxis("Horizontal");
             vertical = Input.GetAxis("Vertical");
             isRunning = Input.GetKey(KeyCode.LeftShift);
 #endif
+        }
```

---

### 2.3 Updating `ThirdPersonCameraController.cs`
We will update [ThirdPersonCameraController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCameraController.cs) to handle:
1.  **360 touch/mouse dragging** when clicking/touching the screen outside the joystick area.
2.  **Auto-alignment behind the player** if the player is moving and not manually looking around.

```csharp
    [Header("Follow Settings")]
    [Tooltip("How fast the camera returns behind the player's back.")]
    public float autoAlignSpeed = 3f;
    [Tooltip("Seconds of inactivity before auto-aligning behind the player.")]
    public float autoAlignDelay = 1.5f;

    private float lastDragTime = 0f;
    private bool isDragging = false;
```

In the `LateUpdate()` method, we replace mouse axis checks with:

```csharp
        // Check touch or mouse click-drag inputs
        float deltaX = 0f;
        float deltaY = 0f;
        bool inputDetected = false;

        // Detect screen drag (Touch or Mouse drag)
        if (Input.GetMouseButton(0))
        {
            // Verify we aren't clicking the joystick UI (using EventSystem raycasting)
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                deltaX = Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                deltaY = Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                inputDetected = true;
            }
        }
        else if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    deltaX = touch.deltaPosition.x * xSpeed * 0.005f;
                    deltaY = touch.deltaPosition.y * ySpeed * 0.005f;
                    inputDetected = true;
                }
            }
        }

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

        // Auto-align camera behind the player if moving and not dragging
        if (!isDragging && Time.time - lastDragTime > autoAlignDelay)
        {
            // Only align if the player is actually moving
            Vector3 playerVelocity = target.GetComponent<CharacterController>() != null 
                ? target.GetComponent<CharacterController>().velocity 
                : Vector3.zero;

            if (new Vector3(playerVelocity.x, 0f, playerVelocity.z).magnitude > 0.1f)
            {
                // Align camera rotation with player's rotation angle
                float targetAngle = target.eulerAngles.y;
                x = Mathf.LerpAngle(x, targetAngle, Time.deltaTime * autoAlignSpeed);
            }
        }
```

---

## 3. UI Hierarchy Configuration (To be added)

We will structure the joystick gameobjects inside the `Canvas` as follows:

```
Canvas/
└── VirtualJoystick/                       [RectTransform: Anchored Bottom-Left, size 160x160]
    │                                      [Components: Image (transparent circle), VirtualJoystick.cs]
    └── KnobHandle/                        [RectTransform: Centered, size 60x60]
                                           [Components: Image (solid circle)]
```

---

## 4. Request for Permission

Please review the plan above. If you approve, reply with **"Approved"** or **"Yes, proceed"**, and I will:
1.  Create and compile the `VirtualJoystick.cs` script.
2.  Update `ThirdPersonCharacterController.cs` and `ThirdPersonCameraController.cs` with the code above.
3.  Inject the `VirtualJoystick` UI GameObjects hierarchy into [Mob Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob%20Squad%203d%20world%20scene.unity) Canvas.
