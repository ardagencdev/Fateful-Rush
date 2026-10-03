using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

// Same Unity component. Inspector data and lifecycle entry points remain in SoundManager.cs.
public partial class SoundManager
{
    public static void ConfigureAsWorld3D(AudioSource source)
    {
        if (source == null)
            return;

        if (Instance != null)
        {
            Instance.ConfigureWorldAudioSource(source);
            return;
        }

        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.spread = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 25f;
        source.maxDistance = 60f;

        GameAudioMixerController.Route(
            source,
            GameAudioMixerController.AudioBus.GameplaySFX
        );
    }

    public void ConfigureWorldAudioSource(AudioSource source)
    {
        ConfigureWorldAudioSource(
            source,
            GameAudioMixerController.AudioBus.GameplaySFX
        );
    }

    public void ConfigureWorldAudioSource(
        AudioSource source,
        GameAudioMixerController.AudioBus bus)
    {
        if (source == null)
            return;

        source.spatialBlend = 1f;
        source.dopplerLevel = 0f;
        source.spread = spatialSpread;
        source.rolloffMode = spatialRolloffMode;
        source.minDistance = spatialMinDistance;
        source.maxDistance = spatialMaxDistance;

        GameAudioMixerController.Route(source, bus);
    }

    public static float GetVariedPitch(float basePitch, float jitterAmount)
    {
        float jitter = Mathf.Max(0f, jitterAmount);

        if (jitter <= 0f)
            return basePitch;

        return Mathf.Clamp(
            basePitch + Random.Range(-jitter, jitter),
            -3f,
            3f
        );
    }

    public static float GetVariedVolumeMultiplier(
        float baseMultiplier,
        float jitterAmount)
    {
        float jitter = Mathf.Max(0f, jitterAmount);

        if (jitter <= 0f)
            return Mathf.Max(0f, baseMultiplier);

        return Mathf.Max(
            0f,
            baseMultiplier * (1f + Random.Range(-jitter, jitter))
        );
    }

    private void PlayVariedCenteredSound(
        AudioClip primaryClip,
        AudioClip[] extraClips,
        ref int lastVariantIndex,
        float pitchJitter,
        float volumeJitter)
    {
        AudioClip clip = SelectVariationClip(
            primaryClip,
            extraClips,
            ref lastVariantIndex
        );

        float pitch = enableSfxVariation
            ? GetVariedPitch(1f, pitchJitter)
            : 1f;

        float volume = enableSfxVariation
            ? GetVariedVolumeMultiplier(1f, volumeJitter)
            : 1f;

        PlayCenteredSound(clip, volume, pitch);
    }

    private void PlayVariedWorldSound(
        AudioClip primaryClip,
        AudioClip[] extraClips,
        ref int lastVariantIndex,
        Vector3 worldPosition,
        float pitchJitter,
        float volumeJitter)
    {
        AudioClip clip = SelectVariationClip(
            primaryClip,
            extraClips,
            ref lastVariantIndex
        );

        float pitch = enableSfxVariation
            ? GetVariedPitch(1f, pitchJitter)
            : 1f;

        float volume = enableSfxVariation
            ? GetVariedVolumeMultiplier(1f, volumeJitter)
            : 1f;

        PlayWorldSound(clip, worldPosition, volume, pitch);
    }

