using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.InputSystem;

public class GyroDetectionManager : MonoBehaviour
{
    [Header("Managers")]
    public G3GameManager gameManager;
    public WeaponSelectManager weaponManager;

    [Header("UI Panels")]
    public GameObject placePhonePanel;
    public GameObject countdownPanel;
    public GameObject weaponSelectPanel;
    public GameObject eliminatedPanel;

    [Header("UI Elements")]
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI debugText;
    public TextMeshProUGUI stillnessCountdownText;
    public UnityEngine.UI.Slider stillnessProgressBar;

    [Header("Detection Settings")]
    [Tooltip("Enable debug mode to see gyro values")]
    public bool debugMode = true;

    [Tooltip("Enable editor testing mode (works without gyroscope)")]
    public bool editorTestMode = true;

    [Header("Input Settings")]
    [Tooltip("Use new Input System (recommended for projects using Input System package)")]
    public bool useNewInputSystem = true;

    [Tooltip("Minimum time (seconds) device must remain still to trigger countdown")]
    public float stillnessThresholdTime = 1f;

    [Tooltip("Maximum allowed movement to be considered 'still'")]
    public float stillnessSensitivity = 0.5f;

    [Tooltip("Target angle: 0° = flat on table (screen up), 90° = vertical standing")]
    public float targetAngle = 0f;

    [Tooltip("Allowed deviation from target angle (± degrees). 15° means accepts 0°-15° range")]
    public float angleTolerance = 15f;

    [Tooltip("Countdown duration")]
    public float countdownDuration = 3f;

    [Header("Early Pickup Settings")]
    [Tooltip("Time window (seconds) after countdown when pickup is valid")]
    public float validPickupWindow = 2f;

    // State tracking
    private float stillnessTimer = 0f;
    private float countdownTimer = 0f;
    private float countdownEndTime = 0f;
    private bool isCountingDown = false;
    private bool canPickup = false;

    // Gyroscope data for comparison
    private Vector3 lastGyroRotation;
    private Vector3 lastAcceleration;

    private void Start()
    {
        Debug.Log($"[GyroDetectionManager] Start - Editor Mode: {Application.isEditor}, Test Mode: {editorTestMode}");
        EnableGyroscope();
        ShowPlacePhonePanel();

        // Initialize UI elements
        if (stillnessProgressBar != null)
        {
            stillnessProgressBar.value = 0f;
            stillnessProgressBar.maxValue = 1f;
        }
        if (stillnessCountdownText != null)
        {
            stillnessCountdownText.text = Mathf.CeilToInt(stillnessThresholdTime).ToString();
        }
    }

    private void EnableGyroscope()
    {
        bool inEditor = Application.isEditor;

        if (!inEditor && !SystemInfo.supportsGyroscope)
        {
            Debug.LogWarning("Gyroscope not supported on this device");
            if (statusText != null)
                statusText.text = "Gyroscope not supported";
            return;
        }

        if (inEditor && editorTestMode)
        {
            Debug.Log("Running in Editor Test Mode - use keyboard controls");
            if (statusText != null)
                statusText.text = "Editor Mode: Press SPACE to start";
            Input.gyro.enabled = false;
            return;
        }

        Input.gyro.enabled = true;
        lastGyroRotation = Input.gyro.rotationRate;
        lastAcceleration = Input.acceleration;

        Debug.Log("Gyroscope enabled");
    }

    private void Update()
    {
        bool inEditor = Application.isEditor && editorTestMode;

        if (inEditor)
        {
            HandleEditorTestInput();
        }

        if (!inEditor && !Input.gyro.enabled) return;

        switch (GetCurrentState())
        {
            case GameState.WaitingForStill:
                CheckForStillness();
                break;
            case GameState.CountingDown:
                RunCountdown();
                break;
            case GameState.CanPickup:
                CheckForPickup();
                break;
        }

        if (debugMode && debugText != null)
        {
            UpdateDebugText(inEditor);
        }
    }

