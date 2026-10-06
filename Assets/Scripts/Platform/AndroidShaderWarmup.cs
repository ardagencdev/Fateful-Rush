using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Prepares the known variants during the intro's fully black exit.</summary>
public sealed class AndroidShaderWarmup : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string CollectionResourceName = "FatefulRushRuntimeShaders";
    private const int VariantsPerFrame = 1;
    public static bool IsComplete { get; private set; }
    public static string WarmupState { get; private set; } = "PENDING";
#if FATEFULRUSH_DIAGNOSTICS
    // Observation only: never load, replace or warm anything from diagnostics.
    internal static ShaderVariantCollection DiagnosticsCollection
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return retainedCollection;
#else
            return null;
#endif
        }
    }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
    private static AndroidShaderWarmup instance;
    private static bool preparationRequested;
    // Only the curated runtime collection is retained. No textures, scene
    // renderers, material instances or warmup MonoBehaviour are kept alive.
    private static ShaderVariantCollection retainedCollection;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsComplete = false;
        WarmupState = "PENDING";
#if UNITY_ANDROID && !UNITY_EDITOR
        instance = null;
        preparationRequested = false;
        retainedCollection = null;
#endif
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (instance != null || IsComplete) return;
        GameObject root = new GameObject("AndroidShaderWarmup");
        instance = root.AddComponent<AndroidShaderWarmup>();
        DontDestroyOnLoad(root);
#else
        IsComplete = true;
        WarmupState = "SKIPPED_EDITOR_OR_DESKTOP";
#endif
    }

    // Called only after all intro visuals and audio have faded away.
    public static IEnumerator PrepareForMenu()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (IsComplete) yield break;
        preparationRequested = true;
        if (instance == null) Bootstrap();
        while (!IsComplete) yield return null;
#else
        yield break;
#endif
    }

    private IEnumerator Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Direct MainMenu startup remains supported when testing without Intro.
        while (!preparationRequested && SceneManager.GetActiveScene().name != MainMenuSceneName)
            yield return null;

        WarmupState = "LOADING_COLLECTION";
        ShaderVariantCollection collection = Resources.Load<ShaderVariantCollection>(CollectionResourceName);
        if (collection == null || collection.variantCount == 0)
        {
            Debug.LogWarning("[ShaderWarmup] Known shader collection is missing or empty.");
            Finish("MISSING_COLLECTION");
            yield break;
        }

        // Single-mode scene loads can unload unreferenced assets. Keep the
        // prepared shaders reachable after the temporary host is destroyed.
        retainedCollection = collection;

        WarmupState = "WARMING";
        int framesRemaining = Mathf.Clamp(collection.variantCount + 32, 32, 512);
        float deadline = Time.realtimeSinceStartup + 10f;
        bool finished = collection.isWarmedUp;
        bool failed = false;
        while (!finished && framesRemaining-- > 0 && Time.realtimeSinceStartup < deadline)
        {
            try
            {
                finished = collection.WarmUpProgressively(VariantsPerFrame);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("[ShaderWarmup] Preparation failed: " + exception.Message);
                failed = true;
                break;
            }
            if (!finished) yield return null;
        }
        if (failed)
        {
            Finish("FAILED");
            yield break;
        }
        if (!finished)
            Debug.LogWarning("[ShaderWarmup] Preparation timed out; continuing startup safely.");
        Finish(finished ? "DONE" : "TIMED_OUT");
#else
        Finish("SKIPPED_EDITOR_OR_DESKTOP");
        yield break;
#endif
    }

    private void Finish(string state)
    {
        WarmupState = state;
        IsComplete = true; // Terminal state; failure is exposed separately above.
#if UNITY_ANDROID && !UNITY_EDITOR
        instance = null;
#endif
        Destroy(gameObject);
    }
}
