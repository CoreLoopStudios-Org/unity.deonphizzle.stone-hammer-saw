using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SceneLoadingController : MonoBehaviour
{
    [Header("Loading UI References")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Image loadingBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private float loadingDuration = 3.0f;
    [SerializeField] private GameObject redirectPanel;

    private void OnEnable()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);

            if (loadingBar != null)
            {
                loadingBar.gameObject.SetActive(true);
                loadingBar.fillAmount = 0f;
                loadingBar.DOFillAmount(1f, loadingDuration)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        loadingPanel.SetActive(false);
                        if (redirectPanel != null)
                        {
                            redirectPanel.SetActive(true);
                        }
                    });
            }
            else
            {
                // Fallback if loading bar is unassigned or missing
                DOVirtual.DelayedCall(loadingDuration, () =>
                {
                    loadingPanel.SetActive(false);
                    if (redirectPanel != null)
                    {
                        redirectPanel.SetActive(true);
                    }
                });
            }

            if (loadingText != null)
            {
                loadingText.DOFade(0.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            }
        }
    }
}
