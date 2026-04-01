using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TarodevController
{
    public class PlayerInput : MonoBehaviour
    {
        public enum ControlScheme
        {
            InputSystemActions,
            KeyboardWASD,
            KeyboardArrows,
            KeyboardNumpad,
            Gamepad
        }

        [Header("Control Scheme")]
        [SerializeField] private ControlScheme scheme = ControlScheme.InputSystemActions;

        [Header("Keyboard Bindings")]
        [SerializeField] private bool allowDash = true;

#if ENABLE_INPUT_SYSTEM
        private PlayerInputActions _actions;
        private InputAction _move;
        private InputAction _jump;
        private InputAction _dash;
#endif

#if ENABLE_INPUT_SYSTEM
private bool _lastLeftTriggerPressed;
private bool _lastRightTriggerPressed;
#endif

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            _actions = new PlayerInputActions();
            _move = _actions.Player.Move;
            _jump = _actions.Player.Jump;
            _dash = _actions.Player.Dash;
#endif
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _actions?.Enable();
#endif
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _actions?.Disable();
#endif
        }

        public FrameInput Gather()
        {
            switch (scheme)
            {
                case ControlScheme.KeyboardWASD:
                case ControlScheme.KeyboardArrows:
                case ControlScheme.KeyboardNumpad:
                    return GatherKeyboard();

                case ControlScheme.Gamepad:
                    return GatherGamepad();

                case ControlScheme.InputSystemActions:
                default:
#if ENABLE_INPUT_SYSTEM
                    return new FrameInput
                    {
                        JumpDown = _jump.WasPressedThisFrame(),
                        JumpHeld = _jump.IsPressed(),
                        DashDown = _dash.WasPressedThisFrame(),
                        Move = _move.ReadValue<Vector2>()
                    };
#else
                    return new FrameInput
                    {
                        JumpDown = Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Jump"),
                        JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetButton("Jump"),
                        DashDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetButtonDown("Fire3"),
                        Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
                    };
#endif
            }
        }

        private FrameInput GatherKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return default;

            Key left, right, up, down;
            Key jumpKey, dashKey;

            switch (scheme)
            {
                case ControlScheme.KeyboardWASD:
                    left = Key.A;
                    right = Key.D;
                    up = Key.W;
                    down = Key.S;

                    // restore your original behavior
                    jumpKey = Key.W;
                    dashKey = Key.LeftShift;
                    break;

                case ControlScheme.KeyboardArrows:
                    left = Key.LeftArrow;
                    right = Key.RightArrow;
                    up = Key.UpArrow;
                    down = Key.DownArrow;

                    // restore your original behavior
                    jumpKey = Key.UpArrow;
                    dashKey = Key.RightShift;
                    break;

                case ControlScheme.KeyboardNumpad:
                    left = Key.Numpad4;
                    right = Key.Numpad6;
                    up = Key.Numpad8;
                    down = Key.Numpad5;

                    // matches the same "up = jump" style as your other schemes
                    jumpKey = Key.Numpad8;
                    dashKey = Key.Numpad0;
                    break;

                default:
                    return default;
            }

            float x = 0f;
            float y = 0f;

            if (kb[left].isPressed) x -= 1f;
            if (kb[right].isPressed) x += 1f;
            if (kb[down].isPressed) y -= 1f;
            if (kb[up].isPressed) y += 1f;

            return new FrameInput
            {
                Move = new Vector2(x, y),
                JumpDown = kb[jumpKey].wasPressedThisFrame,
                JumpHeld = kb[jumpKey].isPressed,
                DashDown = allowDash && kb[dashKey].wasPressedThisFrame
            };
#else
            return default;
#endif
        }

        private FrameInput GatherGamepad()
        {
        #if ENABLE_INPUT_SYSTEM
            var gp = Gamepad.current;
            if (gp == null) return default;

            Vector2 move = gp.leftStick.ReadValue();

            // Optional: let d-pad also work if you want
            Vector2 dpad = gp.dpad.ReadValue();
            if (dpad != Vector2.zero)
                move = dpad;

            // Trigger dash detection
            bool leftTriggerPressed = gp.leftTrigger.ReadValue() > 0.5f;
            bool rightTriggerPressed = gp.rightTrigger.ReadValue() > 0.5f;

            bool leftTriggerDown = leftTriggerPressed && !_lastLeftTriggerPressed;
            bool rightTriggerDown = rightTriggerPressed && !_lastRightTriggerPressed;

            _lastLeftTriggerPressed = leftTriggerPressed;
            _lastRightTriggerPressed = rightTriggerPressed;

            return new FrameInput
            {
                Move = move,
                JumpDown = gp.buttonSouth.wasPressedThisFrame,
                JumpHeld = gp.buttonSouth.isPressed,

                // Dash works on either trigger OR the right-facing face button
                DashDown = allowDash && (
                    leftTriggerDown ||
                    rightTriggerDown ||
                    gp.buttonEast.wasPressedThisFrame
                )
            };
        #else
            return default;
        #endif
        }

        public void SetScheme(ControlScheme newScheme) => scheme = newScheme;
        public ControlScheme GetScheme() => scheme;
    }

    public struct FrameInput
    {
        public Vector2 Move;
        public bool JumpDown;
        public bool JumpHeld;
        public bool DashDown;
    }
}