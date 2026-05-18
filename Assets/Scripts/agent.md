# Project: Destiny of the Stone-hammer-saw

## Overview
A competitive multiplayer Unity game based on the "Stone Saw Hammer" (Sequence 1) confrontation style. Players engage in quick-draw duels where timing and weapon selection determine the outcome.

## Technology Stack
- **Engine:** Unity
- **Language:** C#
- **Multiplayer:** Photon PUN 2 (MasterClient, RPCs, Custom Properties)
- **Networking Model:** Server-authoritative timing (MasterClient handles result resolution).

## Project Structure (Accessible)
- `Assets/Scripts/`: Main gameplay and networking logic.
  - `Test.cs`: Initial boilerplate.
  - `agent.md`: This project guide.

## Gameplay Mechanics
### Flow
1. **Matchmaking:** Two players connect via Photon and enter a room.
2. **Ready Phase:** Players enter a ready state; synchronization baseline established.
3. **Weapon Selection:** A 3-second timer starts. Players must select one of 5 tools from the `Weapon-Select-Panel`.
4. **Action/Commit:** Selection is committed. Timing is recorded (Server-authoritative).
5. **Resolution:** MasterClient calculates the winner based on weapon dominance and timing quality.
6. **Results:** Winner/Loss/Draw panels are displayed.

### Weapon Dominance (User Specified)
The current implementation uses 5 tools with the following logic:
- **Mini Saw:** Beats Mini Stone; Loses to Big Saw, Hammer.
- **Big Saw:** Beats Mini Saw, Hammer.
- **Hammer:** Beats Mini Saw, Mini Stone; Loses to Big Saw, Big Stone.
- **Mini Stone:** Loses to Mini Saw, Hammer, Big Stone.
- **Big Stone:** Beats Hammer, Mini Stone.

*Note: Canonical documentation (PDFs) suggests a core triangle (Hammer > Stone > Saw > Hammer). We are currently following the user-provided 5-tool expansion.*

### Timing Engine (Canonical Specifications)
- **Sequence 1 (Stone Saw Hammer):** Uses "AFTER" timing (+10ms to +250ms relative to valid reference).
- **Grading:** 
  - Perfect: <= 10ms
  - Great: <= 30ms
  - Good: <= 80ms
- **Draw Logic:** Mirror Draw (same weapon + timing) or Timing Draw (diff weapon + timing within epsilon).

## Technical Requirements
- **Synchronization:** Implement `start_at_server_ms` and `drift_ms` monitoring to ensure fairness.
- **State Management:** Use Photon RPCs for commit actions and Custom Properties for player states.
- **Validation:** MasterClient MUST validate all timing and weapon outcomes.
- **UI Integration:** Connect functional logic to `loading-panel`, `stonesawhammerpanel`, `weapon-selectpanel`, `CharacterPanel`, and `Win/Loss` panels.

## Agent Instructions
- Prioritize server-authoritative logic to prevent cheating.
- Maintain consistency with the "Destiny of the Stone-hammer-saw" canonical naming.
- Ensure smooth UI transitions between the lobby and game scenes.
- Follow component-based design for script modularity (e.g., `DuelManager`, `SelectionManager`, `NetworkSync`).
