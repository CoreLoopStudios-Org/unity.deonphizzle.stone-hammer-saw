using UnityEngine;
using DG.Tweening;

public class TimedPanelTransition : MonoBehaviour
{
    [SerializeField] private GameObject currentPanel;
    [SerializeField] private GameObject nextPanel;
    [SerializeField] private float waitDuration = 3.0f;

    private void OnEnable()
    {
        // When this panel is set active, wait for waitDuration and transition to next panel
        DOVirtual.DelayedCall(waitDuration, () =>
        {
            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
            }
        });
    }
}
