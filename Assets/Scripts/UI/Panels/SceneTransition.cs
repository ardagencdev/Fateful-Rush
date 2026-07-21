using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance;

    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 0.35f;

    private bool isTransitioning;

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

        if (fadeImage == null)
            CreateFadeImage();

        PrepareFadeCanvas();

        SetAlpha(0f);

        fadeImage.raycastTarget = false;
        fadeImage.gameObject.SetActive(false);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private IEnumerator FadeOutMusic()
    {
        MenuMusicApply menuMusic =
            FindAnyObjectByType<MenuMusicApply>();

        if (menuMusic != null)
        {
            menuMusic.FadeOutMusic();

            yield return new WaitForSecondsRealtime(
                menuMusic.FadeOutDuration
            );

            yield break;
        }

        GameplayMusicFade gameplayMusic =
            FindAnyObjectByType<GameplayMusicFade>();

        if (gameplayMusic != null)
        {
            gameplayMusic.FadeOut();

            yield return new WaitForSecondsRealtime(
                gameplayMusic.FadeOutDuration
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -=
                OnSceneLoaded;

            Instance = null;
        }
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        PrepareFadeCanvas();
    }

    public void LoadSceneWithFade(
        string sceneName)
    {
        if (isTransitioning)
            return;

        if (string.IsNullOrEmpty(sceneName))
            return;

        StartCoroutine(
            TransitionRoutine(sceneName)
        );
    }

    public void LoadSceneWithFade(
        int sceneIndex)
    {
        if (isTransitioning)
            return;

        if (sceneIndex < 0 ||
            sceneIndex >=
            SceneManager.sceneCountInBuildSettings)
        {
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

    private IEnumerator QuitRoutine()
    {
        isTransitioning = true;

        Time.timeScale = 0f;

        yield return FadeOutMusic();
        yield return FadeOut();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator TransitionRoutine(
        string sceneName)
    {
        isTransitioning = true;

        Time.timeScale = 0f;

        yield return FadeOutMusic();
        yield return FadeOut();

        AsyncOperation load =
            SceneManager.LoadSceneAsync(sceneName);

        while (load != null &&
               !load.isDone)
        {
            yield return null;
        }

        Time.timeScale = 1f;

        yield return null;

        PrepareFadeCanvas();

        yield return FadeIn();

        isTransitioning = false;
    }

    private IEnumerator TransitionRoutine(
        int sceneIndex)
    {
        isTransitioning = true;

        Time.timeScale = 0f;

        yield return FadeOutMusic();
        yield return FadeOut();

        AsyncOperation load =
            SceneManager.LoadSceneAsync(sceneIndex);

        while (load != null &&
               !load.isDone)
        {
            yield return null;
        }

        Time.timeScale = 1f;

        yield return null;

        PrepareFadeCanvas();

        yield return FadeIn();

        isTransitioning = false;
    }

    private IEnumerator FadeOut()
    {
        PrepareFadeCanvas();

        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        yield return Fade(0f, 1f);
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
        float timer = 0f;

        float duration =
            Mathf.Max(
                0.01f,
                fadeDuration
            );

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            progress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            SetAlpha(
                Mathf.Lerp(
                    from,
                    to,
                    progress
                )
            );

            yield return null;
        }

        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
            return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    private void PrepareFadeCanvas()
    {
        if (fadeImage == null)
            return;

        Canvas canvas =
            fadeImage.GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 9999;
        }

        fadeImage.color = new Color(
            0f,
            0f,
            0f,
            fadeImage.color.a
        );

        fadeImage.transform.SetAsLastSibling();
    }

    private void CreateFadeImage()
    {
        Canvas canvas =
            GetComponentInChildren<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject =
                new GameObject(
                    "TransitionCanvas"
                );

            canvasObject.transform.SetParent(
                transform,
                false
            );

            canvas =
                canvasObject.AddComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = 9999;

            CanvasScaler scaler =
                canvasObject
                    .AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode
                    .ScaleWithScreenSize;

            scaler.referenceResolution =
                new Vector2(
                    1080f,
                    1920f
                );

            scaler.matchWidthOrHeight = 0.5f;

            canvasObject
                .AddComponent<GraphicRaycaster>();
        }

        GameObject imageObject =
            new GameObject("FadeImage");

        imageObject.transform.SetParent(
            canvas.transform,
            false
        );

        fadeImage =
            imageObject.AddComponent<Image>();

        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;

        RectTransform rectTransform =
            imageObject.GetComponent<RectTransform>();

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void OnValidate()
    {
        fadeDuration =
            Mathf.Max(
                0f,
                fadeDuration
            );
    }
}