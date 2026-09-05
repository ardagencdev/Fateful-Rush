using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Moves Unity's first-use shader warm-up cost to the Main Menu instead of
/// letting the first gameplay levels pay that cost during enemy/effect renders.
///
/// This is intentionally Android-only in player builds. It runs once per app
/// launch, before the ad SDK is allowed to begin its heavier startup work.
/// </summary>
public sealed class AndroidShaderWarmup : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";

    public static bool IsComplete { get; private set; }

    private static AndroidShaderWarmup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (instance != null)
            return;

        GameObject root = new GameObject("AndroidShaderWarmup");
        instance = root.AddComponent<AndroidShaderWarmup>();
        DontDestroyOnLoad(root);
#else
        IsComplete = true;
#endif
    }

    private IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Wait until the menu scene is active. This keeps the one-time warm-up
        // outside active gameplay even if the project boot scene changes later.
        while (SceneManager.GetActiveScene().name != MainMenuSceneName)
            yield return null;

        // Give the scene/UI one frame to initialize before doing the one-time
        // shader preparation. Any hitch happens here, not during Level 1.
        yield return null;

        try
        {
            Shader.WarmupAllShaders();
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning(
                "[ShaderWarmup] Warmup failed safely: " + exception.Message
            );
        }

        IsComplete = true;
        instance = null;
        Destroy(gameObject);
#else
        IsComplete = true;
        yield break;
#endif
    }
}
