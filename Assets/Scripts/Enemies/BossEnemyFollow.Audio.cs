using System.Collections;
using UnityEngine;

// Same Unity component. Inspector data and lifecycle entry points remain in BossEnemyFollow.cs.
public partial class BossEnemyFollow
{
    private void CreateBossSfxSource()
    {
        bossSfxSource = gameObject.AddComponent<AudioSource>();
        bossSfxSource.playOnAwake = false;
        bossSfxSource.loop = false;
        bossSfxSource.volume = SoundManager.SFXVolume;

        SoundManager manager = SoundManager.Instance;

        if (manager != null)
        {
            manager.ConfigureWorldAudioSource(bossSfxSource);

            AudioSource template = manager.sfxSource;

            if (template != null)
            {
                bossSfxSource.outputAudioMixerGroup =
                    template.outputAudioMixerGroup;

                bossSfxSource.priority =
                    template.priority;

                bossSfxSource.bypassEffects =
                    template.bypassEffects;

                bossSfxSource.bypassListenerEffects =
                    template.bypassListenerEffects;

                bossSfxSource.bypassReverbZones =
                    template.bypassReverbZones;

                bossSfxSource.ignoreListenerVolume =
                    template.ignoreListenerVolume;
            }
        }
        else
        {
            SoundManager.ConfigureAsWorld3D(
                bossSfxSource
            );
        }

        GameAudioMixerController.Route(
            bossSfxSource,
            GameAudioMixerController.AudioBus.CriticalSFX
        );

        // Boss gameplay SFX'leri pause'dan muaf olmamali.
        bossSfxSource.ignoreListenerPause = false;
    }

    private void PlayBossSfx(
        AudioClip clip,
        float volumeMultiplier = 1f,
        float scaledDuration = 0f)
    {
        if (clip == null ||
            GameStateManager.IsGameplayEnded)
        {
            return;
        }

        if (bossSfxSource == null)
            CreateBossSfxSource();

        bossSfxVolumeMultiplier =
            Mathf.Max(0f, volumeMultiplier);

        bossSfxFollowsGameTime = scaledDuration > 0.01f;
        bossSfxBasePitch = bossSfxFollowsGameTime
            ? Mathf.Clamp(clip.length / scaledDuration, 0.25f, 3f)
            : 1f;

        bossSfxPausedByGame = false;

        bossSfxSource.Stop();
        bossSfxSource.clip = clip;
        bossSfxSource.pitch = GetBossSfxPitch();
        ApplyBossSfxVolume();
        bossSfxSource.Play();

        if (Time.timeScale <= 0f)
        {
            bossSfxSource.Pause();
            bossSfxPausedByGame = true;
        }
    }

    private void UpdateBossSfxState()
    {
        if (bossSfxSource == null)
            return;

        ApplyBossSfxVolume();
        bossSfxSource.pitch = GetBossSfxPitch();

        if (GameStateManager.IsGameplayEnded)
        {
            StopBossSfx();
            return;
        }

        bool shouldPause =
            Time.timeScale <= 0f;

        if (shouldPause)
        {
            if (!bossSfxPausedByGame &&
                bossSfxSource.isPlaying)
            {
                bossSfxSource.Pause();
                bossSfxPausedByGame = true;
            }

            return;
        }

        if (bossSfxPausedByGame)
        {
            bossSfxSource.UnPause();
            bossSfxPausedByGame = false;
        }
    }

    private float GetBossSfxPitch()
    {
        if (!bossSfxFollowsGameTime)
            return Mathf.Clamp(bossSfxBasePitch, 0.01f, 3f);

        float gameplayScale = 1f;

        if (SlowPowerUp.isSlowActive)
        {
            gameplayScale = Mathf.Clamp(
                SlowPowerUp.currentSlowMultiplier,
                0.01f,
                1f
            );
        }

        return Mathf.Clamp(
            bossSfxBasePitch * gameplayScale,
            0.01f,
            3f
        );
    }

    private void ApplyBossSfxVolume()
    {
        if (bossSfxSource == null)
            return;

        bossSfxSource.volume =
            SoundManager.SFXVolume *
            bossSfxVolumeMultiplier;
    }

    private void StopBossSfx()
    {
        if (bossSfxSource == null)
            return;

        bossSfxSource.Stop();
        bossSfxSource.clip = null;
        bossSfxPausedByGame = false;
        bossSfxBasePitch = 1f;
        bossSfxFollowsGameTime = false;
    }
}
