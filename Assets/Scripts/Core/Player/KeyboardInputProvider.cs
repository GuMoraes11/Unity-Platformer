using UnityEngine;
using TarodevController;
using UnityEngine.InputSystem;

public class KeyboardInputProvider : MonoBehaviour, IFrameInputProvider
{
    public enum Layout
    {
        WASD,
        Arrows,
        Numpad
    }

    [Header("Layout")]
    public Layout layout = Layout.WASD;

    [Header("Buttons")]
    public bool allowDash = true;

    private Key LeftKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.A;
                case Layout.Arrows: return Key.LeftArrow;
                case Layout.Numpad: return Key.Numpad4;
                default:            return Key.A;
            }
        }
    }

    private Key RightKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.D;
                case Layout.Arrows: return Key.RightArrow;
                case Layout.Numpad: return Key.Numpad6;
                default:            return Key.D;
            }
        }
    }

    private Key UpKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.W;
                case Layout.Arrows: return Key.UpArrow;
                case Layout.Numpad: return Key.Numpad8;
                default:            return Key.W;
            }
        }
    }

    private Key DownKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.S;
                case Layout.Arrows: return Key.DownArrow;
                case Layout.Numpad: return Key.Numpad5;
                default:            return Key.S;
            }
        }
    }

    private Key JumpKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.Space;
                case Layout.Arrows: return Key.RightCtrl;
                case Layout.Numpad: return Key.Numpad0; // change if you want
                default:            return Key.Space;
            }
        }
    }

    private Key DashKey
    {
        get
        {
            switch (layout)
            {
                case Layout.WASD:   return Key.LeftShift;
                case Layout.Arrows: return Key.RightShift;
                case Layout.Numpad: return Key.NumpadEnter; // change if you want
                default:            return Key.LeftShift;
            }
        }
    }

    public FrameInput Gather()
    {
        var kb = Keyboard.current;
        if (kb == null)
            return default;

        float x = 0f;
        float y = 0f;

        if (kb[LeftKey].isPressed)  x -= 1f;
        if (kb[RightKey].isPressed) x += 1f;
        if (kb[DownKey].isPressed)  y -= 1f;
        if (kb[UpKey].isPressed)    y += 1f;

        bool jumpHeld = kb[JumpKey].isPressed;
        bool jumpDown = kb[JumpKey].wasPressedThisFrame;
        bool dashDown = allowDash && kb[DashKey].wasPressedThisFrame;

        return new FrameInput
        {
            Move = new Vector2(x, y),
            JumpDown = jumpDown,
            JumpHeld = jumpHeld,
            DashDown = dashDown
        };
    }
}