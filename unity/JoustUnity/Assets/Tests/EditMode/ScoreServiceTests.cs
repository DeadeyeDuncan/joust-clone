using Joust.Flow;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// Scoring is pinned to the 1982 arcade, not to the pygame clone in this
    /// repository. The clone pays 1500 for a Shadow Lord; the arcade pays 1000,
    /// and the arcade wins.
    /// </summary>
    public class ScoreServiceTests
    {
        [Test]
        public void TierValuesMatchTheArcade()
        {
            Assert.AreEqual(500, ScoreService.PointsFor(EnemyTier.Bounder));
            Assert.AreEqual(750, ScoreService.PointsFor(EnemyTier.Hunter));
            Assert.AreEqual(1000, ScoreService.PointsFor(EnemyTier.ShadowLord));
        }

        [Test]
        public void ShadowLordIsNotTheClonesFifteenHundred()
        {
            Assert.AreNotEqual(1500, ScoreService.PointsFor(EnemyTier.ShadowLord));
        }

        [Test]
        public void EggChainEscalatesThenCaps()
        {
            Assert.AreEqual(250, ScoreService.EggChainValue(0));
            Assert.AreEqual(500, ScoreService.EggChainValue(1));
            Assert.AreEqual(750, ScoreService.EggChainValue(2));
            Assert.AreEqual(1000, ScoreService.EggChainValue(3));
            Assert.AreEqual(1000, ScoreService.EggChainValue(9));
        }

        [Test]
        public void NegativeChainIndexScoresNothing()
        {
            Assert.AreEqual(0, ScoreService.EggChainValue(-1));
        }

        [Test]
        public void ExtraLifeEveryTwentyThousand()
        {
            Assert.AreEqual(0, ScoreService.ExtraLivesEarned(0, 19999));
            Assert.AreEqual(1, ScoreService.ExtraLivesEarned(0, 20000));
            Assert.AreEqual(1, ScoreService.ExtraLivesEarned(19000, 21000));
            Assert.AreEqual(0, ScoreService.ExtraLivesEarned(21000, 22000));
        }

        [Test]
        public void CrossingTwoThresholdsAtOnceAwardsTwo()
        {
            Assert.AreEqual(2, ScoreService.ExtraLivesEarned(0, 41000));
        }

        [Test]
        public void ScoreGoingNowhereAwardsNothing()
        {
            Assert.AreEqual(0, ScoreService.ExtraLivesEarned(25000, 25000));
        }
    }
}
