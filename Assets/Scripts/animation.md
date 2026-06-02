# Custom DOTween Combat Animation Specification

This document details the step-by-step specifications for the custom 4-stage DOTween combat sequence between the Attacker and the Victim.

---

## 1. Exposed Inspector Fields (`[SerializeField]`)

To configure the animation parts in the Unity Editor:
1.  **Attacker Torso/Spine (`Transform`)**: To rotate the upper body for a wind-up and forward punch twist.
2.  **Attacker Hand/Fist (`Transform`)**: The right hand bone that delivers the punch.
3.  **Victim Head (`Transform`)**: The target bone for the impact.
4.  **Victim Body/Root (`Transform`)**: The root of the Victim to handle the push-back displacement.

---

## 2. Animation Sequence Stages

```
   [CLICK]
      │
      ├── (Step 1: Wind-up/Anticipation) ~0.25s / Ease.OutQuad
      │     ├── Torso rotates backward (-25 degrees around Y-axis)
      │     └── Hand pulls back slightly
      │
      ├── (Step 2: The Strike) ~0.12s / Ease.InQuad
      │     ├── Torso violently rotates forward (+38 degrees around Y-axis)
      │     └── Hand moves instantly (DOMove) to Victim's Head position
      │
      ├── (Step 3: Impact & Recoil) [Callback on Hit]
      │     ├── Camera Shake (0.2s)
      │     ├── Victim Head snaps back via DOPunchRotation
      │     └── Victim Body stumbles back via DOMove
      │
      └── (Step 4: Reset to Idle) ~0.3s / Ease.OutCubic
            ├── Torso returns to original local rotation
            └── Hand returns to original local position & rotation
```

---

## 3. Implementation Script: `DOTweenCombatController.cs`

Below is the fully refactored script adhering to safety best practices (caching original local offsets, checking for null references, and automatic fallback bone searching if fields are left unassigned).
