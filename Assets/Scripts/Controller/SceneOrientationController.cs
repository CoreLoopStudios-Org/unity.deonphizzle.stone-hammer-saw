using UnityEngine;

public class SceneOrientationController : MonoBehaviour
{
    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void OnDestroy()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }
}