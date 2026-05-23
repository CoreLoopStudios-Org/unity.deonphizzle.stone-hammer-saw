# Project Analysis & Guide: Destiny of the Stone-hammer-saw

This document provides a comprehensive analysis of the project's current state, codebase architecture, implemented mechanics, resolved issues, remaining bugs, and next steps for completion.

---

## 1. Executive Summary

**Destiny of the Stone-hammer-saw** is a 1v1 competitive multiplayer duel game built in Unity. Players engage in quick-draw weapon selection duels. The project has transitioned from the initially planned **Photon PUN 2** framework to **Photon Fusion (Shared Mode)**. 

During the latest iteration, we resolved critical deadlocks in the network initialization flow, fixed scene loading timing bugs on the client side, replaced the old slot-machine layout with a clean vertical swiping weapon selection carousel, fixed several editor GUI crashes caused by layout dirtying, and added an automated Scroll View layout helper to format the Weapon Selection UI cards exactly to Figma specifications.

---

## 2. Project Architecture & Directory Structure

The codebase is organized into a clean MVC-like structure within the `Assets/Scripts` directory:

```
Assets/Scripts/
├── Multiplayer/
│   └── MatchmakingManager.cs        # Fusion connection, room joining & callback handling
├── Controller/
│   ├── GameplayController.cs        # Duel state, network synchronization & round resolution
│   └── SlotMachineSelector.cs       # Old slot selector logic (legacy reference)
├── View/
│   ├── UIManager.cs                 # Canvas management, panel transitions & DOTween animations
│   ├── WeaponCarouselLayout.cs      # Centered vertical weapon selection carousel layout & scrolling
│   └── WeaponCarouselItem.cs        # Tap/click detection and selection confirm for carousel items
├── Editor/
│   └── WeaponScrollSetupHelper.cs   # Editor utility to auto-configure Scroll View Content sizes
└── Deprecated Reference Files/
    ├── CODE_ARCHITECTURE_PREVIEW.md # Deprecated: PUN 2 blueprint code
    ├── IMPLEMENTATION_CHECKLIST.md      # Deprecated: PUN 2 checklist
    ├── MULTIPLAYER_INTEGRATION_GUIDE.md # Deprecated: PUN 2 migration guide
    └── game3.md                         # Deprecated: PUN 2 project analysis
```

---

## 3. Technology Stack & Framework Analysis

### Core Tech Stack
*   **Engine:** Unity 2022.3+ / 2023+ (uses modern UI/EventSystem and TMPro)
*   **Language:** C#
*   **Networking:** Photon Fusion (v2) running in **Shared Mode**
*   **Animations & UI Transitions:** DOTween (Demigiant)

### Photon Fusion (Shared Mode) vs. Deprecated PUN 2
All legacy documentation (`game3.md`, `MULTIPLAYER_INTEGRATION_GUIDE.md`, etc.) refers to Photon PUN 2. However, the active C# scripts are implemented with Photon Fusion. 
*   `GameplayController` inherits from `NetworkBehaviour` instead of `MonoBehaviourPun` or `MonoBehaviourPunCallbacks`.
*   Scores and rounds are synchronized using `[Networked]` properties rather than Photon Custom Properties.
*   RPCs use the `[Rpc(RpcSources.All, RpcTargets.Proxies)]` attributes instead of `[PunRPC]`.
*   Matchmaking is handled via a `NetworkRunner` starting a game with `GameMode.Shared`.

---

## 4. Game Mechanics Analysis

### 4.1 The 5-Weapon System
The game uses five weapons, representing an expansion of the classic Rock-Paper-Scissors triad. The weapon indices are:
*   **0:** Mini Saw
*   **1:** Big Saw
*   **2:** Hammer
*   **3:** Mini Stone
*   **4:** Big Stone

The dominance rules implemented in `GameplayController.DetermineWinner(mine, opp)` are:
*   **Mini Saw (0):** Beats Mini Stone (3). Loses to Big Saw (1), Hammer (2), Big Stone (4).
*   **Big Saw (1):** Beats Mini Saw (0). Loses to Hammer (2).
*   **Hammer (2):** Beats Mini Saw (0), Big Saw (1), Mini Stone (3). Loses to Big Stone (4).
*   **Mini Stone (3):** Beats nothing. Loses to Mini Saw (0), Hammer (2), Big Stone (4).
*   **Big Stone (4):** Beats Mini Saw (0), Mini Stone (3). Loses to Hammer (2).

> [!WARNING]
> **Dominance Asymmetry:** The matrix is mathematically imbalanced.
> *   `Mini Stone` (3) cannot win against any weapon.
> *   `Big Saw` (1) and `Big Stone` (4) have incomplete loss definitions, leaving their interactions undefined (resulting in a draw).
> *   *Recommendation:* Rebalance the matrix to ensure each weapon beats exactly two other weapons and loses to exactly two other weapons.

