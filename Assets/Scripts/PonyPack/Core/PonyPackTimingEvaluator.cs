using System;
using System.Collections.Generic;

namespace PonyPack
{
    /// <summary>
    /// Pure C# rules for Pony Pack timing (no Unity types, so it is unit-testable).
    /// A player is SAFE when their action time is within +/- halfWindow seconds of the window center.
    /// All times are in the same unit (seconds) and on the same clock, e.g. server-normalized time.
    /// The round state machine decides WHEN to call this; this class only decides WHO is safe.
    /// </summary>
    public sealed class PonyPackTimingEvaluator
    {
        public const float DefaultHalfWindowSeconds = 0.5f;

        /// <summary>Half of the valid window, in seconds. The window is center +/- HalfWindow (inclusive).</summary>
        public float HalfWindow { get; }

        public PonyPackTimingEvaluator(float halfWindowSeconds = DefaultHalfWindowSeconds)
        {
            if (float.IsNaN(halfWindowSeconds) || halfWindowSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(halfWindowSeconds), "Half window must be greater than 0.");
            HalfWindow = halfWindowSeconds;
        }

        /// <summary>
        /// Judge one player. Pass actionTime = null (or NaN) when the player never acted.
        /// </summary>
        public TimingJudgement Evaluate(float windowCenterTime, float? actionTime)
        {
            if (!actionTime.HasValue || float.IsNaN(actionTime.Value))
                return new TimingJudgement(TimingOutcome.NoAction, 0f, float.PositiveInfinity);

            float offset = actionTime.Value - windowCenterTime;
            float score = Math.Abs(offset);

            TimingOutcome outcome;
            if (score <= HalfWindow) outcome = TimingOutcome.Safe;
            else if (offset < 0f) outcome = TimingOutcome.TooEarly;
            else outcome = TimingOutcome.TooLate;

            return new TimingJudgement(outcome, offset, score);
        }

        /// <summary>
        /// Returns the indices of the best SAFE players (lowest score). More than one index is returned
        /// when scores are tied within tieEpsilon seconds, so the caller can apply the tie-break rule
        /// once the game design fixes it. Returns an empty list when nobody is safe.
        /// </summary>
        public static List<int> FindBest(IReadOnlyList<TimingJudgement> judgements, float tieEpsilon = 0.0001f)
        {
            if (judgements == null) throw new ArgumentNullException(nameof(judgements));

            var best = new List<int>();
            float bestScore = float.PositiveInfinity;

            for (int i = 0; i < judgements.Count; i++)
            {
                if (!judgements[i].IsSafe) continue;
                float s = judgements[i].Score;

                if (s < bestScore - tieEpsilon)
                {
                    best.Clear();
                    best.Add(i);
                    bestScore = s;
                }
                else if (Math.Abs(s - bestScore) <= tieEpsilon)
                {
                    best.Add(i);
                    if (s < bestScore) bestScore = s;
                }
            }
            return best;
        }
    }
}
