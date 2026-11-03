using System;
using Game3;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PonyPack
{
    public enum RoundState { Idle, Preparation, Countdown, Trigger, Result }

    /// <summary>
    /// Pony Pack round flow as a small state machine (local, one player for now).
    /// The existing GyroDetector measures the phone; this class decides WHEN things count,
    /// and PonyPackTimingEvaluator decides WHO is safe.
    ///
    /// Idle -> Preparation (hold still) -> Countdown -> Trigger (GO!) -> Result (SAFE / ELIMINATED)
    ///
    /// Only SetState() writes State. Every gyro event is guarded by the current state.
    /// </summary>
    public class PonyPackRound : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GyroDetector gyro;
        [Tooltip("Optional: shows the current instruction / result.")]
        [SerializeField] TMP_Text statusText;

        [Header("Timing rule (placeholder until the lead confirms)")]
        [Tooltip("Center of the valid window, in seconds AFTER the GO signal.")]
        [SerializeField, Min(0f)] float windowCenterAfterGo = 0.5f;
        [Tooltip("Half of the valid window in seconds. Valid = center +/- this.")]
        [SerializeField, Min(0.01f)] float halfWindowSeconds = 0.5f;

        [Tooltip("Tick only if nothing else starts the gyro (e.g. the StartDetection component on a panel). " +
                 "Leave off when the scene's panel flow starts detection.")]
        [SerializeField] bool startOnPlay = false;

        [Header("Events (hook panels / audio here)")]
        public UnityEvent onPreparation = new();
        public UnityEvent onCountdown = new();
        public UnityEvent onTrigger = new();
        public UnityEvent onSafe = new();
        public UnityEvent onEliminated = new();

        /// <summary>Time.time when the GO signal started.</summary>
        public float GoTime => _goTime;
        /// <summary>Time.time of the center of the valid window.</summary>
        public float WindowCenterTime => _goTime + windowCenterAfterGo;
        public float HalfWindowSeconds => halfWindowSeconds;

        public RoundState State { get; private set; } = RoundState.Idle;
        public TimingJudgement LastJudgement { get; private set; }

        /// <summary>Fired on every state change (UI, audio and tests can listen).</summary>
        public event Action<RoundState> StateChanged;

        PonyPackTimingEvaluator _evaluator;
        float _goTime;   // Time.time when the Trigger state was entered

        void Awake()
        {
            _evaluator = new PonyPackTimingEvaluator(halfWindowSeconds);
            // The phone lies flat and untouched during a round: do not let the screen dim or sleep.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        void OnEnable()
        {
            if (gyro == null) { Debug.LogError("[PonyPackRound] GyroDetector is not assigned", this); return; }
            gyro.onDetectionStarted.AddListener(OnGyroDetectionStarted);
            gyro.onCountdownStarted.AddListener(OnGyroCountdownStarted);
            gyro.onCountdownComplete.AddListener(OnGyroCountdownComplete);
            gyro.onPickupSuccess.AddListener(OnGyroPickup);
            gyro.onTooLate.AddListener(OnGyroPickup);
            gyro.onEarlyPickup.AddListener(OnGyroEarly);
            gyro.onPickupWindowExpired.AddListener(OnGyroNoAction);
        }

        void OnDisable()
        {
            if (gyro == null) return;
            gyro.onDetectionStarted.RemoveListener(OnGyroDetectionStarted);
            gyro.onCountdownStarted.RemoveListener(OnGyroCountdownStarted);
            gyro.onCountdownComplete.RemoveListener(OnGyroCountdownComplete);
            gyro.onPickupSuccess.RemoveListener(OnGyroPickup);
            gyro.onTooLate.RemoveListener(OnGyroPickup);
            gyro.onEarlyPickup.RemoveListener(OnGyroEarly);
            gyro.onPickupWindowExpired.RemoveListener(OnGyroNoAction);
        }

        void Start()
        {
            if (startOnPlay) StartRound();
        }

        /// <summary>Begin a new round (also hook this to the Next-Round button).</summary>
        public void StartRound()
        {
            if (gyro == null) return;
            gyro.StopDetection();
            gyro.StartDetection();   // raises onDetectionStarted, which moves us to Preparation
        }

        // ---- gyro events: each one is guarded by the current state ----

        // Whoever starts the gyro (this script or the scene's StartDetection component) begins a round.
        void OnGyroDetectionStarted()
        {
            if (State != RoundState.Idle && State != RoundState.Result) return;
            SetState(RoundState.Preparation);
        }

        void OnGyroCountdownStarted(float seconds)
        {
            if (State != RoundState.Preparation) return;
            SetState(RoundState.Countdown);
        }

        void OnGyroCountdownComplete()
        {
            if (State != RoundState.Countdown) return;
            SetState(RoundState.Trigger);
        }

        // Pickup inside the gyro's own window (success) or after it (too late): the evaluator decides.
        void OnGyroPickup()
        {
            if (State != RoundState.Trigger) return;
            float center = _goTime + windowCenterAfterGo;
            Finish(_evaluator.Evaluate(center, Time.time));
        }

        void OnGyroEarly()
        {
            if (State != RoundState.Preparation && State != RoundState.Countdown) return;
            // Moved before GO: eliminated, no meaningful score.
            Finish(new TimingJudgement(TimingOutcome.TooEarly, float.NaN, float.PositiveInfinity));
        }

        void OnGyroNoAction()
        {
            if (State != RoundState.Trigger) return;
            Finish(_evaluator.Evaluate(_goTime + windowCenterAfterGo, null));
        }

        // ---- state machine core ----

        void Finish(TimingJudgement judgement)
        {
            LastJudgement = judgement;
            SetState(RoundState.Result);
        }

        /// <summary>The ONLY place that changes State.</summary>
        void SetState(RoundState next)
        {
            State = next;
            Debug.Log($"[PonyPackRound] -> {next}" + (next == RoundState.Result ? $" ({LastJudgement.Outcome}, score {LastJudgement.Score:0.000})" : ""));
            OnEnterState(next);
            StateChanged?.Invoke(next);
        }

        void OnEnterState(RoundState s)
        {
            switch (s)
            {
                case RoundState.Preparation:
                    SetStatus("Hold your phone still");
                    onPreparation.Invoke();
                    break;
                case RoundState.Countdown:
                    SetStatus("Get ready...");
                    onCountdown.Invoke();
                    break;
                case RoundState.Trigger:
                    _goTime = Time.time;
                    SetStatus("GO!");
                    onTrigger.Invoke();
                    break;
                case RoundState.Result:
                    if (LastJudgement.IsSafe) { SetStatus("SAFE"); onSafe.Invoke(); }
                    else { SetStatus("ELIMINATED (" + LastJudgement.Outcome + ")"); onEliminated.Invoke(); }
                    break;
            }
        }

        void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }
    }
}
