using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class SlotMachineSelector : MonoBehaviour
{
    [System.Serializable]
    public class WeaponElement
    {
        public RectTransform rect;
        public int weaponID; 
    }

    [Header("Slot Settings")]
    [SerializeField] public List<WeaponElement> elements = new List<WeaponElement>();
    public float spinSpeed = 1500f;  
    public float spacing = 260f;    

    private bool isSpinning = false;
    private float topY;
    private float bottomY;
    private float slotHeight; // টোটাল হাইট ক্যালকুলেট করার জন্য

    private void OnEnable()
    {
        if (elements.Count > 0)
        {
            SetupInitialPositions();
            isSpinning = true;
        }
    }

    private void SetupInitialPositions()
    {
        slotHeight = elements.Count * spacing;
        topY = slotHeight / 2f;
        bottomY = -slotHeight / 2f;

        for (int i = 0; i < elements.Count; i++)
        {
            // অস্ত্রগুলোকে একদম পারফেক্ট গ্যাপে বসানো
            float startY = (elements.Count / 2f * spacing) - (i * spacing);
            if(elements[i].rect != null)
                elements[i].rect.anchoredPosition = new Vector2(0, startY);
        }
    }

    private void Update()
    {
        if (!isSpinning) return;

        foreach (var el in elements)
        {
            if (el.rect == null) continue;
            
            el.rect.anchoredPosition += Vector2.down * spinSpeed * Time.deltaTime;
            
            // ইনফিনিট লুপের সীমানা চেক
            if (el.rect.anchoredPosition.y <= bottomY)
            {
                float overshoot = bottomY - el.rect.anchoredPosition.y;
                el.rect.anchoredPosition = new Vector2(0, topY - overshoot);
            }
        }
    }
    
    public void TapToSelectWeapon()
    {
        if (!isSpinning) return;
        isSpinning = false;

        // কোন এলিমেন্টটি ফ্রেমের মাঝখানে (y=0) সবচেয়ে কাছে আছে বের করা
        WeaponElement closestElement = null;
        float minDistance = float.MaxValue;

        foreach (var el in elements)
        {
            float dist = Mathf.Abs(el.rect.anchoredPosition.y);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestElement = el;
            }
        }
        
        // সবগুলোকে স্মুথলি সেন্টারে নিয়ে আসা (Snapping Animation)
        if (closestElement != null)
        {
            float offsetToCenter = -closestElement.rect.anchoredPosition.y;
            foreach (var el in elements)
            {
                if(el.rect != null)
                    el.rect.DOAnchorPosY(el.rect.anchoredPosition.y + offsetToCenter, 0.5f)
                          .SetEase(Ease.OutBack);
            }

            Debug.Log("Slot Machine Selected Weapon ID: " + closestElement.weaponID);
            
            // সিলেক্ট হওয়ার পর GameplayController-কে কল করা
            // এখানে ডিলিট বা ডিলে দিয়েছি যাতে এনিমেশন শেষ হওয়ার পর লজিক ট্রিগার হয়
            DOVirtual.DelayedCall(0.5f, () => {
                if (GameplayController.Instance != null)
                    GameplayController.Instance.SelectWeapon(closestElement.weaponID);
            });
        }
    }
}