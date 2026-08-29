using Joust.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Joust.Player
{
    /// <summary>
    /// Translates keyboard and gamepad input into motor commands.
    ///
    /// Bindings mirror the pygame reference: left/right to steer, space to flap,
    /// with the gamepad's left stick and south button doing the same.
    /// </summary>
    [RequireComponent(typeof(RiderMotor))]
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private float stickDeadzone = 0.3f;

        private RiderMotor _motor;

        private void Awake() => _motor = GetComponent<RiderMotor>();

        private void Update()
        {
            if (_motor == null)
            {
                return;
            }

            _motor.SetThrust(ReadThrust());

            if (FlapPressedThisFrame())
            {
                _motor.Flap();
            }
        }

        private float ReadThrust()
        {
            var thrust = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) thrust -= 1f;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) thrust += 1f;
            }

            var gamepad = Gamepad.current;
            if (gamepad != null && Mathf.Abs(thrust) < 0.01f)
            {
                var stick = gamepad.leftStick.ReadValue().x;
                if (Mathf.Abs(stick) > stickDeadzone)
                {
                    thrust = Mathf.Sign(stick);
                }
                else if (gamepad.dpad.left.isPressed)
                {
                    thrust = -1f;
                }
                else if (gamepad.dpad.right.isPressed)
                {
                    thrust = 1f;
                }
            }

            return Mathf.Clamp(thrust, -1f, 1f);
        }

        private static bool FlapPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;
        }
    }
}
