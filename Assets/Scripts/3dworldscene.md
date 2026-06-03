# 3D World Scene Documentation

This document provides a comprehensive reference for the 3D World scenes, hierarchies, custom character controllers, cameras, material bindings, and rendering configurations in the project.

---

## 1. Global Graphics & Rendering Architecture
To ensure URP Lit materials render correctly in both the Unity Editor and at runtime, the project utilizes the following graphics configuration:
*   **Active Custom Render Pipeline:** [New Universal Render Pipeline Asset.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/New%20Universal%20Render%20Pipeline%20Asset.asset) (GUID `74770c6bda810f04680736e1b312404c`)
*   **Active Renderer Setup:** [New Universal Render Pipeline Asset_Renderer.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/New%20Universal%20Render%20Pipeline%20Asset_Renderer.asset) (3D Forward Renderer)
*   **Global Graphics Configuration:** References the pipeline asset in [GraphicsSettings.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/ProjectSettings/GraphicsSettings.asset) under the `m_CustomRenderPipeline` parameter.

---

## 2. Sandbox Scene Comparison

There are two duplicated test scenes in the repository with spelling variations. They differ significantly in how the main camera system is rigged and configured:

### 2.1 Scene A: Mob Squad 3D World Scene
*   **Path:** [Mob Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob%20Squad%203d%20world%20scene.unity)
*   **Camera Integration:** The `Main Camera` is hard-parented directly to the character controller `Pangopal_01`. It **lacks** any camera control or orbit scripts.
*   **Behavior:** The camera moves rigidly with the character, preventing any free-look or mouse-driven orbit control.

### 2.2 Scene B: Mov Squad 3D World Scene
*   **Path:** [Mov Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Mov%20Squad%203d%20world%20scene.unity)
*   **Camera Integration:** The `Main Camera` is an independent root node in the scene tree and is controlled dynamically by the [ThirdPersonCameraController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCameraController.cs) script.
*   **Behavior:** The camera orbits smoothly around `Pangopal_01` based on mouse axis input and supports zoom scrolling.

---

## 3. Core Component Inspector Settings

### 3.1 Player Character Controller (`Pangopal_01`)
In both scenes, the character rig `Pangopal_01` runs the [ThirdPersonCharacterController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCharacterController.cs) script, configured with:
*   **Walk Speed:** `3.0`
*   **Run Speed:** `6.0`
*   **Rotation Speed:** `10.0`
*   **Gravity:** `9.81`
*   **Camera Reference (`cameraTransform`):** Bound to the `Main Camera` transform (`fileID: 961739753`). This allows WASD movement directions to orient dynamically relative to the direction the camera is facing.

### 3.2 Orbit Camera Controller (`Main Camera` in Mov Squad Scene)
Controlled by the [ThirdPersonCameraController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ThirdPersonCameraController.cs) script with the following configurations:
*   **Target:** `Pangopal_01` (`fileID: 4931594387397547289`)
*   **Offset Height:** `1.5`
*   **Distance:** `4.0` (Min: `2.0`, Max: `10.0`)
*   **X/Y Speed Sensitivity:** `120.0`
*   **Zoom Speed:** `2.0`
*   **Vertical Tilt Constraints:** Min `-20.0` to Max `60.0` degrees
*   **Smooth Time:** `0.12`

### 3.3 Screen Orientation Control
Both scenes run [SceneOrientationController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/SceneOrientationController.cs) attached to `OrientationController`. This locks the game screen to `LandscapeLeft` on launch and restores it to `Portrait` when exiting the sandbox.

---

## 4. Scene Environment & Material Registry

The 3D landscape environment consists of a mesh GameObject (`background`) using [background.fbx](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/background.fbx) (52 KB). This mesh is mapped to three primary materials:
1.  **Ground Terrain:** [GroundMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/GroundMaterial.mat) (bound to `Ground_diffuse.jpeg`)
2.  **Far Terrain:** [TerraindMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/TerraindMaterial.mat) (bound to `terrain_diffuse.png`)
3.  **Sky Box:** [SkyMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/SkyMaterial.mat) (bound to `Sky.png`)

### 4.1 Underlying Asset Registry (GUIDs)
*   **`Ground_diffuse.jpeg`:** `bd7e13ad3e8e2e24d9f6f2cde4dbb063`
*   **`Ground_normal.jpeg`:** `699cc79feab17614983934ed9f7a13c7`
*   **`terrain_diffuse.png`:** `ac4b2ba881fedad478009c9dcee25775`
*   **`Sky.png`:** `55dc6d671e87d7d43832458843f5f71e`
