using Game3;
using UnityEngine;

public class StartDetection : MonoBehaviour
{
    public GyroDetector gyroDetector;

    private void Start()
    {
        if (gyroDetector == null)
        {
            Debug.LogWarning("[StartDetection] GyroDetector not assigned");
            return;
        }

        gyroDetector.StartDetection();
    }
}
