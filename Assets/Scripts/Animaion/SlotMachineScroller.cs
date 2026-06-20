using UnityEngine;

public class SlotMachineScroller : MonoBehaviour
{
    [Header("Slot Images")]
    [Tooltip(" Your 4 Image RectTransform ")]
    public RectTransform[] slotImages;

    [Header("Scroll Settings")]
    public float scrollSpeed = 500f; 
    
    [Header("Position Thresholds")]
    public float bottomThreshold = -300f; 
    public float resetPositionY = 300f;

    void Update()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            slotImages[i].anchoredPosition += Vector2.down * scrollSpeed * Time.deltaTime;
            if (slotImages[i].anchoredPosition.y <= bottomThreshold)
            {
                float overshoot = bottomThreshold - slotImages[i].anchoredPosition.y;
                slotImages[i].anchoredPosition = new Vector2(
                    slotImages[i].anchoredPosition.x, 
                    resetPositionY - overshoot
                );
            }
        }
    }
}