# Pangupops Character Movement & Diagnostic Analysis

This document provides a detailed breakdown of the movement systems, camera relationships, animator integrations, and a technical diagnostic analysis of the left leg distortion bug in run mode.

---

## 1. Character Architecture & Components
The player character GameObject `Pangopal_01` (representing the Pangolin model `pangopan`) is configured in the scene with the following movement and animation components:
*   **`CharacterController`:** Controls collision capsule bounds and motion velocity.
*   **`Animator`:** Plays keyframed locomotion animations via the [PangolinThirdPerson.controller](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Animation/PangolinThirdPerson.controller) blend tree.
*   **`ThirdPersonCharacterController`:** A custom C# script processing WASD/Joystick inputs and calculating movement relative to camera look perspective.
*   **`ProceduralHumanoidAnimator`:** A custom procedural script designed to mathematically calculate bone rotations for breathing, spine sway, and running.

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
4.  Smoothly rotates (`Quaternion.Slerp`) the character to face the direction of travel.

---

## 3. Left Leg Run Distortion: Root Cause Analysis

When the character runs, there is visible distortion or glitches on the left leg. This behavior is caused by two overlapping technical issues:

### 3.1 Cause A: The Script-Animator Conflict (Critical)
The character GameObject `Pangopal_01` has **both** keyframed and procedural animation systems running simultaneously:
1.  **Keyframed System:** The `Animator` plays the clip [Pangu Fast Run.fbx](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Animation/Movement%20Animation/Pangu%20Fast%20Run.fbx) using Humanoid retargeting.
2.  **Procedural System:** The [ProceduralHumanoidAnimator.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ProceduralHumanoidAnimator.cs) script runs in `LateUpdate()`, completely overwriting the joint rotations of the hips, spine, arms, thighs, and calves with simple trigonometric functions.

This causes a severe conflict:
*   **Asymmetrical Default Pose Capture:** In `ProceduralHumanoidAnimator.Start()`, the script saves the "default" T-pose rotations by calling `SaveDefaultPose()`. However, Unity's `Animator` updates in the first frame *before* `Start()` executes, applying the default frame of the active locomotion clip (usually an asymmetrical Idle stance). 
*   Because of this, `leftThighDefaultRot` is saved with a skewed offset. When the script multiplies this offset by the procedural swing angle in `LateUpdate()`, the left leg rotates past its anatomical limits, causing distortion.

#### Solution:
If you are using FBX animation clips (which is the case in this scene, as the animator controller is populated with clips), you should **disable or remove** the `ProceduralHumanoidAnimator` component from `Pangopal_01`. They should not be used together.

---

### 3.2 Cause B: Humanoid Avatar Auto-Mapping Error (Secondary)
The character model `Pangopal_01` is built using the Character Creator skeleton format, which contains multiple "twist" helper bones in the extremities, e.g.:
*   `CC_Base_L_Thigh` (the primary upper leg joint)
*   `CC_Base_L_ThighTwist01` / `CC_Base_L_ThighTwist02` (deformation helpers)

In [Pangopal_01.Fbx.meta](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/characters/pongotest/Pangopal_01.Fbx.meta), the `human` mapping list is empty:
```yaml
  humanDescription:
    serializedVersion: 3
    human: []
```
This forces Unity to use **Automatic Rig Mapping** on import. 
*   **The Glitch:** Because of the similar naming conventions, Unity's auto-mapper can mistakenly bind `CC_Base_L_ThighTwist01` as the `LeftUpperLeg` humanoid bone instead of `CC_Base_L_Thigh`.
*   **Result:** When Mecanim plays the run animation, the large leg rotation angles are applied to the twist helper bone instead of the main joint. This twists the middle of the thigh mesh, causing a crumpled, collapsing distortion visible in run mode.

#### Solution:
1.  Select [Pangopal_01.Fbx](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/characters/pongotest/Pangopal_01.Fbx) in the Unity Project window.
2.  Go to the **Rig** tab in the Inspector and click **Configure...** under the Humanoid Avatar settings.
3.  Ensure the bone mappings for the **Left Leg** are assigned exactly as follows:
    *   **Left Upper Leg:** `CC_Base_L_Thigh` (not `CC_Base_L_ThighTwist01` or `Pelvis`)
    *   **Left Lower Leg:** `CC_Base_L_Calf` (not a calf twist bone)
    *   **Left Foot:** `CC_Base_L_Foot`
4.  Repeat for the right leg and click **Apply**.
