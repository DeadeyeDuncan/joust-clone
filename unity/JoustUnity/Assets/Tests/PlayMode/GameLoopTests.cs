using System.Collections;
using Joust.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// Score, lives and the egg chain, wired as the game actually runs them.
    /// </summary>
    public class GameLoopTests
    {
        private GameObject _go;

        private GameDirector Spawn()
        {
            _go = new GameObject("director");
            return _go.AddComponent<GameDirector>();
        }

        [TearDown]
        public void Cleanup()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [UnityTest]
        public IEnumerator KillsAndEggsAccumulateScore()
        {
            var director = Spawn();
            yield return null;

            director.AwardKill(EnemyTier.Bounder);
            director.AwardEgg();

            Assert.AreEqual(750, director.Score, "500 for the bounder, 250 for the first egg");
        }

        [UnityTest]
        public IEnumerator EggChainEscalatesWithinAWave()
        {
            var director = Spawn();
            yield return null;

            director.AwardEgg();
            director.AwardEgg();

            Assert.AreEqual(750, director.Score, "250 then 500");
        }

        [UnityTest]
        public IEnumerator EggChainResetsOnDeath()
        {
            var director = Spawn();
            yield return null;

            director.AwardEgg();
            director.AwardEgg();
            director.LoseLife();
            director.AwardEgg();

            Assert.AreEqual(1000, director.Score, "250 + 500, then the chain restarts at 250");
        }

        [UnityTest]
        public IEnumerator StartsWithThreeLives()
        {
            var director = Spawn();
            yield return null;

            Assert.AreEqual(3, director.Lives);
        }

        [UnityTest]
        public IEnumerator LivesRunOutAndRaiseGameOver()
        {
            var director = Spawn();
            yield return null;

            var over = false;
            director.GameOver += () => over = true;

            director.LoseLife();
            director.LoseLife();
            Assert.IsFalse(over, "still one life left");

            director.LoseLife();

            Assert.AreEqual(0, director.Lives);
            Assert.IsTrue(over);
        }

        [UnityTest]
        public IEnumerator CrossingTwentyThousandAwardsALife()
        {
            var director = Spawn();
            yield return null;

            for (var i = 0; i < 40; i++) director.AwardKill(EnemyTier.ShadowLord);

            Assert.AreEqual(40000, director.Score);
            Assert.AreEqual(5, director.Lives, "three to start, plus one at 20k and one at 40k");
        }

        [UnityTest]
        public IEnumerator LosingALifeBelowZeroIsNotPossible()
        {
            var director = Spawn();
            yield return null;

            for (var i = 0; i < 6; i++) director.LoseLife();

            Assert.AreEqual(0, director.Lives);
        }

        [UnityTest]
        public IEnumerator GameOverRaisesOnlyOnce()
        {
            var director = Spawn();
            yield return null;

            var count = 0;
            director.GameOver += () => count++;

            for (var i = 0; i < 6; i++) director.LoseLife();

            Assert.AreEqual(1, count);
        }
    }
}
