using System.Collections;
using Joust.Flow;
using Joust.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// Egg lifecycle: it hatches on a timer into a promoted rider, unless it is
    /// collected first. Collecting is the player's way of denying the respawn.
    /// </summary>
    public class EggTests
    {
        private static Egg Spawn(EnemyTier layingTier, float hatchSeconds)
        {
            var go = new GameObject("egg");
            go.AddComponent<Rigidbody>().useGravity = false;
            var egg = go.AddComponent<Egg>();
            egg.Configure(layingTier, hatchSeconds);
            return egg;
        }

        [UnityTest]
        public IEnumerator HatchesAfterItsTimer()
        {
            var egg = Spawn(EnemyTier.Bounder, 0.15f);
            var hatchedAs = (EnemyTier?)null;
            egg.Hatched += (_, tier) => hatchedAs = tier;

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(EnemyTier.Hunter, hatchedAs,
                "a bounder's egg hatches one tier stronger");
        }

        [UnityTest]
        public IEnumerator CollectingBeforeHatchDeniesTheRespawn()
        {
            var egg = Spawn(EnemyTier.Bounder, 0.5f);
            var hatched = false;
            egg.Hatched += (_, _) => hatched = true;

            var collected = false;
            egg.Collected += _ => collected = true;

            egg.Collect();
            yield return new WaitForSeconds(0.7f);

            Assert.IsTrue(collected);
            Assert.IsFalse(hatched, "a collected egg must never hatch");
        }

        [UnityTest]
        public IEnumerator CollectingTwiceRaisesOneEvent()
        {
            var egg = Spawn(EnemyTier.Bounder, 5f);
            var count = 0;
            egg.Collected += _ => count++;

            egg.Collect();
            egg.Collect();
            yield return null;

            Assert.AreEqual(1, count);
        }
    }
}
