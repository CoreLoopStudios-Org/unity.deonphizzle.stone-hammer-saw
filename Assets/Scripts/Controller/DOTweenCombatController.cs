using UnityEngine;
using DG.Tweening;

public class DOTweenCombatController : MonoBehaviour
{
    [Header("Exposed Inspector Fields")]
    [SerializeField] private Transform attackerTorso;
    [SerializeField] private Transform attackerHand;
    [SerializeField] private Transform victimHead;
    [SerializeField] private Transform victimBody;

    [Header("Sequence Settings")]
    [SerializeField] private float windupDuration = 0.25f;
    [SerializeField] private float strikeDuration = 0.12f;
    [SerializeField] private float resetDuration = 0.3f;
    [SerializeField] private float pushDistance = 1.5f;

    // Cached starting local coordinates
    private Vector3 handOriginalLocalPos;
    private Quaternion handOriginalLocalRot;
    private Quaternion torsoOriginalLocalRot;
    private Vector3 victimBodyStartPos;

    private bool isAttacking = false;

    private void Start()
    {
        // 1. Automatic Fallback Search if fields are left unassigned in the Inspector
        if (attackerHand == null) attackerHand = FindChildRecursive(transform, "CC_Base_R_Hand");
        if (attackerTorso == null) attackerTorso = FindChildRecursive(transform, "CC_Base_Spine01");
        
        GameObject victimGo = GameObject.Find("Victim");
        if (victimGo != null)
        {
            if (victimBody == null) victimBody = victimGo.transform;
            if (victimHead == null) victimHead = FindChildRecursive(victimGo.transform, "CC_Base_Head");
        }

        // Safety verification
        if (attackerHand == null || attackerTorso == null || victimHead == null || victimBody == null)
        {
            Debug.LogError("[DOTweenCombat] Setup incomplete! Some Transform references are missing.");
            return;
        }

        // Disable Animator component to prevent Mixamo overrides
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;

        Animator victimAnim = victimBody.GetComponent<Animator>();
        if (victimAnim != null) victimAnim.enabled = false;

        // 2. Cache initial local coordinates
        handOriginalLocalPos = attackerHand.localPosition;
        handOriginalLocalRot = attackerHand.localRotation;
        torsoOriginalLocalRot = attackerTorso.localRotation;
        victimBodyStartPos = victimBody.position;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            PlayRealisticCombatSequence();
        }
    }

    private void PlayRealisticCombatSequence()
    {
        isAttacking = true;

        Vector3 direction = (victimBody.position - transform.position).normalized;

        // Calculate rotation quaternions relative to the starting rotation
        Quaternion windupTorsoRot = torsoOriginalLocalRot * Quaternion.Euler(0f, -25f, 0f);
        Vector3 windupHandPos = handOriginalLocalPos + new Vector3(-0.08f, 0.05f, -0.15f); // Pull hand back slightly

        Quaternion strikeTorsoRot = torsoOriginalLocalRot * Quaternion.Euler(0f, 38f, 0f);

        // Sequence construction
        Sequence combatSeq = DOTween.Sequence();

        // --- STEP 1: Wind-up / Anticipation (Slow) ---
        combatSeq.Append(attackerTorso.DOLocalRotateQuaternion(windupTorsoRot, windupDuration).SetEase(Ease.OutQuad));
        combatSeq.Join(attackerHand.DOLocalMove(windupHandPos, windupDuration).SetEase(Ease.OutQuad));

        // --- STEP 2: The Strike (Violent / Fast) ---
        combatSeq.Append(attackerTorso.DOLocalRotateQuaternion(strikeTorsoRot, strikeDuration).SetEase(Ease.InQuad));
        combatSeq.Join(attackerHand.DOMove(victimHead.position, strikeDuration).SetEase(Ease.InQuad));

        // --- STEP 3: Impact & Recoil (Callback) ---
        combatSeq.AppendCallback(() =>
        {
            // A. Camera Shake
            Camera.main.transform.DOShakePosition(0.2f, 0.4f, 15, 90f);

            // B. Victim Head Snap Back
            victimHead.DOPunchRotation(new Vector3(-45f, 0f, 0f), 0.5f, 10, 1f);

            // C. Victim Body Stumble Back
            victimBody.DOMove(victimBody.position + direction * pushDistance, 0.5f).SetEase(Ease.OutQuad);
        });

        // --- STEP 4: Reset to Idle ---
        combatSeq.Append(attackerTorso.DOLocalRotateQuaternion(torsoOriginalLocalRot, resetDuration).SetEase(Ease.OutCubic));
        combatSeq.Join(attackerHand.DOLocalMove(handOriginalLocalPos, resetDuration).SetEase(Ease.OutCubic));
        combatSeq.Join(attackerHand.DOLocalRotateQuaternion(handOriginalLocalRot, resetDuration).SetEase(Ease.OutCubic));

        // Reset attack flag
        combatSeq.OnComplete(() =>
        {
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
