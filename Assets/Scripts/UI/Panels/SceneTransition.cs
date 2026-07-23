using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Fade")]
    [SerializeField] private Image fadeImage;

    [SerializeField, Min(0f)]
    private float fadeDuration = 1f;

    private bool isTransitioning;
    private bool isQuitting;

    public bool IsTransitioning => isTransitioning;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstanceExists()
    {
        if (Instance != null)
            return;

        SceneTransition existing =
            FindAnyObjectByType<SceneTransition>();

        if (existing != null)
            return;

        GameObject transitionObject =
            new GameObject("SceneTransition");

        transitionObject.AddComponent<SceneTransition>();

        Debug.LogWarning(
            "[SceneTransition] Sahnede SceneTransition bulunamadığı " +
            "için otomatik olarak oluşturuldu."
        );
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        EnsureFadeImage();

        PrepareFadeCanvas();

        SetAlpha(0f);

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        PrepareFadeCanvas();
    }

    public void LoadSceneWithFade(string sceneName)
    {
        if (isTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[SceneTransition] Sahne bulunamadı veya " +
                $"Build Profiles'a eklenmemiş: '{sceneName}'",
                this
            );

            return;
        }

        StartCoroutine(
            TransitionRoutine(sceneName)
        );
    }

    public void LoadSceneWithFade(int sceneIndex)
    {
        if (isTransitioning)
            return;

        if (sceneIndex < 0 ||
            sceneIndex >=
            SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError(
                $"[SceneTransition] Geçersiz sahne indexi: " +
                $"{sceneIndex}",
                this
            );

            return;
        }

        StartCoroutine(
            TransitionRoutine(sceneIndex)
        );
    }

    public void QuitGameWithFade()
    {
        if (isTransitioning)
            return;

        StartCoroutine(QuitRoutine());
    }

    private IEnumerator TransitionRoutine(
        string sceneName)
    {
        isTransitioning = true;

        Time.timeScale = 0f;

        yield return FadeOutEverything();

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneName);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"[SceneTransition] Sahne yükleme işlemi " +
                $"başlatılamadı: '{sceneName}'",
                this
            );

            Time.timeScale = 1f;
            isTransitioning = false;

            yield return FadeIn();

            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return FinishTransition();
    }

    private IEnumerator TransitionRoutine(
        int sceneIndex)
    {
        isTransitioning = true;

        Time.timeScale = 0f;

        yield return FadeOutEverything();

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneIndex);

        if (loadOperation == null)
        {
            Debug.LogError(
                $"[SceneTransition] Sahne yükleme işlemi " +
                $"başlatılamadı. Index: {sceneIndex}",
                this
            );

            Time.timeScale = 1f;
            isTransitioning = false;

            yield return FadeIn();

            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return FinishTransition();
    }

    private IEnumerator FinishTransition()
    {
        Time.timeScale = 1f;

        yield return null;

        PrepareFadeCanvas();

        yield return FadeIn();

        isTransitioning = false;
    }

    private IEnumerator QuitRoutine()
    {
        isTransitioning = true;
        isQuitting = true;

        Time.timeScale = 0f;

        yield return FadeOutEverything();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeOutEverything()
    {
        Coroutine musicFade =
            StartCoroutine(FadeOutMusic());

        Coroutine screenFade =
            StartCoroutine(FadeOut());

        yield return musicFade;
        yield return screenFade;
    }

    private IEnumerator FadeOutMusic()
    {
        MenuMusicApply menuMusic =
            FindAnyObjectByType<MenuMusicApply>();

        if (menuMusic != null)
        {
            menuMusic.FadeOutMusic();

            float duration =
                Mathf.Max(
                    0f,
                    menuMusic.FadeOutDuration
                );

            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    duration
                );
            }

            yield break;
        }

        GameplayMusicFade gameplayMusic =
            FindAnyObjectByType<GameplayMusicFade>();

        if (gameplayMusic != null)
        {
            gameplayMusic.FadeOut();

            float duration =
                Mathf.Max(
                    0f,
                    gameplayMusic.FadeOutDuration
                );

            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    duration
                );
            }
        }
    }

    private IEnumerator FadeOut()
    {
        PrepareFadeCanvas();

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return Fade(
            fadeImage.color.a,
            1f
        );
    }

    private IEnumerator FadeIn()
    {
        PrepareFadeCanvas();

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        SetAlpha(1f);

        yield return Fade(1f, 0f);

        SetAlpha(0f);

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);
    }

    private IEnumerator Fade(
        float from,
        float to)
    {
        if (fadeDuration <= 0f)
        {
            SetAlpha(to);
            yield break;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / fadeDuration
                );

            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            SetAlpha(
                Mathf.Lerp(
                    from,
                    to,
                    smoothProgress
                )
            );

            yield return null;
        }

        SetAlpha(to);
    }

    private void EnsureFadeImage()
    {
        if (fadeImage == null)
        {
            CreateFadeImage();
        }
    }

    private void PrepareFadeCanvas()
    {
        EnsureFadeImage();

        if (fadeImage == null)
            return;

        Canvas canvas =
            fadeImage.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.overrideSorting = true;
            canvas.sortingOrder =
                short.MaxValue;

            canvas.targetDisplay = 0;
            canvas.gameObject.SetActive(true);
        }

        RectTransform rectTransform =
            fadeImage.rectTransform;

        rectTransform.anchorMin =
            Vector2.zero;

        rectTransform.anchorMax =
            Vector2.one;

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.offsetMin =
            Vector2.zero;

        rectTransform.offsetMax =
            Vector2.zero;

        Color color = fadeImage.color;

        color.r = 0f;
        color.g = 0f;
        color.b = 0f;

        fadeImage.color = color;

        fadeImage.transform.SetAsLastSibling();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;

        color.a =
            Mathf.Clamp01(alpha);

        fadeImage.color = color;
    }

    private void CreateFadeImage()
    {
        GameObject canvasObject =
            new GameObject(
                "TransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        canvasObject.transform.SetParent(
            transform,
            false
        );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.overrideSorting = true;
        canvas.sortingOrder =
            short.MaxValue;

        canvas.targetDisplay = 0;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1080f, 1920f);

        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject =
            new GameObject(
                "FadeImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        imageObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform rectTransform =
            imageObject.GetComponent<RectTransform>();

        rectTransform.anchorMin =
            Vector2.zero;

        rectTransform.anchorMax =
            Vector2.one;

        rectTransform.pivot =
            new Vector2(0.5f, 0.5f);

        rectTransform.offsetMin =
            Vector2.zero;

        rectTransform.offsetMax =
            Vector2.zero;

        fadeImage =
            imageObject.GetComponent<Image>();

        fadeImage.sprite = null;
        fadeImage.type =
            Image.Type.Simple;

        fadeImage.color =
            new Color(0f, 0f, 0f, 0f);

        fadeImage.raycastTarget = false;
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -=
            OnSceneLoaded;

        Instance = null;

        if (!isQuitting)
        {
            Time.timeScale = 1f;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );
    }
#endif
}