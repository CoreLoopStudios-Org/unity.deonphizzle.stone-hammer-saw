using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Helper script for testing GyroDetectionManager in Unity Editor.
/// Attach this to the same GameObject as GyroDetectionManager.
/// Provides keyboard shortcuts to simulate different states.
/// </summary>
public class GyroDetectionTestHelper : MonoBehaviour
{
    [Header("References")]
    public GyroDetectionManager gyroManager;
    public WeaponSelectManager weaponManager;

    [Header("Input Settings")]
    [Tooltip("Use new Input System (recommended for projects using Input System package)")]
    public bool useNewInputSystem = true;

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
        bool spacePressed = false;
        bool cPressed = false;
        bool pPressed = false;
        bool ePressed = false;
        bool rPressed = false;
        bool onePressed = false;
        bool twoPressed = false;
        bool threePressed = false;

        if (useNewInputSystem && Keyboard.current != null)
        {
            // New Input System
            spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            cPressed = Keyboard.current.cKey.wasPressedThisFrame;
            pPressed = Keyboard.current.pKey.wasPressedThisFrame;
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
            rPressed = Keyboard.current.rKey.wasPressedThisFrame;
            onePressed = Keyboard.current.digit1Key.wasPressedThisFrame;
            twoPressed = Keyboard.current.digit2Key.wasPressedThisFrame;
            threePressed = Keyboard.current.digit3Key.wasPressedThisFrame;
        }
        else
        {
            // Old Input System (fallback)
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            cPressed = Input.GetKeyDown(KeyCode.C);
            pPressed = Input.GetKeyDown(KeyCode.P);
            ePressed = Input.GetKeyDown(KeyCode.E);
            rPressed = Input.GetKeyDown(KeyCode.R);
            onePressed = Input.GetKeyDown(KeyCode.Alpha1);
            twoPressed = Input.GetKeyDown(KeyCode.Alpha2);
            threePressed = Input.GetKeyDown(KeyCode.Alpha3);
        }

        // Space: Simulate phone being still on table
        if (spacePressed)
        {
            SimulateStillPhone();
        }

        // C: Simulate countdown (skip stillness detection)
        if (cPressed)
        {
            SimulateCountdown();
        }

        // P: Simulate successful pickup (at correct time)
        if (pPressed)
        {
            SimulatePickup(successful: true);
        }

        // E: Simulate early pickup (before valid time)
        if (ePressed)
        {
            SimulatePickup(successful: false);
        }

        // R: Reset round
        if (rPressed)
        {
            SimulateReset();
        }

        // Number keys: Select weapons
        if (onePressed)
        {
            SimulateWeaponSelection(0);
        }
        if (twoPressed)
        {
            SimulateWeaponSelection(1);
        }
        if (threePressed)
        {
            SimulateWeaponSelection(2);
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
        if (gyroManager == null)
        {
            Debug.LogError("GyroDetectionManager reference not set!");
            return;
        }

        Debug.Log($"TEST: Simulating {(successful ? "successful" : "early")} pickup...");

        if (successful)
        {
            gyroManager.TestSimulateSuccessfulPickup();
        }
        else
        {
            gyroManager.TestSimulateEarlyPickup();
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

    [ContextMenu("Test: Select Weapon")]
    public void SimulateWeaponSelection(int weaponId = 0)
    {
        if (weaponManager == null)
        {
            Debug.LogError("WeaponSelectManager reference not set!");
            return;
        }

        Debug.Log($"TEST: Selecting weapon {weaponId}...");
        weaponManager.SelectWeapon(weaponId);
    }

    private void OnGUI()
    {
        if (!enableTesting) return;

        GUILayout.BeginArea(new Rect(10, 10, 380, 250));
        GUILayout.Box("Gyro Test Controls");
        GUILayout.Label("GYRO TEST:");
        GUILayout.Label("Space - Start countdown");
        GUILayout.Label("P - Try pickup (auto-detects timing)");
        GUILayout.Label("E - Force elimination (early pickup)");
        GUILayout.Label("R - Reset round");
        GUILayout.Label("");
        GUILayout.Label("WEAPON TEST:");
        GUILayout.Label("1, 2, 3 - Select weapon");
        GUILayout.Label("");
        GUILayout.Label("During countdown: P = eliminated");
        GUILayout.Label("After countdown: P = success");
        GUILayout.EndArea();
    }
}
