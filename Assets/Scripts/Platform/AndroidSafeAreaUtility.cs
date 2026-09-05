using UnityEngine;

/// <summary>
/// Converts Android's current Screen.safeArea into Canvas-space insets.
/// Background/world rendering can stay edge-to-edge while critical HUD
/// controls are kept clear of display cutouts and system gesture areas.
/// </summary>
public static class AndroidSafeAreaUtility
{
    public readonly struct Insets
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public Insets(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }

    public static Insets GetCanvasInsets(RectTransform rectTransform)
    {
        if (rectTransform == null || Screen.width <= 0 || Screen.height <= 0)
            return new Insets(0f, 0f, 0f, 0f);

        Rect safeArea = Screen.safeArea;

        float leftPixels = Mathf.Max(0f, safeArea.xMin);
        float rightPixels = Mathf.Max(0f, Screen.width - safeArea.xMax);
        float bottomPixels = Mathf.Max(0f, safeArea.yMin);
        float topPixels = Mathf.Max(0f, Screen.height - safeArea.yMax);

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null && canvas.scaleFactor > 0.0001f
            ? canvas.scaleFactor
            : 1f;

        return new Insets(
            leftPixels / canvasScale,
            rightPixels / canvasScale,
            topPixels / canvasScale,
            bottomPixels / canvasScale
        );
    }
}
