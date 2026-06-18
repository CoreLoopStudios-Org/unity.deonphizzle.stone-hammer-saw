using UnityEngine;

public class SceneOrientationController : MonoBehaviour
{
    void Start()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }

    void OnDestroy()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }
}