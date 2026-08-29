using Joust.Enemies;
using NUnit.Framework;
using UnityEngine;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// The three tiers must be distinguishable by their decisions, not merely by
    /// their speed multiplier. From the arcade: a Bounder flies erratically with
    /// little pursuit, a Hunter actively pursues, a Shadow Lord is the most
    /// aggressive.
    /// </summary>
    public class TierBehaviourTests
    {
        private const float LavaLine = 1.5f;

        [Test]
        public void EveryProfileFlapsOverTheLava()
        {
            foreach (AiProfile profile in System.Enum.GetValues(typeof(AiProfile)))
            {
                var d = BuzzardDecision.Decide(new Vector2(0f, 0.4f), new Vector2(9f, 9f),
                    LavaLine, 0.99f, profile);
                Assert.IsTrue(d.Flap, $"{profile} must save itself over the lava");
            }
        }

        [Test]
        public void APursuerCommitsTowardTheTarget()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(9f, 5f),
                LavaLine, 0.99f, AiProfile.Pursuer);
            Assert.AreEqual(1f, d.Thrust, 1e-4f);
        }

        [Test]
        public void ADominatorCommitsTowardTheTarget()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(9f, 5f),
                LavaLine, 0.99f, AiProfile.Dominator);
            Assert.AreEqual(1f, d.Thrust, 1e-4f);
        }

        [Test]
        public void AWandererDoesNotAlwaysChase()
        {
            // With the dice against chasing, a bounder should not commit.
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(9f, 5f),
                LavaLine, 0.95f, AiProfile.Wanderer);
            Assert.AreNotEqual(1f, d.Thrust,
                "a wanderer that always chases is just a hunter");
        }

        [Test]
        public void ADominatorClimbsToStayAboveALevelTarget()
        {
            // Height wins duels, so the most aggressive tier seeks height even
            // when it is already level with the player.
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(4f, 5f),
                LavaLine, 0.99f, AiProfile.Dominator);
            Assert.IsTrue(d.Flap);
        }

        [Test]
        public void APursuerDoesNotClimbWhenAlreadyLevel()
        {
            var d = BuzzardDecision.Decide(new Vector2(0f, 5f), new Vector2(4f, 5f),
                LavaLine, 0.99f, AiProfile.Pursuer);
            Assert.IsFalse(d.Flap);
        }

        [Test]
        public void EveryProfileClimbsForATargetAbove()
        {
            foreach (AiProfile profile in System.Enum.GetValues(typeof(AiProfile)))
            {
                var d = BuzzardDecision.Decide(new Vector2(0f, 3f), new Vector2(0f, 9f),
                    LavaLine, 0.99f, profile);
                Assert.IsTrue(d.Flap, $"{profile} must contest a higher lance");
            }
        }
    }
}
