# DOTween Combat Animation: Attacker vs. Victim

This document details the analysis of the existing animation setup in `PonyPackScene` and outlines the plan to replace it with a robust, synchronized DOTween-driven animation.

---

## 1. Analysis of Existing Animations & Setup

In [PonyPackScene.unity](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scenes/PonyPackScene.unity), we analyzed the animation controllers, clips, and script components:

*   **Animation Controllers:**
    *   **Attacker**: Uses `AttackerAnim.controller` with a `"Hit"` trigger parameter mapped to the `hit.fbx` punch animation.
    *   **Victim**: Uses `VictimAnim.controller` with a `"FallDown"` trigger parameter mapped to the `falldown.fbx` animation.
*   **Attached Scripts (Legacy Physics-Based Setup):**
    *   **Attacker Root**: Has [PlayerAttack.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/PlayerAttack.cs) attached, which listens for left mouse clicks to trigger `"Hit"`.
    *   **Victim Root**: Incorrectly has [HitDetector.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/HitDetector.cs) attached. (Because it is attached to the Victim instead of the Attacker's hand, tag checks fail and it never triggers).

---

## 2. Plan to Remove Legacy Components & Configure DOTween

To implement the new DOTween combat sequence, we will perform the following cleanups and updates:

1.  **Remove Legacy Scripts from Scene:**
    *   Remove [PlayerAttack.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/PlayerAttack.cs) from the **Attacker** GameObject.
    *   Remove [HitDetector.cs](file:///C:/Users/User/Documents/GitHub/unity.deonphizzle.stone-hammer-saw/Assets/Scripts/Animaion/HitDetector.cs) from the **Victim** GameObject.
2.  **Create and Attach `DOTweenCombatController.cs`**:
    *   Create a new script: `DOTweenCombatController.cs`.
    *   Attach it to the **Attacker** root GameObject.
3.  **Coordinate the Attack Sequence via Script**:
    Instead of relying on unstable physics callbacks:
    *   **0.00s (Lunge)**: DOMove Attacker root close to the Victim's coordinates.
    *   **0.05s (Punch)**: Set the Animator trigger `"Hit"`.
    *   **0.17s (Impact)**: 
        *   Trigger the Victim's `"FallDown"` Animator trigger.
        *   Apply `DOPunchRotation` on the Victim's head bone (`CC_Base_Head`) to snap it back.
        *   Push the Victim's root back with `DOMove` and shake the main camera using `DOShakePosition`.
    *   **0.35s (Return)**: Move the Attacker back to their starting position.

---

## 3. Script Reference: `DOTweenCombatController.cs`

```csharp
using UnityEngine;
using DG.Tweening;

public class DOTweenCombatController : MonoBehaviour
{
    [Header("Fighters")]
    public Transform attacker;
    public Transform victim;

    [Header("Settings")]
    public float attackDistance = 1.3f;
    public float lungeDuration = 0.15f;
    public float returnDuration = 0.25f;
    public float pushDistance = 1.8f;

    private Animator attackerAnim;
    private Animator victimAnim;
    private Transform victimHead;

    private Vector3 attackerStartPos;
    private Vector3 victimStartPos;
    private bool isAttacking = false;

    private void Start()
    {
        if (attacker == null) attacker = this.transform;
        if (victim == null) victim = GameObject.Find("Victim")?.transform;

        if (attacker == null || victim == null)
        {
            Debug.LogError("[DOTweenCombat] Attacker or Victim Transform is missing!");
            return;
        }

        // Cache positions
        attackerStartPos = attacker.position;
        victimStartPos = victim.position;

        // Cache animators
        attackerAnim = attacker.GetComponent<Animator>();
        victimAnim = victim.GetComponent<Animator>();

        // Recursively find Victim's Head bone
        victimHead = FindChildRecursive(victim, "CC_Base_Head");
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            PlayCombatSequence();
        }
    }

    private void PlayCombatSequence()
    {
        isAttacking = true;

        Vector3 direction = (victim.position - attacker.position).normalized;
        Vector3 targetLungePos = victim.position - (direction * attackDistance);

        // Sequence Builder
        Sequence fightSeq = DOTween.Sequence();

        // 1. Lunge forward & trigger punch animation simultaneously
        fightSeq.Append(attacker.DOMove(targetLungePos, lungeDuration).SetEase(Ease.OutCubic));
        fightSeq.JoinCallback(() => {
            if (attackerAnim != null) attackerAnim.SetTrigger("Hit");
        });

        // 2. Point of impact (impact frame triggers when fist reaches head)
        fightSeq.AppendInterval(0.12f);
        fightSeq.AppendCallback(() => {
            // A. Camera Shake
            Camera.main.transform.DOShakePosition(0.2f, 0.4f, 15, 90f);

            // B. Victim Head Snap Back
            if (victimHead != null)
            {
                victimHead.DOPunchRotation(new Vector3(-35f, 0f, 0f), 0.4f, 8, 1f);
            }

            // C. Push back Victim & Trigger FallDown Animation
            victim.DOMove(victim.position + direction * pushDistance, 0.4f).SetEase(Ease.OutQuad);
            if (victimAnim != null)
            {
                victimAnim.SetTrigger("FallDown");
            }
        });

        // 3. Return Attacker back to start position
        fightSeq.Append(attacker.DOMove(attackerStartPos, returnDuration).SetEase(Ease.InOutQuad));

        // 4. Reset state
        fightSeq.OnComplete(() => {
            isAttacking = false;
        });
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent.name == childName) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}
```
