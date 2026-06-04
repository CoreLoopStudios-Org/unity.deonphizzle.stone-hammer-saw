using UnityEngine;
using UnityEngine.EventSystems;

public class TapToClosePanel : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Deactivate this panel to reveal the 3D gameplay view underneath
        gameObject.SetActive(false);
    }
}
