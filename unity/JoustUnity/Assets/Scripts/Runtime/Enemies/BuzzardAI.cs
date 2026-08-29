using Joust.Core;
using Joust.Movement;
using UnityEngine;

namespace Joust.Enemies
{
    /// <summary>
    /// Drives a <see cref="RiderMotor"/> from <see cref="BuzzardDecision"/>.
    ///
    /// Deliberately thin: it supplies the world state and the dice, and applies
    /// the result. All the judgement lives in the pure function, where it is
    /// tested.
    /// </summary>
    [RequireComponent(typeof(RiderMotor))]
    public class BuzzardAI : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float decisionInterval = 0.25f;

        private RiderMotor _motor;
        private float _nextDecision;

        /// <summary>Height below which the rider prioritises climbing over pursuit.</summary>
        private static float LavaLine => ArenaMetrics.Units(50f);

        public void Configure(Transform pursue) => target = pursue;

        private void Awake() => _motor = GetComponent<RiderMotor>();

        private void Update()
        {
            if (target == null || _motor == null || Time.time < _nextDecision)
            {
                return;
            }

            // Decisions are spaced rather than per-frame, so a bounder commits to
            // a direction long enough to read as intent instead of jitter.
            _nextDecision = Time.time + decisionInterval;

            var decision = BuzzardDecision.Decide(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(target.position.x, target.position.y),
                LavaLine,
                Random.value);

            _motor.SetThrust(decision.Thrust);
            if (decision.Flap)
            {
                _motor.Flap();
            }
        }
    }
}