    private void HandleEditorTestInput()
    {
        bool spacePressed = false;
        bool pPressed = false;
        bool ePressed = false;
        bool rPressed = false;

        if (useNewInputSystem && Keyboard.current != null)
        {
            // New Input System
            spacePressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            pPressed = Keyboard.current.pKey.wasPressedThisFrame;
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
            rPressed = Keyboard.current.rKey.wasPressedThisFrame;
        }
        else
        {
            // Old Input System (fallback)
            spacePressed = Input.GetKeyDown(KeyCode.Space);
            pPressed = Input.GetKeyDown(KeyCode.P);
            ePressed = Input.GetKeyDown(KeyCode.E);
            rPressed = Input.GetKeyDown(KeyCode.R);
        }

        if (spacePressed)
        {
            Debug.Log("[GyroDetectionManager] SPACE pressed - Starting countdown");
            TestSimulateStillPhone();
        }
        if (pPressed)
        {
            Debug.Log("[GyroDetectionManager] P pressed - Attempting pickup");
            SimulatePickup();
        }
        if (ePressed)
        {
            Debug.Log("[GyroDetectionManager] E pressed - Force early pickup (eliminated)");
            OnEarlyPickup();
        }
        if (rPressed)
        {
            Debug.Log("[GyroDetectionManager] R pressed - Resetting round");
            ResetRound();
        }
    }

    private void SimulatePickup()
    {
        Debug.Log($"[GyroDetectionManager] Pickup attempted - canPickup = {canPickup}");

        if (canPickup)
        {
            // Successful pickup - at the right time
            OnSuccessfulPickup();
            canPickup = false;
        }
        else if (isCountingDown)
        {
            // Early pickup during countdown - eliminated
            Debug.Log("[GyroDetectionManager] Picked up during countdown - ELIMINATED!");
            OnEarlyPickup();
        }
        else
        {
            Debug.Log("[GyroDetectionManager] Pickup not valid - no active countdown");
        }
    }

    // Public methods for test helper
    public void TestSimulateSuccessfulPickup()
    {
        if (canPickup)
        {
            OnSuccessfulPickup();
            canPickup = false;
        }
    }

    public void TestSimulateEarlyPickup()
    {
        OnEarlyPickup();
    }

    private void UpdateDebugText(bool inEditor)
    {
        string debugInfo = "";
        Vector3 gyro = Input.gyro.rotationRate;
        Vector3 accel = Input.acceleration;

        if (inEditor)
        {
            debugInfo = $"[EDITOR MODE]\n" +
                       $"State: {GetCurrentState()}\n" +
                       $"SPACE: Start | P: Pickup (auto) | E: Force elim | R: Reset\n" +
                       $"Still Timer: {stillnessTimer:F2}s\n" +
                       $"Can Pickup: {canPickup}";
        }
        else
        {
            float currentAngle = Vector3.Angle(accel, Vector3.down);
            float angleDifference = Mathf.Abs(currentAngle - targetAngle);
            float stillnessProgress = 0f;
            if (GetCurrentState() == GameState.WaitingForStill && stillnessThresholdTime > 0)
            {
                stillnessProgress = (stillnessTimer / stillnessThresholdTime) * 100f;
            }
            debugInfo = $"[GYRO DEBUG]\n" +
                       $"Gyro Enabled: {Input.gyro.enabled}\n" +
                       $"Supported: {SystemInfo.supportsGyroscope}\n" +
                       $"State: {GetCurrentState()}\n" +
                       $"Gyro: ({gyro.x:F2}, {gyro.y:F2}, {gyro.z:F2})\n" +
                       $"Accel: ({accel.x:F2}, {accel.y:F2}, {accel.z:F2})\n" +
                       $"Angle: {currentAngle:F1}° (Target: {targetAngle}° ± {angleTolerance}°)\n" +
                       $"Stillness: {stillnessProgress:F0}%\n" +
                       $"Correct: {angleDifference <= angleTolerance}";
        }

        // Use debugText if assigned, otherwise fallback to statusText
        if (debugText != null)
        {
            debugText.text = debugInfo;
        }
        else if (statusText != null)
        {
            // Fallback - show simplified debug in status text
            statusText.text = inEditor ? $"Editor: {GetCurrentState()}" : $"State: {GetCurrentState()}";
        }
    }

