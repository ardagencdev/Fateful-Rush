using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

// Same Unity component. Inspector data and lifecycle entry points remain in SoundManager.cs.
public partial class SoundManager
{
    public void PlayCoinSound()
    {
        PlayVariedCenteredSound(
            coinSound,
            coinSoundVariants,
            ref lastCoinVariantIndex,
            coinPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayCoinSound(Vector3 worldPosition)
    {
        PlayVariedWorldSound(
            coinSound,
            coinSoundVariants,
            ref lastCoinVariantIndex,
            worldPosition,
            coinPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayCoinSound(string skinId)
    {
        AudioClip clip = GetCoinClipForSkin(skinId);
        bool regularCoin = clip == coinSound;

        PlayVariedCenteredSound(
            clip,
            regularCoin ? coinSoundVariants : null,
            ref lastCoinVariantIndex,
            coinPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayCoinSound(string skinId, Vector3 worldPosition)
    {
        AudioClip clip = GetCoinClipForSkin(skinId);
        bool regularCoin = clip == coinSound;

        PlayVariedWorldSound(
            clip,
            regularCoin ? coinSoundVariants : null,
            ref lastCoinVariantIndex,
            worldPosition,
            coinPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayLoseSound() => PlayCenteredCriticalSound(loseSound);

    public void PlayWinSound() => PlayCenteredCriticalSound(winSound);

    public void PlayArmorCollectSound()
    {
        PlayVariedCenteredSound(
            armorCollectSound,
            armorCollectSoundVariants,
            ref lastArmorCollectVariantIndex,
            pickupPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayArmorCollectSound(Vector3 worldPosition)
    {
        PlayVariedWorldSound(
            armorCollectSound,
            armorCollectSoundVariants,
            ref lastArmorCollectVariantIndex,
            worldPosition,
            pickupPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayArmorBreakSound() => PlayCenteredCriticalSound(armorBreakSound);

    public void PlayArmorBreakSound(Vector3 worldPosition) =>
        PlayWorldCriticalSound(armorBreakSound, worldPosition);

    public void PlaySlowCollectSound()
    {
        PlayVariedCenteredSound(
            slowCollectSound,
            slowCollectSoundVariants,
            ref lastSlowCollectVariantIndex,
            pickupPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlaySlowCollectSound(Vector3 worldPosition)
    {
        PlayVariedWorldSound(
            slowCollectSound,
            slowCollectSoundVariants,
            ref lastSlowCollectVariantIndex,
            worldPosition,
            pickupPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayDashSound()
    {
        PlayVariedCenteredSound(
            dashSound,
            dashSoundVariants,
            ref lastDashVariantIndex,
            dashPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayDashSound(Vector3 worldPosition)
    {
        PlayVariedWorldSound(
            dashSound,
            dashSoundVariants,
            ref lastDashVariantIndex,
            worldPosition,
            dashPitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayVoidCloneSound()
    {
        PlayVariedCenteredSound(
            voidCloneSound,
            voidCloneSoundVariants,
            ref lastCloneVariantIndex,
            clonePitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayVoidCloneSound(Vector3 worldPosition)
    {
        PlayVariedWorldSound(
            voidCloneSound,
            voidCloneSoundVariants,
            ref lastCloneVariantIndex,
            worldPosition,
            clonePitchJitter,
            frequentSfxVolumeJitter
        );
    }

    public void PlayMissionBriefingOpenSound() =>
        PlayCenteredUISound(missionBriefingOpenSound);

    public void PlayMissionBriefingOpenSound(RectTransform sourceRect) =>
        PlayUISound(missionBriefingOpenSound, sourceRect);

    public void PlayPremiumInterfaceSound() =>
        PlayCenteredUISound(premiumInterfaceSound);

    public void PlayPremiumInterfaceSound(RectTransform sourceRect) =>
        PlayUISound(premiumInterfaceSound, sourceRect);

    public void PlayMissionSelectSound() =>
        PlayCenteredUISound(missionSelectSound);

    public void PlayMissionSelectSound(RectTransform sourceRect) =>
        PlayUISound(missionSelectSound, sourceRect);

    public void PlayStartButtonSound() =>
        PlayCenteredUISound(startButtonSound);

    public void PlayStartButtonSound(RectTransform sourceRect) =>
        PlayUISound(startButtonSound, sourceRect);

    public void PlayLockedLevelSound() =>
        PlayCenteredUISound(lockedLevelSound);

    public void PlayLockedLevelSound(RectTransform sourceRect) =>
        PlayUISound(lockedLevelSound, sourceRect);

    public void PlayMenuButtonSound() =>
        PlayCenteredUISound(menuButtonSound);

    public void PlayMenuButtonSound(RectTransform sourceRect) =>
        PlayUISound(menuButtonSound, sourceRect);

    public void PlayBackButtonSound() =>
        PlayCenteredUISound(backButtonSound);

    public void PlayBackButtonSound(RectTransform sourceRect) =>
        PlayUISound(backButtonSound, sourceRect);

    public void PlayOptionButtonSound() =>
        PlayCenteredUISound(optionButtonSound);

    public void PlayOptionButtonSound(RectTransform sourceRect) =>
        PlayUISound(optionButtonSound, sourceRect);

    public void PlayNextButtonSound() =>
        PlayCenteredUISound(previousButtonSound);

    public void PlayNextButtonSound(RectTransform sourceRect) =>
        PlayUISound(previousButtonSound, sourceRect);

    public void PlayPreviousButtonSound() =>
        PlayCenteredUISound(previousButtonSound);

    public void PlayPreviousButtonSound(RectTransform sourceRect) =>
        PlayUISound(previousButtonSound, sourceRect);

    public void PlaySkinEquipSound() =>
        PlayCenteredUISound(skinEquipSound);

    public void PlaySkinEquipSound(RectTransform sourceRect) =>
        PlayUISound(skinEquipSound, sourceRect);

    public void PlayExitButtonSound() =>
        PlayCenteredUISound(exitButtonSound);

    public void PlayExitButtonSound(RectTransform sourceRect) =>
        PlayUISound(exitButtonSound, sourceRect);

    public void PlayRestartButtonSound() =>
        PlayCenteredUISound(restartButtonSound);

    public void PlayRestartButtonSound(RectTransform sourceRect) =>
        PlayUISound(restartButtonSound, sourceRect);

    public void PlaySpaceBombSpawnSound() =>
        PlayCenteredCriticalSound(spaceBombSpawnSound, spaceBombSpawnVolume);

    public void PlaySpaceBombSpawnSound(Vector3 worldPosition) =>
        PlayWorldCriticalSound(spaceBombSpawnSound, worldPosition, spaceBombSpawnVolume);

    public void PlayBossAoeWarningSound() =>
        PlayCenteredCriticalSound(bossAoeWarningSound, bossAoeWarningVolume);

    public void PlayBossAoeWarningSound(Vector3 worldPosition) =>
        PlayWorldCriticalSound(bossAoeWarningSound, worldPosition, bossAoeWarningVolume);

    public void PlayBossSplitSound() =>
        PlayCenteredCriticalSound(bossSplitSound, bossSplitVolume);

    public void PlayBossSplitSound(Vector3 worldPosition) =>
        PlayWorldCriticalSound(bossSplitSound, worldPosition, bossSplitVolume);

    public void PlayLaserWarningSound() =>
        PlayCenteredCriticalSound(laserWarningSound, laserWarningVolume);

    public void PlayLaserWarningSound(Vector3 worldPosition) =>
        PlayWorldCriticalSound(laserWarningSound, worldPosition, laserWarningVolume);

    public void PlayComboStageSound() =>
        PlayCenteredUISound(comboStageSound, comboStageVolume);

    public void PlayComboStageSound(RectTransform sourceRect) =>
        PlayUISound(comboStageSound, sourceRect, comboStageVolume);

    public void PlayNewSkinUnlockedSound() =>
        PlayCenteredUISound(newSkinUnlockedSound, newSkinUnlockedVolume);

    public void PlayNewSkinUnlockedSound(RectTransform sourceRect) =>
        PlayUISound(newSkinUnlockedSound, sourceRect, newSkinUnlockedVolume);

    public void PlayNearMissSound(
        Vector3 worldPosition,
        float closeness01 = 1f)
    {
        if (nearMissSound == null)
            return;

        float closeness =
            Mathf.Clamp01(closeness01);

        float volume =
            nearMissVolume *
            Mathf.Lerp(0.82f, 1f, closeness);

        float basePitch =
            Mathf.Lerp(0.985f, 1.015f, closeness);

        float pitch =
            enableSfxVariation
                ? GetVariedPitch(
                    basePitch,
                    nearMissPitchJitter
                )
                : basePitch;

        PlayWorldCriticalSound(
            nearMissSound,
            worldPosition,
            volume,
            pitch
        );
    }

    public void PlayBeaconActivationWaveSound() =>
        PlayCenteredSound(beaconActivationWaveSound, beaconActivationVolume);

    public void PlayBeaconActivationWaveSound(Vector3 worldPosition) =>
        PlayWorldSound(beaconActivationWaveSound, worldPosition, beaconActivationVolume);

    public void PlayBeaconLoopWaveSound() =>
        PlayCenteredSound(beaconLoopWaveSound, beaconLoopVolume);

    public void PlayBeaconLoopWaveSound(Vector3 worldPosition) =>
        PlayWorldSound(beaconLoopWaveSound, worldPosition, beaconLoopVolume);

    public void PlayBeaconDeathSound() =>
        PlayCenteredSound(beaconDeathSound, beaconDeathVolume);

    public void PlayBeaconDeathSound(Vector3 worldPosition) =>
        PlayWorldSound(beaconDeathSound, worldPosition, beaconDeathVolume);

    public void PlayCustomSound(AudioClip customClip)
    {
        PlayCenteredSound(customClip);
    }

    public void PlayCustomSoundAtWorld(
        AudioClip customClip,
        Vector3 worldPosition,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayWorldSound(
            customClip,
            worldPosition,
            volumeMultiplier,
            pitch
        );
    }

    public void PlayCustomSoundAtUI(
        AudioClip customClip,
        RectTransform sourceRect,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayUISound(
            customClip,
            sourceRect,
            volumeMultiplier,
            pitch
        );
    }

    public void PlayCriticalSoundAtWorld(
        AudioClip customClip,
        Vector3 worldPosition,
        float volumeMultiplier = 1f,
        float pitch = 1f)
    {
        PlayWorldCriticalSound(
            customClip,
            worldPosition,
            volumeMultiplier,
            pitch
        );
    }
}
