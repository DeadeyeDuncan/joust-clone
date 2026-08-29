using System.Collections;
using Joust.Core;
using Joust.Movement;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Joust.Tests.PlayMode
{
    /// <summary>
    /// Flight behaviour under real physics. Feel is a human judgement, but these
    /// are the properties that must hold whatever the tuning ends up being.
    /// </summary>
    public class RiderMotorTests
    {
        private GameObject _go;

        private RiderMotor Spawn()
        {
            _go = new GameObject("rider");
            _go.AddComponent<Rigidbody>();
            var motor = _go.AddComponent<RiderMotor>();
            motor.Configure(ScriptableObject.CreateInstance<TuningProfile>());
            return motor;
        }

        [TearDown]
        public void Cleanup()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        [UnityTest]
        public IEnumerator FallsUnderGravityWhenAirborne()
        {
            var motor = Spawn();
            var startY = motor.transform.position.y;

            for (var i = 0; i < 20; i++) yield return new WaitForFixedUpdate();

            Assert.Less(motor.transform.position.y, startY);
        }

        [UnityTest]
        public IEnumerator FlapProducesUpwardVelocity()
        {
            var motor = Spawn();
            motor.Flap();

            yield return new WaitForFixedUpdate();

            Assert.Greater(motor.Velocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator RepeatedFlapsDoNotAccumulateClimb()
        {
            var motor = Spawn();
            motor.Flap();
            yield return new WaitForFixedUpdate();
            var afterOne = motor.Velocity.y;

            motor.Flap();
            motor.Flap();
            motor.Flap();
            yield return new WaitForFixedUpdate();

            Assert.LessOrEqual(motor.Velocity.y, afterOne + 0.01f,
                "flap must SET vertical velocity, not add to it");
        }

        [UnityTest]
        public IEnumerator ThrustIsClampedToMaxSpeed()
        {
            var motor = Spawn();
            motor.SetThrust(1f);

            for (var i = 0; i < 300; i++) yield return new WaitForFixedUpdate();

            Assert.LessOrEqual(Mathf.Abs(motor.Velocity.x), 12.01f);
        }

        [UnityTest]
        public IEnumerator ThrustReversesDirection()
        {
            var motor = Spawn();

            motor.SetThrust(1f);
            for (var i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            Assert.Greater(motor.Velocity.x, 0f);

            motor.SetThrust(-1f);
            for (var i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            Assert.Less(motor.Velocity.x, 0f);
        }

        [UnityTest]
        public IEnumerator MovementStaysOnThePlane()
        {
            var motor = Spawn();
            motor.SetThrust(1f);
            motor.Flap();

            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

            Assert.AreEqual(0f, motor.transform.position.z, 1e-3f,
                "gameplay is 2.5D: Z must stay frozen");
        }

        [UnityTest]
        public IEnumerator ReleasingThrustDecaysHorizontalSpeed()
        {
            var motor = Spawn();
            motor.SetThrust(1f);
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            var moving = motor.Velocity.x;

            motor.SetThrust(0f);
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            Assert.Less(motor.Velocity.x, moving);
        }
    }
}
