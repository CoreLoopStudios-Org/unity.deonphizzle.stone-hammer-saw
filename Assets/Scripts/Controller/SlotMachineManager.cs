using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class SlotMachineManager : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 3.5f;
    public List<RectTransform> weaponItems;
    public GameObject highlightOverlay;
    
    public event System.Action<int> OnWeaponSelected;
    
    private bool isSpinning = false;

    void Start()
    {
        // Don't auto-spin. Wait for ChestOpeningSequence to trigger us.
        isSpinning = false;
    }

    public void ResetAndStartSpin()
    {
        isSpinning = true;
        if (scrollRect != null)
        {
            scrollRect.velocity = Vector2.zero; // Clear inertia
            scrollRect.verticalNormalizedPosition = 0f;
        }
        CancelInvoke("StopSpinning");
        Invoke("StopSpinning", 3f);
    }

    void Update()
    {
        if (isSpinning && scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;
            if (scrollRect.verticalNormalizedPosition >= 1f)
                scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Ei function ti call hobe 3s por OR tap korle
    public void StopSpinning()
    {
        StopSpinningInternal(-1);
    }

    public void StopSpinning(int forcedIndex)
    {
        StopSpinningInternal(forcedIndex);
    }

    private void StopSpinningInternal(int forcedIndex)
    {
        if (!isSpinning) return; // Jodi already theme jay, tobe abar stop hobe na
        
        isSpinning = false;
        CancelInvoke("StopSpinning"); // 3s er timer ti bondho kore dilam

        if (scrollRect != null)
        {
            scrollRect.velocity = Vector2.zero; // Clear inertia
        }

        int selectedIndex = forcedIndex;
        if (selectedIndex == -1)
        {
            float pos = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
            float segment = 1f / 5f; 
            selectedIndex = Mathf.Clamp(Mathf.RoundToInt(pos / segment), 0, 4);
        }
        float targetPos = selectedIndex * (1f / 5f);

        if (scrollRect != null)
        {
            scrollRect.DONormalizedPos(new Vector2(0, targetPos), 0.5f).SetEase(Ease.OutBack)
                .OnComplete(() => ApplyHighlight(selectedIndex));
        }
        else
        {
            ApplyHighlight(selectedIndex);
        }
    }

    void ApplyHighlight(int index)
    {
        if (highlightOverlay != null && weaponItems != null && index >= 0 && index < weaponItems.Count)
        {
            highlightOverlay.transform.SetParent(weaponItems[index]);
            highlightOverlay.transform.localPosition = Vector3.zero;
            weaponItems[index].DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);
        }

        if (GameplayController.Instance != null)
            GameplayController.Instance.SelectWeapon(index);

        OnWeaponSelected?.Invoke(index);
    }
}