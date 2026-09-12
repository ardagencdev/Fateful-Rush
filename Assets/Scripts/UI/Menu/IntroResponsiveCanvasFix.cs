using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Keeps the Intro UI visually consistent between the Unity Game view and
/// real Android resolutions. IntroScene historically used Constant Pixel Size,
/// which made logos/text appear much smaller on high-resolution phones.
/// </summary>
public static class IntroResponsiveCanvasFix
{
    private static readonly Vector2 ReferenceResolution =
        new Vector2(1920f, 1080f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() ||
            scene.name.IndexOf("Intro", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            return;
        }

        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(
            FindObjectsInactive.Include
        );

        bool changed = false;

        for (int i = 0; i < scalers.Length; i++)
        {
            CanvasScaler scaler = scalers[i];

            if (scaler == null || scaler.gameObject.scene != scene)
                continue;

            // Leave the high-priority transition/fade canvas alone. The actual
            // intro artwork lives under the regular Canvas object.
            if (!string.Equals(
                    scaler.gameObject.name,
                    "Canvas",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // Matches the landscape UI behaviour used by the gameplay scene:
            // preserve the designed horizontal proportions on wide phones.
            scaler.matchWidthOrHeight = 0f;
            scaler.referencePixelsPerUnit = 100f;
            changed = true;
        }

        if (changed)
            Canvas.ForceUpdateCanvases();
    }
}
