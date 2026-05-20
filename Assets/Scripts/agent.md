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

## Project Status Analysis

### Current File Structure
```
unity.deonphizzle.stone-hammer-saw/
└── Assets/
    └── Scripts/
        ├── Test.cs (Boilerplate - empty MonoBehaviour)
        ├── Test.cs.meta
        ├── agent.md (Project documentation) ← THIS FILE
        └── agent.md.meta
```

### Implementation Status

| Component | Status | Priority |
|-----------|--------|----------|
| **DuelManager** | ❌ Not Started | 🔴 Critical |
| **SelectionManager** | ❌ Not Started | 🔴 Critical |
| **NetworkSync** | ❌ Not Started | 🔴 Critical |
| **WeaponSystem** | ❌ Not Started | 🔴 Critical |
| **TimingEngine** | ❌ Not Started | 🔴 Critical |
| **UIManager** | ❌ Not Started | 🔴 High |
| **Photon Integration** | ❌ Not Started | 🔴 Critical |
| **Player Synchronization** | ❌ Not Started | 🔴 Critical |

### Weapon Dominance Matrix (Reference)

| Weapon | Beats | Loses To |
|--------|-------|----------|
| **Mini Saw** | Mini Stone | Big Saw, Hammer |
| **Big Saw** | Mini Saw, Hammer | _(none in dominance)_ |
| **Hammer** | Mini Saw, Mini Stone | Big Saw, Big Stone |
| **Mini Stone** | _(none in dominance)_ | Mini Saw, Hammer, Big Stone |
| **Big Stone** | Hammer, Mini Stone | _(none in dominance)_ |

### Required UI Panels Reference
- `loading-panel` - Matchmaking and loading state
- `stonesawhammerpanel` - Main game duel interface
- `weapon-selectpanel` - Weapon selection timer and choices
- `CharacterPanel` - Player identity/display
- `Win/Loss panels` - Result display and progression

## Implementation Roadmap

### Phase 1: Core Networking & Synchronization
1. **NetworkSync.cs** - Photon initialization, room management, player properties
2. **TimingEngine.cs** - AFTER timing implementation with `start_at_server_ms` and `drift_ms` tracking
3. **WeaponSystem.cs** - Weapon enum and dominance logic

### Phase 2: Game Flow Logic
1. **DuelManager.cs** - Game state machine (Ready → Selection → Resolution → Results)
2. **SelectionManager.cs** - Weapon selection, 3-second timer, commit logic
3. **ResultResolver.cs** - Winner calculation with both weapon dominance and timing grading

### Phase 3: UI & Player Experience
1. **UIManager.cs** - Panel transitions and state display
2. **PlayerController.cs** - Player input handling
3. **AnimationManager.cs** - Visual feedback and animations

### Phase 4: Testing & Validation
1. Unit tests for weapon dominance logic
2. Timing accuracy tests
3. Network synchronization stress tests
4. Full game flow integration tests

## Key Technical Constraints

1. **Server-Authoritative Timing**
   - MasterClient MUST validate all weapon selections and timing
   - Client-side UI is for local feedback only
   - All final decisions made server-side to prevent cheating

2. **Synchronization Requirements**
   - Track `start_at_server_ms` as timing reference point
   - Monitor `drift_ms` to detect and compensate for network latency
   - Use Photon Custom Properties for persistent player state

3. **Photon Integration Pattern**
   - RPCs for immediate action commits (weapon selection)
   - Custom Properties for player state (ready, weapon choice, timing)
   - MasterClient authority for final result computation

4. **Timing Accuracy**
   - AFTER timing: +10ms to +250ms window from reference
   - Grading tiers determine winner quality (Perfect > Great > Good)
   - Draw scenarios (Mirror Draw or Timing Draw) must trigger results appropriately

## Coding Standards & Architecture

### Naming Conventions
- Use `Stone-Hammer-Saw` terminology consistently
- Class names follow `ManagerNameManager` pattern (DuelManager, SelectionManager)
- Network methods prefixed with `RPC_` for RPCs, `OnPhoton_` for message handlers

### Component Design
- Each manager handles one responsibility (Single Responsibility Principle)
- Use Photon `IOnPhotonSerializeView` for continuous sync when needed
- Use RPCs for discrete action confirmations

### Dependencies
- Photon PUN 2 must be installed in Unity project
- No external dependencies beyond PUN 2 and Unity standard libraries

## Next Steps (Post-Analysis)

1. **Initialize Photon Integration** - Set up PUN 2 networking layer
2. **Implement WeaponSystem.cs** - Begin with dominance logic (stateless, easily testable)
3. **Build TimingEngine.cs** - Core timing grading and comparison logic
4. **Create NetworkSync.cs** - Handle Photon room management
5. **Develop DuelManager.cs** - State machine orchestration
6. **Build UI Binding** - Connect panels to game state updates

---

**Analysis Date:** May 20, 2026  
**Status:** Project architecture defined; core implementation pending

