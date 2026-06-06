using UnityEngine;
using TMPro;
using DG.Tweening;
using System;

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

    [Header("Detection Settings")]
    [Tooltip("Minimum time (seconds) device must remain still to trigger countdown")]
    public float stillnessThresholdTime = 0.5f;

    [Tooltip("Maximum allowed movement to be considered 'still'")]
    public float stillnessSensitivity = 0.3f;

    [Tooltip("Maximum tilt angle from flat to be considered 'on table'")]
    public float maxTiltAngle = 20f;

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
        EnableGyroscope();
        ShowPlacePhonePanel();
    }

    private void EnableGyroscope()
    {
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.LogWarning("Gyroscope not supported on this device");
            if (statusText != null)
                statusText.text = "Gyroscope not supported";
            return;
        }

        Input.gyro.enabled = true;
        lastGyroRotation = Input.gyro.rotationRate;
        lastAcceleration = Input.acceleration;

        Debug.Log("Gyroscope enabled");
    }

    private void Update()
    {
        if (!Input.gyro.enabled) return;

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
    }

    private void CheckForStillness()
    {
        Vector3 currentGyroRotation = Input.gyro.rotationRate;
        Vector3 currentAcceleration = Input.acceleration;

        // Calculate movement deltas
        float gyroDelta = Vector3.Distance(currentGyroRotation, lastGyroRotation);
        float accelDelta = Vector3.Distance(currentAcceleration, lastAcceleration);

        // Check if device is flat (using acceleration - gravity points down when flat)
        float tiltFromFlat = Vector3.Angle(currentAcceleration, Vector3.down);
        bool isFlat = tiltFromFlat <= maxTiltAngle;

        // Check if device is still
        bool isStill = (gyroDelta < stillnessSensitivity) && (accelDelta < stillnessSensitivity);

        if (isFlat && isStill)
        {
            stillnessTimer += Time.deltaTime;

            if (statusText != null)
            {
                float progress = Mathf.Min(stillnessTimer / stillnessThresholdTime, 1f);
                statusText.text = $"Hold still... {Mathf.FloorToInt(progress * 100)}%";
            }

            if (stillnessTimer >= stillnessThresholdTime)
            {
                StartCountdown();
            }
        }
        else
        {
            stillnessTimer = 0f;
            if (statusText != null)
                statusText.text = isFlat ? "Hold still..." : "Place phone flat on table";
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
        float tiltFromFlat = Vector3.Angle(currentAcceleration, Vector3.down);

        // Device picked up if there's significant movement or tilt change
        bool devicePickedUp = gyroMovement > 1f || accelDelta > 0.5f || tiltFromFlat > maxTiltAngle;

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
