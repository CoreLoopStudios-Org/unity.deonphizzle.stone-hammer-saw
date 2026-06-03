using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public static VirtualJoystick Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("The visual background circle of the joystick.")]
    public RectTransform container;
    [Tooltip("The handle knob of the joystick.")]
    public RectTransform handle;
    [Tooltip("The large touch zone panel that captures clicks.")]
    public RectTransform touchZone;

    private Vector2 inputVector = Vector2.zero;
    private Vector2 defaultContainerPosition;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (touchZone == null)
        {
            touchZone = GetComponent<RectTransform>();
        }

        if (container != null)
        {
            defaultContainerPosition = container.anchoredPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;

        // Convert screen point to touchZone local point
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            touchZone, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position))
        {
            // Move the visual background circle (container) to the click position
            if (container != null)
            {
                container.anchoredPosition = position;
            }
            
            // Reset knob to center initially
            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }

            // Immediately simulate drag to process first input
            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position = Vector2.zero;

        // Calculate handle position relative to the visual container (which is the new center)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            container, 
            eventData.position, 
            eventData.pressEventCamera, 
            out position))
        {
            float width = container.sizeDelta.x;
            float height = container.sizeDelta.y;

            // Map drag offset to a -1 to +1 range
            position.x = (position.x / (width * 0.5f));
            position.y = (position.y / (height * 0.5f));

            inputVector = new Vector2(position.x, position.y);
            inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;

            // Update knob handle position visually relative to container
            if (handle != null)
            {
                handle.anchoredPosition = new Vector2(
                    inputVector.x * (width * 0.5f), 
                    inputVector.y * (height * 0.5f)
                );
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        // Return visual background circle to default position
        if (container != null)
        {
            container.anchoredPosition = defaultContainerPosition;
        }
    }

    public Vector2 GetInputDirection()
    {
        return inputVector;
    }
}
