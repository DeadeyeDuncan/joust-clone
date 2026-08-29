// SPIKE: throwaway, not carried into M1.
using UnityEngine;
using UnityEngine.InputSystem;

namespace Joust.Movement
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightPrototype : MonoBehaviour
    {
        [SerializeField] private float gravity = 24f;
        [SerializeField] private float flapImpulse = 9f;
        [SerializeField] private float thrustAcceleration = 17f;
        [SerializeField] private float maxHorizontalSpeed = 12f;
        [SerializeField] private float airDrag = 0.6f;
        [SerializeField] private float groundSkidDeceleration = 27f;
        [SerializeField] private float groundCheckDistance = 1.1f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Rigidbody _body;
        private float _moveInput;
        private bool _flapQueued;
        private bool _grounded;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            _moveInput = 0f;
            if (keyboard.leftArrowKey.isPressed)
            {
                _moveInput -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                _moveInput += 1f;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _flapQueued = true;
            }
        }

        private void FixedUpdate()
        {
            _grounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                groundCheckDistance,
                groundLayers);

            var velocity = _body.linearVelocity;

            if (_flapQueued)
            {
                velocity.y = flapImpulse;
                _grounded = false;
                _flapQueued = false;
            }

            if (Mathf.Abs(_moveInput) > 0.01f)
            {
                velocity.x += _moveInput * thrustAcceleration * Time.fixedDeltaTime;
            }
            else if (_grounded)
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, groundSkidDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, airDrag * Time.fixedDeltaTime);
            }

            velocity.x = Mathf.Clamp(velocity.x, -maxHorizontalSpeed, maxHorizontalSpeed);

            if (!_grounded)
            {
                velocity.y -= gravity * Time.fixedDeltaTime;
            }
            else if (velocity.y < 0f)
            {
                velocity.y = 0f;
            }

            _body.linearVelocity = velocity;
        }
    }
}
