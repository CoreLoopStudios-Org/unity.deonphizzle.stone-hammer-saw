---
name: stone-hammer-saw
description: Coordinates tasks in the Destiny of the Stone-hammer-saw Unity project, including gameplay logic, Photon Fusion networking, DOTween animations, and procedural animation updates.
---

# Destiny of the Stone-hammer-saw Skill

This skill guides agents when editing, refactoring, debugging, or extending the **Destiny of the Stone-hammer-saw** Unity project.

---

## 1. Core Architecture & Stack

- **Unity & Editor Automation**: Uses custom Unity skills via the Unity Editor REST API (running on port `8090`).
- **Networking**: Photon Fusion (v2) in **Shared Mode**. Key states, score synchronization, and actions must utilize networked properties (`[Networked]`) and RPCs (`[Rpc(RpcSources.All, RpcTargets.StateAuthority)]`).
- **Animations**: Driven procedurally using DOTween rotations and custom joint tracking. Do not rely on conventional Animator clips for combat/movement animation overrides.

---

## 2. Codebase Index & Core Scripts

- **Matchmaking**: [MatchmakingManager.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Multiplayer/MatchmakingManager.cs) manages Fusion sessions.
- **Gameplay**: [GameplayController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/GameplayController.cs) handles round mechanics, score checking, and weapon resolution.
- **Animations**: [ProceduralHumanoidAnimator.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ProceduralHumanoidAnimator.cs) (locomotion) and [DOTweenCombatController.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/DOTweenCombatController.cs) (combat strikes).
- **UI**: [UIManager.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/View/UIManager.cs) and [SlotMachineManager.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/SlotMachineManager.cs).

---

## 3. Gameplay Mechanics & Weapon Dominance Matrix

The game maps weapons to indices `0-4`:
- `0`: Mini Saw
- `1`: Big Saw
- `2`: Hammer
- `3`: Mini Stone
- `4`: Big Stone

### Resolution Logic (Current Asymmetric Design)
- **Mini Saw (0)** wins over Mini Stone (3). Loses to Big Saw (1), Hammer (2), Big Stone (4).
- **Big Saw (1)** wins over Mini Saw (0). Loses to Hammer (2).
- **Hammer (2)** wins over Mini Saw (0), Big Saw (1), Mini Stone (3). Loses to Big Stone (4).
- **Mini Stone (3)** loses to all items (wins against nothing).
- **Big Stone (4)** wins over Mini Saw (0), Mini Stone (3). Loses to Hammer (2).

> [!WARNING]
> Matchups such as `Big Saw` vs. `Big Stone` default to a Draw. Keep this imbalance in mind unless refactoring it to a symmetric 5-weapon system (where each beats exactly two weapons and loses to two).

---

## 4. Development & Refactoring Workflow

1. **Graph Exploration**: Always run `detect_changes`, `query_graph`, or `semantic_search_nodes` before reading files.
2. **Unity Connection**: Verify Editor connection via `editor_get_state` or `debug_check_compilation` before submitting visual tweaks.
3. **Photon Fusion Syncing**: Make sure changes to gameplay state are wrapped in Photon Fusion network properties and synchronized properly across master/client instances.
