using System;
using NUnit.Framework;

namespace PonyPack.Tests
{
    public class PonyPackTimingEvaluatorTests
    {
        const float Center = 10f;
        PonyPackTimingEvaluator _eval;

        [SetUp]
        public void SetUp() => _eval = new PonyPackTimingEvaluator(0.5f);

        // ---- single player ----

        [TestCase(10.0f)]
        [TestCase(10.4f)]
        [TestCase(9.6f)]
        public void Inside_window_is_safe(float action)
        {
            var j = _eval.Evaluate(Center, action);
            Assert.AreEqual(TimingOutcome.Safe, j.Outcome);
            Assert.IsTrue(j.IsSafe);
        }

        [TestCase(10.5f)]
        [TestCase(9.5f)]
        public void Exactly_on_the_edge_is_safe(float action)
        {
            Assert.AreEqual(TimingOutcome.Safe, _eval.Evaluate(Center, action).Outcome);
        }

        [Test]
        public void Before_window_is_too_early()
        {
            var j = _eval.Evaluate(Center, 9.0f);
            Assert.AreEqual(TimingOutcome.TooEarly, j.Outcome);
            Assert.Less(j.Offset, 0f);
            Assert.IsFalse(j.IsSafe);
        }

        [Test]
        public void After_window_is_too_late()
        {
            var j = _eval.Evaluate(Center, 11.0f);
            Assert.AreEqual(TimingOutcome.TooLate, j.Outcome);
            Assert.Greater(j.Offset, 0f);
            Assert.IsFalse(j.IsSafe);
        }

        [Test]
        public void No_action_is_eliminated_with_infinite_score()
        {
            var j = _eval.Evaluate(Center, null);
            Assert.AreEqual(TimingOutcome.NoAction, j.Outcome);
            Assert.IsFalse(j.IsSafe);
            Assert.IsTrue(float.IsPositiveInfinity(j.Score));
        }

        [Test]
        public void NaN_action_counts_as_no_action()
        {
            Assert.AreEqual(TimingOutcome.NoAction, _eval.Evaluate(Center, float.NaN).Outcome);
        }

        [Test]
        public void Score_is_distance_from_center()
        {
            Assert.AreEqual(0.3f, _eval.Evaluate(Center, 9.7f).Score, 1e-4f);
            Assert.AreEqual(0.3f, _eval.Evaluate(Center, 10.3f).Score, 1e-4f);
        }

        [Test]
        public void Window_size_is_configurable()
        {
            var wide = new PonyPackTimingEvaluator(2f);
            Assert.AreEqual(TimingOutcome.Safe, wide.Evaluate(Center, 11.5f).Outcome);
            Assert.AreEqual(TimingOutcome.TooLate, _eval.Evaluate(Center, 11.5f).Outcome);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void Invalid_window_throws(float half)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PonyPackTimingEvaluator(half));
        }

        // ---- several players ----

        static TimingJudgement[] Judge(PonyPackTimingEvaluator e, params float?[] actions)
        {
            var result = new TimingJudgement[actions.Length];
            for (int i = 0; i < actions.Length; i++) result[i] = e.Evaluate(Center, actions[i]);
            return result;
        }

        [Test]
        public void Closest_to_center_wins()
        {
            var best = PonyPackTimingEvaluator.FindBest(Judge(_eval, 10.4f, 10.1f));
            CollectionAssert.AreEqual(new[] { 1 }, best);
        }

        [Test]
        public void Early_and_late_distance_are_compared_equally()
        {
            var best = PonyPackTimingEvaluator.FindBest(Judge(_eval, 9.8f, 10.1f));
            CollectionAssert.AreEqual(new[] { 1 }, best);
        }

        [Test]
        public void Eliminated_players_never_win()
        {
            var best = PonyPackTimingEvaluator.FindBest(Judge(_eval, 12.0f, 10.45f));
            CollectionAssert.AreEqual(new[] { 1 }, best);
        }

        [Test]
        public void Tie_returns_all_tied_players()
        {
            var best = PonyPackTimingEvaluator.FindBest(Judge(_eval, 9.8f, 10.2f, 10.45f));
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, best);
        }

        [Test]
        public void Nobody_safe_returns_empty()
        {
            var best = PonyPackTimingEvaluator.FindBest(Judge(_eval, 8f, 12f, null));
            Assert.IsEmpty(best);
        }

        [Test]
        public void Empty_list_returns_empty()
        {
            Assert.IsEmpty(PonyPackTimingEvaluator.FindBest(new TimingJudgement[0]));
        }

        [Test]
        public void Null_list_throws()
        {
            Assert.Throws<ArgumentNullException>(() => PonyPackTimingEvaluator.FindBest(null));
        }
    }
}
