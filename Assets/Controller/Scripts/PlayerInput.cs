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
            // Uses your existing Input System action map (Move/Jump/Dash)
            InputSystemActions,

            // Split-keyboard layouts for couch co-op
            KeyboardWASD,
            KeyboardArrows
        }

        [Header("Control Scheme")]
        [SerializeField] private ControlScheme scheme = ControlScheme.InputSystemActions;

        [Header("Keyboard Bindings (used for KeyboardWASD/KeyboardArrows)")]
        [SerializeField] private bool allowDash = true;

#if ENABLE_INPUT_SYSTEM
        private PlayerInputActions _actions;
        private InputAction _move, _jump, _dash;
#endif

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            // Keep your existing Input System wiring
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
            // If this player is configured for split-keyboard, read directly from Keyboard.
            if (scheme == ControlScheme.KeyboardWASD || scheme == ControlScheme.KeyboardArrows)
            {
                return GatherKeyboard();
            }

            // Otherwise use your normal action map.
#if ENABLE_INPUT_SYSTEM
            return new FrameInput
            {
                JumpDown = _jump.WasPressedThisFrame(),
                JumpHeld = _jump.IsPressed(),
                DashDown = _dash.WasPressedThisFrame(),
                Move = _move.ReadValue<Vector2>()
            };
#else
            // Legacy fallback
            return new FrameInput
            {
                JumpDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetButtonDown("Jump"),
                JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.C) || Input.GetButton("Jump"),
                DashDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetButtonDown("Fire3"),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };
#endif
        }

        private FrameInput GatherKeyboard()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null) return default;

            // Layout keys
            Key left, right, up, down;
            Key jumpKey, dashKey;

            if (scheme == ControlScheme.KeyboardWASD)
			{
		    	left = Key.A; right = Key.D; up = Key.W; down = Key.S;
		    	jumpKey = Key.W;
			    dashKey = Key.LeftShift;
		}
			else // KeyboardArrows
			{
			    left = Key.LeftArrow; right = Key.RightArrow; up = Key.UpArrow; down = Key.DownArrow;
			    jumpKey = Key.UpArrow;
			    dashKey = Key.RightShift;
			}

            float x = 0f;
            float y = 0f;

            if (kb[left].isPressed) x -= 1f;
            if (kb[right].isPressed) x += 1f;
            if (kb[down].isPressed) y -= 1f;
            if (kb[up].isPressed) y += 1f;

            bool jumpHeld = kb[jumpKey].isPressed;
            bool jumpDown = kb[jumpKey].wasPressedThisFrame;
            bool dashDown = allowDash && kb[dashKey].wasPressedThisFrame;

            return new FrameInput
            {
                Move = new Vector2(x, y),
                JumpDown = jumpDown,
                JumpHeld = jumpHeld,
                DashDown = dashDown
            };
#else
            // If the Input System isn't enabled, you *can’t* split keyboards cleanly like this.
            // (Old Input Manager doesn't support per-player keyboard separation.)
            return default;
#endif
        }

        // Optional helper so other scripts can set this at runtime (spawner does this).
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