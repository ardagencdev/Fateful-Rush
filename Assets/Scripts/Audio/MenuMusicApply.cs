using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicApply : MonoBehaviour
{
    [Header("Menu Musics")]
    public AudioClip[] menuMusics;

    [Range(0f, 1f)]
    public float menuMusicBaseVolume = 0.2f;

    [Header("Fade")]
    public float fadeInDuration = 0.6f;
    public float fadeOutDuration = 0.4f;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyMusicVolume();
    }

    private void Start()
    {
        PlayRandomMenuMusic();
    }

    public void ApplyMusicVolume()
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        bool menuMusicOn = PlayerPrefs.GetInt("MenuMusicOn", 1) == 1;

        audioSource.mute = !soundOn || !menuMusicOn;
        RefreshVolume();
    }

    private void PlayRandomMenuMusic()
    {
        if (menuMusics == null || menuMusics.Length == 0)
            return;

        int randomIndex = Random.Range(0, menuMusics.Length);

        audioSource.clip = menuMusics[randomIndex];
        audioSource.loop = true;

        audioSource.volume = 0f;
        audioSource.Play();

        FadeTo(GetTargetVolume(), fadeInDuration);
    }

    public void FadeOutMusic()
    {
        FadeTo(0f, fadeOutDuration, true);
    }

    public void RefreshVolume()
    {
        FadeTo(GetTargetVolume(), 0.2f);
    }

    private float GetTargetVolume()
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        bool menuMusicOn = PlayerPrefs.GetInt("MenuMusicOn", 1) == 1;

        if (!soundOn || !menuMusicOn)
            return 0f;

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        return musicVolume * menuMusicBaseVolume;
    }

    private void FadeTo(float target, float duration, bool stopAfter = false)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(target, duration, stopAfter));
    }

    private IEnumerator FadeRoutine(float target, float duration, bool stopAfter)
    {
        float start = audioSource.volume;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / duration);
            t = t * t * (3f - 2f * t);

            audioSource.volume = Mathf.Lerp(start, target, t);

            yield return null;
        }

        audioSource.volume = target;

        if (stopAfter)
            audioSource.Stop();

        fadeRoutine = null;
    }
}