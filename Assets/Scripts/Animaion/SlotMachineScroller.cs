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
    private Vector3[] arrowOriginalScales;

    void Start()
    {
        // Cache initial positions and start spinning immediately
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
            // Set initial speed to start spinning like before
            col.currentSpeed = targetScrollSpeed;
            col.isSpinning = true;
        }

        // Cache original scales of arrows before starting animation
        if (arrowImages != null && arrowImages.Length > 0)
        {
            arrowOriginalScales = new Vector3[arrowImages.Length];
            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] != null)
                {
                    arrowOriginalScales[i] = arrowImages[i].localScale;
                }
            }
        }

        // Start arrow animation
        StartArrowAnimation();

        // Automatically start the 5-second spin and stop routine on launch
        StartCoroutine(AutoSpinStopRoutine());
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

    private IEnumerator AutoSpinStopRoutine()
    {
        isWholeMachineSpinning = true;

        // 1. Spin for 5 seconds
        yield return new WaitForSeconds(5.0f);

        // 2. Staggered sequential stop
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

            // Snap the column to its nearest alignment position
            SnapColumn(col);

            // Wait a small delay before stopping the next column
            yield return new WaitForSeconds(0.4f);
        }

        isWholeMachineSpinning = false;

        // 3. Select weapon
        SelectWeapon();
    }

    [ContextMenu("Start Spin")]
    public void StartSpin()
    {
        if (isWholeMachineSpinning) return;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isWholeMachineSpinning = true;

        // Reset previous selection UI if active
        ResetSelection();

        // 1. Accelerate all columns to target speed using DOTween
        foreach (var col in columns)
        {
            if (col == null) continue;
            col.isSpinning = true;
            // Tween currentSpeed from 0 to targetScrollSpeed
            DOTween.To(() => col.currentSpeed, x => col.currentSpeed = x, targetScrollSpeed, 0.5f);
        }

        // 2. Wait for 5 seconds (the spin timer)
        yield return new WaitForSeconds(5.0f);

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
        if (arrowImages == null || arrowImages.Length == 0 || arrowOriginalScales == null) return;

        // Loop wave scale/fade animation preserving original aspect ratios
        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < arrowImages.Length; i++)
        {
            if (arrowImages[i] == null) continue;
            RectTransform arrow = arrowImages[i];
            Vector3 originalScale = arrowOriginalScales[i];
            
            // Pulse scale and local position downwards
            float originalY = arrow.anchoredPosition.y;
            seq.Insert(i * 0.2f, arrow.DOScale(originalScale * 1.3f, 0.3f).SetLoops(2, LoopType.Yoyo));
            seq.Insert(i * 0.2f, arrow.DOAnchorPosY(originalY - 12f, 0.3f).SetLoops(2, LoopType.Yoyo));
        }

        seq.SetLoops(-1);
        arrowSequenceTween = seq;
    }

    private void SelectWeapon()
    {
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

        // 2. Bring selected image to the front using Canvas override
        addedCanvas = bestImage.gameObject.AddComponent<Canvas>();
        addedCanvas.overrideSorting = true;
        addedCanvas.sortingOrder = 105;

        addedRaycaster = bestImage.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // 3. Play pop-up animation (scale and rotation)
        bestImage.DOScale(selectScaleMultiplier, selectTweenDuration).SetEase(Ease.OutBack);
        bestImage.DORotate(new Vector3(0, 0, 360f), selectTweenDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);

        // 4. Cyan glow pulse loop effect
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

            // Restore scale and rotation
            currentlySelectedImage.DOScale(1f, 0.3f);
            currentlySelectedImage.DORotate(Vector3.zero, 0.3f);

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