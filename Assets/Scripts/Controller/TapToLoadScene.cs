using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TapToLoadScene : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene Transition Settings")]
    [SerializeField] private string sceneToLoad;

    [Header("Panel Activation Settings")]
    [SerializeField] private GameObject panelToActivate;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (panelToActivate != null)
        {
            panelToActivate.SetActive(true);
            gameObject.SetActive(false);
        }
        else if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
