# 🎰 Slot Machine Scroller & Tool Animation Implementation Plan

## 🔍 Bug Analysis

### 1. Why does the slot machine auto-spin at game start?
In the **Mob Squad 3D world scene**, the slot machine UI is managed by the `SlotMachineScroller` script attached to the **WeaponSelect manaeger** GameObject.
* By default, the `WeaponSelect manaeger` GameObject is set to **Active** (`m_IsActive: 1`) in [Mob Squad 3d world scene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/Mob Squad 3d world scene.unity).
* In [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs#L88), the `OnEnable()` callback automatically runs `StartCoroutine(SpinRoutine())` when the GameObject becomes active. Since the object starts active on scene load, it spins immediately.

### 2. Why is it disconnected from the chest proximity trigger?
* Previously, the proximity trigger in [ChestOpeningSequence.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ChestOpeningSequence.cs) attempted to find a script named `SlotMachineManager` via `FindObjectOfType<SlotMachineManager>()`.
* However, in the 3D world scene, **there is no `SlotMachineManager` in the scene hierarchy**. The slot machine in this scene is exclusively controlled by `SlotMachineScroller`.
* Consequently, the chest proximity trigger was never communicating with the actual scroller.

### 3. The 1-Column Scroller Array Bug
* In [SlotMachineScroller.cs:L300](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs#L300), the selection logic contains a safeguard check:
  ```csharp
  if (columns == null || columns.Length <= 1) return;
  ```
  And attempts to resolve the selected weapon using the middle column `columns[1]`.
* In the 3D world scene, the scroller is configured with a **single column** (`columns.Length = 1`).
* This safeguard caused the script to return early and **do nothing** when the spin completed.

---

## 🛠️ Step-by-Step Implementation Plan

### Step 1: Prevent Auto-Spin at Scene Start
To prevent the slot machine from appearing and spinning at start, we will dynamically locate and deactivate the `WeaponSelect manaeger` GameObject inside the `Start()` method of `ChestOpeningSequence`.
```csharp
private GameObject scrollerManagerGo;

private void Start()
{
    // ... initializations ...
    scrollerManagerGo = GameObject.Find("WeaponSelect manaeger");
    if (scrollerManagerGo != null)
    {
        scrollerManagerGo.SetActive(false);
    }
}
```

### Step 2: Fix and Update `SlotMachineScroller.cs`
1. Add an event to notify observers when selection completes:
   ```csharp
   public event System.Action<int> OnWeaponSelected;
   ```
2. Modify `SelectWeapon()` to support single-column setups:
   ```csharp
   if (columns == null || columns.Length == 0) return;
   var selectCol = columns.Length > 1 ? columns[1] : columns[0];
   ```
3. Map the selected image's name (which are GameObjects named `"1"`, `"2"`, `"3"`, `"4"`, `"5"`, `"6"`) to the corresponding weapon index `0-4` using modulo arithmetic, and fire the event:
   ```csharp
   int weaponIndex = 2; // Default to Hammer (2)
   if (bestImage != null && int.TryParse(bestImage.name, out int parsedNum))
   {
       weaponIndex = (parsedNum - 1) % 5;
   }
   OnWeaponSelected?.Invoke(weaponIndex);
   ```

### Step 3: Connect Chest Proximity to the Scroller
1. In `ChestOpeningSequence.ShowWeaponSelectPanel()`, locate the inactive `WeaponSelect manaeger` GameObject using transform child search (`weaponSelectPanel.transform.parent.Find("WeaponSelect manaeger")`) to avoid failures with `FindObjectOfType`.
2. Activate the scroller manager: `scrollerManagerGo.SetActive(true)`.
3. Subscribe to its `OnWeaponSelected` event.

### Step 4: Add Delayed Activation and Hammer Attack Playback
1. When the scroller selects a weapon, trigger a coroutine:
   ```csharp
   private void OnScrollerWeaponSelected(int weaponIndex)
   {
       StartCoroutine(DelayedEquipScrollerWeapon(weaponIndex));
   }
   ```
2. In the coroutine, wait **1.0 second** (allowing the player to see the glowing, selected weapon in the slot machine), deactivate the `WeaponSelect manaeger`, and then trigger `SelectWeapon(weaponIndex)`.
3. `SelectWeapon(weaponIndex)` initiates `SpawnAndEquipWeapon(weaponIndex)`, which runs the DOTween sequence to rise from the chest, fly to the hand, and automatically trigger the character's **Attack** animation.

---

## 🔄 Interaction Flow Diagram

```mermaid
sequenceDiagram
    participant Player as Pangopal_01
    participant Box as Box (ChestOpeningSequence)
    participant Scroller as SlotMachineScroller (WeaponSelect manaeger)
    participant UI as Panel (WeoponSelect-Panel-Moiib Squad New)
    
    Note over Player,Scroller: Game starts — Scroller is deactivated immediately
    Player->>Box: Walks near Box (OnTriggerEnter)
    Box->>Box: Play opening animations & glow VFX
    Note over Box: Wait 1.0 second
    Box->>UI: SetActive(true)
    Box->>Scroller: SetActive(true) (Starts spinning)
    Note over Scroller: Spin for 4.0 - 6.5s
    Scroller->>Scroller: Decelerate & Snap
    Scroller->>Scroller: Select weapon visually (e.g. index 2 - Hammer)
    Scroller->>Box: OnWeaponSelected(2)
    Note over Box: Wait 1.0s (Show selected weapon)
    Box->>Scroller: SetActive(false)
    Box->>UI: SetActive(false)
    Box->>Box: Instantiate Hammer (from prefab)
    Box->>Player: DOTween weapon flying to CC_Base_R_Hand
    Player->>Player: Trigger Attack animator hit animation
```

---

## 🧪 Verification Plan

- [ ] Verify `WeaponSelect manaeger` is hidden and does not spin at start.
- [ ] Walk `Pangopal_01` to the Box. Verify the chest animation plays and the slot machine begins spinning.
- [ ] Let the spin stop. Verify the selected weapon is highlighted with a cyan glow pulse.
- [ ] Verify the UI remains visible for 1 second, then disappears.
- [ ] Verify the weapon (Hammer or other prefab) is instantiated, flies to the hand, and the character plays the **Attack** hit animation.

---

*Generated by Antigravity – your AI coding partner.*
