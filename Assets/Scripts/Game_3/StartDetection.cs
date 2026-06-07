using Game3;
using UnityEngine;

public class StartDetection : MonoBehaviour
{
    public GyroDetector gyroDetector;

    private void Start()
    {
        gyroDetector.StartDetection();
    }
}
