namespace PonyPack
{
    /// <summary>Result category for one player's action in one round.</summary>
    public enum TimingOutcome
    {
        /// <summary>The player acted inside the valid window.</summary>
        Safe,
        /// <summary>The player acted before the window opened.</summary>
        TooEarly,
        /// <summary>The player acted after the window closed.</summary>
        TooLate,
        /// <summary>The player never acted this round.</summary>
        NoAction
    }

    /// <summary>
    /// Immutable result of judging one player. Score is the distance in seconds from the
    /// window center (lower is better). Score is float.PositiveInfinity when there was no action.
    /// </summary>
    public readonly struct TimingJudgement
    {
        public TimingOutcome Outcome { get; }

        /// <summary>Signed seconds: action time minus window center. Negative = early, positive = late. 0 when there was no action.</summary>
        public float Offset { get; }

        /// <summary>Absolute distance from the window center in seconds. Lower is better.</summary>
        public float Score { get; }

        public bool IsSafe => Outcome == TimingOutcome.Safe;

        public TimingJudgement(TimingOutcome outcome, float offset, float score)
        {
            Outcome = outcome;
            Offset = offset;
            Score = score;
        }
    }
}
