using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class SlotMachineManager : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 0.5f;
    public List<RectTransform> weaponItems;
    public GameObject highlightOverlay;
    
    private bool isSpinning = true;

    void Start()
    {
        // 3 second por automatic theme jabe
        Invoke("StopSpinning", 3f);
    }

    void Update()
    {
        if (isSpinning)
        {
            scrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;
            if (scrollRect.verticalNormalizedPosition >= 1f)
                scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // Ei function ti call hobe 3s por OR tap korle
    public void StopSpinning()
    {
        if (!isSpinning) return; // Jodi already theme jay, tobe abar stop hobe na
        
        isSpinning = false;
        CancelInvoke("StopSpinning"); // 3s er timer ti bondho kore dilam

        float pos = scrollRect.verticalNormalizedPosition;
        float segment = 1f / 5f; 
        int selectedIndex = Mathf.RoundToInt(pos / segment) % 5;
        float targetPos = selectedIndex * segment;

        scrollRect.DONormalizedPos(new Vector2(0, targetPos), 0.5f).SetEase(Ease.OutBack)
            .OnComplete(() => ApplyHighlight(selectedIndex));
    }

    void ApplyHighlight(int index)
    {
        highlightOverlay.transform.SetParent(weaponItems[index]);
        highlightOverlay.transform.localPosition = Vector3.zero;
        weaponItems[index].DOScale(1.2f, 0.3f).SetLoops(2, LoopType.Yoyo);

        if (GameplayController.Instance != null)
            GameplayController.Instance.SelectWeapon(index);
    }
}