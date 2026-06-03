using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleMobileJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform joystickKnob; // আপনার tip অবজেক্টটি এখানে বসবে
    public static Vector2 InputDirection;

    private RectTransform joystickBackground;
    private float maxRadius;

    private void Start()
    {
        joystickBackground = GetComponent<RectTransform>();
        // ব্যাকগ্রাউন্ডের সাইজ অনুযায়ী নব (Knob) কতটুকু সরবে তা ঠিক করা
        maxRadius = joystickBackground.rect.width / 2f; 
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        // Screen Space - Overlay Canvas values require a null camera parameter
        Canvas canvas = joystickBackground.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBackground, eventData.position, cam, out position);
        
        // Calculate the touch position relative to the center of the background instead of its pivot
        Vector2 pivotOffset = new Vector2(
            (0.5f - joystickBackground.pivot.x) * joystickBackground.rect.width,
            (0.5f - joystickBackground.pivot.y) * joystickBackground.rect.height
        );
        Vector2 localCenterPosition = position - pivotOffset;

        // tip বা নবটিকে ব্যাকগ্রাউন্ডের বাইরে যেতে না দেওয়া
        Vector2 clampedPosition = Vector2.ClampMagnitude(localCenterPosition, maxRadius);
        joystickKnob.anchoredPosition = clampedPosition;

        // আউটপুট সিগন্যাল (-1 থেকে 1) প্লেয়ারের কাছে পাঠানো
        InputDirection = clampedPosition / maxRadius;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // আঙুল ছেড়ে দিলে tip একদম মাঝে চলে আসবে
        joystickKnob.anchoredPosition = Vector2.zero;
        InputDirection = Vector2.zero;
    }
}