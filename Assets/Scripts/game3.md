# Project Analysis: Destiny of the Stone-hammer-saw (game3.md)

## 1. Executive Summary
"Destiny of the Stone-hammer-saw" is a competitive 1v1 multiplayer duel game built in Unity. It expands the traditional "Rock-Paper-Scissors" (Stone-Saw-Hammer) mechanic into a sophisticated 5-weapon system with server-authoritative timing and resolution logic.

## 2. Core Gameplay Mechanics
### 2.1 The 5-Weapon System
The game utilizes an expanded dominance matrix to provide more strategic depth than the base triad:
*   **Mini Saw:** Beats Mini Stone; Loses to Big Saw, Hammer.
*   **Big Saw:** Beats Mini Saw, Hammer. (No losses currently defined).
*   **Hammer:** Beats Mini Saw, Mini Stone; Loses to Big Saw, Big Stone.
*   **Mini Stone:** Loses to Mini Saw, Hammer, Big Stone. (No wins currently defined).
*   **Big Stone:** Beats Hammer, Mini Stone. (No losses currently defined).

### 2.2 Timing Engine (AFTER Timing)
The outcome of a duel is not just determined by the weapon selected, but by the **timing quality** of the selection relative to a reference start point (+10ms to +250ms window):
*   **Perfect:** ≤ 10ms
*   **Great:** ≤ 30ms
*   **Good:** ≤ 80ms
*   **Bad:** > 80ms or outside window.

### 2.3 Game Flow
1.  **Lobby:** Players connect via Photon PUN 2.
2.  **Ready Phase:** Synchronization baseline established.
3.  **Selection (3s):** Players select a weapon from the UI panel.
4.  **Resolution:** MasterClient validates timing and dominance.
5.  **Results:** Win/Loss/Draw panels displayed based on authoritative data.

## 3. Technical Architecture
### 3.1 Networking (Photon PUN 2)
*   **Model:** Server-authoritative (MasterClient logic).
*   **Sync Tools:** RPCs for actions (e.g., `RPC_CommitWeapon`), Custom Properties for state (Ready, Weapon choice).
*   **Security:** MasterClient performs final calculations to prevent client-side timing manipulation.

### 3.2 System Components (Planned)
*   **DuelManager:** Orchestrates the game state machine.
*   **SelectionManager:** Handles the 3s selection timer and local input.
*   **WeaponSystem:** Stateless logic for checking weapon dominance.
*   **TimingEngine:** Calculates and grades selection timing.
*   **ResultResolver:** MasterClient-exclusive logic for final outcome.
*   **UIManager:** Manages transitions between 5+ functional UI panels.

## 4. Current Project Status
| Component | Status |
|-----------|--------|
| Documentation | ✅ Complete (agent.md, ARCHITECTURE.md, etc.) |
| Code Blueprint | ✅ Complete (CODE_ARCHITECTURE_PREVIEW.md) |
| Functional Code | ❌ Not Started (Test.cs is empty) |
| Assets/UI | ⚠️ Defined in docs, implementation pending |

## 5. Critical Analysis & Observations
### 5.1 Logic Bug Identified
The preview code in `ResultResolver.cs` currently has a comparison error in `p1Grade.CompareTo(p2Grade) > 0`. Since `Perfect` is the first element in the enum (index 0), a *better* grade has a *lower* numerical value. The current logic would incorrectly reward the player with the *worse* grade.

### 5.2 Balancing Concerns
The 5-weapon dominance matrix is currently asymmetrical:
*   **Mini Stone** has no dominance over any other weapon.
*   **Big Saw** and **Big Stone** have no losses defined in the documentation.
*   *Recommendation:* Revisit the dominance matrix to ensure every weapon has at least one win and one loss condition to maintain competitive balance.

### 5.3 Timing Window
The 250ms "AFTER" window is extremely tight for human reaction. It requires high precision and might be influenced by network jitter despite drift compensation logic.

## 6. Next Phase: Implementation Roadmap
1.  **Environment Setup:** Import Photon PUN 2 and configure App ID.
2.  **Core Systems:** Implement `WeaponSystem` and `TimingEngine` (unit-testable logic).
3.  **Networking:** Build `NetworkSync` for room management.
4.  **Game Loop:** Implement `DuelManager` to connect the phases.
5.  **UI Binding:** Connect existing UI panels to the managers.

---
*Analysis generated on May 21, 2026*
