using System.Collections;
using Joust.Combat;
using Joust.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// The duel rule under real physics: the higher lance unseats the lower, an
    /// equal-height clash unseats nobody, and one overlap resolves exactly once
    /// even though OnTriggerEnter fires on both colliders.
    /// </summary>
    public class CombatContactTests
    {
        private readonly System.Collections.Generic.List<GameObject> _spawned = new();

        private Rider Spawn(string name, Vector3 position, float lanceHeight)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 1f;
            collider.isTrigger = true;

            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var lance = new GameObject("lance").transform;
            lance.SetParent(go.transform, false);
            lance.localPosition = new Vector3(0f, lanceHeight, 0f);

            var rider = go.AddComponent<Rider>();
            rider.Configure(lance, EnemyTier.Bounder);
            go.AddComponent<CombatContact>().Configure(rider, 0.2f);

            _spawned.Add(go);
            return rider;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator HigherLanceUnseatsLower()
        {
            var high = Spawn("high", Vector3.zero, 1.5f);
            var low = Spawn("low", new Vector3(0.3f, 0f, 0f), 0.2f);

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(high.Mounted, "the higher rider stays mounted");
            Assert.IsFalse(low.Mounted, "the lower rider is unseated");
        }

        [UnityTest]
        public IEnumerator OrderOfSpawningDoesNotDecideTheWinner()
        {
            var low = Spawn("low", Vector3.zero, 0.2f);
            var high = Spawn("high", new Vector3(0.3f, 0f, 0f), 1.5f);

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(high.Mounted, "height decides, not spawn order");
            Assert.IsFalse(low.Mounted);
        }

        [UnityTest]
        public IEnumerator EqualHeightsUnseatNobody()
        {
            var a = Spawn("a", Vector3.zero, 1f);
            var b = Spawn("b", new Vector3(0.3f, 0f, 0f), 1.05f);

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.IsTrue(a.Mounted);
            Assert.IsTrue(b.Mounted);
        }

        [UnityTest]
        public IEnumerator OneOverlapRaisesOneResolution()
        {
            var high = Spawn("high", Vector3.zero, 1.5f);
            Spawn("low", new Vector3(0.3f, 0f, 0f), 0.2f);

            var resolutions = 0;
            foreach (var contact in Object.FindObjectsByType<CombatContact>(FindObjectsSortMode.None))
            {
                contact.Resolved += (_, _) => resolutions++;
            }

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, resolutions,
                "OnTriggerEnter fires on both colliders; only one may resolve");
            Assert.IsTrue(high.Mounted);
        }

        [UnityTest]
        public IEnumerator UnseatingRaisesTheRidersEvent()
        {
            var high = Spawn("high", Vector3.zero, 1.5f);
            var low = Spawn("low", new Vector3(0.3f, 0f, 0f), 0.2f);

            Rider unseated = null;
            low.Unseated += r => unseated = r;

            for (var i = 0; i < 5; i++) yield return new WaitForFixedUpdate();

            Assert.AreSame(low, unseated);
            Assert.IsTrue(high.Mounted);
        }

        [UnityTest]
        public IEnumerator AnAlreadyUnseatedRiderIsNotUnseatedTwice()
        {
            var rider = Spawn("solo", Vector3.zero, 1f);
            var count = 0;
            rider.Unseated += _ => count++;

            rider.Unseat();
            rider.Unseat();
            yield return null;

            Assert.AreEqual(1, count);
        }
    }
}