    private AudioClip SelectVariationClip(
        AudioClip primaryClip,
        AudioClip[] extraClips,
        ref int lastVariantIndex)
    {
        if (!enableSfxVariation ||
            extraClips == null ||
            extraClips.Length == 0)
        {
            lastVariantIndex = -1;
            return primaryClip;
        }

        int validExtraCount = 0;

        for (int i = 0; i < extraClips.Length; i++)
        {
            if (extraClips[i] != null)
                validExtraCount++;
        }

        int totalCount = (primaryClip != null ? 1 : 0) + validExtraCount;

        if (totalCount <= 0)
            return null;

        if (totalCount == 1)
        {
            lastVariantIndex = 0;

            if (primaryClip != null)
                return primaryClip;

            for (int i = 0; i < extraClips.Length; i++)
            {
                if (extraClips[i] != null)
                    return extraClips[i];
            }
        }

        int selectedIndex;
        int safety = 0;

        do
        {
            selectedIndex = Random.Range(0, totalCount);
            safety++;
        }
        while (selectedIndex == lastVariantIndex && safety < 8);

        lastVariantIndex = selectedIndex;

        if (primaryClip != null)
        {
            if (selectedIndex == 0)
                return primaryClip;

            selectedIndex--;
        }

        for (int i = 0; i < extraClips.Length; i++)
        {
            AudioClip candidate = extraClips[i];

            if (candidate == null)
                continue;

            if (selectedIndex == 0)
                return candidate;

            selectedIndex--;
        }

        return primaryClip;
    }

    private AudioClip GetCoinClipForSkin(string skinId)
    {
        string normalizedSkinId =
            string.IsNullOrWhiteSpace(skinId)
                ? string.Empty
                : skinId.Trim().ToLowerInvariant();

        if (normalizedSkinId == "dark" && darkCoinSound != null)
            return darkCoinSound;

        if (normalizedSkinId == "golden" && goldenCoinSound != null)
            return goldenCoinSound;

        return coinSound;
    }

    private void PlayCenteredSound(
        AudioClip clip,
        float volumeMultiplier = 1f,
        float pitch = 1f,
        GameAudioMixerController.AudioBus bus =
            GameAudioMixerController.AudioBus.GameplaySFX)
    {
        PlaySpatialSound(
            clip,
            GetCenteredWorldPosition(),
            volumeMultiplier,
            pitch,
            bus
        );
    }

    private void PlayCenteredUISound(
        AudioClip clip,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayCenteredSound(
            clip,
            volumeMultiplier,
            pitch,
            GameAudioMixerController.AudioBus.UISFX
        );
    }

    private void PlayCenteredCriticalSound(
        AudioClip clip,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayCenteredSound(
            clip,
            volumeMultiplier,
            pitch,
            GameAudioMixerController.AudioBus.CriticalSFX
        );
    }

    private void PlayWorldCriticalSound(
        AudioClip clip,
        Vector3 worldPosition,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayWorldSound(
            clip,
            worldPosition,
            volumeMultiplier,
            pitch,
            GameAudioMixerController.AudioBus.CriticalSFX
        );
    }

    private void PlayWorldSound(
        AudioClip clip,
        Vector3 worldPosition,
        float volumeMultiplier = 1f,
        float pitch = 1f,
        GameAudioMixerController.AudioBus bus =
            GameAudioMixerController.AudioBus.GameplaySFX)
    {
        PlaySpatialSound(
            clip,
            worldPosition,
            volumeMultiplier,
            pitch,
            bus
        );
    }

    private void PlayUISound(
        AudioClip clip,
        RectTransform sourceRect,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        Vector3 position = sourceRect != null
            ? GetUIWorldPosition(sourceRect)
            : GetCenteredWorldPosition();

        PlaySpatialSound(
            clip,
            position,
            volumeMultiplier,
            pitch,
            GameAudioMixerController.AudioBus.UISFX
        );
    }

    private void PlaySpatialSound(
        AudioClip clip,
        Vector3 position,
        float volumeMultiplier,
        float pitch,
        GameAudioMixerController.AudioBus bus)
    {
        if (clip == null)
            return;

        float sfxVolume = SFXVolume;

        if (sfxVolume <= 0f)
            return;

        AudioSource source = GetAvailableSpatialSource();

        if (source == null)
            return;

        source.transform.position = position;
        source.volume = sfxVolume;
        source.pitch = Mathf.Clamp(pitch, -3f, 3f);

        GameAudioMixerController.Route(source, bus);

        source.PlayOneShot(clip, Mathf.Max(0f, volumeMultiplier));
    }

