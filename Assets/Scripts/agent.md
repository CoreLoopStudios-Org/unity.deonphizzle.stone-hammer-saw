# Project Analysis & Guide: Destiny of the Stone-hammer-saw

This document provides a comprehensive analysis of the project's current state, codebase architecture, implemented mechanics, identified bugs, and next steps for completion.

---

## 1. Executive Summary

**Destiny of the Stone-hammer-saw** is a 1v1 competitive multiplayer duel game built in Unity. Players engage in quick-draw weapon selection duels. The project has transitioned from the initially planned **Photon PUN 2** framework to **Photon Fusion (Shared Mode)**. 

While the basic multiplayer matchmaking and round flow are implemented, there are critical architectural mismatches between the design specifications and the current codebase (most notably, the complete absence of the requested Timing Engine), as well as logic bugs in the round resolution script.

---

## 2. Project Architecture & Directory Structure

The codebase is organized into a clean MVC-like structure within the `Assets/Scripts` directory:

```
Assets/Scripts/
├── Multiplayer/
│   └── MatchmakingManager.cs        # Fusion connection, room joining & callback handling
├── Controller/
│   └── GameplayController.cs        # Duel state, network synchronization & round resolution
├── View/
│   └── UIManager.cs                 # Canvas management, panel transitions & DOTween animations
├── Test.cs                          # Empty boilerplate script
├── CODE_ARCHITECTURE_PREVIEW.md     # Deprecated: PUN 2 blueprint code
├── IMPLEMENTATION_CHECKLIST.md      # Deprecated: PUN 2 checklist
├── MULTIPLAYER_INTEGRATION_GUIDE.md # Deprecated: PUN 2 migration guide
└── game3.md                         # Deprecated: PUN 2 project analysis
```

---

## 3. Technology Stack & Framework Analysis

### Core Tech Stack
*   **Engine:** Unity
*   **Language:** C#
*   **Networking:** Photon Fusion (v2) running in **Shared Mode**
*   **Animations & UI Transitions:** DOTween (Demigiant)

### Photon Fusion (Shared Mode) vs. Deprecated PUN 2
All legacy documentation (`game3.md`, `MULTIPLAYER_INTEGRATION_GUIDE.md`, etc.) refers to Photon PUN 2. However, the active C# scripts are implemented with Photon Fusion. 

Key changes introduced by Photon Fusion:
*   `GameplayController` inherits from `NetworkBehaviour` instead of `MonoBehaviourPun` or `MonoBehaviourPunCallbacks`.
*   Scores and rounds are synchronized using `[Networked]` properties rather than Photon Custom Properties.
*   RPCs use the `[Rpc(RpcSources.All, RpcTargets.All/Proxies)]` attributes instead of `[PunRPC]`.
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
> *   `Big Saw` (1) has only one win condition (`Mini Saw`) and one loss condition (`Hammer`), leaving its interaction with `Big Stone` undefined (resulting in a draw).
> *   `Big Stone` (4) has only one loss condition (`Hammer`).

---

## 5. Implementation Status

| Component | Status | Description | Priority |
| :--- | :--- | :--- | :--- |
| **Matchmaking** | 🟢 Functional | Handled by `MatchmakingManager.cs`. Connects, joins `"StoneHammerSawRoom"`, and starts game when 2 players join. | Medium |
| **State Machine** | 🟡 Partial | `GameplayController.cs` manages round progression but relies on `Invoke` timers and direct RPCs instead of a formal state machine. | Medium |
| **UI Flow & Animations** | 🟢 Functional | `UIManager.cs` utilizes DOTween for smooth transitions between screens. | Low |
| **Timing Engine** | 🔴 Missing | The "AFTER" timing (+10ms to +250ms window with Perfect/Great/Good grading) is specified in docs but **not implemented in C#**. | 🔴 Critical |
| **Round Resolution** | 🔴 Bugged | Current implementation contains critical score and draw logic bugs. | 🔴 Critical |

---

## 6. Critical Bugs & Architectural Concerns

### 🔴 Bug 1: Draw Logic Awards Points to Player 2
In `GameplayController.cs`:
```csharp
bool iWon = DetermineWinner(myWeaponIndex, opponentWeaponIndex);

if (Object.HasStateAuthority)
{
    if (iWon) p1Score++; else p2Score++;
}
```
If both players select the same weapon, `DetermineWinner` returns `false`. Because `iWon` is false, the Master Client (who has State Authority) will execute the `else` block: `p2Score++`. This incorrectly awards a point to Player 2 on a draw.

### 🔴 Bug 2: Missing Timing Engine ("AFTER" Timing)
The legacy documentation specifies that if weapon selections result in a draw, the player with the better "AFTER" timing (within a +10ms to +250ms window) wins. This has not been translated into the Fusion implementation:
*   `GameplayController.SelectWeapon` only accepts a weapon index and does not measure elapsed time since the selection phase started.
*   No timing offset is sent over the network, making timing-based resolution impossible in the current codebase.

### 🟡 Bug 3: Hardcoded Player Scoring Roles
The score update assumes Player 1 has State Authority:
*   If Player 1 wins (`iWon == true`), `p1Score` increments. If Player 1 loses (`iWon == false`), `p2Score` increments.
*   This relies entirely on the Master Client being Player 1 and having State Authority. If authority shifts or if Player 2 were to execute this block, the score mapping would break.

---

## 7. Actionable Implementation Roadmap

### Phase 1: Bug Fixes & Refactoring (Immediate)
1.  **Fix Draw Scoring:** Modify `GameplayController.cs` to check for draws explicitly before incrementing scores.
2.  **Ensure Safe Authority Mapping:** Ensure that `p1Score` and `p2Score` increments map correctly to the Player Ref or Client ID, rather than assuming the local player's win/loss state maps directly to `p1Score`/`p2Score`.

### Phase 2: Implement the Timing Engine
1.  **Select Time Measurement:** In `GameplayController.cs` and `UIManager.cs`, track the exact millisecond timestamp when the weapon selection panel is opened.
2.  **Capture Response Time:** When a player clicks a weapon, compute the difference in milliseconds between the selection start time and the current time.
3.  **Sync Timing via RPC:** Modify `RPC_ReceiveOpponentWeapon` to accept both `weaponIndex` and `responseTimeMs`.
4.  **Integrate Timing Grades:** Create a `TimingEngine` helper to evaluate response times based on target windows:
    *   **Perfect:** $\le 10$ms
    *   **Great:** $\le 30$ms
    *   **Good:** $\le 80$ms
    *   **Bad:** $> 80$ms (or outside the $10\text{ms} - 250\text{ms}$ window)
5.  **Resolve Ties with Timing:** If weapon dominance results in a draw (or if the user requests timing-first resolution), compare the timing grades to determine the winner.

### Phase 3: Cleanup Legacy Documentation
*   Remove or archive `CODE_ARCHITECTURE_PREVIEW.md`, `IMPLEMENTATION_CHECKLIST.md`, and `MULTIPLAYER_INTEGRATION_GUIDE.md` as they reference the obsolete Photon PUN 2 setup and can confuse future development agents.

---

**Analysis Date:** May 22, 2026  
**Status:** Architecture migrated to Fusion; bug resolution and Timing Engine implementation pending.
