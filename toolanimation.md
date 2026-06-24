# 🎰 Slot Machine & Tool Animation Bug Fix Plan

This document analyzes the animation timing and UI persistence bugs in the **Mob Squad 3D world scene**, outlines the root causes, and proposes a step-by-step resolution plan.

---

## 🔍 Bug Analysis & Root Causes

### 1. Why does the hammer animation play BEFORE the spin selected tool shows?
* **The Premature Timer:** In [ChestOpeningSequence.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ChestOpeningSequence.cs), when the panel opens, a countdown timer (`StartCountdownTimer()`) is started with a hardcoded limit of **5.0 seconds**.
* **The Spin Duration:** In [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs), the slot machine spins for a random time of **4.0 to 6.5 seconds**, followed by a staggered deceleration and snapping sequence that takes **1.4 seconds** (1.0s to slow down and 0.4s to snap/delay). Thus, the minimum spin duration is **5.4 seconds** and can go up to **7.9 seconds**.
* **The Conflict:** Because the countdown timer (5.0s) is shorter than the spin duration (5.4s - 7.9s), the countdown timer always expires *before* the spin completes. It automatically calls `SelectWeapon(2)` (Hammer), which spawns the hammer and triggers the player's `Attack` animation while the slot machine is still spinning.
* **Proximity Attack Trigger:** Tapping the screen to stop the spin may also trigger manual attack inputs from control scripts if inputs are not locked.

### 2. Why does the selected tool UI stay on the screen forever?
* **The Reparenting Issue:** In [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs#L350), when a weapon is selected, the image (`bestImage`) is reparented to the `Canvas` (via `bestImage.SetParent(this.transform.parent, true)`) so it can center and scale up.
* **Missing Cleanup:** When [ChestOpeningSequence.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ChestOpeningSequence.cs) disables the slot machine manager after 1.0 second, the selected image remains on the screen because it is now a direct child of the `Canvas` and is never reparented back or cleaned up on disable.

---

## 🛠️ Step-by-Step Resolution Plan

### Step 1: Prevent the Premature Countdown Timer
Modify [ChestOpeningSequence.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Controller/ChestOpeningSequence.cs) to skip starting the `StartCountdownTimer()` if the panel is the new slot machine panel (`isNewPanel == true`). The slot machine completes its spin and calls `SelectWeapon` automatically, so no forced countdown is needed.
```csharp
// Inside ChestOpeningSequence.cs: ShowWeaponSelectPanel()
if (!isNewPanel)
{
    if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
    countdownCoroutine = StartCoroutine(StartCountdownTimer());
}
```

### Step 2: Auto-Clean and Hide the Selected Tool UI on Disable
Modify [SlotMachineScroller.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/SlotMachineScroller.cs) to call `ResetSelection()` directly inside the `OnDisable()` callback. This ensures that when the scroller manager is deactivated (1 second after selection), the scaled-up tool image is automatically reparented back to its original slot column, making it inactive and clean.
```csharp
// Inside SlotMachineScroller.cs
void OnDisable()
{
    StopAllCoroutines();
    isWholeMachineSpinning = false;
    
    foreach (var col in columns)
    {
        if (col != null)
        {
            col.currentSpeed = 0f;
            col.isSpinning = false;
        }
    }
    
    if (arrowSequenceTween != null) arrowSequenceTween.Kill();
    DOTween.Kill("GlowPulse");
    
    // Automatically reset parent and clean up UI elements when disabled
    ResetSelection();
}
```

### Step 3: Temporary Input Lock (Optional Safety)
Ensure the player character's controller input is ignored or disabled while the slot machine is spinning or selection is active, preventing manual attack triggers.

---

## 🔄 Corrected Interaction Flow

1. **Chest Open:** Player walks to the chest, lid opens, particles/effects play.
2. **Spin Start:** After 1.0s, the slot machine UI appears and starts spinning.
3. **Spin Stop:** The columns decelerate, snap, and display the selected tool in the center of the screen with a cyan glow.
4. **1-Second Delay:** The selected tool is displayed for exactly 1.0 second.
5. **Tool Deactivation & Equip:** After 1.0s, the scroller manager is deactivated. The selected UI tool disappears automatically because of `ResetSelection()` on disable.
6. **Animation Play:** The physical weapon (e.g. Sledgehammer) is instantiated, flies to the character's hand, and triggers the `Attack` animation on the character's animator upon reaching the hand.

---

## 🧪 Verification Plan

- [ ] Verify that no countdown timer appears or fires prematurely during the slot machine spin.
- [ ] Verify that tapping the screen to stop the spin does not trigger the character's attack animation.
- [ ] Verify that when the spin ends, the selected weapon icon is scaled up and glows for exactly 1.0 second.
- [ ] Verify that after 1.0 second, the selected UI icon disappears completely from the screen.
- [ ] Verify that the physical hammer is instantiated, flies to the hand, and triggers the hit animation only after reaching the hand.
