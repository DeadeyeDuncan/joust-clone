using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator RigidbodyWithCustomGravityFalls()
        {
            var go = new GameObject("faller");
            var body = go.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            var startY = go.transform.position.y;
            for (var i = 0; i < 30; i++)
            {
                body.AddForce(new Vector3(0f, -30f, 0f), ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }

            Assert.Less(go.transform.position.y, startY, "custom gravity did not move the body downward");
            Object.Destroy(go);
        }
    }
}
