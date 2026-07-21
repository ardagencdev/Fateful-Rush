using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicFade : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float gameplayMusicBaseVolume = 0.2f;

    [Header("Fade Durations")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.6f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.4f;

    public float FadeOutDuration => fadeOutDuration;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        source = GetComponent<AudioSource>();

        source.playOnAwake = false;
        source.volume = 0f;
    }

    public void PlayAndFadeIn()
    {
        StopCurrentFade();

        source.Stop();
        source.volume = 0f;
        source.Play();

        FadeTo(
            GetTargetVolume(),
            fadeInDuration
        );
    }

    public void FadeIn()
    {
        if (!source.isPlaying)
            source.Play();

        FadeTo(
            GetTargetVolume(),
            fadeInDuration
        );
    }

    public void FadeOut()
    {
        if (!source.isPlaying)
            return;

        FadeTo(
            0f,
            fadeOutDuration,
            true
        );
    }

    public void StopImmediately()
    {
        StopCurrentFade();

        source.Stop();
        source.volume = 0f;
    }

    public void RefreshVolume()
    {
        if (!source.isPlaying)
            return;

        FadeTo(
            GetTargetVolume(),
            0.2f
        );
    }

    private float GetTargetVolume()
    {
        bool soundOn =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        bool gameplayMusicOn =
            PlayerPrefs.GetInt("GameplayMusicOn", 1) == 1;

        if (!soundOn || !gameplayMusicOn)
            return 0f;

        float userMusicVolume =
            PlayerPrefs.GetFloat(
                "MusicVolume",
                1f
            );

        return userMusicVolume *
               gameplayMusicBaseVolume;
    }

    private void FadeTo(
        float targetVolume,
        float duration,
        bool stopAfterFade = false)
    {
        StopCurrentFade();

        fadeRoutine = StartCoroutine(
            FadeRoutine(
                targetVolume,
                duration,
                stopAfterFade
            )
        );
    }

    private IEnumerator FadeRoutine(
        float targetVolume,
        float duration,
        bool stopAfterFade)
    {
        duration = Mathf.Max(
            0.01f,
            duration
        );

        float startVolume = source.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            progress =
                progress *
                progress *
                (3f - 2f * progress);

            source.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                progress
            );

            yield return null;
        }

        source.volume = targetVolume;

        if (stopAfterFade)
        {
            source.Stop();
            source.volume = 0f;
        }

        fadeRoutine = null;
    }

    private void StopCurrentFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private void OnDisable()
    {
        StopCurrentFade();
    }

    private void OnValidate()
    {
        fadeInDuration =
            Mathf.Max(
                0f,
                fadeInDuration
            );

        fadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );

        gameplayMusicBaseVolume =
            Mathf.Clamp01(
                gameplayMusicBaseVolume
            );
    }
}