using UnityEngine;
using UnityEngine.Events;

namespace Game3
{
    public class GyroDetector : MonoBehaviour
    {
        #region Events

        [Header("Events")]
        [Tooltip("Fired when stillness detection starts")]
        public UnityEvent onDetectionStarted = new();

        [Tooltip("Fired continuously during stillness detection with progress (0-1)")]
        public UnityEvent<float> onStillnessProgress = new();

        [Tooltip("Fired when phone is stable and stillness threshold reached")]
        public UnityEvent onPhoneStable = new();

        [Tooltip("Fired when stillness is lost before reaching threshold")]
        public UnityEvent onStillnessLost = new();

        [Tooltip("Fired when countdown starts, parameter is countdown duration")]
        public UnityEvent<float> onCountdownStarted = new();

        [Tooltip("Fired continuously during countdown with remaining time")]
        public UnityEvent<float> onCountdownTick = new();

        [Tooltip("Fired when countdown completes - ready for pickup")]
        public UnityEvent onCountdownComplete = new();

        [Tooltip("Fired when valid pickup is detected")]
        public UnityEvent onPickupSuccess = new();

        [Tooltip("Fired when early pickup is detected")]
        public UnityEvent onEarlyPickup = new();

        [Tooltip("Fired when pickup window expires without pickup")]
        public UnityEvent onPickupWindowExpired = new();

        [Tooltip("Fired when pickup is too late (after pickup window duration)")]
        public UnityEvent onTooLate = new();

        #endregion

        #region Settings

        [Header("Detection Settings")]
        [Tooltip("Enable editor testing mode (works without gyroscope)")]
        public bool editorTestMode = false;

        [Tooltip("Show debug info in console")]
        public bool debugMode = false;

        [Header("Stillness Settings")]
        [Tooltip("Minimum time (seconds) device must remain still")]
        public float stillnessThresholdTime = 1f;

        [Tooltip("Maximum allowed movement to be considered 'still'")]
        public float stillnessSensitivity = 0.5f;

        [Tooltip("Target angle: 0° = flat on table (screen up), 90° = vertical standing")]
        public float targetAngle = 0f;

        [Tooltip("Allowed deviation from target angle (± degrees)")]
        public float angleTolerance = 15f;

        [Header("Countdown Settings")]
        [Tooltip("Countdown duration after phone is stable")]
        public float countdownDuration = 3f;

        [Tooltip("Total pickup window duration (seconds) after countdown for valid pickup")]
        public float pickupWindowDuration = 5f;

        [Header("Pickup Detection")]
        [Tooltip("Gyro movement threshold to detect pickup")]
        public float pickupGyroThreshold = 1f;

        [Tooltip("Acceleration delta threshold to detect pickup")]
        public float pickupAccelThreshold = 0.5f;

        [Tooltip("Angle multiplier for pickup detection")]
        public float pickupAngleMultiplier = 2f;

        #endregion

        #region State

        public GyroState CurrentState { get; private set; } = GyroState.Idle;

        private float _stillnessTimer;
        private float _countdownTimer;
        private float _countdownEndTime;
        private Vector3 _lastGyroRotation;
        private Vector3 _lastAcceleration;

        #endregion

        #region Lifecycle

        private void Start()
        {
            EnableGyroscope();
        }

        private void Update()
        {
            if (editorTestMode && Application.isEditor)
            {
                HandleEditorTestInput();
                return;
            }

            if (!Input.gyro.enabled) return;

            UpdateDetection();
        }

        private void OnDestroy()
        {
            if (Input.gyro.enabled) Input.gyro.enabled = false;
        }

        #endregion

        #region Public Control

        /// <summary>
        /// Start detecting for stable phone position
        /// </summary>
        public void StartDetection()
        {
            if (CurrentState != GyroState.Idle && CurrentState != GyroState.Completed && CurrentState != GyroState.Eliminated)
            {
                if (debugMode) Debug.LogWarning("[GyroDetector] Already detecting, ignored");
                return;
            }

            ResetState();
            SetState(GyroState.DetectingStillness);
            onDetectionStarted.Invoke();

            if (debugMode) Debug.Log("[GyroDetector] Detection started");
        }

        /// <summary>
        /// Stop and reset detection
        /// </summary>
        public void StopDetection()
        {
            ResetState();
        }

        /// <summary>
        /// Force trigger countdown (for testing)
        /// </summary>
        public void ForceCountdown()
        {
            StartCountdown();
        }

        /// <summary>
        /// Force trigger successful pickup (for testing)
        /// </summary>
        public void ForceSuccess()
        {
            OnPickupSuccess();
        }

        /// <summary>
        /// Force trigger early pickup (for testing)
        /// </summary>
        public void ForceEarlyPickup()
        {
            OnEarlyPickup();
        }

        #endregion

        #region Detection Logic

        private void UpdateDetection()
        {
            Vector3 currentGyro = Input.gyro.rotationRate;
            Vector3 currentAccel = Input.acceleration;

            switch (CurrentState)
            {
                case GyroState.DetectingStillness:
                    CheckStillness(currentGyro, currentAccel);
                    break;

                case GyroState.CountingDown:
                    CheckCountdown(currentGyro, currentAccel);
                    break;

                case GyroState.ReadyForPickup:
                    CheckPickup(currentGyro, currentAccel);
                    break;
            }

            _lastGyroRotation = currentGyro;
            _lastAcceleration = currentAccel;
        }

