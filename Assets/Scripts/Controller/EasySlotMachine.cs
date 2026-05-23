using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EasySlotMachine : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 0.5f; 
    private bool isSpinning = true;

    void Update()
    {
        if (isSpinning)
        {
            // অটোমেটিক স্ক্রলিং
            scrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;

            // যখনই ১-এ পৌঁছাবে, আবার ০-তে ফিরে আসবে (ইনফিনিট লুপ)
            if (scrollRect.verticalNormalizedPosition >= 1f)
                scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void StopSpinning(int selectedIndex)
    {
        isSpinning = false;
        
        // প্রতিটি আইটেমের জন্য পজিশন: ৫টি আইটেম থাকলে গ্যাপ ০.২
        float targetPos = (float)selectedIndex / 5f; 
        
        // সুন্দর স্ন্যাপিং এফেক্ট
        scrollRect.DONormalizedPos(new Vector2(0, targetPos), 0.5f).SetEase(Ease.OutBack);
    }
}