using System.Linq;
using Joust.Flow;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// When a wave is over, and what the next one throws at you.
    /// </summary>
    public class WaveRulesTests
    {
        [Test]
        public void AWaveWithEnemiesLeftIsNotClear()
        {
            Assert.IsFalse(WaveRules.IsWaveClear(enemies: 2, eggs: 0));
        }

        [Test]
        public void AWaveWithEggsLeftIsNotClear()
        {
            // An egg on the ground is an enemy that has not hatched yet.
            Assert.IsFalse(WaveRules.IsWaveClear(enemies: 0, eggs: 1));
        }

        [Test]
        public void AnEmptyBoardIsClear()
        {
            Assert.IsTrue(WaveRules.IsWaveClear(enemies: 0, eggs: 0));
        }

        [Test]
        public void TheFirstWaveOpensWithThreeBounders()
        {
            var opening = WaveRules.OpeningComposition(1);
            Assert.AreEqual(3, opening.Count);
            Assert.IsTrue(opening.All(t => t == EnemyTier.Bounder));
        }

        [Test]
        public void EarlyWavesAreAllBounders()
        {
            Assert.IsTrue(WaveRules.OpeningComposition(2).All(t => t == EnemyTier.Bounder));
        }

        [Test]
        public void WavesGrow()
        {
            Assert.GreaterOrEqual(WaveRules.OpeningComposition(5).Count,
                WaveRules.OpeningComposition(1).Count);
        }

        [Test]
        public void HuntersAppearBeforeShadowLords()
        {
            var firstHunter = Enumerable.Range(1, 30)
                .First(w => WaveRules.OpeningComposition(w).Contains(EnemyTier.Hunter));
            var firstLord = Enumerable.Range(1, 30)
                .First(w => WaveRules.OpeningComposition(w).Contains(EnemyTier.ShadowLord));

            Assert.Less(firstHunter, firstLord);
        }

        [Test]
        public void WavesAreCappedSoTheArenaStaysPlayable()
        {
            Assert.LessOrEqual(WaveRules.OpeningComposition(50).Count, 8);
        }

        [Test]
        public void SurvivalBonusOnlyOnASurvivalWaveSurvived()
        {
            var survival = Enumerable.Range(1, 30).First(WaveRules.IsSurvivalWave);

            Assert.AreEqual(3000, WaveRules.SurvivalBonus(survival, lostALife: false));
            Assert.AreEqual(0, WaveRules.SurvivalBonus(survival, lostALife: true));
        }

        [Test]
        public void NoSurvivalBonusOnAnOrdinaryWave()
        {
            var ordinary = Enumerable.Range(1, 30).First(w => !WaveRules.IsSurvivalWave(w));
            Assert.AreEqual(0, WaveRules.SurvivalBonus(ordinary, lostALife: false));
        }
    }
}
