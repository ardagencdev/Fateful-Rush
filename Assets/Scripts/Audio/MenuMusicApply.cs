using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MenuMusicApply : MonoBehaviour
{
    [Header("Menu Musics")]
    [SerializeField]
    private AudioClip[] menuMusics;

    [SerializeField, Range(0f, 1f)]
    private float menuMusicBaseVolume = 0.2f;

    [Header("Fade")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 0.6f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.4f;

    public float FadeOutDuration => fadeOutDuration;

    private AudioSource audioSource;
    private Coroutine fadeRoutine;
    private Coroutine playlistRoutine;

    private readonly List<int> shuffledPlaylist = new();

    private int playlistPosition;
    private int lastPlayedIndex = -1;
    private bool isStoppingMusic;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.mute = false;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        StartMenuPlaylist();
    }

    public void ApplyMusicVolume()
    {
        audioSource.mute = false;
        RefreshVolume();
    }

    private void StartMenuPlaylist()
    {
        if (playlistRoutine != null)
            StopCoroutine(playlistRoutine);

        isStoppingMusic = false;
        playlistRoutine = StartCoroutine(MenuPlaylistRoutine());
    }

    private IEnumerator MenuPlaylistRoutine()
    {
        while (true)
        {
            AudioClip nextClip = GetNextShuffledMusic();

            if (nextClip == null)
            {
                playlistRoutine = null;
                yield break;
            }

            audioSource.clip = nextClip;
            audioSource.volume = 0f;
            audioSource.Play();

            FadeTo(GetTargetVolume(), fadeInDuration);

            float waitTime = Mathf.Max(
                0f,
                nextClip.length - fadeOutDuration
            );

            yield return new WaitForSecondsRealtime(waitTime);

            FadeTo(0f, fadeOutDuration, true);

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, fadeOutDuration)
            );
        }
    }

    private AudioClip GetNextShuffledMusic()
    {
        if (menuMusics == null || menuMusics.Length == 0)
            return null;

        if (shuffledPlaylist.Count == 0 ||
            playlistPosition >= shuffledPlaylist.Count)
        {
            BuildNewShufflePlaylist();
        }

        if (shuffledPlaylist.Count == 0)
            return null;

        int index = shuffledPlaylist[playlistPosition];
        playlistPosition++;

        lastPlayedIndex = index;
        return menuMusics[index];
    }

    private void BuildNewShufflePlaylist()
    {
        shuffledPlaylist.Clear();

        if (menuMusics == null)
            return;

        for (int i = 0; i < menuMusics.Length; i++)
        {
            if (menuMusics[i] != null)
                shuffledPlaylist.Add(i);
        }

        for (int i = 0; i < shuffledPlaylist.Count; i++)
        {
            int randomIndex =
                Random.Range(i, shuffledPlaylist.Count);

            (shuffledPlaylist[i], shuffledPlaylist[randomIndex]) =
                (shuffledPlaylist[randomIndex], shuffledPlaylist[i]);
        }

        if (shuffledPlaylist.Count > 1 &&
            shuffledPlaylist[0] == lastPlayedIndex)
        {
            int swapIndex =
                Random.Range(1, shuffledPlaylist.Count);

            (shuffledPlaylist[0], shuffledPlaylist[swapIndex]) =
                (shuffledPlaylist[swapIndex], shuffledPlaylist[0]);
        }

        playlistPosition = 0;
    }

    public void FadeOutMusic()
    {
        isStoppingMusic = true;

        if (playlistRoutine != null)
        {
            StopCoroutine(playlistRoutine);
            playlistRoutine = null;
        }

        FadeTo(0f, fadeOutDuration, true);
    }

    public void RefreshVolume()
    {
        if (isStoppingMusic)
            return;

        FadeTo(GetTargetVolume(), 0.2f);
    }

    private float GetTargetVolume()
    {
        bool soundOn =
            PlayerPrefs.GetInt("SoundOn", 1) == 1;

        bool menuMusicOn =
            PlayerPrefs.GetInt("MenuMusicOn", 1) == 1;

        if (!soundOn || !menuMusicOn)
            return 0f;

        float musicVolume =
            PlayerPrefs.GetFloat("MusicVolume", 1f);

        return musicVolume * menuMusicBaseVolume;
    }

    private void FadeTo(
        float targetVolume,
        float duration,
        bool stopAfter = false)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        fadeRoutine = StartCoroutine(
            FadeRoutine(
                targetVolume,
                duration,
                stopAfter
            )
        );
    }

    private IEnumerator FadeRoutine(
        float targetVolume,
        float duration,
        bool stopAfter)
    {
        duration = Mathf.Max(0.01f, duration);

        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(timer / duration);

            progress =
                progress *
                progress *
                (3f - 2f * progress);

            audioSource.volume = Mathf.Lerp(
                startVolume,
                targetVolume,
                progress
            );

            yield return null;
        }

        audioSource.volume = targetVolume;

        if (stopAfter)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }

        fadeRoutine = null;
    }

    private void OnDisable()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (playlistRoutine != null)
            StopCoroutine(playlistRoutine);

        fadeRoutine = null;
        playlistRoutine = null;
    }

    private void OnValidate()
    {
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        menuMusicBaseVolume =
            Mathf.Clamp01(menuMusicBaseVolume);
    }
}