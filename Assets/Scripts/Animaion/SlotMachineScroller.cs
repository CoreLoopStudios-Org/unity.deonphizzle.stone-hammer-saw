using UnityEngine;
using System.Collections;
using DG.Tweening;

public class SlotMachineScroller : MonoBehaviour
{
    [System.Serializable]
    public class SlotColumn
    {
        public string columnName;
        public RectTransform[] slotImages;
        [HideInInspector] public float currentSpeed;
        [HideInInspector] public Vector2[] initialPositions;
        [HideInInspector] public bool isSpinning;
    }

    [Header("Columns")]
    public SlotColumn[] columns;

    [Header("Scroll Settings")]
    public float targetScrollSpeed = 500f; 
    public float bottomThreshold = -300f; 
    public float resetPositionY = 300f;

    [Header("Arrows (Upper Bg Panel)")]
    public RectTransform[] arrowImages; // Drag Arrow Left, Arrow Mid, Arrow Right
    public float arrowAnimationSpeed = 0.5f;
    public float arrowMoveDistance = 30f;
    
    [Header("Selection & VFX")]
    public float dimAlpha = 0.75f;
    public float selectScaleMultiplier = 1.8f;
    public float selectTweenDuration = 0.5f;

    private bool isWholeMachineSpinning = false;
    private GameObject dimOverlay;
    private Tween arrowSequenceTween;
    private Canvas addedCanvas;
    private UnityEngine.UI.GraphicRaycaster addedRaycaster;
    private RectTransform currentlySelectedImage;
    private Vector3 currentlySelectedImageOriginalScale = Vector3.one;
    private Vector3[] arrowOriginalScales;
    private float[] arrowOriginalYPositions;

    // Cache original parent hierarchy values to restore on spin reset
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPosition;
    private Vector3 originalLocalScale;
    private Vector2 originalAnchorMin;
    private Vector2 originalAnchorMax;
    private Vector2 originalPivot;

