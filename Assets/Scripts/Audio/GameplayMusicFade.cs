using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameplayMusicFade : MonoBehaviour
{
    [Header("Music Clips")]
    [Tooltip("Bütün levellardaki tutorial ekranlarında kullanılacak ortak müzik.")]
    [SerializeField]
    private AudioClip tutorialMusic;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)]
    private float gameplayMusicBaseVolume = 0.2f;

    [Header("Fade Durations")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.6f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.4f;

    [Header("Music Transition")]
    [SerializeField, Min(0f)]
    private float clipTransitionDuration = 0.8f;

    public float FadeOutDuration => fadeOutDuration;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        source = GetComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
    }

    public void PlayTutorialMusic()
    {
        if (tutorialMusic == null)
        {
            Debug.LogWarning(
                "GameplayMusicFade: Tutorial Music atanmamış."
            );

            StopImmediately();
            return;
        }

        PlayClipAndFadeIn(tutorialMusic);
    }

    public void PlayClipAndFadeIn(AudioClip clip)
    {
        if (clip == null)
        {
            StopImmediately();
            return;
        }

        StopCurrentFade();

        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
        source.Play();

        FadeTo(
            GetTargetVolume(),
            fadeInDuration
        );
    }

    public void TransitionToClip(AudioClip newClip)
    {
        if (newClip == null)
        {
            FadeOut();
            return;
        }

        if (source.isPlaying &&
            source.clip == newClip)
        {
            FadeTo(
                GetTargetVolume(),
                fadeInDuration
            );

            return;
        }

        StopCurrentFade();

        fadeRoutine = StartCoroutine(
            TransitionClipRoutine(newClip)
        );
    }

    public void PlayAndFadeIn()
    {
        if (source.clip == null)
            return;

        StopCurrentFade();

        source.Stop();
        source.loop = true;
        source.volume = 0f;
        source.Play();

        FadeTo(
            GetTargetVolume(),
            fadeInDuration
        );
    }

    public void FadeIn()
    {
        if (source.clip == null)
            return;

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
        source.clip = null;
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

    private IEnumerator TransitionClipRoutine(
        AudioClip newClip
    )
    {
        float halfDuration =
            Mathf.Max(
                0.01f,
                clipTransitionDuration * 0.5f
            );

        if (source.isPlaying)
        {
            yield return FadeVolumeRoutine(
                0f,
                halfDuration
            );
        }

        source.Stop();
        source.clip = newClip;
        source.loop = true;
        source.volume = 0f;
        source.Play();

        yield return FadeVolumeRoutine(
            GetTargetVolume(),
            halfDuration
        );

        fadeRoutine = null;
    }

    private float GetTargetVolume()
    {
        bool soundOn =
            PlayerPrefs.GetInt(
                "SoundOn",
                1
            ) == 1;

        bool gameplayMusicOn =
            PlayerPrefs.GetInt(
                "GameplayMusicOn",
                1
            ) == 1;

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
        bool stopAfterFade = false
    )
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
        bool stopAfterFade
    )
    {
        yield return FadeVolumeRoutine(
            targetVolume,
            duration
        );

        if (stopAfterFade)
        {
            source.Stop();
            source.volume = 0f;
        }

        fadeRoutine = null;
    }

    private IEnumerator FadeVolumeRoutine(
        float targetVolume,
        float duration
    )
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

        clipTransitionDuration =
            Mathf.Max(
                0f,
                clipTransitionDuration
            );

        gameplayMusicBaseVolume =
            Mathf.Clamp01(
                gameplayMusicBaseVolume
            );
    }
}