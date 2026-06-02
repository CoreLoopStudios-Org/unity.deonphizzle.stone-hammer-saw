using UnityEngine;
using DG.Tweening;

public class DOTweenCombatController : MonoBehaviour
{
    [Header("Fighters")]
    public Transform attacker;
    public Transform victim;

    [Header("Settings")]
    public float attackDistance = 1.3f;
    public float lungeDuration = 0.18f;
    public float returnDuration = 0.25f;
    public float pushDistance = 1.8f;
    public float fallDuration = 0.6f;

    private Animator attackerAnim;
    private Animator victimAnim;
    
    private Transform attackerHand;
    private Transform victimHead;

    private Vector3 attackerStartPos;
    private Vector3 handOriginalLocalPos;
    
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

        // Cache initial positions
        attackerStartPos = attacker.position;

        // Cache and DISABLE animators to prevent Mixamo overrides
        attackerAnim = attacker.GetComponent<Animator>();
        victimAnim = victim.GetComponent<Animator>();
        if (attackerAnim != null) attackerAnim.enabled = false;
        if (victimAnim != null) victimAnim.enabled = false;

        // Recursively find bone references
        attackerHand = FindChildRecursive(attacker, "CC_Base_R_Hand");
        victimHead = FindChildRecursive(victim, "CC_Base_Head");

        if (attackerHand != null)
        {
            handOriginalLocalPos = attackerHand.localPosition;
        }
        else
        {
            Debug.LogError("[DOTweenCombat] CC_Base_R_Hand bone not found under Attacker!");
        }

        if (victimHead == null)
        {
            Debug.LogWarning("[DOTweenCombat] CC_Base_Head bone not found under Victim. Defaulting to Victim root.");
            victimHead = victim;
        }
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
        if (attackerHand == null || victimHead == null) return;
        isAttacking = true;

        Vector3 direction = (victim.position - attacker.position).normalized;
        Vector3 targetLungePos = victim.position - (direction * attackDistance);

        // Sequence Builder
        Sequence fightSeq = DOTween.Sequence();

        // 1. Lunge forward & stretch the hand bone straight to the head
        fightSeq.Append(attacker.DOMove(targetLungePos, lungeDuration).SetEase(Ease.OutCubic));
        fightSeq.Join(attackerHand.DOMove(victimHead.position, lungeDuration).SetEase(Ease.OutQuad));

        // 2. Point of impact
        fightSeq.AppendInterval(0.12f);
        fightSeq.AppendCallback(() => {
            // A. Camera Shake
            Camera.main.transform.DOShakePosition(0.2f, 0.4f, 15, 90f);

            // B. Victim Head Snap Back
            if (victimHead != null && victimHead != victim)
            {
                victimHead.DOPunchRotation(new Vector3(-35f, 0f, 0f), 0.4f, 8, 1f);
            }

            // C. Push back & Rotate Victim 90 degrees (Fall Down flat on the ground)
            victim.DOMove(victim.position + direction * pushDistance, fallDuration).SetEase(Ease.OutQuad);
            victim.DORotate(new Vector3(90f, victim.eulerAngles.y, 0f), fallDuration).SetEase(Ease.OutBounce);
        });

        // 3. Return Attacker and retract the hand bone
        fightSeq.Append(attacker.DOMove(attackerStartPos, returnDuration).SetEase(Ease.InOutQuad));
        fightSeq.Join(attackerHand.DOLocalMove(handOriginalLocalPos, returnDuration).SetEase(Ease.InQuad));

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