    private void PrepareSpatialPool()
    {
        int targetCount = Mathf.Max(1, spatialPoolSize);

        while (spatialSources.Count < targetCount)
            spatialSources.Add(CreateSpatialSource(spatialSources.Count));
    }

    private AudioSource GetAvailableSpatialSource()
    {
        PrepareSpatialPool();

        int count = spatialSources.Count;

        for (int i = 0; i < count; i++)
        {
            int index = (spatialSourceCursor + i) % count;
            AudioSource candidate = spatialSources[index];

            if (candidate != null && !candidate.isPlaying)
            {
                spatialSourceCursor = (index + 1) % Mathf.Max(1, count);
                return candidate;
            }
        }

        if (spatialSources.Count < Mathf.Max(1, spatialPoolMaxSize))
        {
            AudioSource created = CreateSpatialSource(spatialSources.Count);
            spatialSources.Add(created);
            spatialSourceCursor = 0;
            return created;
        }

        int fallbackIndex = spatialSourceCursor % Mathf.Max(1, spatialSources.Count);
        spatialSourceCursor = (fallbackIndex + 1) % Mathf.Max(1, spatialSources.Count);

        AudioSource fallback = spatialSources[fallbackIndex];

        if (fallback != null)
            fallback.Stop();

        return fallback;
    }

    private AudioSource CreateSpatialSource(int index)
    {
        GameObject sourceObject = new GameObject($"SpatialSFX_{index:00}");
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.volume = SFXVolume;

        ConfigureWorldAudioSource(source);
        CopyTemplateSettings(source);

        return source;
    }

    private void CopyTemplateSettings(AudioSource target)
    {
        if (target == null || sfxSource == null)
            return;

        target.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
        target.priority = sfxSource.priority;
        target.bypassEffects = sfxSource.bypassEffects;
        target.bypassListenerEffects = sfxSource.bypassListenerEffects;
        target.bypassReverbZones = sfxSource.bypassReverbZones;
        target.ignoreListenerPause = sfxSource.ignoreListenerPause;
        target.ignoreListenerVolume = sfxSource.ignoreListenerVolume;
    }

    private Vector3 GetCenteredWorldPosition()
    {
        Transform listenerTransform = GetListenerTransform();

        if (listenerTransform == null)
            return transform.position;

        return listenerTransform.position +
               listenerTransform.forward * centeredVirtualDepth;
    }

    private Vector3 GetUIWorldPosition(RectTransform sourceRect)
    {
        Transform listenerTransform = GetListenerTransform();

        if (listenerTransform == null || sourceRect == null)
            return GetCenteredWorldPosition();

        Canvas canvas = sourceRect.GetComponentInParent<Canvas>();
        Camera eventCamera = null;

        if (canvas != null &&
            canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;

            if (eventCamera == null)
                eventCamera = Camera.main;
        }

        Vector3 rectWorldCenter =
            sourceRect.TransformPoint(sourceRect.rect.center);

        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(
                eventCamera,
                rectWorldCenter
            );

        float safeWidth = Mathf.Max(1f, Screen.width);
        float safeHeight = Mathf.Max(1f, Screen.height);

        float normalizedX = Mathf.Clamp(
            (screenPoint.x / safeWidth - 0.5f) * 2f,
            -1f,
            1f
        );

        float normalizedY = Mathf.Clamp(
            (screenPoint.y / safeHeight - 0.5f) * 2f,
            -1f,
            1f
        );

        return listenerTransform.position +
               listenerTransform.forward * uiVirtualDepth +
               listenerTransform.right * normalizedX * uiHorizontalExtent +
               listenerTransform.up * normalizedY * uiVerticalExtent;
    }

    private Transform GetListenerTransform()
    {
        if (cachedListener == null || !cachedListener.isActiveAndEnabled)
            cachedListener = FindAnyObjectByType<AudioListener>();

        if (cachedListener != null)
            return cachedListener.transform;

        Camera mainCamera = Camera.main;

        return mainCamera != null
            ? mainCamera.transform
            : null;
    }
}
