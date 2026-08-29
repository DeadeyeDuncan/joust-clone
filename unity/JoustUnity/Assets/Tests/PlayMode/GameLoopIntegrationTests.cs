using System.Collections;
using System.Linq;
using Joust.Combat;
using Joust.Core;
using Joust.Enemies;
using Joust.Flow;
using Joust.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// The loop end to end, with real components talking to each other.
    ///
    /// The rules underneath are covered headlessly, but "unseat an enemy, an egg
    /// appears, it hatches one tier stronger, the wave advances" is a wiring
    /// claim, and wiring claims are exactly the kind that read correctly and
    /// behave wrongly.
    /// </summary>
    public class GameLoopIntegrationTests
    {
        private GameObject _root;
        private RiderFactory _factory;
        private EggSpawner _eggSpawner;
        private GameDirector _director;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("loop_fixture");

            var tuning = ScriptableObject.CreateInstance<TuningProfile>();

            _factory = _root.AddComponent<RiderFactory>();
            _factory.Configure(tuning, null, null, null);

            _eggSpawner = _root.AddComponent<EggSpawner>();
            _director = _root.AddComponent<GameDirector>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var rider in Object.FindObjectsByType<Rider>(FindObjectsSortMode.None))
            {
                // BuzzardAI requires RiderMotor, and tearing the GameObject down
                // can destroy the motor first, which Unity logs as an error and
                // the test framework treats as a failure. Remove the dependent
                // component before the object it depends on.
                var ai = rider.GetComponent<BuzzardAI>();
                if (ai != null)
                {
                    Object.DestroyImmediate(ai);
                }

                Object.DestroyImmediate(rider.gameObject);
            }

            foreach (var egg in Object.FindObjectsByType<Egg>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(egg.gameObject);
            }

            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        [UnityTest]
        public IEnumerator TheFactoryBuildsAUsableEnemy()
        {
            var enemy = _factory.CreateEnemy(EnemyTier.Bounder, new Vector3(0f, 5f, 0f));
            yield return null;

            Assert.IsNotNull(enemy, "factory returned nothing");
            Assert.IsTrue(enemy.Mounted);
            Assert.IsNotNull(enemy.Lance, "a rider with no lance cannot joust");
            Assert.IsNotNull(enemy.GetComponent<Joust.Movement.RiderMotor>());
            Assert.IsNotNull(enemy.GetComponent<CombatContact>());
        }

        [UnityTest]
        public IEnumerator StrongerTiersFlyFaster()
        {
            var bounder = _factory.CreateEnemy(EnemyTier.Bounder, new Vector3(-4f, 5f, 0f));
            var lord = _factory.CreateEnemy(EnemyTier.ShadowLord, new Vector3(4f, 5f, 0f));
            yield return null;

            var bounderSpeed = bounder.GetComponent<Joust.Movement.RiderMotor>().SpeedMultiplier;
            var lordSpeed = lord.GetComponent<Joust.Movement.RiderMotor>().SpeedMultiplier;

            Assert.Greater(lordSpeed, bounderSpeed);
        }

        [UnityTest]
        public IEnumerator UnseatingAnEnemyLeavesAnEggOfItsTier()
        {
            var enemy = _factory.CreateEnemy(EnemyTier.Bounder, new Vector3(0f, 6f, 0f));
            _eggSpawner.Watch(enemy);
            yield return null;

            enemy.Unseat();
            yield return null;

            var eggs = Object.FindObjectsByType<Egg>(FindObjectsSortMode.None);
            Assert.AreEqual(1, eggs.Length, "an unseated enemy must leave exactly one egg");
            Assert.AreEqual(EnemyTier.Bounder, eggs[0].LayingTier);
        }

        [UnityTest]
        public IEnumerator AnUnseatedPlayerLeavesNoEgg()
        {
            var player = _factory.CreatePlayer(new Vector3(0f, 6f, 0f));
            _eggSpawner.Watch(player);

            var down = false;
            _eggSpawner.PlayerDown += _ => down = true;
            yield return null;

            player.Unseat();
            yield return null;

            Assert.IsTrue(down, "a downed player must raise PlayerDown");
            Assert.AreEqual(0, Object.FindObjectsByType<Egg>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator AnEggHatchesOneTierStronger()
        {
            var egg = _eggSpawner.Spawn(new Vector3(0f, 6f, 0f), Vector2.zero, EnemyTier.Bounder);
            egg.Configure(EnemyTier.Bounder, 0.15f);

            EnemyTier? hatched = null;
            egg.Hatched += (_, tier) => hatched = tier;

            yield return new WaitForSeconds(0.4f);

            Assert.AreEqual(EnemyTier.Hunter, hatched,
                "a bounder egg must hatch a hunter, which is what makes the wave respond to play");
        }

        [UnityTest]
        public IEnumerator CollectingAnEggScoresTheChain()
        {
            var egg = _eggSpawner.Spawn(new Vector3(0f, 6f, 0f), Vector2.zero, EnemyTier.Bounder);
            egg.Collected += _ => _director.AwardEgg();
            yield return null;

            egg.Collect();
            yield return null;

            Assert.AreEqual(250, _director.Score, "the first egg of a chain scores 250");
        }

        [UnityTest]
        public IEnumerator AWaveIsOnlyClearWhenEggsAreGoneToo()
        {
            var enemy = _factory.CreateEnemy(EnemyTier.Bounder, new Vector3(0f, 6f, 0f));
            _eggSpawner.Watch(enemy);
            yield return null;

            enemy.Unseat();
            yield return null;

            var enemies = Object.FindObjectsByType<Rider>(FindObjectsSortMode.None)
                .Count(r => !r.IsPlayer && r.Mounted);
            var eggs = Object.FindObjectsByType<Egg>(FindObjectsSortMode.None).Length;

            Assert.IsFalse(WaveRules.IsWaveClear(enemies, eggs),
                "the last enemy is dead but its egg is still live, so the wave continues");
        }
    }
}