    private void CheckForStillness()
    {
        Vector3 currentGyroRotation = Input.gyro.rotationRate;
        Vector3 currentAcceleration = Input.acceleration;

        // Calculate movement deltas
        float gyroDelta = Vector3.Distance(currentGyroRotation, lastGyroRotation);
        float accelDelta = Vector3.Distance(currentAcceleration, lastAcceleration);

        // Check if device is at correct angle
        // Vector3.Angle(accel, Vector3.down) gives: 0°=flat on table, 90°=vertical
        float currentAngle = Vector3.Angle(currentAcceleration, Vector3.down);

        // Check if within target angle ± tolerance
        float angleDifference = Mathf.Abs(currentAngle - targetAngle);
        bool isCorrectAngle = angleDifference <= angleTolerance;

        // Check if device is still
        bool isStill = (gyroDelta < stillnessSensitivity) && (accelDelta < stillnessSensitivity);

        if (isCorrectAngle && isStill)
        {
            stillnessTimer += Time.deltaTime;
            float progress = Mathf.Min(stillnessTimer / stillnessThresholdTime, 1f);
            float remainingTime = Mathf.Max(0, stillnessThresholdTime - stillnessTimer);

            // Update status text
            if (statusText != null)
            {
                statusText.text = "Hold still...";
            }

            // Update countdown text
            if (stillnessCountdownText != null)
            {
                stillnessCountdownText.text = $"{Mathf.CeilToInt(remainingTime)}";
            }

            // Update progress bar
            if (stillnessProgressBar != null)
            {
                stillnessProgressBar.value = progress;
            }

            if (stillnessTimer >= stillnessThresholdTime)
            {
                StartCountdown();
            }
        }
        else
        {
            stillnessTimer = 0f;

            // Reset progress bar
            if (stillnessProgressBar != null)
            {
                stillnessProgressBar.value = 0f;
            }

            // Reset countdown text
            if (stillnessCountdownText != null)
            {
                stillnessCountdownText.text = Mathf.CeilToInt(stillnessThresholdTime).ToString();
            }

            if (statusText != null)
            {
                if (!isCorrectAngle)
                {
                    statusText.text = $"Place phone flat (currently {currentAngle:F0}°)";
                }
                else
                {
                    statusText.text = "Hold still...";
                }
            }
        }

        // Update last values
        lastGyroRotation = currentGyroRotation;
        lastAcceleration = currentAcceleration;
    }

    private void StartCountdown()
    {
        isCountingDown = true;
        countdownTimer = countdownDuration;
        stillnessTimer = 0f;

        ShowCountdownPanel();

        if (countdownText != null)
        {
            countdownText.text = countdownDuration.ToString();
        }

        Debug.Log("Countdown started");
    }

    private void RunCountdown()
    {
        countdownTimer -= Time.deltaTime;

        // Check for pickup during countdown (elimination condition)
        Vector3 currentGyroRotation = Input.gyro.rotationRate;
        Vector3 currentAcceleration = Input.acceleration;

        float gyroMovement = currentGyroRotation.magnitude;
        float accelDelta = Vector3.Distance(currentAcceleration, lastAcceleration);
        float currentAngle = Vector3.Angle(currentAcceleration, Vector3.down);
        float angleDifference = Mathf.Abs(currentAngle - targetAngle);

        // Phone picked up during countdown = eliminated
        bool pickedUpDuringCountdown = gyroMovement > 1f || accelDelta > 0.5f || angleDifference > angleTolerance * 1.5f;

        if (pickedUpDuringCountdown)
        {
            Debug.Log("[GyroDetectionManager] Phone picked up during countdown - ELIMINATED!");
            OnEarlyPickup();
            return;
        }

        // Update last values for next frame
        lastGyroRotation = currentGyroRotation;
        lastAcceleration = currentAcceleration;

        if (countdownText != null)
        {
            int displayValue = Mathf.CeilToInt(countdownTimer);
            countdownText.text = displayValue.ToString();

            // Optional: Add a pulse effect
            countdownText.transform.localScale = Vector3.one * (1f + (countdownTimer % 1f) * 0.1f);
        }

        if (countdownTimer <= 0f)
        {
            countdownTimer = 0f;
            isCountingDown = false;
            countdownEndTime = Time.time;
            canPickup = true;
            Debug.Log("[GyroDetectionManager] Countdown finished - canPickup = true. Press P now!");
            ShowWeaponSelectPanel();
        }
    }

