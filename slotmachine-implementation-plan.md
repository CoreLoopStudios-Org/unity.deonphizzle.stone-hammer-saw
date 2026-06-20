# Slot Machine Implementation Plan

This document outlines the visual analysis, scene architecture, and step-by-step technical plan to upgrade the weapon selection slot machine in the **Mob Squad 3D World Scene** using **DOTween** animations, a 5-second timer, arrow animations, and single-weapon selection VFX.

---

## 1. Visual Analysis of Slot Machine Animation
Based on the keyframe storyboard of [ei_slot_maching_ta_colum_move.mp4](file:///C:/Users/User/Downloads/ei_slot_maching_ta_colum_move.mp4):

### 1.1 Column Scrolling & Deceleration
*   **Spin Start**: Clicking **ACTIVATE** ignites orange/yellow electrical border lights and starts rapid, infinite vertical downward scrolling across all 3 columns.
*   **Spin Duration**: The spin runs at constant maximum speed for a set period.
*   **Staggered Sequential Stop**: The columns stop one by one to build anticipation (Column 1 stops first, then Column 2, then Column 3).
*   **Deceleration**: As each column stops, it decelerates smoothly rather than stopping abruptly, snapping perfectly into vertical grid alignment.

### 1.2 Top Arrow Animation
*   Three blue neon arrows pointing downwards are located at the top of the interface (`Upper-Bg`).
*   During idle and spin, they animate sequentially downwards in a pulsing wave (Left $\rightarrow$ Mid $\rightarrow$ Right) to draw focus and indicate scroll direction.

### 1.3 Weapon Selection & VFX
*   **Target Selection**: When all columns stop, the middle row is evaluated, and one item is selected (the middle item of the grid, i.e., Column 2, Row 2).
*   **Isolation Pop-Up**: The chosen weapon is highlighted with an orange border. The rest of the slot machine dims out behind a dark screen overlay.
*   **VFX Explosion**: The selected weapon scales up significantly, moves forward (z-axis/foreground), and rotates. A cyan electrical discharge/glow is emitted around it, confirming selection. The activate button text updates to "SET".

---

## 2. Analysis of the Mob Squad 3D World Scene
Based on the inspection of [Mob Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob%20Squad%203d%20world%20scene.unity):

*   **Scene Components**:
    *   **3D Character**: `Pangopal_01` runs the `ThirdPersonCharacterController` script (configured for walking, running, and orienting relative to camera directions).
    *   **Main Camera**: Attached directly to the player character transform, resulting in rigid tracking.
    *   **Orientation**: Runs the `SceneOrientationController` script to force Portrait screen mode.
    *   **3D Background**: Mesh `background` using URP Lit materials (`GroundMaterial`, `TerraindMaterial`, `SkyMaterial`).
*   **Slot Machine UI Panel** (`WeoponSelect-Panel-Moiib Squad New`):
    *   Contains three sibling columns: `HeadlineFContainer` (Left Column), `HeadlineFContainer (1)` (Middle Column), and `HeadlineFContainer (2)` (Right Column).
    *   Each column contains 9 slot images (RectTransforms).
    *   Currently, each column GameObject has its own copy of the [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs) component, driving scrolling individually.

---

## 3. Technical Implementation Plan

### Step 1: Script Refactor ([SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs))
We will modify the script to support a manager structure, a 5-second timer, DOTween ease-in/out speeds, sequential stopping, snapping, and selection VFX.

#### 1.1 Structural Changes (Grouping Columns)
We will define a serialized class representing individual columns:
```csharp
[System.Serializable]
public class SlotColumn
{
    public string columnName;
    public RectTransform[] slotImages;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public Vector2[] initialPositions;
    [HideInInspector] public bool isSpinning;
}
```

#### 1.2 Core Scrolling Math Preservation
The update logic will loop through each column and apply the exact original translation and wrapping math:
```csharp
column.slotImages[i].anchoredPosition += Vector2.down * column.currentSpeed * Time.deltaTime;
if (column.slotImages[i].anchoredPosition.y <= bottomThreshold)
{
    float overshoot = bottomThreshold - column.slotImages[i].anchoredPosition.y;
    column.slotImages[i].anchoredPosition = new Vector2(
        column.slotImages[i].anchoredPosition.x, 
        resetPositionY - overshoot
    );
}
```

#### 1.3 Timer & Deceleration Logic
*   Add a public method `StartSpin()` to initiate the sequence.
*   Upon initiation:
    1. Animate the scroll speeds of all columns from `0` to the target `scrollSpeed` using `DOTween.To` over 0.5s.
    2. Start a 5-second countdown timer.
    3. During the 5-second duration, columns scroll continuously.
    4. At the end of the timer, trigger a stopping sequence with a stagger (e.g. stop Column 1, wait 0.5s, stop Column 2, wait 0.5s, stop Column 3).

#### 1.4 Perfect Snapping Math
To ensure the images align perfectly with the grid upon stopping, we calculate the wrap-aware shift required to bring the nearest image back to its design-time spacing:
```csharp
float L = resetPositionY - bottomThreshold;
float spacing = L / column.slotImages.Length;
float currentY = column.slotImages[0].anchoredPosition.y;
float initialY = column.initialPositions[0].y;

float diff = currentY - initialY;
float wrappedDiff = Mathf.Repeat(diff + L / 2f, L) - L / 2f;
float k = Mathf.Round(wrappedDiff / spacing);
float targetDiff = k * spacing;
float shift = targetDiff - wrappedDiff; // Offset to add to all images to align them
```
We will decelerate the column speed to `0`, then play a DOTween float transition to smoothly shift all images by `shift` using `Ease.OutBack` to create a realistic slot bounce.

---

### Step 2: Top Arrow Animation inside `Upper-Bg`
*   Add references to the 3 child RectTransforms of `Upper-Bg` (`Arrow Left`, `Arrow Mid`, `Arrow Right`).
*   Implement a looping DOTween sequence:
    *   Pulse the scale of the arrows sequentially (Left $\rightarrow$ Mid $\rightarrow$ Right).
    *   Fade the color transparency down and reset to create a wave-like flowing movement.

---

### Step 3: Selection Pop-up & Dimming VFX
*   Identify the selected weapon (e.g. the image in the center position of Column 2).
*   Create a dynamic fullscreen overlay GameObject (`DimOverlay`) with a dark color, fading its transparency from `0` to `0.75` using `DOColor` to dim out the rest of the board.
*   Dynamically add a Canvas component to the selected weapon's GameObject at runtime, setting `overrideSorting = true` and `sortingOrder = 101` (placing it in front of the dim overlay).
*   Tween the scale of the selected weapon from `1.0` to `1.8` using `DOScale` with `Ease.OutBack`.
*   Play a pulsing scale loop (`1.8` to `2.0` and back) and toggle a glowing border/frame effect around it.

---

## 4. Scene Modifications Workflow
1.  Open the scene [Mob Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob%20Squad%203d%20world%20scene.unity).
2.  Create a new empty GameObject named `WeaponSelect manager`.
3.  Attach the updated `SlotMachineScroller` script component to `WeaponSelect manager`.
4.  Remove the old `SlotMachineScroller` script component from the three columns:
    *   `HeadlineFContainer`
    *   `HeadlineFContainer (1)`
    *   `HeadlineFContainer (2)`
5.  Configure the `WeaponSelect manager` Inspector:
    *   Set the size of the `Columns` array to 3.
    *   Add references to `slotImages` for each of the three column containers.
    *   Reference the 3 arrow gameobjects inside the `Upper-Bg` panel.
6.  Save the scene.

---

## 5. Risk Assessment & Verification Plan
*   **Compile Safety**: Ensure `DG.Tweening` namespace is fully imported and there are no compilation errors.
*   **Scroll Integrity**: Verify that the wrap-around coordinates (`bottomThreshold` and `resetPositionY`) match the default values in the scene.
*   **Asset References**: Ensure the original image references inside the scene are preserved and not lost during script attachment.