    // গেম শুরু হওয়ার সময় একবার পজিশনগুলো সেভ করে রাখা
    void Awake()
    {
        foreach (var col in columns)
        {
            if (col == null || col.slotImages == null) continue;
            col.initialPositions = new Vector2[col.slotImages.Length];
            for (int i = 0; i < col.slotImages.Length; i++)
            {
                if (col.slotImages[i] != null)
                {
                    col.initialPositions[i] = col.slotImages[i].anchoredPosition;
                }
            }
        }

        if (arrowImages != null && arrowImages.Length > 0)
        {
            arrowOriginalScales = new Vector3[arrowImages.Length];
            arrowOriginalYPositions = new float[arrowImages.Length];
            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] != null)
                {
                    arrowOriginalScales[i] = arrowImages[i].localScale;
                    arrowOriginalYPositions[i] = arrowImages[i].anchoredPosition.y;
                }
            }
        }
    }

    // প্যানেল ওপেন বা Active হওয়ার সাথে সাথেই এই মেথড কল হবে
    void OnEnable()
    {
        StartArrowAnimation();

        // যদি আগে থেকে স্পিন না চলতে থাকে, তাহলে অটোমেটিক স্পিন শুরু করবে
        if (!isWholeMachineSpinning)
        {
            StartCoroutine(SpinRoutine());
        }
    }

    // প্যানেল ক্লোজ বা Hide হওয়ার সময় সবকিছু রিসেট করে দেওয়া
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
        
        ResetSelection();
    }

    void Update()
    {
        // Scroll each column using its current speed
        foreach (var col in columns)
        {
            if (col == null || col.slotImages == null || col.currentSpeed <= 0.01f) continue;

            for (int i = 0; i < col.slotImages.Length; i++)
            {
                if (col.slotImages[i] == null) continue;

                // Core scrolling math (preserved exactly as requested)
                col.slotImages[i].anchoredPosition += Vector2.down * col.currentSpeed * Time.deltaTime;
                if (col.slotImages[i].anchoredPosition.y <= bottomThreshold)
                {
                    float overshoot = bottomThreshold - col.slotImages[i].anchoredPosition.y;
                    col.slotImages[i].anchoredPosition = new Vector2(
                        col.slotImages[i].anchoredPosition.x, 
                        resetPositionY - overshoot
                    );
                }
            }
        }
    }

    private IEnumerator SpinRoutine()
    {
        isWholeMachineSpinning = true;

        // Reset previous selection UI if active
        ResetSelection();

        // 1. Accelerate all columns to a random target speed
        foreach (var col in columns)
        {
            if (col == null) continue;
            col.isSpinning = true;
            col.currentSpeed = 0f; // জিরো থেকে স্পিড শুরু হবে
    
            // প্রতিবার স্পিনের সময় স্পিড একটু কম-বেশি হবে
            float randomSpeed = targetScrollSpeed + Random.Range(-60f, 60f);
            DOTween.To(() => col.currentSpeed, x => col.currentSpeed = x, randomSpeed, 0.5f);
        }

        // 2. Wait for a random spin time OR until player touches the screen
        float spinTime = Random.Range(4.0f, 6.5f);
        float elapsedTime = 0f;

        while (elapsedTime < spinTime)
        {
            // ০.৫ সেকেন্ড যাওয়ার পর থেকে টাচ চেক করবে (যেন প্যানেল ওপেন করার টাচেই থেমে না যায়)
            if (elapsedTime > 0.5f && Input.GetMouseButtonDown(0))
            {
                break; // টাচ পেলেই থামার সিকুয়েন্স শুরু করবে
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 3. Staggered sequential stop
        for (int c = 0; c < columns.Length; c++)
        {
            var col = columns[c];
            if (col == null) continue;
            
            // Decelerate column speed to 0 using DOTween
            yield return DOTween.To(() => col.currentSpeed, x => col.currentSpeed = x, 0f, 1.0f)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();

            col.isSpinning = false;
            col.currentSpeed = 0f;

            // 4. Snap the column to its nearest alignment position
            SnapColumn(col);

            // Wait a small delay before stopping the next column
            yield return new WaitForSeconds(0.4f);
        }

        isWholeMachineSpinning = false;

        // 5. Select one weapon
        SelectWeapon();
    }

    private void SnapColumn(SlotColumn col)
    {
        if (col == null || col.slotImages == null || col.slotImages.Length == 0) return;

        float L = resetPositionY - bottomThreshold;
        float spacing = L / col.slotImages.Length;
        float currentY = col.slotImages[0].anchoredPosition.y;
        float initialY = col.initialPositions[0].y;

        // Compute alignment shift
        float diff = currentY - initialY;
        float wrappedDiff = Mathf.Repeat(diff + L / 2f, L) - L / 2f;
        float k = Mathf.Round(wrappedDiff / spacing);
        float targetDiff = k * spacing;
        float shift = targetDiff - wrappedDiff;

        // Animate the shift using DOTween with Ease.OutBack for slot machine bounce
        for (int i = 0; i < col.slotImages.Length; i++)
        {
            if (col.slotImages[i] == null) continue;
            float startY = col.slotImages[i].anchoredPosition.y;
            float targetY = startY + shift;

            // Animate local Y position
            col.slotImages[i].DOAnchorPosY(targetY, 0.4f).SetEase(Ease.OutBack);
        }
    }

    private void StartArrowAnimation()
    {
        if (arrowImages == null || arrowImages.Length == 0 || arrowOriginalScales == null || arrowOriginalYPositions == null) return;
        
        // আগে কোনো এনিমেশন চলতে থাকলে সেটা বন্ধ করে দেওয়া
        if (arrowSequenceTween != null) arrowSequenceTween.Kill();

        // Loop wave scale/fade animation preserving original aspect ratios
        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < arrowImages.Length; i++)
        {
            if (arrowImages[i] == null) continue;
            RectTransform arrow = arrowImages[i];
            Vector3 originalScale = arrowOriginalScales[i];
            float originalY = arrowOriginalYPositions[i];
            
            // Pulse scale and local position downwards
            seq.Insert(i * 0.2f, arrow.DOScale(originalScale * 1.3f, 0.3f).SetLoops(2, LoopType.Yoyo));
            seq.Insert(i * 0.2f, arrow.DOAnchorPosY(originalY - arrowMoveDistance, 0.3f).SetLoops(2, LoopType.Yoyo));
        }

        seq.SetLoops(-1);
        arrowSequenceTween = seq;
    }

    private void StopAndResetArrows()
    {
        if (arrowSequenceTween != null)
        {
            arrowSequenceTween.Kill();
        }

        if (arrowImages != null && arrowOriginalScales != null && arrowOriginalYPositions != null)
        {
            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] != null)
                {
                    // Kill any active tweens on individual arrows
                    arrowImages[i].DOKill();
                    // Smoothly snap back in 0.2s using DOTween
                    arrowImages[i].DOScale(arrowOriginalScales[i], 0.2f);
                    arrowImages[i].DOAnchorPosY(arrowOriginalYPositions[i], 0.2f);
                }
            }
        }
    }

    private void SelectWeapon()
    {
        StopAndResetArrows();

        // Select the center item of columns[1] (middle column)
        if (columns == null || columns.Length <= 1) return;
        var middleCol = columns[1];
        if (middleCol == null || middleCol.slotImages == null || middleCol.slotImages.Length == 0) return;

        RectTransform bestImage = null;
        float bestDist = float.MaxValue;
        foreach (var img in middleCol.slotImages)
        {
            if (img == null) continue;
            float dist = Mathf.Abs(img.anchoredPosition.y); // Viewport center is Y=0
            if (dist < bestDist)
            {
                bestDist = dist;
                bestImage = img;
            }
        }

        if (bestImage == null) return;
        currentlySelectedImage = bestImage;

        // Cache original values before parent switch
        originalParent = bestImage.parent;
        originalSiblingIndex = bestImage.GetSiblingIndex();
        originalAnchoredPosition = bestImage.anchoredPosition;
        originalLocalScale = bestImage.localScale;
        originalAnchorMin = bestImage.anchorMin;
        originalAnchorMax = bestImage.anchorMax;
        originalPivot = bestImage.pivot;

        // 1. Create dim overlay
        dimOverlay = new GameObject("DimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        dimOverlay.transform.SetParent(this.transform.parent, false);
        dimOverlay.transform.SetSiblingIndex(this.transform.GetSiblingIndex()); // behind manager / columns
        
        var rect = dimOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var imgComp = dimOverlay.GetComponent<UnityEngine.UI.Image>();
        imgComp.color = new Color(0, 0, 0, 0);
        imgComp.DOColor(new Color(0, 0, 0, dimAlpha), selectTweenDuration);

        // Implementation of 5-step centering fix:
        // 1. Store the bestImage.position (world position) in a temporary variable.
        Vector3 storedWorldPosition = bestImage.position;

        // 2. Reparent the bestImage to this.transform.parent (using worldPositionStays: true).
        bestImage.SetParent(this.transform.parent, true);

        // 3. Set bestImage.anchorMin, bestImage.anchorMax, and bestImage.pivot all to new Vector2(0.5f, 0.5f).
        bestImage.anchorMin = new Vector2(0.5f, 0.5f);
        bestImage.anchorMax = new Vector2(0.5f, 0.5f);
        bestImage.pivot = new Vector2(0.5f, 0.5f);

        // 4. Immediately re-apply the stored world position so the image doesn't visually jump when the anchors change.
        bestImage.position = storedWorldPosition;

        // Cache scale converted to the panel's uniform space
        currentlySelectedImageOriginalScale = bestImage.localScale;

        // 3. Bring selected image to the front using Canvas override
        addedCanvas = bestImage.gameObject.AddComponent<Canvas>();
        addedCanvas.overrideSorting = true;
        addedCanvas.sortingOrder = 105;

        addedRaycaster = bestImage.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 5. Animate to the center (0, 0), scale, and rotate in uniform space
        bestImage.DOAnchorPos(Vector2.zero, selectTweenDuration).SetEase(Ease.OutBack);
        bestImage.DOScale(currentlySelectedImageOriginalScale * selectScaleMultiplier, selectTweenDuration).SetEase(Ease.OutBack);
        bestImage.DORotate(new Vector3(0, 0, 360f), selectTweenDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

        // 5. Cyan glow pulse loop effect
        var neonColor = new Color(0f, 1f, 1f, 1f); // Neon Cyan
        var weaponImage = bestImage.GetComponent<UnityEngine.UI.Image>();
        if (weaponImage != null)
        {
            weaponImage.DOColor(neonColor, 0.4f).SetLoops(-1, LoopType.Yoyo).SetId("GlowPulse");
        }
    }

    private void ResetSelection()
    {
        if (currentlySelectedImage != null)
        {
            // Kill glow pulse tween
            DOTween.Kill("GlowPulse");

            var weaponImage = currentlySelectedImage.GetComponent<UnityEngine.UI.Image>();
            if (weaponImage != null)
            {
                weaponImage.color = Color.white;
            }

            // Kill any active tweens on the image
            currentlySelectedImage.DOKill();

            // Reparent back to original column parent
            currentlySelectedImage.SetParent(originalParent, true);
            currentlySelectedImage.SetSiblingIndex(originalSiblingIndex);

            // Restore original local values and anchors/pivot
            currentlySelectedImage.anchorMin = originalAnchorMin;
            currentlySelectedImage.anchorMax = originalAnchorMax;
            currentlySelectedImage.pivot = originalPivot;
            currentlySelectedImage.anchoredPosition = originalAnchoredPosition;
            currentlySelectedImage.localRotation = Quaternion.identity;
            currentlySelectedImage.localScale = originalLocalScale;

            // Destroy components added for rendering overlay sorting
            if (addedRaycaster != null) Destroy(addedRaycaster);
            if (addedCanvas != null) Destroy(addedCanvas);
            currentlySelectedImage = null;
        }

        if (dimOverlay != null)
        {
            Destroy(dimOverlay);
        }
    }

    void OnDestroy()
    {
        if (arrowSequenceTween != null)
        {
            arrowSequenceTween.Kill();
        }
        DOTween.Kill("GlowPulse");
    }
}