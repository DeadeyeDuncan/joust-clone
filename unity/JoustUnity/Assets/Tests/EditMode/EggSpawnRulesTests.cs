using Joust.World;
using NUnit.Framework;
using UnityEngine;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// Who leaves an egg, and where it goes. In the arcade an unseated enemy
    /// becomes an egg; an unseated player loses a life and respawns.
    /// </summary>
    public class EggSpawnRulesTests
    {
        [Test]
        public void EnemiesLeaveAnEgg()
        {
            Assert.IsTrue(EggSpawnRules.ShouldDropEgg(wasPlayer: false));
        }

        [Test]
        public void ThePlayerDoesNotLeaveAnEgg()
        {
            Assert.IsFalse(EggSpawnRules.ShouldDropEgg(wasPlayer: true));
        }

        [Test]
        public void AnEggInheritsMostOfTheRidersMomentum()
        {
            var thrown = EggSpawnRules.LaunchVelocity(new Vector2(6f, 2f));
            Assert.Greater(thrown.x, 0f, "an egg from a rider moving right keeps going right");
            Assert.Less(thrown.x, 6f, "but slower than the rider was going");
        }

        [Test]
        public void AnEggIsAlwaysTossedUpwardsFirst()
        {
            // Even a rider struck while diving should pop its egg up, so the egg
            // arcs and lands rather than being driven straight into the lava.
            var thrown = EggSpawnRules.LaunchVelocity(new Vector2(0f, -9f));
            Assert.Greater(thrown.y, 0f);
        }

        [Test]
        public void AStationaryRiderDropsItsEggStraightUp()
        {
            var thrown = EggSpawnRules.LaunchVelocity(Vector2.zero);
            Assert.AreEqual(0f, thrown.x, 1e-4f);
            Assert.Greater(thrown.y, 0f);
        }
    }
}
