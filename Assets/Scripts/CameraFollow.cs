using UnityEngine;

/// <summary>
/// Simple camera follow behavior used by MobSquadGameManager.
/// If you have a more advanced camera system, replace this with your own implementation.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    /// <summary>Target transform that the camera will follow.</summary>
    public Transform target;
    /// <summary>Offset from the target position.</summary>
    public Vector3 offset = new Vector3(0f, 2f, -5f);

    private void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
            transform.LookAt(target.position + Vector3.up * 1f);
        }
    }
}
