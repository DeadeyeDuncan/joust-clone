using Joust.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// AI decisions as a pure function, testable without a scene. This is the
    /// M0 lesson applied: pure logic beside a thin MonoBehaviour keeps behaviour
    /// checkable in milliseconds instead of play sessions.
    /// </summary>
    public class BuzzardDecisionTests
    {
        private const float LavaLine = 1.5f;

        [Test]
        public void FlapsWhenBelowTheLavaLine()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 0.5f), new Vector2(0f, 8f), LavaLine, 0.99f);
            Assert.IsTrue(d.Flap, "a rider below the lava line must always flap, whatever the dice say");
        }

        [Test]
        public void ThrustsTowardTheTarget()
        {
            var right = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(6f, 5f), LavaLine, 0.5f);
            Assert.Greater(right.Thrust, 0f);

            var left = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(-6f, 5f), LavaLine, 0.5f);
            Assert.Less(left.Thrust, 0f);
        }

        [Test]
        public void FlapsWhenTheTargetIsAbove()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 3f), new Vector2(0f, 9f), LavaLine, 0.99f);
            Assert.IsTrue(d.Flap, "it must climb to contest a higher lance");
        }

        [Test]
        public void DoesNotAlwaysFlapWhenLevelWithTheTarget()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(3f, 5f), LavaLine, 0.99f);
            Assert.IsFalse(d.Flap, "level flight should be dice-driven, not constant");
        }

        [Test]
        public void FlapsSometimesWhenLevelWithTheTarget()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(3f, 5f), LavaLine, 0.01f);
            Assert.IsTrue(d.Flap);
        }

        [Test]
        public void StandingOnTheTargetGivesNoThrust()
        {
            var d = BuzzardDecision.Decide(new Vector2(4f, 5f), new Vector2(4f, 5f), LavaLine, 0.5f);
            Assert.AreEqual(0f, d.Thrust, 1e-4f);
        }

        [Test]
        public void SurvivalBeatsPursuit()
        {
            // Target is far to the left and above, but the rider is over the lava:
            // it must still flap rather than dive after the player.
            var d = BuzzardDecision.Decide(new Vector2(0f, 0.2f), new Vector2(-9f, 12f), LavaLine, 0.99f);
            Assert.IsTrue(d.Flap);
        }
    }
}
