using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class IntroController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField]
    private string nextSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField, Min(0f)]
    private float startDelay = 0.35f;

    [SerializeField, Min(0f)]
    private float fadeInDuration = 1.5f;

    [SerializeField, Min(0f)]
    private float holdDuration = 2.2f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.8f;

    [Header("Intro Sound")]
    [SerializeField]
    private AudioSource introAudioSource;

    [SerializeField]
    private AudioClip introSound;

    [SerializeField]
    private bool fadeOutSoundWhenSkipping = true;

    [Header("Logo")]
    [SerializeField]
    private CanvasGroup logoGroup;

    [SerializeField]
    private RectTransform logoTransform;

    [SerializeField, Min(0f)]
    private float startScale = 0.72f;

    [SerializeField, Min(0f)]
    private float overshootScale = 1.04f;

    [SerializeField, Min(0f)]
    private float endScale = 1f;

    [Header("Glow")]
    [SerializeField]
    private CanvasGroup glowGroup;

    [SerializeField]
    private RectTransform glowTransform;

    [SerializeField, Min(0f)]
    private float glowStartScale = 0.95f;

    [SerializeField, Min(0f)]
    private float glowEndScale = 1.22f;

    [SerializeField, Range(0f, 1f)]
    private float glowMaxAlpha = 0.45f;

    [Header("Fade Out Animation")]
    [SerializeField, Min(0f)]
    private float logoFadeOutScale = 0.96f;

    [SerializeField, Min(0f)]
    private float glowFadeOutScale = 1.35f;

    [Header("Optional")]
    [SerializeField]
    private CanvasGroup tapToSkipGroup;

    private Coroutine introRoutine;
    private Coroutine loadingRoutine;

    private bool isLoading;

    private void Awake()
    {
        RefreshReferences();
        ConfigureCanvasGroups();
        ResetIntroVisuals();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        PlayIntroSound();

        introRoutine = StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (isLoading)
            return;

        if (WasSkipInputPressed())
            SkipIntro();
    }

    private void OnDisable()
    {
        StopActiveRoutines();
    }

    private bool WasSkipInputPressed()
    {
        bool mousePressed =
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        bool touchPressed =
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

        bool keyboardPressed =
            Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame;

        return mousePressed ||
               touchPressed ||
               keyboardPressed;
    }

    private void PlayIntroSound()
    {
        if (introAudioSource == null ||
            introSound == null)
        {
            return;
        }

        introAudioSource.clip = introSound;
        introAudioSource.loop = false;
        introAudioSource.ignoreListenerPause = true;
        introAudioSource.volume = Mathf.Clamp01(
            SoundManager.SFXVolume
        );

        introAudioSource.Play();
    }

    private IEnumerator IntroRoutine()
    {
        ResetIntroVisuals();

        if (startDelay > 0f)
            yield return new WaitForSecondsRealtime(startDelay);

        if (fadeInDuration <= 0f)
        {
            ApplyFullyVisibleState();
        }
        else
        {
            yield return FadeInRoutine();
        }

        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        introRoutine = null;

        BeginLoadingSequence();
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeInDuration
            );

            float smoothProgress = Smooth01(progress);
            float cubicProgress = EaseOutCubic(progress);

            SetAlpha(
                logoGroup,
                smoothProgress
            );

            SetAlpha(
                glowGroup,
                Mathf.Lerp(
                    0f,
                    glowMaxAlpha,
                    smoothProgress
                )
            );

            if (tapToSkipGroup != null)
            {
                float skipProgress = Mathf.Clamp01(
                    (progress - 0.5f) / 0.5f
                );

                SetAlpha(
                    tapToSkipGroup,
                    Smooth01(skipProgress)
                );
            }

            UpdateLogoScale(progress);
            UpdateGlowScale(cubicProgress);

            yield return null;
        }

        ApplyFullyVisibleState();
    }

    private void UpdateLogoScale(float progress)
    {
        if (logoTransform == null)
            return;

        const float overshootPoint = 0.75f;

        float scale;

        if (progress < overshootPoint)
        {
            float overshootProgress =
                progress / overshootPoint;

            scale = Mathf.Lerp(
                startScale,
                overshootScale,
                EaseOutCubic(overshootProgress)
            );
        }
        else
        {
            float settleProgress =
                (progress - overshootPoint) /
                (1f - overshootPoint);

            scale = Mathf.Lerp(
                overshootScale,
                endScale,
                EaseInOut(settleProgress)
            );
        }

        logoTransform.localScale =
            Vector3.one * scale;
    }

    private void UpdateGlowScale(float progress)
    {
        if (glowTransform == null)
            return;

        float scale = Mathf.Lerp(
            glowStartScale,
            glowEndScale,
            progress
        );

        glowTransform.localScale =
            Vector3.one * scale;
    }

    private void SkipIntro()
    {
        if (isLoading)
            return;

        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        BeginLoadingSequence();
    }

    private void BeginLoadingSequence()
    {
        if (isLoading)
            return;

        isLoading = true;

        loadingRoutine =
            StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        float logoStartAlpha =
            GetAlpha(logoGroup, 1f);

        float glowStartAlpha =
            GetAlpha(glowGroup, glowMaxAlpha);

        float skipStartAlpha =
            GetAlpha(tapToSkipGroup, 1f);

        float soundStartVolume =
            introAudioSource != null
                ? introAudioSource.volume
                : 0f;

        Vector3 logoStartScaleValue =
            logoTransform != null
                ? logoTransform.localScale
                : Vector3.one;

        Vector3 glowStartScaleValue =
            glowTransform != null
                ? glowTransform.localScale
                : Vector3.one;

        if (fadeOutDuration <= 0f)
        {
            ApplyFullyHiddenState();

            if (fadeOutSoundWhenSkipping &&
                introAudioSource != null)
            {
                introAudioSource.volume = 0f;
                introAudioSource.Stop();
            }

            LoadNextScene();
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / fadeOutDuration
            );

            float easedProgress =
                EaseInOut(progress);

            SetAlpha(
                logoGroup,
                Mathf.Lerp(
                    logoStartAlpha,
                    0f,
                    easedProgress
                )
            );

            SetAlpha(
                glowGroup,
                Mathf.Lerp(
                    glowStartAlpha,
                    0f,
                    easedProgress
                )
            );

            SetAlpha(
                tapToSkipGroup,
                Mathf.Lerp(
                    skipStartAlpha,
                    0f,
                    easedProgress
                )
            );

            if (fadeOutSoundWhenSkipping &&
                introAudioSource != null)
            {
                introAudioSource.volume = Mathf.Lerp(
                    soundStartVolume,
                    0f,
                    easedProgress
                );
            }

            if (logoTransform != null)
            {
                logoTransform.localScale =
                    Vector3.LerpUnclamped(
                        logoStartScaleValue,
                        Vector3.one * logoFadeOutScale,
                        easedProgress
                    );
            }

            if (glowTransform != null)
            {
                glowTransform.localScale =
                    Vector3.LerpUnclamped(
                        glowStartScaleValue,
                        Vector3.one * glowFadeOutScale,
                        easedProgress
                    );
            }

            yield return null;
        }

        ApplyFullyHiddenState();

        if (fadeOutSoundWhenSkipping &&
            introAudioSource != null)
        {
            introAudioSource.volume = 0f;
            introAudioSource.Stop();
        }

        loadingRoutine = null;

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "IntroController next scene name is empty.",
                this
            );

            isLoading = false;
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void ResetIntroVisuals()
    {
        SetAlpha(logoGroup, 0f);
        SetAlpha(glowGroup, 0f);
        SetAlpha(tapToSkipGroup, 0f);

        if (logoTransform != null)
        {
            logoTransform.localScale =
                Vector3.one * startScale;
        }

        if (glowTransform != null)
        {
            glowTransform.localScale =
                Vector3.one * glowStartScale;
        }
    }

    private void ApplyFullyVisibleState()
    {
        SetAlpha(logoGroup, 1f);
        SetAlpha(glowGroup, glowMaxAlpha);
        SetAlpha(tapToSkipGroup, 1f);

        if (logoTransform != null)
        {
            logoTransform.localScale =
                Vector3.one * endScale;
        }

        if (glowTransform != null)
        {
            glowTransform.localScale =
                Vector3.one * glowEndScale;
        }
    }

    private void ApplyFullyHiddenState()
    {
        SetAlpha(logoGroup, 0f);
        SetAlpha(glowGroup, 0f);
        SetAlpha(tapToSkipGroup, 0f);

        if (logoTransform != null)
        {
            logoTransform.localScale =
                Vector3.one * logoFadeOutScale;
        }

        if (glowTransform != null)
        {
            glowTransform.localScale =
                Vector3.one * glowFadeOutScale;
        }
    }

    private void RefreshReferences()
    {
        if (introAudioSource == null)
            introAudioSource = GetComponent<AudioSource>();
    }

    private void ConfigureCanvasGroups()
    {
        ConfigureCanvasGroup(logoGroup);
        ConfigureCanvasGroup(glowGroup);
        ConfigureCanvasGroup(tapToSkipGroup);
    }

    private static void ConfigureCanvasGroup(
        CanvasGroup group)
    {
        if (group == null)
            return;

        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void StopActiveRoutines()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
            introRoutine = null;
        }

        if (loadingRoutine != null)
        {
            StopCoroutine(loadingRoutine);
            loadingRoutine = null;
        }
    }

    private static void SetAlpha(
        CanvasGroup group,
        float value)
    {
        if (group != null)
            group.alpha = Mathf.Clamp01(value);
    }

    private static float GetAlpha(
        CanvasGroup group,
        float fallback)
    {
        return group != null
            ? group.alpha
            : fallback;
    }

    private static float Smooth01(float value)
    {
        value = Mathf.Clamp01(value);

        return value *
               value *
               (3f - 2f * value);
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);

        float inverse = 1f - value;

        return 1f -
               inverse *
               inverse *
               inverse;
    }

    private static float EaseInOut(float value)
    {
        value = Mathf.Clamp01(value);

        if (value < 0.5f)
            return 4f * value * value * value;

        float inverse = -2f * value + 2f;

        return 1f -
               inverse *
               inverse *
               inverse / 2f;
    }

    private void OnValidate()
    {
        startDelay = Mathf.Max(0f, startDelay);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);

        startScale = Mathf.Max(0f, startScale);
        overshootScale = Mathf.Max(0f, overshootScale);
        endScale = Mathf.Max(0f, endScale);

        glowStartScale = Mathf.Max(0f, glowStartScale);
        glowEndScale = Mathf.Max(0f, glowEndScale);
        glowMaxAlpha = Mathf.Clamp01(glowMaxAlpha);

        logoFadeOutScale =
            Mathf.Max(0f, logoFadeOutScale);

        glowFadeOutScale =
            Mathf.Max(0f, glowFadeOutScale);
    }
}