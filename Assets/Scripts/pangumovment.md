# Pangupops Character Movement & Combat Analysis

This document provides a detailed breakdown of the movement systems, camera relationships, animator integrations, and combat triggers configured on the **Pangupops** character (GameObject: `Pangopal_01`) in the 3D sandbox scene.

---

## 1. Character Architecture
The player character GameObject `Pangopal_01` represents the Pangolin validation rig:
*   **Mesh Renderer:** Renders the Pangolin model mesh (`pangopan`).
*   **Rig Skeletons:** Fully rigged skeleton starting at the root node `RL_BoneRoot` and utilizing bipedal bones (`CC_Base_` skeletons).
*   **Physics Component:** Uses Unity's built-in `CharacterController` component for collision and movement handling.
*   **Animation Component:** Uses the standard Unity `Animator` component to blend movement speeds and play triggers.

---

## 2. Character Movement System

The character movement is driven by the [ThirdPersonCharacterController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCharacterController.cs) script.

### 2.1 Configuration Parameters
*   **Walk Speed:** `3.0` units/sec (active during basic keyboard direction presses).
*   **Run Speed:** `6.0` units/sec (active when holding down the sprint key).
*   **Rotation Speed:** `10.0` (speed of smooth interpolation facing the movement direction).
*   **Gravity:** `9.81` units/sec² (applied downward when in the air).

### 2.2 Input Binding & Resolution
The script supports both legacy and modern Unity Input Systems:
*   **WASD & Arrow Keys:** Controls 2D direction axis (`Horizontal` and `Vertical`).
*   **Left Shift:** Toggles the running (sprinting) state.
*   **Implementation Flow:**
    ```csharp
    // 1. Read input axes
    float horizontal = Input.GetAxis("Horizontal");
    float vertical = Input.GetAxis("Vertical");
    bool isRunning = Input.GetKey(KeyCode.LeftShift);
    ```

### 2.3 Camera Perspective Alignment
Rather than moving in absolute world coordinates, movement direction is resolved relative to the camera's current perspective:
1.  Obtains the forward and right vectors of the referenced `cameraTransform` (defaulting to the main camera if null).
2.  Projects these vectors onto the horizontal XZ plane (`y = 0.0f`) and normalizes them.
3.  Calculates the target movement vector relative to the camera view:
    ```csharp
    Vector3 camForward = cameraTransform.forward;
    camForward.y = 0f;
    camForward.Normalize();

    Vector3 camRight = cameraTransform.right;
    camRight.y = 0f;
    camRight.Normalize();

    Vector3 moveDirection = (camForward * inputDir.z + camRight * inputDir.x).normalized;
    ```
4.  Smoothly rotates (`Quaternion.Slerp`) the character to face the direction of travel:
    ```csharp
    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    ```

### 2.4 Physics & Gravity Application
*   When grounded (`controller.isGrounded`), vertical velocity is locked at a stable `-0.5f` to prevent floating.
*   When airborne, gravity accumulates per frame: `verticalVelocity -= gravity * Time.deltaTime`.
*   Applies horizontal movement and vertical gravity concurrently using `CharacterController.Move(velocity * Time.deltaTime)`.

### 2.5 Animator Integration
The controller binds a parameter directly to the Animator component to manage locomotion states:
*   **Parameter:** `"Speed"` (Float)
*   **Values:**
    *   `0.0` = Idle
    *   `1.0` = Walk (when input magnitude $> 0.05$ and shift is not pressed)
    *   `2.0` = Run (when input magnitude $> 0.05$ and shift is pressed)
*   **Dampening:** Uses `animator.SetFloat("Speed", speedParam, 0.15f, Time.deltaTime)` to smoothly blend character animations (preventing sudden, twitchy animation snaps).

---

## 3. Combat Animations & Hit Detection

Locomotion is paired with attack and hit detection scripts:

### 3.1 Player Attack Trigger
The player can trigger attacks via the [PlayerAttack.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/PlayerAttack.cs) script:
*   **Input:** Left Mouse Click (`Input.GetMouseButtonDown(0)`).
*   **Action:** Triggers the `"Hit"` animation on the player's `Animator`.

### 3.2 Physics Hit Detector
The player has a weapon/attack trigger zone monitored by the [HitDetector.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/HitDetector.cs) script:
*   **Detection:** Checks for collision overlaps (`OnTriggerEnter`).
*   **Filters:** Checks if the overlapped object (or its parent) has the tag `"Victim"`.
*   **Feedback:** Finds the animator on the victim object (`other.GetComponentInParent<Animator>()`) and fires the `"FallDown"` animation trigger to make the opponent react to the hit.
