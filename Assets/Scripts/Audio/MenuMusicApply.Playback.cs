using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Same Unity component. Inspector data and lifecycle entry points remain in MenuMusicApply.cs.
public partial class MenuMusicApply
{
    private void PrepareAudioSources()
    {
        AudioSource[] sources =
            GetComponents<AudioSource>();

        sourceA = sources[0];

        if (sources.Length >= 2)
        {
            sourceB = sources[1];
        }
        else
        {
            sourceB =
                gameObject.AddComponent<AudioSource>();

            CopyAudioSourceSettings(
                sourceA,
                sourceB
            );
        }

        ConfigureAudioSource(sourceA);
        ConfigureAudioSource(sourceB);

        activeSource = sourceA;
        standbySource = sourceB;

        masterVolume = 0f;
        activeGain = 1f;
        standbyGain = 0f;

        ApplySourceVolumes();
    }

    private static void ConfigureAudioSource(
        AudioSource source
    )
    {
        source.playOnAwake = false;
        source.loop = false;
        source.mute = false;
        source.spatialBlend = 0f;
        source.volume = 0f;

        GameAudioMixerController.Route(
            source,
            GameAudioMixerController.AudioBus.Music
        );
    }

    private static void CopyAudioSourceSettings(
        AudioSource source,
        AudioSource target
    )
    {
        target.outputAudioMixerGroup =
            source.outputAudioMixerGroup;

        target.priority = source.priority;
        target.pitch = source.pitch;
        target.panStereo = source.panStereo;
        target.spatialBlend = source.spatialBlend;

        target.bypassEffects =
            source.bypassEffects;

        target.bypassListenerEffects =
            source.bypassListenerEffects;

        target.bypassReverbZones =
            source.bypassReverbZones;
    }

    private void StartMenuPlaylist()
    {
        StopActiveRoutines();
        ClearInterruptionSnapshot();

        sourceA.Stop();
        sourceB.Stop();

        sourceA.clip = null;
        sourceB.clip = null;

        activeSource = sourceA;
        standbySource = sourceB;

        isStoppingMusic = false;

        AudioClip firstClip =
            GetFirstValidTrack(
                out int firstIndex
            );

        if (firstClip == null)
        {
            Debug.LogWarning(
                "MenuMusicApply üzerinde geçerli bir menü müziği bulunamadı.",
                this
            );

            return;
        }

        lastPlayedIndex = firstIndex;

        activeSource.clip = firstClip;
        activeSource.volume = 0f;

        activeGain = 1f;
        standbyGain = 0f;
        masterVolume = 0f;

        ApplySourceVolumes();

        activeSource.Play();

        StartMasterVolumeFade(
            GetTargetVolume(),
            fadeInDuration
        );

        playlistRoutine =
            StartCoroutine(
                MenuPlaylistRoutine()
            );
    }

    private IEnumerator MenuPlaylistRoutine()
    {
        while (!isStoppingMusic &&
               activeSource != null &&
               activeSource.clip != null)
        {
            // Do not treat ad/background suspension as the end of a song.
            while (!isStoppingMusic)
            {
                if (IsPlaybackInterrupted())
                {
                    CaptureInterruptionSnapshot();
                    yield return null;
                    continue;
                }

                RestoreInterruptionSnapshotIfReady();

                if (activeSource != null &&
                    activeSource.isPlaying)
                {
                    yield return null;
                    continue;
                }

                break;
            }

            if (isStoppingMusic)
                break;

            activeSource.Stop();
            activeSource.clip = null;

            activeGain = 0f;
            ApplySourceVolumes();

            if (interTrackDelay > 0f)
            {
                yield return WaitForPlaybackAwareDelay(
                    interTrackDelay
                );
            }

            if (isStoppingMusic)
                break;

            AudioClip nextClip =
                GetNextShuffledMusic();

            if (nextClip == null)
                break;

            standbySource.clip = nextClip;
            standbySource.volume = 0f;
            standbyGain = 0f;

            ApplySourceVolumes();
            standbySource.Play();

            yield return FadeInStandbyRoutine(
                GetSafeNextTrackFadeInDuration(
                    nextClip
                )
            );

            if (isStoppingMusic)
                break;

            SwapSources();

            activeGain = 1f;
            standbyGain = 0f;

            ApplySourceVolumes();
        }

        playlistRoutine = null;
    }

