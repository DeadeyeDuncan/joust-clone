using Joust.Flow;
using Joust.World;
using NUnit.Framework;

namespace Joust.Tests.EditMode
{
    /// <summary>
    /// The arcade escalates difficulty through the player's own kills: a
    /// defeated rider's egg hatches one tier stronger. The pygame clone in this
    /// repository spawns from a fixed per-wave list instead and loses that
    /// feedback loop entirely.
    /// </summary>
    public class EggRulesTests
    {
        [Test]
        public void BounderPromotesToHunter()
        {
            Assert.AreEqual(EnemyTier.Hunter, EggRules.Promote(EnemyTier.Bounder));
        }

        [Test]
        public void HunterPromotesToShadowLord()
        {
            Assert.AreEqual(EnemyTier.ShadowLord, EggRules.Promote(EnemyTier.Hunter));
        }

        [Test]
        public void ShadowLordIsTheCeiling()
        {
            Assert.AreEqual(EnemyTier.ShadowLord, EggRules.Promote(EnemyTier.ShadowLord));
        }

        [Test]
        public void PromotionNeverSkipsATier()
        {
            Assert.AreEqual(EnemyTier.Hunter, EggRules.Promote(EnemyTier.Bounder),
                "a Bounder must not jump straight to Shadow Lord");
        }

        [Test]
        public void RepeatedPromotionConvergesOnShadowLord()
        {
            var tier = EnemyTier.Bounder;
            for (var i = 0; i < 5; i++)
            {
                tier = EggRules.Promote(tier);
            }

            Assert.AreEqual(EnemyTier.ShadowLord, tier);
        }
    }
}
