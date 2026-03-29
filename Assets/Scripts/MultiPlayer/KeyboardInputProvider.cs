using UnityEngine;
using TarodevController;
using UnityEngine.InputSystem;

public class KeyboardInputProvider : MonoBehaviour, IFrameInputProvider
{
    public enum Layout
    {
        WASD,
        Arrows
    }

    [Header("Layout")]
    public Layout layout = Layout.WASD;

    [Header("Buttons")]
    public bool allowDash = true;

    // You can tweak these if you want different bindings.
    private Key LeftKey   => layout == Layout.WASD ? Key.A : Key.LeftArrow;
    private Key RightKey  => layout == Layout.WASD ? Key.D : Key.RightArrow;
    private Key UpKey     => layout == Layout.WASD ? Key.W : Key.UpArrow;
    private Key DownKey   => layout == Layout.WASD ? Key.S : Key.DownArrow;

    // Jump/Dash defaults (change if you want)
    private Key JumpKey   => layout == Layout.WASD ? Key.Space : Key.RightCtrl;
    private Key DashKey   => layout == Layout.WASD ? Key.LeftShift : Key.RightShift;

    public FrameInput Gather()
    {
        var kb = Keyboard.current;
        if (kb == null)
        {
            return default;
        }

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