using System.Collections;
using Joust.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// The hazard under test: OnTriggerEnter fires on BOTH colliders of a pair,
    /// so a naive handler resolves the same duel twice and can kill both riders.
    /// Watching a console for duplicate log lines is not evidence; this is.
    /// </summary>
    public class JoustContactPlayModeTests
    {
        private GameObject _a;
        private GameObject _b;

        [SetUp]
        public void ResetCounter()
        {
            JoustContact.ResetResolutionCount();
        }

        [TearDown]
        public void Cleanup()
        {
            if (_a != null) Object.Destroy(_a);
            if (_b != null) Object.Destroy(_b);
        }

        private static GameObject MakeRider(string name, Vector3 position, float lanceHeight)
        {
            var rider = new GameObject(name);
            rider.transform.position = position;

            var collider = rider.AddComponent<SphereCollider>();
            collider.radius = 1f;
            collider.isTrigger = true;

            var body = rider.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var lance = new GameObject("lance").transform;
            lance.SetParent(rider.transform, false);
            lance.localPosition = new Vector3(0f, lanceHeight, 0f);

            var contact = rider.AddComponent<JoustContact>();
            contact.Configure(lance, 0.5f);
            return rider;
        }

        [UnityTest]
        public IEnumerator OverlappingRidersResolveExactlyOnce()
        {
            _a = MakeRider("rider_high", Vector3.zero, 1.5f);
            _b = MakeRider("rider_low", new Vector3(0.2f, 0f, 0f), 0.2f);

            for (var i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(
                1,
                JoustContact.ResolutionCount,
                "one overlap between two riders must resolve exactly once, not once per participant");
        }

        [UnityTest]
        public IEnumerator TheHigherLanceIsRecordedAsTheWinner()
        {
            _a = MakeRider("rider_high", Vector3.zero, 1.5f);
            _b = MakeRider("rider_low", new Vector3(0.2f, 0f, 0f), 0.2f);

            for (var i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.AreEqual(1, JoustContact.ResolutionCount, "expected exactly one resolution");
            Assert.AreSame(
                _a.transform,
                JoustContact.LastWinner,
                "the rider with the higher lance must be recorded as the winner");
        }
    }
}
