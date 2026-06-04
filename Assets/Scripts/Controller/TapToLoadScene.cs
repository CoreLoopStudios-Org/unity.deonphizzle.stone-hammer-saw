using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class TapToLoadScene : MonoBehaviour, IPointerClickHandler
{
    [Header("Scene Transition Settings")]
    [SerializeField] private string sceneToLoad;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
