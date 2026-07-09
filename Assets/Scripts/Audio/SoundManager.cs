using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("SFX Source")]
    public AudioSource sfxSource;

    [Header("Core Sounds")]
    public AudioClip coinSound;
    public AudioClip loseSound;
    public AudioClip winSound;

    [Header("Power Up Sounds")]
    public AudioClip armorCollectSound;
    public AudioClip armorBreakSound;
    public AudioClip slowCollectSound;

    [Header("Player Sounds")]
    public AudioClip dashSound;
    public AudioClip voidCloneSound;

    [Header("UI Sounds")]
    public AudioClip menuButtonSound;
    public AudioClip backButtonSound;
    public AudioClip tutorialOpenSound;
    public AudioClip premiumInterfaceSound;
    public AudioClip missionSelectSound;
    public AudioClip optionButtonSound;

    [Header("Beacon Enemy Sounds")]
    public AudioClip beaconActivationWaveSound;
    public AudioClip beaconLoopWaveSound;
    public AudioClip beaconDeathSound;

    [Range(0f, 1f)] public float beaconActivationVolume = 1f;
    [Range(0f, 1f)] public float beaconLoopVolume = 0.25f;
    [Range(0f, 2f)] public float beaconDeathVolume = 1.4f;

    public static float SFXVolume
    {
        get
        {
            bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

            if (!soundOn)
                return 0f;

            return PlayerPrefs.GetFloat("SFXVolume", 1f);
        }
    }

    private void Awake()
    {
        Instance = this;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        ApplySFXVolume();
    }

    public void ApplySFXVolume()
    {
        if (sfxSource != null)
            sfxSource.volume = SFXVolume;
    }

    public void PlayCoinSound() => PlaySound(coinSound);
    public void PlayLoseSound() => PlaySound(loseSound);
    public void PlayWinSound() => PlaySound(winSound);
    public void PlayArmorCollectSound() => PlaySound(armorCollectSound);
    public void PlayArmorBreakSound() => PlaySound(armorBreakSound);
    public void PlaySlowCollectSound() => PlaySound(slowCollectSound);
    public void PlayDashSound() => PlaySound(dashSound);
    public void PlayVoidCloneSound() => PlaySound(voidCloneSound);

    public void PlayTutorialOpenSound() => PlaySound(tutorialOpenSound);
    public void PlayPremiumInterfaceSound() => PlaySound(premiumInterfaceSound);

    public void PlayMissionSelectSound() => PlaySound(missionSelectSound);

    public void PlayBeaconActivationWaveSound()
    {
        PlaySound(beaconActivationWaveSound, beaconActivationVolume);
    }

    public void PlayBeaconLoopWaveSound()
    {
        PlaySound(beaconLoopWaveSound, beaconLoopVolume);
    }

    public void PlayBeaconDeathSound()
    {
        PlaySound(beaconDeathSound);
    }

    public void PlayMenuButtonSound() => PlaySound(menuButtonSound);

    public void PlayBackButtonSound() => PlaySound(backButtonSound);

    public void PlayOptionButtonSound() => PlaySound(optionButtonSound);

    private void PlaySound(AudioClip clip)
    {
        PlaySound(clip, 1f);
    }

    private void PlaySound(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, SFXVolume * volumeMultiplier);
    }
}