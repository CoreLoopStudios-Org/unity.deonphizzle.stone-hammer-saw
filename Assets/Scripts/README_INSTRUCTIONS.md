# Destiny of the Stone-hammer-saw

A competitive multiplayer Unity game where players engage in high-pressure quick-draw duels. This project implements a server-authoritative timing system and a custom 5-weapon "Rock-Paper-Scissors" selection mechanic.

## Prerequisites
- **Unity 2022.3+** (or compatible version)
- **Photon PUN 2** (Free or Plus) imported from the Unity Asset Store.
- **TextMeshPro** (Essentials imported).

## Setup Instructions

### 1. Photon Configuration
- Go to `Window > Photon Unity Networking > PUN Wizard`.
- Setup your **AppId** from the Photon Dashboard.

### 2. Scene Setup
1. **Create a "Managers" Object:**
   - Create an empty GameObject in your scene named `Managers`.
   - Attach the following components:
     - `NetworkManager`
     - `DuelManager`
     - `PhotonView`
2. **Setup UI Controller:**
   - Attach the `DuelUIController` script to your main **Canvas**.
   - In the Inspector, drag and drop your UI panels into the corresponding slots:
     - `Loading Panel`
     - `Weapon Select Panel`
     - `Win Panel`
     - `Loss Panel`
     - `Draw Panel` (Create one if not already present)
3. **Link Managers:**
   - On the `DuelManager` component, drag the Canvas (with `DuelUIController`) into the `Ui Controller` field.

### 3. Button Configuration
For each button in your `Weapon-Select-Panel`, add an `OnClick` event:
- **Target:** The GameObject with `DuelUIController`.
- **Function:** `DuelUIController.OnWeaponSelected`.
- **Parameter (Int):**
  - `1`: Mini Saw
  - `2`: Big Saw
  - `3`: Hammer
  - `4`: Mini Stone
  - `5`: Big Stone

## How to Play
1. **Connect:** Launch the game (or two builds). The `NetworkManager` will automatically connect to Photon and join a room.
2. **Matchmaking:** Once two players are in the room, the `DuelManager` will trigger the duel start.
3. **Selection:** The `Weapon-Select-Panel` will appear. You have **3 seconds** to pick a tool.
4. **Resolution:** After both players select (or time runs out), the MasterClient calculates the winner.
5. **Result:** The Win, Loss, or Draw panel is displayed based on the outcome.

## Win Logic Matrix
The outcome is determined by the following dominance rules:

| Weapon | Beats | Loses To |
| :--- | :--- | :--- |
| **Big Saw** | Mini Saw, Hammer | Big Stone (Draws Big Saw) |
| **Hammer** | Mini Saw, Mini Stone | Big Saw, Big Stone |
| **Big Stone** | Hammer, Mini Stone | (Draws Big Stone) |
| **Mini Saw** | Mini Stone | Big Saw, Hammer |
| **Mini Stone** | - | Mini Saw, Hammer, Big Stone |

## Technical Notes
- **Server-Authoritative:** Results are calculated only on the MasterClient to ensure synchronization and fairness.
- **RPC Based:** Player actions are sent to the MasterClient via `RPC_SubmitSelection`.
- **Expandable:** You can modify the `Beats` method in `DuelManager.cs` to add more complex weapon interactions.
