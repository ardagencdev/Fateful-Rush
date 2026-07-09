using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class IntroController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.35f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float holdDuration = 2.2f;
    [SerializeField] private float fadeOutDuration = 0.8f;

    [Header("Intro Sound")]
    [SerializeField] private AudioSource introAudioSource;
    [SerializeField] private AudioClip introSound;
    [SerializeField] private bool fadeOutSoundWhenSkipping = true;

    [Header("Logo")]
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private RectTransform logoTransform;
    [SerializeField] private float startScale = 0.72f;
    [SerializeField] private float overshootScale = 1.04f;
    [SerializeField] private float endScale = 1f;

    [Header("Glow")]
    [SerializeField] private CanvasGroup glowGroup;
    [SerializeField] private RectTransform glowTransform;
    [SerializeField] private float glowStartScale = 0.95f;
    [SerializeField] private float glowEndScale = 1.22f;
    [SerializeField] private float glowMaxAlpha = 0.45f;

    [Header("Optional")]
    [SerializeField] private CanvasGroup tapToSkipGroup;

    private bool isLoading;

    private void Awake()
    {
        if (introAudioSource == null)
            introAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
        PlayIntroSound();
        StartCoroutine(IntroRoutine());
    }

    private void Update()
    {
        if (isLoading) return;

        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool keyboardPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;

        if (mousePressed || touchPressed || keyboardPressed)
            SkipIntro();
    }

    private void PlayIntroSound()
    {
        if (introAudioSource == null || introSound == null)
            return;

        introAudioSource.clip = introSound;
        introAudioSource.loop = false;
        introAudioSource.ignoreListenerPause = true;
        introAudioSource.volume = SoundManager.SFXVolume;
        introAudioSource.Play();
    }

    private IEnumerator IntroRoutine()
    {
        SetAlpha(logoGroup, 0f);
        SetAlpha(glowGroup, 0f);
        SetAlpha(tapToSkipGroup, 0f);

        if (logoTransform != null)
            logoTransform.localScale = Vector3.one * startScale;

        if (glowTransform != null)
            glowTransform.localScale = Vector3.one * glowStartScale;

        yield return new WaitForSecondsRealtime(startDelay);

        float t = 0f;

        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeInDuration);
            float eased = EaseOutCubic(p);

            SetAlpha(logoGroup, Smooth01(p));
            SetAlpha(glowGroup, Mathf.Lerp(0f, glowMaxAlpha, Smooth01(p)));

            if (tapToSkipGroup != null)
                SetAlpha(tapToSkipGroup, Mathf.Clamp01((p - 0.5f) / 0.5f));

            if (logoTransform != null)
            {
                float scale;

                if (p < 0.75f)
                    scale = Mathf.Lerp(startScale, overshootScale, EaseOutCubic(p / 0.75f));
                else
                    scale = Mathf.Lerp(overshootScale, endScale, EaseInOut((p - 0.75f) / 0.25f));

                logoTransform.localScale = Vector3.one * scale;
            }

            if (glowTransform != null)
                glowTransform.localScale = Vector3.one * Mathf.Lerp(glowStartScale, glowEndScale, eased);

            yield return null;
        }

        SetAlpha(logoGroup, 1f);
        SetAlpha(glowGroup, glowMaxAlpha);
        SetAlpha(tapToSkipGroup, 1f);

        if (logoTransform != null)
            logoTransform.localScale = Vector3.one * endScale;

        yield return new WaitForSecondsRealtime(holdDuration);

        SkipIntro();
    }

    private void SkipIntro()
    {
        if (isLoading) return;

        isLoading = true;
        StartCoroutine(LoadNextSceneRoutine());
    }

    private IEnumerator LoadNextSceneRoutine()
    {
        float t = 0f;

        float logoStartAlpha = logoGroup != null ? logoGroup.alpha : 1f;
        float glowStartAlpha = glowGroup != null ? glowGroup.alpha : 1f;
        float skipStartAlpha = tapToSkipGroup != null ? tapToSkipGroup.alpha : 1f;

        float soundStartVolume = introAudioSource != null ? introAudioSource.volume : 0f;

        Vector3 logoStartScale = logoTransform != null ? logoTransform.localScale : Vector3.one;
        Vector3 glowStartScaleValue = glowTransform != null ? glowTransform.localScale : Vector3.one;

        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / fadeOutDuration);
            float eased = EaseInOut(p);

            SetAlpha(logoGroup, Mathf.Lerp(logoStartAlpha, 0f, eased));
            SetAlpha(glowGroup, Mathf.Lerp(glowStartAlpha, 0f, eased));
            SetAlpha(tapToSkipGroup, Mathf.Lerp(skipStartAlpha, 0f, eased));

            if (fadeOutSoundWhenSkipping && introAudioSource != null)
                introAudioSource.volume = Mathf.Lerp(soundStartVolume, 0f, eased);

            if (logoTransform != null)
                logoTransform.localScale = Vector3.Lerp(logoStartScale, Vector3.one * 0.96f, eased);

            if (glowTransform != null)
                glowTransform.localScale = Vector3.Lerp(glowStartScaleValue, Vector3.one * 1.35f, eased);

            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void SetAlpha(CanvasGroup group, float value)
    {
        if (group != null)
            group.alpha = value;
    }

    private float Smooth01(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }

    private float EaseInOut(float x)
    {
        x = Mathf.Clamp01(x);
        return x < 0.5f
            ? 4f * x * x * x
            : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;
    }
}