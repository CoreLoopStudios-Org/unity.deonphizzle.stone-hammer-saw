# Pungopups Party Games (Game -3) Full Game Documentation

This document contains the full Game Design Document (GDD) and specifications for **Pungopups Party Games**, compiled and localized to the project root.

---

## 1. Game Overview

**Pungopups Party Games** is a multiplayer mobile game that contains **three different mini-games inside one application**.

Each game mode is redesigned with **fun animations, weapons, and 3D Pungopups characters** to create a highly engaging, competitive experience with simple rules and quick matches.

The three core game modes are:
1.  **Stone Saw Hammer** (Inspired by Rock–Paper–Scissors)
2.  **Pony Pack** (Reaction Speed Game using Gyroscope input)
3.  **Mob Squad** (Survival Game inspired by Musical Chairs)

---

## 2. Player User Flow

### 2.1 First-Time Experience
1.  **Username Entry**: The player enters a custom username.
2.  **Pungopups Character Selection**: The player selects a character mesh from a pool of pre-built 3D Pungopups.
3.  **Confirmation**: The player profile is saved locally, and they transition to the Main Menu.

### 2.2 Main Menu
Displays the player profile details (username, character model) and offers game modes selection:
*   [Stone Saw Hammer Mode](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/HomeScene.unity)
*   [Pony Pack Mode](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity)
*   [Mob Squad Mode](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob-Squad-Scene.unity)

---

## 3. Common Gameplay Elements

### 3.1 3D Pungopups Characters
All players are represented by stylized 3D Pungopups characters equipped with:
*   Idle animations
*   Funny reaction animations
*   Attack/Striking animations
*   Victory/Defeat animations

### 3.2 Weapon Categories & Levels
Weapons are themed after real-life rock cutting tools. Each category has multiple power levels (higher level beats lower level):
1.  **Rock Tools** (e.g., Rock Level 1, Rock Level 2, etc.)
2.  **Saw Tools** (e.g., Saw Level 1, Saw Level 2, etc.)
3.  **Hammer Tools** (e.g., Hammer Level 1, Hammer Level 2, etc.)

#### 3.2.1 Special Rare Weapons
*   **Pistol** & **Rocket Launcher** appear extremely rarely in the selection pool as high-power wildcards.

### 3.3 Weapon Selection System
Weapons are selected via a fast-scrolling interface:
1.  The weapon items spin/scroll vertically at a rapid rate.
2.  The player taps the screen.
3.  The scrolling stops and snaps to the current item.
4.  The snapped item is selected. This provides a surprise factor making rounds exciting and fair.

---

## 4. Game Mode 1 — Stone Saw Hammer (Rock–Paper–Scissors Duel)

A 1v1 competitive matchmaking mode.

### 4.1 Match Start & Loop
1.  Two players join a session via matchmaking.
2.  Both players click the **Ready** button.
3.  A `3, 2, 1 - GO` countdown displays.
4.  The weapon selection scroll view starts spinning.
5.  Both players tap to stop the scroll.
6.  The characters play funny quarrel / idle animations on screen while selections are locked in.
7.  A 2-3 second battle animation plays (using procedural attacks).
8.  The winner is resolved based on the weapon levels (highest level wins).
9.  After match options: Replay Match or Return to Main Menu.

---

## 5. Game Mode 2 — Pony Pack (Reaction Speed Match)

A 1v1 reaction game utilizing mobile device stability sensors (Gyroscope).

### 5.1 Match Preparation
1.  Players connect to the match.
2.  Before clicking **Ready**, each player must place their phone on a flat surface or hold it completely still.
3.  The **Ready** button only unlocks once the device detects zero movement activity.

### 5.2 Reaction Trigger
1.  A background song plays while players wait.
2.  The music stops suddenly.
3.  Players must quickly pick up their phone. The movement is captured via the gyroscope sensor.
4.  **Early Pickup Penalty**: Picking up the phone before the music cuts out results in immediate elimination and loss.

### 5.3 Resolution
1.  If both pick up the phone correctly, a fast-scrolling weapon selection appears.
2.  If pickup timing was identical, the higher weapon level wins.

---

## 6. Game Mode 3 — Mob Squad (Musical Chairs Battle Royale)

A 5 to 8 player survival arena game.

### 6.1 Run Phase
1.  All players stand behind a **green starting line** in a 3D arena.
2.  A mystical item box is situated on a table roughly 10 meters away.
3.  Music begins, and players wait.
4.  When the music stops, players run towards the table.

### 6.2 Interaction & Weapons
1.  The fastest players to reach the box can interact with it.
2.  Interacting triggers the weapon selection scroll screen.
3.  Acquired weapons execute automatic attacks on nearby players:
    *   **Low Level Rock**: Deals minor damage to a single nearby player.
    *   **Saw**: Instantly kills a nearby player.
4.  Eliminated players drop out; remaining players step back to the green line for the next round.

---

## 7. Technical Specifications & Art Style

### 7.1 Art Style
*   **Visuals**: 3D cartoon style.
*   **Characters**: Cute, expressive Pungopups.

### 7.2 Camera Perspectives
*   **Stone Saw Hammer**: Close-up characters face-off view.
*   **Pony Pack**: Close-up duel view.
*   **Mob Squad**: High-angle top-down arena overview.

### 7.3 Game Feel
*   Fast-paced matchmaking session lengths.
*   High responsiveness.
*   Exaggerated physical reactions to victory and defeat.
