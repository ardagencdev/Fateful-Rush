using System.Collections;
using System.Collections.Generic;
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
    private Coroutine playlistRoutine;

    private readonly List<int> shuffledPlaylist = new List<int>();
    private int playlistPosition;
    private int lastPlayedIndex = -1;
    private bool isStoppingMusic;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        ApplyMusicVolume();
    }

    private void Start()
    {
        StartMenuPlaylist();
    }

    public void ApplyMusicVolume()
    {
        bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;
        bool menuMusicOn = PlayerPrefs.GetInt("MenuMusicOn", 1) == 1;

        audioSource.mute = !soundOn || !menuMusicOn;
        RefreshVolume();
    }

    private void StartMenuPlaylist()
    {
        if (playlistRoutine != null)
            StopCoroutine(playlistRoutine);

        playlistRoutine = StartCoroutine(MenuPlaylistRoutine());
    }

    private IEnumerator MenuPlaylistRoutine()
    {
        while (true)
        {
            AudioClip nextClip = GetNextShuffledMusic();

            if (nextClip == null)
                yield break;

            audioSource.clip = nextClip;
            audioSource.volume = 0f;
            audioSource.Play();

            FadeTo(GetTargetVolume(), fadeInDuration);

            float waitTime = Mathf.Max(0f, nextClip.length - fadeOutDuration);
            yield return new WaitForSecondsRealtime(waitTime);

            FadeTo(0f, fadeOutDuration, true);
            yield return new WaitForSecondsRealtime(fadeOutDuration);
        }
    }

    private AudioClip GetNextShuffledMusic()
    {
        if (menuMusics == null || menuMusics.Length == 0)
            return null;

        if (menuMusics.Length == 1)
        {
            lastPlayedIndex = 0;
            return menuMusics[0];
        }

        if (shuffledPlaylist.Count == 0 || playlistPosition >= shuffledPlaylist.Count)
            BuildNewShufflePlaylist();

        int index = shuffledPlaylist[playlistPosition];
        playlistPosition++;

        lastPlayedIndex = index;
        return menuMusics[index];
    }

    private void BuildNewShufflePlaylist()
    {
        shuffledPlaylist.Clear();

        for (int i = 0; i < menuMusics.Length; i++)
            shuffledPlaylist.Add(i);

        for (int i = 0; i < shuffledPlaylist.Count; i++)
        {
            int randomIndex = Random.Range(i, shuffledPlaylist.Count);
            (shuffledPlaylist[i], shuffledPlaylist[randomIndex]) =
                (shuffledPlaylist[randomIndex], shuffledPlaylist[i]);
        }

        if (shuffledPlaylist.Count > 1 && shuffledPlaylist[0] == lastPlayedIndex)
        {
            int swapIndex = Random.Range(1, shuffledPlaylist.Count);
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