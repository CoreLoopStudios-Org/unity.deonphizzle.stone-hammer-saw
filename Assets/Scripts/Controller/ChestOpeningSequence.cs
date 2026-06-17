using UnityEngine;
using DG.Tweening;

public class ChestOpeningSequence : MonoBehaviour
{
    [Header("Target References")]
    [Tooltip("The root transform of the chest box. Usually named 'Chest-Box'.")]
    public Transform chestBox;
    [Tooltip("The lid transform that will rotate open.")]
    public Transform chestLid;
    [Tooltip("The character name that triggers the chest. Usually 'Pangopal_01'.")]
    public string targetCharacterName = "Pangopal_01";

    [Header("Chest Animation Settings")]
    public Vector3 shakeStrength = new Vector3(0.08f, 0.04f, 0.08f);
    public float shakeDuration = 0.5f;
    public int shakeVibrato = 15;
    
    public Vector3 lidOpenRotation = new Vector3(-110f, 0f, 0f);
    public float lidOpenDuration = 0.8f;
    public Ease lidOpenEase = Ease.OutBack;

    [Header("Tool / Item Settings")]
    public GameObject toolPrefab;
    public Transform spawnPoint;
    public Vector3 toolTargetScale = Vector3.one;
    public float floatHeight = 1.3f;
    public float popDuration = 0.9f;
    public Ease popScaleEase = Ease.OutBack;
    public Ease popMoveEase = Ease.OutQuad;

    [Header("Tool Idle Floating Settings")]
    public float floatRange = 0.12f;
    public float floatCycleDuration = 1.8f;
    public float spinSpeed = 40f; // Degrees per second

    [Header("Visual Effects Hook")]
    public ParticleSystem openParticleSystem;

    private bool isOpened = false;
    private GameObject spawnedTool;
    private Sequence idleSequence;

    private void Start()
    {
        // Fallbacks if not assigned in Inspector
        if (chestBox == null) chestBox = transform;
        if (spawnPoint == null) spawnPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Trigger only when Pangopal_01 arrives and chest is closed
        if (!isOpened && other.gameObject.name.Contains(targetCharacterName))
        {
            PlayOpeningSequence();
        }
    }

    [ContextMenu("Test Opening Sequence")]
    public void PlayOpeningSequence()
    {
        if (isOpened) return;
        isOpened = true;

        Vector3 originalBoxPos = chestBox.position;

        // Build DOTween Sequence
        Sequence openSeq = DOTween.Sequence();

        // 1. Shake the Box (Anticipation)
        openSeq.Append(chestBox.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true));

        // 2. Open Lid, Play Particles, and Spawn Tool
        openSeq.AppendCallback(() =>
        {
            if (openParticleSystem != null)
            {
                openParticleSystem.Play();
            }

            if (toolPrefab != null)
            {
                // Instantiate at zero scale
                spawnedTool = Instantiate(toolPrefab, spawnPoint.position, spawnPoint.rotation);
                spawnedTool.transform.localScale = Vector3.zero;

                // Animate pop-up, rotation, and scaling
                Sequence toolPopSeq = DOTween.Sequence();
                toolPopSeq.Append(spawnedTool.transform.DOScale(toolTargetScale, popDuration).SetEase(popScaleEase));
                toolPopSeq.Join(spawnedTool.transform.DOMoveY(spawnPoint.position.y + floatHeight, popDuration).SetEase(popMoveEase));
                toolPopSeq.Join(spawnedTool.transform.DORotate(new Vector3(0f, 360f, 0f), popDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
                
                // Chain the looping idle float & spin upon completion
                toolPopSeq.OnComplete(StartToolIdleAnimation);
            }
        });

        // Open chest lid
        if (chestLid != null)
        {
            openSeq.Join(chestLid.DOLocalRotate(lidOpenRotation, lidOpenDuration).SetEase(lidOpenEase));
        }

        // Return box position to normal on sequence finish/kill
        openSeq.OnKill(() => chestBox.position = originalBoxPos);
    }

    private void StartToolIdleAnimation()
    {
        if (spawnedTool == null) return;

        Vector3 peakPosition = spawnedTool.transform.position;
        idleSequence = DOTween.Sequence();

        // Loop A: Floating Yoyo (sine wave)
        idleSequence.Append(spawnedTool.transform.DOMoveY(peakPosition.y + floatRange, floatCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo));

        // Loop B: Slow Spinning (incremental rotation)
        spawnedTool.transform.DORotate(new Vector3(0f, 360f, 0f), 360f / spinSpeed, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    private void OnDestroy()
    {
        if (idleSequence != null) idleSequence.Kill();
    }
}
