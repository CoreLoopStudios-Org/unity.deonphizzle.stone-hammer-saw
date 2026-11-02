using UnityEngine;
using TMPro;

namespace Game3
{
    public class GyroUIHandler : MonoBehaviour
    {
        [Header("References")]
        public GyroDetector gyroDetector;

        [Header("Stillness UI")]
        public UnityEngine.UI.Slider stillnessSlider;
        public TextMeshProUGUI stillnessCountdownText;
        public TextMeshProUGUI statusText;

        [Header("Countdown UI")]
        public TextMeshProUGUI countdownText;

        [Header("Messages")]
        [Tooltip("Message shown when detection starts")]
        public string startMessage = "Place phone flat on surface...";

        [Tooltip("Message shown while holding still")]
        public string holdingMessage = "Hold still...";

        [Tooltip("Message shown when angle is wrong")]
        public string wrongAngleMessage = "Place phone flat";

        [Tooltip("Message when stillness is lost")]
        public string stillnessLostMessage = "Hold still...";

        [Tooltip("Message when the phone is picked up inside the window")]
        public string pickupSuccessMessage = "In time!";
        [Tooltip("Message when the phone is picked up before the countdown ends")]
        public string earlyPickupMessage = "Too early!";
        [Tooltip("Message when the phone is picked up after the window")]
        public string tooLateMessage = "Too late!";
        [Tooltip("Message when the window ends with no pickup")]
        public string windowExpiredMessage = "Time is up!";

        private void Start()
        {
            if (gyroDetector == null)
            {
                Debug.LogWarning("[GyroUIHandler] GyroDetector not assigned");
                return;
            }

            SubscribeEvents();
        }

        private void OnDestroy()
        {
            if (gyroDetector != null)
                UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            gyroDetector.onDetectionStarted.AddListener(OnDetectionStarted);
            gyroDetector.onStillnessProgress.AddListener(OnStillnessProgress);
            gyroDetector.onStillnessLost.AddListener(OnStillnessLost);
            gyroDetector.onCountdownStarted.AddListener(OnCountdownStarted);
            gyroDetector.onCountdownTick.AddListener(OnCountdownTick);
            gyroDetector.onCountdownComplete.AddListener(OnCountdownComplete);
            gyroDetector.onPickupSuccess.AddListener(OnPickupSuccess);
            gyroDetector.onEarlyPickup.AddListener(OnEarlyPickup);
            gyroDetector.onTooLate.AddListener(OnTooLate);
            gyroDetector.onPickupWindowExpired.AddListener(OnPickupWindowExpired);
        }

        private void UnsubscribeEvents()
        {
            gyroDetector.onDetectionStarted.RemoveListener(OnDetectionStarted);
            gyroDetector.onStillnessProgress.RemoveListener(OnStillnessProgress);
            gyroDetector.onStillnessLost.RemoveListener(OnStillnessLost);
            gyroDetector.onCountdownStarted.RemoveListener(OnCountdownStarted);
            gyroDetector.onCountdownTick.RemoveListener(OnCountdownTick);
            gyroDetector.onCountdownComplete.RemoveListener(OnCountdownComplete);
            gyroDetector.onPickupSuccess.RemoveListener(OnPickupSuccess);
            gyroDetector.onEarlyPickup.RemoveListener(OnEarlyPickup);
            gyroDetector.onTooLate.RemoveListener(OnTooLate);
            gyroDetector.onPickupWindowExpired.RemoveListener(OnPickupWindowExpired);
        }

        private void OnDetectionStarted()
        {
            UpdateStatusText(startMessage);
            ResetStillnessUI();
        }

        private void OnStillnessProgress(float progress)
        {
            if (stillnessSlider != null)
                stillnessSlider.value = progress;

            if (stillnessCountdownText != null)
            {
                float remaining = gyroDetector.stillnessThresholdTime * (1f - progress);
                stillnessCountdownText.text = Mathf.CeilToInt(remaining).ToString();
            }

            if (progress > 0.1f)
                UpdateStatusText(holdingMessage);
        }

        private void OnStillnessLost()
        {
            ResetStillnessUI();
            UpdateStatusText(stillnessLostMessage);
        }

        private void OnCountdownStarted(float duration)
        {
            if (countdownText != null)
                countdownText.text = duration.ToString("0");

            ResetStillnessUI();
        }

        private void OnCountdownTick(float remaining)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(remaining).ToString();
        }

        private void OnCountdownComplete()
        {
            if (countdownText != null)
                countdownText.text = "0";
        }

        private void OnPickupSuccess() => UpdateStatusText(pickupSuccessMessage);

        private void OnEarlyPickup() => UpdateStatusText(earlyPickupMessage);

        private void OnTooLate() => UpdateStatusText(tooLateMessage);

        private void OnPickupWindowExpired() => UpdateStatusText(windowExpiredMessage);


        private void ResetStillnessUI()
        {
            if (stillnessSlider != null)
                stillnessSlider.value = 0f;

            if (stillnessCountdownText != null)
                stillnessCountdownText.text = Mathf.CeilToInt(gyroDetector.stillnessThresholdTime).ToString();
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
