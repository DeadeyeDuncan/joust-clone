using Joust.Core;
using UnityEngine;

namespace Joust.Movement
{
    /// <summary>
    /// Arcade flight for a mounted rider, shared by the player and the AI.
    ///
    /// Uses a Rigidbody for collision response but drives velocity directly
    /// rather than accumulating forces: arcade flight needs predictable,
    /// repeatable arcs, and force accumulation gives neither. Gravity is applied
    /// here rather than by the physics engine so it can be tuned independently
    /// of everything else in the scene.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RiderMotor : MonoBehaviour
    {
        [SerializeField] private TuningProfile profile;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody _body;
        private float _thrust;
        private bool _flapQueued;

        public bool Grounded { get; private set; }

        public Vector2 Velocity => _body == null
            ? Vector2.zero
            : new Vector2(_body.linearVelocity.x, _body.linearVelocity.y);

        /// <summary>Facing as -1 or 1, held through a stop so the rider does not snap forward.</summary>
        public int Facing { get; private set; } = 1;

        public void Configure(TuningProfile tuning) => profile = tuning;

        public void SetThrust(float direction)
        {
            _thrust = Mathf.Clamp(direction, -1f, 1f);
            if (Mathf.Abs(_thrust) > 0.01f)
            {
                Facing = _thrust > 0f ? 1 : -1;
            }
        }

        public void Flap() => _flapQueued = true;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        private void FixedUpdate()
        {
            if (profile == null)
            {
                return;
            }

            Grounded = Physics.Raycast(
                transform.position, Vector3.down, groundCheckDistance, groundLayers);

            var velocity = _body.linearVelocity;

            if (_flapQueued)
            {
                // SET, never add. Adding lets a mashed button climb without
                // bound, which is the single change that stops this feeling
                // like Joust.
                velocity.y = profile.flapImpulse;
                Grounded = false;
                _flapQueued = false;
            }

            if (Mathf.Abs(_thrust) > 0.01f)
            {
                velocity.x += _thrust * profile.thrustAcceleration * Time.fixedDeltaTime;
            }
            else
            {
                var decay = Grounded ? profile.groundSkidDeceleration : profile.airDrag;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, decay * Time.fixedDeltaTime);
            }

            velocity.x = Mathf.Clamp(
                velocity.x, -profile.maxHorizontalSpeed, profile.maxHorizontalSpeed);

            if (!Grounded)
            {
                velocity.y -= profile.gravity * Time.fixedDeltaTime;
            }
            else if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            _body.linearVelocity = velocity;
        }
    }
}
