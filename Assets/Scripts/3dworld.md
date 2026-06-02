# 3D World Scene and Material Setup

This document serves as a complete reference for the 3D World scenes, environment meshes, materials, and Universal Render Pipeline (URP) settings in the project.

---

## 1. Global Render Pipeline Setup
To ensure all URP Lit materials render correctly in the Editor and at runtime, the project's global graphics settings must reference a valid render pipeline asset:
*   **Settings File:** [GraphicsSettings.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/ProjectSettings/GraphicsSettings.asset)
*   **Configuration Parameter:** `m_CustomRenderPipeline`
*   **Active URP Asset:** [New Universal Render Pipeline Asset.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/New%20Universal%20Render%20Pipeline%20Asset.asset) (GUID `74770c6bda810f04680736e1b312404c`)
*   **Active Renderer:** [New Universal Render Pipeline Asset_Renderer.asset](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/New%20Universal%20Render%20Pipeline%20Asset_Renderer.asset) (3D Forward Renderer)

---

## 2. Scene Breakdown & Environments

### Scene A: PonyPackScene (Combats Sandbox)
*   **Path:** [PonyPackScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity)
*   **Combat Ring Platform:** `Geometry/Platform/Center` uses [NEWGROUND.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/NEWGROUND.mat) (assigned with `ground_diffuse.png` and `Ground_nromal.jpeg`).
*   **Borders:** `Geometry/Platform/Borders` uses [Black.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/PonyPackScene/PonyPackScene/Black.mat).
*   **Background Environment Mesh:** `Geometry/BackgroundMesh` references [BackgroundMesh.fbx](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/PonyPackScene/PonyPackScene/BackgroundMesh.fbx) using [Ground.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/PonyPackScene/PonyPackScene/Ground.mat).
*   **Characters:** `Attacker` and `Victim` both utilize bipedal rigs rendering the pangolin mesh `pangopan`.

### Scene B: Mov Squad 3d world scene (Test Sandbox)
*   **Path:** [Mov Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Mov%20Squad%203d%20world%20scene.unity)
*   **Environment Mesh:** GameObject `background` references [background.fbx](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/background.fbx) (52 KB) mapped to:
    *   [GroundMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/GroundMaterial.mat) (`Ground_diffuse.jpeg` texture)
    *   [TerraindMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/TerraindMaterial.mat) (`terrain_diffuse.png` texture)
    *   [SkyMaterial.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20Background/SkyMaterial.mat) (`Sky.png` texture)
*   **Rigged Characters:** Includes a bipedal `pangopan` validation rig.

---

## 3. Environment Materials & Texture Registry

### 3.1 "3d World" Folder Environment Materials
These materials are configured to map the background terrain meshes in the environment assets:
*   [lambert1.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20World/lambert1.mat): Albedo uses `terrain_diffuse.png`.
*   [lambert4.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20World/lambert4.mat): Albedo uses `Ground_diffuse.jpeg` and normal map uses `Ground_normal.jpeg` (`_NORMALMAP` keyword enabled).
*   [lambert6.mat](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/3d%20World/lambert6.mat): Albedo uses `Ground_diffuse.jpeg` and normal map uses `Ground_normal.jpeg` (`_NORMALMAP` keyword enabled).

### 3.2 Texture Asset References
*   `Ground_diffuse.jpeg` (GUID `bd7e13ad3e8e2e24d9f6f2cde4dbb063`)
*   `Ground_normal.jpeg` (GUID `699cc79feab17614983934ed9f7a13c7`)
*   `terrain_diffuse.png` (GUID `ac4b2ba881fedad478009c9dcee25775`)
*   `ground_diffuse.png` (GUID `b4b21ddb8910020488d771c575017a93`)
*   `Sky.png` (GUID `55dc6d671e87d7d43832458843f5f71e`)