    private void CheckForPickup()
    {
        // Detect pickup by significant movement or tilt change
        Vector3 currentGyroRotation = Input.gyro.rotationRate;
        Vector3 currentAcceleration = Input.acceleration;

        float gyroMovement = currentGyroRotation.magnitude;
        float accelDelta = Vector3.Distance(currentAcceleration, lastAcceleration);
        float currentAngle = Vector3.Angle(currentAcceleration, Vector3.down);
        float angleDifference = Mathf.Abs(currentAngle - targetAngle);

        // Device picked up if there's significant movement or angle changes too much
        bool devicePickedUp = gyroMovement > 1f || accelDelta > 0.5f || angleDifference > angleTolerance * 2f;

        if (devicePickedUp)
        {
            float timeSinceCountdown = Time.time - countdownEndTime;

            if (timeSinceCountdown <= validPickupWindow)
            {
                // Valid pickup - player can select weapon
                OnSuccessfulPickup();
            }
            else
            {
                // Too early - player eliminated
                OnEarlyPickup();
            }

            canPickup = false;
        }

        lastGyroRotation = currentGyroRotation;
        lastAcceleration = currentAcceleration;
    }

    private void OnSuccessfulPickup()
    {
        Debug.Log("Successful pickup! Weapon selection available.");

        if (weaponManager != null)
        {
            weaponManager.StartWeaponSelection();
        }
    }

    private void OnEarlyPickup()
    {
        Debug.Log("Early pickup! Player eliminated.");
        ShowEliminatedPanel();

        if (G3GameManager.Instance != null)
        {
            G3GameManager.Instance.OnPlayerEliminated();
        }
    }

    private GameState GetCurrentState()
    {
        if (canPickup) return GameState.CanPickup;
        if (isCountingDown) return GameState.CountingDown;
        return GameState.WaitingForStill;
    }

    // UI Panel Management
    private void ShowPlacePhonePanel()
    {
        HideAllPanels();
        if (placePhonePanel != null) placePhonePanel.SetActive(true);
    }

    private void ShowCountdownPanel()
    {
        HideAllPanels();
        if (countdownPanel != null) countdownPanel.SetActive(true);
    }

    private void ShowWeaponSelectPanel()
    {
        HideAllPanels();
        if (weaponSelectPanel != null) weaponSelectPanel.SetActive(true);
        Debug.Log("[GyroDetectionManager] Weapon Select Panel shown - Press P to pickup");
    }

    private void ShowEliminatedPanel()
    {
        HideAllPanels();
        if (eliminatedPanel != null) eliminatedPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        if (placePhonePanel != null) placePhonePanel.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);
        if (weaponSelectPanel != null) weaponSelectPanel.SetActive(false);
        if (eliminatedPanel != null) eliminatedPanel.SetActive(false);
    }

    // Public method to reset the game state
    public void ResetRound()
    {
        isCountingDown = false;
        canPickup = false;
        stillnessTimer = 0f;
        countdownTimer = countdownDuration;
        ShowPlacePhonePanel();
    }

    // Test method to simulate phone being still (for editor testing)
    [ContextMenu("Test: Simulate Still Phone")]
    public void TestSimulateStillPhone()
    {
        Debug.Log("[GyroDetectionManager] TestSimulateStillPhone called");
        StartCountdown();
    }

    private void OnDestroy()
    {
        if (Input.gyro.enabled)
        {
            Input.gyro.enabled = false;
        }
    }

    private enum GameState
    {
        WaitingForStill,
        CountingDown,
        CanPickup
    }
}
