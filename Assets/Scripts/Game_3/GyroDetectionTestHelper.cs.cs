using UnityEngine;

/// <summary>
/// Helper script for testing GyroDetectionManager in Unity Editor.
/// Attach this to the same GameObject as GyroDetectionManager.
/// Provides keyboard shortcuts to simulate different states.
/// </summary>
public class GyroDetectionTestHelper : MonoBehaviour
{
    [Header("References")]
    public GyroDetectionManager gyroManager;

    [Header("Test Settings")]
    [Tooltip("When enabled, allows keyboard simulation of gyro states")]
    public bool enableTesting = true;

    [Tooltip("Simulated stillness threshold for testing")]
    public float simulatedStillnessDuration = 0.5f;

    private float simulationTimer = 0f;
    private bool simulatingStillness = false;
    private bool simulatedCountdownActive = false;
    private float simulatedCountdownTimer = 0f;

    private void Update()
    {
        if (!enableTesting) return;

        HandleTestInput();
    }

    private void HandleTestInput()
    {
        // Space: Simulate phone being still on table
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SimulateStillPhone();
        }

        // C: Simulate countdown (skip stillness detection)
        if (Input.GetKeyDown(KeyCode.C))
        {
            SimulateCountdown();
        }

        // P: Simulate successful pickup (at correct time)
        if (Input.GetKeyDown(KeyCode.P))
        {
            SimulatePickup(successful: true);
        }

        // E: Simulate early pickup (before valid time)
        if (Input.GetKeyDown(KeyCode.E))
        {
            SimulatePickup(successful: false);
        }

        // R: Reset round
        if (Input.GetKeyDown(KeyCode.R))
        {
            SimulateReset();
        }
    }

    [ContextMenu("Test: Simulate Still Phone")]
    public void SimulateStillPhone()
    {
        if (gyroManager == null)
        {
            Debug.LogError("GyroDetectionManager reference not set!");
            return;
        }

        Debug.Log("TEST: Simulating phone still on table...");
        gyroManager.TestSimulateStillPhone();
    }

    [ContextMenu("Test: Force Start Countdown")]
    public void SimulateCountdown()
    {
        if (gyroManager == null)
        {
            Debug.LogError("GyroDetectionManager reference not set!");
            return;
        }

        Debug.Log("TEST: Forcing countdown start...");
        gyroManager.TestSimulateStillPhone();
    }

    [ContextMenu("Test: Simulate Successful Pickup")]
    public void SimulatePickup(bool successful = true)
    {
        Debug.Log($"TEST: Simulating {(successful ? "successful" : "early")} pickup...");

        // This would need GyroDetectionManager to expose a test method
        // For now, this is a placeholder for the testing framework
        if (successful)
        {
            Debug.Log("TEST: Player picked up at correct time - weapon selection should show");
        }
        else
        {
            Debug.Log("TEST: Player picked up too early - should be eliminated");
        }
    }

    [ContextMenu("Test: Reset Round")]
    public void SimulateReset()
    {
        if (gyroManager == null)
        {
            Debug.LogError("GyroDetectionManager reference not set!");
            return;
        }

        Debug.Log("TEST: Resetting round...");
        gyroManager.ResetRound();
    }

    private void OnGUI()
    {
        if (!enableTesting) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Gyro Test Controls");
        GUILayout.Label("Space - Simulate still phone");
        GUILayout.Label("C - Force start countdown");
        GUILayout.Label("P - Simulate successful pickup");
        GUILayout.Label("E - Simulate early pickup");
        GUILayout.Label("R - Reset round");
        GUILayout.EndArea();
    }
}
