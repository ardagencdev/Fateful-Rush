using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Warms the Fateful Rush shader variant collection only after MainMenu is
/// visible, and spreads the work across frames to avoid blocking startup.
/// </summary>
public sealed class AndroidShaderWarmup : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string CollectionResourceName = "FatefulRushRuntimeShaders";
    private const int VariantsPerFrame = 2;
    private const int InitialMenuFrames = 3;

    public static bool IsComplete { get; private set; }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidShaderWarmup instance;
    #endif

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
        while (SceneManager.GetActiveScene().name != MainMenuSceneName)
            yield return null;

        // Guarantee that the menu gets several rendered frames before warmup.
        for (int i = 0; i < InitialMenuFrames; i++)
            yield return null;

        ShaderVariantCollection collection =
            Resources.Load<ShaderVariantCollection>(CollectionResourceName);

        if (collection == null || collection.variantCount == 0)
        {
            Debug.LogWarning(
                "[ShaderWarmup] FatefulRushRuntimeShaders is missing or empty. " +
                "Warmup skipped safely."
            );
            Finish();
            yield break;
        }

        bool finished = collection.isWarmedUp;

        while (!finished)
        {
            // Only spend warmup time while the player is sitting in MainMenu.
            if (SceneManager.GetActiveScene().name != MainMenuSceneName)
            {
                yield return null;
                continue;
            }

            try
            {
                finished = collection.WarmUpProgressively(VariantsPerFrame);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning(
                    "[ShaderWarmup] Progressive warmup failed safely: " +
                    exception.Message
                );
                break;
            }

            if (!finished)
                yield return null;
        }

        Finish();
#else
        IsComplete = true;
        yield break;
#endif
    }

    private void Finish()
    {
        IsComplete = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    instance = null;
#endif

        Destroy(gameObject);
    }
}