        private void CheckStillness(Vector3 gyro, Vector3 accel)
        {
            float gyroDelta = Vector3.Distance(gyro, _lastGyroRotation);
            float accelDelta = Vector3.Distance(accel, _lastAcceleration);

            float currentAngle = Vector3.Angle(accel, Vector3.down);
            bool isCorrectAngle = Mathf.Abs(currentAngle - targetAngle) <= angleTolerance;
            bool isStill = gyroDelta < stillnessSensitivity && accelDelta < stillnessSensitivity;

            if (isCorrectAngle && isStill)
            {
                _stillnessTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(_stillnessTimer / stillnessThresholdTime);
                onStillnessProgress.Invoke(progress);

                if (_stillnessTimer >= stillnessThresholdTime)
                {
                    SetState(GyroState.Stable);
                    onPhoneStable.Invoke();
                    StartCountdown();
                }
            }
            else
            {
                if (_stillnessTimer > 0.1f)
                {
                    onStillnessLost.Invoke();
                }
                _stillnessTimer = 0f;
                onStillnessProgress.Invoke(0f);
            }
        }

        private void CheckCountdown(Vector3 gyro, Vector3 accel)
        {
            _countdownTimer -= Time.deltaTime;
            onCountdownTick.Invoke(_countdownTimer);

            // Check for early pickup
            if (IsPickupDetected(gyro, accel, angleMultiplier: 1.5f))
            {
                OnEarlyPickup();
                return;
            }

            if (_countdownTimer <= 0f)
            {
                _countdownEndTime = Time.time;
                SetState(GyroState.ReadyForPickup);
                onCountdownComplete.Invoke();
            }
        }

        private void CheckPickup(Vector3 gyro, Vector3 accel)
        {
            float elapsed = Time.time - _countdownEndTime;

            // Check for pickup
            if (IsPickupDetected(gyro, accel, pickupAngleMultiplier))
            {
                if (elapsed <= pickupWindowDuration)
                {
                    // Within pickup window - success
                    OnPickupSuccess();
                }
                else
                {
                    // Too late - eliminated
                    OnTooLate();
                }
                return;
            }

            // Check if window expired (no pickup at all)
            if (elapsed > pickupWindowDuration)
            {
                SetState(GyroState.Completed);
                onPickupWindowExpired.Invoke();
            }
        }

        private bool IsPickupDetected(Vector3 gyro, Vector3 accel, float angleMultiplier)
        {
            float gyroMovement = gyro.magnitude;
            float accelDelta = Vector3.Distance(accel, _lastAcceleration);
            float currentAngle = Vector3.Angle(accel, Vector3.down);
            float angleDiff = Mathf.Abs(currentAngle - targetAngle);

            return gyroMovement > pickupGyroThreshold ||
                   accelDelta > pickupAccelThreshold ||
                   angleDiff > angleTolerance * angleMultiplier;
        }

        private void StartCountdown()
        {
            _countdownTimer = countdownDuration;
            _stillnessTimer = 0f;
            SetState(GyroState.CountingDown);
            onCountdownStarted.Invoke(countdownDuration);

            if (debugMode) Debug.Log("[GyroDetector] Countdown started");
        }

        private void OnPickupSuccess()
        {
            SetState(GyroState.Completed);
            onPickupSuccess.Invoke();

            if (debugMode) Debug.Log("[GyroDetector] Pickup success");
        }

        private void OnEarlyPickup()
        {
            SetState(GyroState.Eliminated);
            onEarlyPickup.Invoke();

            if (debugMode) Debug.Log("[GyroDetector] Early pickup - eliminated");
        }

        private void OnTooLate()
        {
            SetState(GyroState.Eliminated);
            onTooLate.Invoke();

            if (debugMode) Debug.Log("[GyroDetector] Too late - eliminated");
        }

        #endregion

        #region State Management

        private void SetState(GyroState newState)
        {
            CurrentState = newState;
        }

        private void ResetState()
        {
            _stillnessTimer = 0f;
            _countdownTimer = countdownDuration;
            SetState(GyroState.Idle);
        }

        #endregion

        #region Editor Testing

        private void HandleEditorTestInput()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (debugMode) Debug.Log("[GyroDetector] SPACE - Start detection");
                StartDetection();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                if (debugMode) Debug.Log("[GyroDetector] C - Force countdown");
                ForceCountdown();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (debugMode) Debug.Log("[GyroDetector] S - Force success");
                ForceSuccess();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (debugMode) Debug.Log("[GyroDetector] E - Force early pickup");
                ForceEarlyPickup();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (debugMode) Debug.Log("[GyroDetector] R - Reset");
                StopDetection();
            }
        }

        #endregion

        #region Gyro Setup

        private void EnableGyroscope()
        {
            bool inEditor = Application.isEditor;

            if (!inEditor && !SystemInfo.supportsGyroscope)
            {
                Debug.LogWarning("[GyroDetector] Gyroscope not supported");
                return;
            }

            if (inEditor && editorTestMode)
            {
                if (debugMode) Debug.Log("[GyroDetector] Editor test mode enabled");
                Input.gyro.enabled = false;
                return;
            }

            Input.gyro.enabled = true;
            _lastGyroRotation = Input.gyro.rotationRate;
            _lastAcceleration = Input.acceleration;

            if (debugMode) Debug.Log("[GyroDetector] Gyroscope enabled");
        }

        #endregion
    }
}
