using Joust.Combat;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    public class JoustResolverTests
    {
        // Unity is Y-up: the greater Y is the higher lance and wins.
        [Test]
        public void HigherLanceWins()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(10f, 4f, 0.5f));
        }

        [Test]
        public void LowerLanceLoses()
        {
            Assert.AreEqual(JoustOutcome.BWins, JoustResolver.Resolve(4f, 10f, 0.5f));
        }

        [Test]
        public void EqualHeightsTie()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceInsideTieBandTies()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7.4f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceExactlyOnTieBandTies()
        {
            Assert.AreEqual(JoustOutcome.Tie, JoustResolver.Resolve(7.5f, 7f, 0.5f));
        }

        [Test]
        public void DifferenceJustOutsideTieBandWins()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(7.6f, 7f, 0.5f));
        }

        [Test]
        public void NegativeCoordinatesCompareByHeightNotMagnitude()
        {
            Assert.AreEqual(JoustOutcome.AWins, JoustResolver.Resolve(-2f, -9f, 0.5f));
        }
    }
}