---

## 5. Implementation Status

| Component | Status | Description | Priority |
| :--- | :--- | :--- | :--- |
| **Matchmaking** | 🟢 Functional | Handled by `MatchmakingManager.cs`. Connects, joins rooms, and starts game when 2 players join. | High |
| **Network Initialization** | 🟢 Functional | Resolved the "Fusion is NOT READY" deadlock by adding the `Scene` parameter to `StartGameArgs` in `MatchmakingManager.cs`. | High |
| **Client Scene Loading** | 🟢 Functional | Resolved the `UIManager or GameplayController is missing` error on the client by triggering the game start flow inside `GameplayController.Spawned()` when `ActivePlayers.Count == 2`. | High |
| **Weapon Selection UI** | 🟢 Functional | Created a modern centered vertical carousel (`WeaponCarouselLayout.cs` and `WeaponCarouselItem.cs`) with swipe drag, mouse wheel increments, snapping, and tap-to-confirm selection. Patched `HomeScene.unity` to use the new scripts. | High |
| **Scroll View Layout Tool** | 🟢 Functional | Added `WeaponScrollSetupHelper.cs` in the `Editor` folder to automatically set up the Scroll View, Spacing (`20`), and size children to Figma specs (`392x240`). | Medium |
| **Editor Stability** | 🟢 Functional | Resolved PropertyEditor `NullReferenceException` and `GUIStyle without skin` warnings by checking layout properties before setting them to avoid dirtying RectTransforms recursively in Edit Mode, and ignoring updates during compilation/import. | Medium |
| **Timing Engine** | 🔴 Missing | The "AFTER" timing (+10ms to +250ms window with Perfect/Great/Good grading) is specified in docs but **not implemented in C#**. | 🔴 Critical |
| **Round Resolution** | 🔴 Bugged | Current implementation contains critical score and draw logic bugs. | 🔴 Critical |

---

## 6. Critical Bugs & Architectural Concerns (Next Focus)

### 🔴 Bug 1: Draw Logic Awards Points to Player 2
In `GameplayController.cs`:
```csharp
if (iAmMaster)
{
    if (iWon) masterScore++; else clientScore++;
}
```
If both players select the same weapon, `DetermineWinner` returns `false`. Because `iWon` is false, the Master Client (who has State Authority) will execute the `else` block: `clientScore++`. This incorrectly awards a point to Player 2 (Client) on every draw.

### 🔴 Bug 2: Missing Timing Engine ("AFTER" Timing)
The legacy documentation specifies that if weapon selections result in a draw, the player with the better "AFTER" timing (within a +10ms to +250ms window) wins. This has not been translated into the Fusion implementation:
*   `GameplayController.SelectWeapon` only accepts a weapon index and does not measure elapsed time since the selection phase started.
*   No timing offset is sent over the network, making timing-based resolution impossible in the current codebase.

### 🟡 Bug 3: Draws Show "Loss" Screen to Both Players
When a round ends in a draw:
*   `iWon` is `false` for both players.
*   `iWonCurrentRound` is set to `false` for both.
*   `ShowRoundResultPanel()` runs:
    `if (iWonCurrentRound) uiManager.ShowWinScreen(round, false); else uiManager.ShowLossScreen(round, false);`
*   Both players will see a "Loss Screen" for a draw.

---

## 7. Actionable Implementation Roadmap (Remaining Steps)

### Phase 1: High-Priority Gameplay Fixes
1.  **Correct Draw Resolution:** Modify `GameplayController.CheckRoundResult` to check for draws explicitly and prevent incrementing scores:
    ```csharp
    if (myWeaponIndex == opponentWeaponIndex) {
        // Draw - do not increment masterScore or clientScore
    }
    ```
2.  **Add Draw UI Screens:** Update `UIManager.cs` and `GameplayController.cs` to show a proper draw message (e.g. "Draw!") instead of showing a Loss screen to both players.

### Phase 2: Implement the Timing Engine
1.  **Select Time Measurement:** In `GameplayController.cs` and `UIManager.cs`, track the exact millisecond timestamp when the weapon selection panel is opened.
2.  **Capture Response Time:** When a player clicks/confirms a weapon, compute the difference in milliseconds between the selection start time and the current time.
3.  **Sync Timing via RPC:** Modify `RPC_ReceiveOpponentWeapon` to accept both `weaponIndex` and `responseTimeMs`.
4.  **Integrate Timing Grades:** Create a `TimingEngine` helper to evaluate response times based on target windows:
    *   **Perfect:** $\le 10$ms
    *   **Great:** $\le 30$ms
    *   **Good:** $\le 80$ms
    *   **Bad:** $> 80$ms (or outside the $10\text{ms} - 250\text{ms}$ window)
5.  **Resolve Ties with Timing:** If weapon dominance results in a draw, compare the timing grades to determine the winner.

---

*Analysis last updated: May 23, 2026*
