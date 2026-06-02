# Scene Analysis: PonyPackScene.unity

This document provides a detailed breakdown of the hierarchy, objects, characters, and UI elements found within the `PonyPackScene` in the project.

---

## 1. Scene Location & Resources

*   **Scene File:** [PonyPackScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity)
*   **Asset Folder:** [Assets/PonyPackScene/](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/PonyPackScene) contains materials (`Ground.mat`, `Gold.mat`, `Black.mat`), light data, and environment mesh FBX models (`BackgroundMesh.fbx`, `SamplesSpotlightModel.fbx`).
*   **Character Model:** Both characters in the scene use the `pangopan` model referencing `Pangopal_01.Fbx` inside [Assets/3d/characters/pongotest/](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/3d/characters/pongotest).

---

## 2. Root GameObject Hierarchy

Our scene parsing reveals the following root hierarchy in [PonyPackScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity):

```
├── Lighting/                      # Lighting setup
│   ├── Adaptive Probe Volume
│   ├── CeilingLight/
│   │   └── Spot Light
│   ├── BackLight/
│   │   └── SamplesSpotlight/
│   │       └── Spot Light
│   ├── Reflection Probe
│   ├── KeyLight/
│   │   └── Tripod_Neck/
│   │       └── Spotlight_Head/... (Wings, Lens, Spot Light)
│   └── FillLight/
│       └── SamplesSpotlight/
│           └── Spot Light
├── Canvas/                        # Scene-local overlay UI
│   ├── Loss (2)                   # Loss panel
│   │   ├── Round text. (1)
│   │   └── Image (1)
│   ├── Tap (1)                    # Instructions
│   │   └── Text (TMP)
│   ├── Win (2)                    # Win panel
│   │   ├── Round text.
│   │   └── Next-Round-Button
│   └── Put (1)                    # Instructions
│       └── Text (TMP)
├── EventSystem
├── Main Camera
├── Geometry/                      # Environment 3D models
│   ├── Platform/                  # The fighting ring
│   │   ├── Center
│   │   └── Borders
│   └── BackgroundMesh             # Surrounding background mesh
├── ProbeVolumePerSceneData
├── StaticLightingSky
├── Attacker/                      # Left Character Rig
│   ├── RL_BoneRoot/... (Waist, Pelvis, Hip bones)
│   └── pangopan                   # Pangolin Mesh
├── Victim/                        # Right Character Rig
│   ├── RL_BoneRoot/... (Waist, Pelvis, Hip bones)
│   └── pangopan                   # Pangolin Mesh
└── Volume Profile
```

---

## 3. Key Observations & Findings

### 3.1 Combat Arena Environment
*   The scene is configured as a 3D battle arena setup.
*   **Platform & Geometry:** The battle takes place on a central ring platform (`Platform`) surrounded by structural borders and illuminated by dedicated spotlight props (`CeilingLight`, `KeyLight`, `FillLight`, and `BackLight`).
*   **Post Processing:** Features URP post-processing via the `Volume Profile` and baked probe volume structures (`Adaptive Probe Volume`, `ProbeVolumePerSceneData`).

### 3.2 Characters (`Attacker` vs. `Victim`)
*   Two rigged characters are placed directly in the scene: **Attacker** (the player character) and **Victim** (the opponent/dummy character).
*   Both objects are fully rigged with bones under `RL_BoneRoot` (`CC_Base_` skeletons) and render the `pangopan` mesh (representing the `Pangopal_01` pangolin model).

### 3.3 UI Overlay Layout
*   The scene contains its own local screen overlays (`Canvas`) for reporting results (Win, Loss, Tap, Put).
*   However, these overlays are basic panels and **do not** feature matchmaking or lobbies.

### 3.4 Missing Architecture (Sandbox Scene)
*   **No Active Controllers:** [PonyPackScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity) does **not** contain the gameplay manager scripts like `GameplayController`, `UIManager`, or `MatchmakingManager`.
*   These crucial multiplayer orchestrators are only instantiated in the [HomeScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/HomeScene.unity).
*   **Conclusion:** This indicates that `PonyPackScene.unity` functions as a **specialized combat/animation sandbox** containing the 3D meshes, rigs, and lights. Under the complete multiplayer flow, players connect via [HomeScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/HomeScene.unity) (which holds the matchmaking, matchmaking UI panels, and player input setup), and this duel arena scene is loaded to execute the combat animations.
