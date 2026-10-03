using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared input sampling only; panel navigation/thresholds remain with the panel.</summary>
public static class MenuSwipeInput
{
    public static bool TryReadMouse(ref bool dragging, ref Vector2 start, out Vector2 end)
    {
        end = default;
        if (Mouse.current == null) return false;
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            start = Mouse.current.position.ReadValue();
            dragging = true;
        }
        if (!Mouse.current.leftButton.wasReleasedThisFrame || !dragging) return false;
        end = Mouse.current.position.ReadValue();
        dragging = false;
        return true;
    }

    public static bool TryReadTouch(ref bool dragging, ref Vector2 start, out Vector2 end)
    {
        end = default;
        if (Touchscreen.current == null) return false;
        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
        {
            start = touch.position.ReadValue();
            dragging = true;
        }
        if (!touch.press.wasReleasedThisFrame || !dragging) return false;
        end = touch.position.ReadValue();
        dragging = false;
        return true;
    }
}
