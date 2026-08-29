using Joust.Enemies;
using Joust.Flow;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// Tier stats live in data, so M4's new enemies are new assets rather than
    /// new branches in a spawner.
    /// </summary>
    public class EnemyDefinitionTests
    {
        [Test]
        public void EveryTierHasADefaultDefinition()
        {
            foreach (EnemyTier tier in System.Enum.GetValues(typeof(EnemyTier)))
            {
                var def = EnemyDefinition.Default(tier);
                Assert.IsNotNull(def, $"no definition for {tier}");
                Assert.AreEqual(tier, def.Tier);
            }
        }

        [Test]
        public void ScoresMatchTheScoreService()
        {
            foreach (EnemyTier tier in System.Enum.GetValues(typeof(EnemyTier)))
            {
                Assert.AreEqual(ScoreService.PointsFor(tier), EnemyDefinition.Default(tier).Points,
                    $"{tier} score must not drift from ScoreService");
            }
        }

        [Test]
        public void StrongerTiersAreFaster()
        {
            var bounder = EnemyDefinition.Default(EnemyTier.Bounder).SpeedMultiplier;
            var hunter = EnemyDefinition.Default(EnemyTier.Hunter).SpeedMultiplier;
            var lord = EnemyDefinition.Default(EnemyTier.ShadowLord).SpeedMultiplier;

            Assert.Less(bounder, hunter);
            Assert.Less(hunter, lord);
        }

        [Test]
        public void TiersUseDistinctAiProfiles()
        {
            Assert.AreEqual(AiProfile.Wanderer, EnemyDefinition.Default(EnemyTier.Bounder).Profile);
            Assert.AreEqual(AiProfile.Pursuer, EnemyDefinition.Default(EnemyTier.Hunter).Profile);
            Assert.AreEqual(AiProfile.Dominator, EnemyDefinition.Default(EnemyTier.ShadowLord).Profile);
        }

        [Test]
        public void TiersAreVisuallyDistinct()
        {
            var bounder = EnemyDefinition.Default(EnemyTier.Bounder).Colour;
            var hunter = EnemyDefinition.Default(EnemyTier.Hunter).Colour;
            var lord = EnemyDefinition.Default(EnemyTier.ShadowLord).Colour;

            Assert.AreNotEqual(bounder, hunter);
            Assert.AreNotEqual(hunter, lord);
            Assert.AreNotEqual(bounder, lord);
        }
    }
}