    private IEnumerator WaitForPlaybackAwareDelay(
        float duration
    )
    {
        float elapsed = 0f;
        duration = Mathf.Max(0f, duration);

        while (!isStoppingMusic &&
               elapsed < duration)
        {
            if (IsPlaybackInterrupted())
            {
                CaptureInterruptionSnapshot();
                yield return null;
                continue;
            }

            RestoreInterruptionSnapshotIfReady();
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeInStandbyRoutine(
        float duration
    )
    {
        duration = Mathf.Max(0.01f, duration);

        float timer = 0f;

        while (timer < duration)
        {
            if (IsPlaybackInterrupted())
            {
                CaptureInterruptionSnapshot();
                yield return null;
                continue;
            }

            RestoreInterruptionSnapshotIfReady();
            timer += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    timer / duration
                );

            /*
             * Eski equal-power crossfade'deki incoming eğriyi koruyoruz.
             * Böylece yeni parça 0'dan sertçe değil, aynı smooth karakterle girer.
             */
            standbyGain = Mathf.Sin(
                progress *
                Mathf.PI *
                0.5f
            );

            ApplySourceVolumes();

            yield return null;
        }

        standbyGain = 1f;
        ApplySourceVolumes();
    }

    private void SwapSources()
    {
        AudioSource previousActive =
            activeSource;

        activeSource = standbySource;
        standbySource = previousActive;
    }

    private AudioClip GetFirstValidTrack(
        out int selectedIndex
    )
    {
        selectedIndex = -1;

        if (menuMusics == null ||
            menuMusics.Length == 0)
        {
            return null;
        }

        int safeFirstIndex =
            Mathf.Clamp(
                firstTrackIndex,
                0,
                menuMusics.Length - 1
            );

        if (menuMusics[safeFirstIndex] != null)
        {
            selectedIndex = safeFirstIndex;
            return menuMusics[safeFirstIndex];
        }

        for (int i = 0;
             i < menuMusics.Length;
             i++)
        {
            if (menuMusics[i] == null)
                continue;

            selectedIndex = i;
            return menuMusics[i];
        }

        return null;
    }

    private AudioClip GetNextShuffledMusic()
    {
        if (menuMusics == null ||
            menuMusics.Length == 0)
        {
            return null;
        }

        if (shuffledPlaylist.Count == 0 ||
            playlistPosition >=
            shuffledPlaylist.Count)
        {
            BuildNewShufflePlaylist();
        }

        if (shuffledPlaylist.Count == 0)
            return null;

        int index =
            shuffledPlaylist[
                playlistPosition
            ];

        playlistPosition++;
        lastPlayedIndex = index;

        return menuMusics[index];
    }

    private void BuildNewShufflePlaylist()
    {
        shuffledPlaylist.Clear();

        if (menuMusics == null)
            return;

        for (int i = 0;
             i < menuMusics.Length;
             i++)
        {
            if (menuMusics[i] != null)
                shuffledPlaylist.Add(i);
        }

        for (int i = 0;
             i < shuffledPlaylist.Count;
             i++)
        {
            int randomIndex =
                Random.Range(
                    i,
                    shuffledPlaylist.Count
                );

            int temporary = shuffledPlaylist[i];
            shuffledPlaylist[i] = shuffledPlaylist[randomIndex];
            shuffledPlaylist[randomIndex] = temporary;
        }

        if (shuffledPlaylist.Count > 1 &&
            shuffledPlaylist[0] ==
            lastPlayedIndex)
        {
            int swapIndex =
                Random.Range(
                    1,
                    shuffledPlaylist.Count
                );

            int temporary = shuffledPlaylist[0];
            shuffledPlaylist[0] = shuffledPlaylist[swapIndex];
            shuffledPlaylist[swapIndex] = temporary;
        }

        playlistPosition = 0;
    }

    private float GetSafeNextTrackFadeInDuration(
        AudioClip clip
    )
    {
        if (clip == null)
            return 0.01f;

        return Mathf.Clamp(
            nextTrackFadeInDuration,
            0.01f,
            Mathf.Max(
                0.01f,
                clip.length * 0.5f
            )
        );
    }

    public void ApplyMusicVolume()
    {
        sourceA.mute = false;
        sourceB.mute = false;

        RefreshVolume();
    }

    public void RefreshVolume()
    {
        if (isStoppingMusic)
            return;

        StartMasterVolumeFade(
            GetTargetVolume(),
            0.2f
        );
    }

    public void FadeOutMusic()
    {
        if (isStoppingMusic)
            return;

        isStoppingMusic = true;

        if (playlistRoutine != null)
        {
            StopCoroutine(playlistRoutine);
            playlistRoutine = null;
        }

        StartMasterVolumeFade(
            0f,
            fadeOutDuration,
            true
        );
    }

    private void StartMasterVolumeFade(
        float targetVolume,
        float duration,
        bool stopAfter = false
    )
    {
        if (volumeRoutine != null)
        {
            StopCoroutine(volumeRoutine);
            volumeRoutine = null;
        }

        volumeRoutine =
            StartCoroutine(
                MasterVolumeFadeRoutine(
                    targetVolume,
                    duration,
                    stopAfter
                )
            );
    }

    private IEnumerator MasterVolumeFadeRoutine(
        float targetVolume,
        float duration,
        bool stopAfter
    )
    {
        duration = Mathf.Max(
            0.01f,
            duration
        );

        float startVolume =
            masterVolume;

        float timer = 0f;

        while (timer < duration)
        {
            if (IsPlaybackInterrupted())
            {
                CaptureInterruptionSnapshot();
                yield return null;
                continue;
            }

            RestoreInterruptionSnapshotIfReady();
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

            masterVolume =
                Mathf.Lerp(
                    startVolume,
                    targetVolume,
                    progress
                );

            ApplySourceVolumes();

            yield return null;
        }

        masterVolume = targetVolume;
        ApplySourceVolumes();

        if (stopAfter)
        {
            sourceA.Stop();
            sourceB.Stop();

            sourceA.clip = null;
            sourceB.clip = null;
        }

        volumeRoutine = null;
    }

    private void ApplySourceVolumes()
    {
        if (activeSource != null)
        {
            activeSource.volume =
                masterVolume *
                activeGain;
        }

        if (standbySource != null)
        {
            standbySource.volume =
                masterVolume *
                standbyGain;
        }
    }

    private float GetTargetVolume()
    {
        bool soundOn =
            PlayerPrefs.GetInt(
                "SoundOn",
                1
            ) == 1;

        bool menuMusicOn =
            PlayerPrefs.GetInt(
                "MenuMusicOn",
                1
            ) == 1;

        if (!soundOn || !menuMusicOn)
            return 0f;

        if (GameAudioMixerController.IsReady)
            return menuMusicBaseVolume;

        float musicVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(
                "MusicVolume",
                1f
            )
        );

        return musicVolume *
               menuMusicBaseVolume;
    }
}
