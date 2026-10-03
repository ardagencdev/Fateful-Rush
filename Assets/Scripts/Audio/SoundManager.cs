using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public partial class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("SFX Source")]
    [Tooltip("Reference/template source. Positional one-shots are played through the internal 3D pool.")]
    public AudioSource sfxSource;

    [Header("3D SFX")]
    [SerializeField, Min(1)]
    private int spatialPoolSize = 20;

    [SerializeField, Min(1)]
    private int spatialPoolMaxSize = 32;

    [SerializeField, Min(0.01f)]
    private float spatialMinDistance = 25f;

    [SerializeField, Min(0.02f)]
    private float spatialMaxDistance = 60f;

    [SerializeField]
    private AudioRolloffMode spatialRolloffMode = AudioRolloffMode.Linear;

    [SerializeField, Range(0f, 360f)]
    private float spatialSpread = 0f;

    [Tooltip("Virtual distance in front of the AudioListener for centered/global SFX.")]
    [SerializeField, Min(0.1f)]
    private float centeredVirtualDepth = 1.5f;

    [Header("UI 3D Placement")]
    [Tooltip("How far left/right a screen-space UI sound can be placed around the listener.")]
    [SerializeField, Min(0.1f)]
    private float uiHorizontalExtent = 6f;

    [Tooltip("How far up/down a screen-space UI sound can be placed around the listener.")]
    [SerializeField, Min(0.1f)]
    private float uiVerticalExtent = 3.5f;

    [Tooltip("Virtual distance in front of the AudioListener used for UI SFX.")]
    [SerializeField, Min(0.1f)]
    private float uiVirtualDepth = 1.5f;

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

    [Header("SFX Variation")]
    [Tooltip("Adds very small random pitch/volume changes to frequently repeated gameplay SFX. Critical warnings and win/lose cues stay fixed.")]
    [SerializeField]
    private bool enableSfxVariation = true;

    [Tooltip("Optional extra clips for the regular coin sound. The original coinSound remains part of the pool.")]
    [SerializeField]
    private AudioClip[] coinSoundVariants;

    [Tooltip("Optional extra clips for dash. The original dashSound remains part of the pool.")]
    [SerializeField]
    private AudioClip[] dashSoundVariants;

    [Tooltip("Optional extra clips for armor pickup. The original clip remains part of the pool.")]
    [SerializeField]
    private AudioClip[] armorCollectSoundVariants;

    [Tooltip("Optional extra clips for slow pickup. The original clip remains part of the pool.")]
    [SerializeField]
    private AudioClip[] slowCollectSoundVariants;

    [Tooltip("Optional extra clips for clone activation. The original clip remains part of the pool.")]
    [SerializeField]
    private AudioClip[] voidCloneSoundVariants;

    [SerializeField, Range(0f, 0.08f)]
    private float coinPitchJitter = 0.025f;

    [SerializeField, Range(0f, 0.08f)]
    private float dashPitchJitter = 0.018f;

    [SerializeField, Range(0f, 0.08f)]
    private float pickupPitchJitter = 0.012f;

    [SerializeField, Range(0f, 0.08f)]
    private float clonePitchJitter = 0.015f;

    [SerializeField, Range(0f, 0.08f)]
    private float frequentSfxVolumeJitter = 0.012f;

    [Header("Prestige Skin Coin Sounds")]
    [Tooltip("Used only while the DARK skin is equipped. Falls back to the normal coin sound if empty.")]
    public AudioClip darkCoinSound;

    [Tooltip("Used only while the GOLDEN skin is equipped. Falls back to the normal coin sound if empty.")]
    public AudioClip goldenCoinSound;

    [Header("UI Sounds")]
    public AudioClip menuButtonSound;
    public AudioClip backButtonSound;
    public AudioClip startButtonSound;
    public AudioClip lockedLevelSound;

    [FormerlySerializedAs("tutorialOpenSound")]
    public AudioClip missionBriefingOpenSound;

    public AudioClip premiumInterfaceSound;
    public AudioClip missionSelectSound;
    public AudioClip optionButtonSound;

    [FormerlySerializedAs("nextButtonSound")]
    [Tooltip("Uses the AudioClip that was previously assigned as Next Page SFX. Now used by the Skin Equip button.")]
    public AudioClip skinEquipSound;

    [Tooltip("Shared page-navigation SFX used by both Next and Previous page buttons.")]
    public AudioClip previousButtonSound;
    public AudioClip exitButtonSound;
    public AudioClip restartButtonSound;

    [Header("Gameplay Event Sounds")]
    public AudioClip spaceBombSpawnSound;
    public AudioClip bossAoeWarningSound;
    public AudioClip bossSplitSound;
    public AudioClip laserWarningSound;
    public AudioClip comboStageSound;
    public AudioClip newSkinUnlockedSound;

    [Header("Near Miss")]
    [Tooltip("Optional override. If empty, the bundled Resources/Audio/NearMissWhoosh clip is loaded automatically.")]
    public AudioClip nearMissSound;

    [Range(0f, 1f)]
    public float nearMissVolume = 0.52f;

    [SerializeField, Range(0f, 0.05f)]
    private float nearMissPitchJitter = 0.012f;

    [Header("Gameplay Event Volumes")]
    [Range(0f, 1f)] public float spaceBombSpawnVolume = 0.9f;
    [Range(0f, 1f)] public float bossAoeWarningVolume = 1f;
    [Range(0f, 1f)] public float bossSplitVolume = 1f;
    [Range(0f, 1f)] public float laserWarningVolume = 0.55f;
    [Range(0f, 1f)] public float comboStageVolume = 0.9f;
    [Range(0f, 1f)] public float newSkinUnlockedVolume = 1f;

    [Header("Beacon Enemy Sounds")]
    public AudioClip beaconActivationWaveSound;
    public AudioClip beaconLoopWaveSound;
    public AudioClip beaconDeathSound;

    [Range(0f, 1f)] public float beaconActivationVolume = 1f;
    [Range(0f, 1f)] public float beaconLoopVolume = 0.25f;
    [Range(0f, 2f)] public float beaconDeathVolume = 1.4f;

    private readonly List<AudioSource> spatialSources =
        new List<AudioSource>();

    private int spatialSourceCursor;
    private AudioListener cachedListener;

    private int lastCoinVariantIndex = -1;
    private int lastDashVariantIndex = -1;
    private int lastArmorCollectVariantIndex = -1;
    private int lastSlowCollectVariantIndex = -1;
    private int lastCloneVariantIndex = -1;

    public float ArmorBreakSoundDuration =>
        armorBreakSound != null ? armorBreakSound.length : 0f;

    public float WinSoundDuration =>
        winSound != null ? winSound.length : 0f;

    public static float SFXVolume
    {
        get
        {
            bool soundOn = PlayerPrefs.GetInt("SoundOn", 1) == 1;

            if (!soundOn)
                return 0f;

            // Mixer kuruluysa kullanici SFX slider gain'i AudioMixer
            // uzerinden uygulanir. Source seviyesinde tekrar carpip volume'u
            // iki kez dusurmemek icin burada unity gain doneriz.
            if (GameAudioMixerController.IsReady)
                return 1f;

            return Mathf.Clamp01(
                PlayerPrefs.GetFloat("SFXVolume", 1f)
            );
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Birden fazla SoundManager bulundu. Fazladan olan siliniyor.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null)
            ConfigureWorldAudioSource(sfxSource);

        if (nearMissSound == null)
        {
            nearMissSound =
                Resources.Load<AudioClip>(
                    "Audio/NearMissWhoosh"
                );
        }

        PrepareSpatialPool();
    }

    private void Start()
    {
        ApplySFXVolume();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void StopAllSfx()
    {
        if (sfxSource != null)
            sfxSource.Stop();

        for (int i = 0; i < spatialSources.Count; i++)
        {
            AudioSource source = spatialSources[i];

            if (source != null)
                source.Stop();
        }
    }

    public void ApplySFXVolume()
    {
        float volume = SFXVolume;

        if (sfxSource != null)
            sfxSource.volume = volume;

        for (int i = 0; i < spatialSources.Count; i++)
        {
            if (spatialSources[i] != null)
                spatialSources[i].volume = volume;
        }
    }

    // ---------------------------------------------------------------------
    // Core / player / pickup SFX
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // UI SFX. No-argument versions remain for compatibility and are still
    // true 3D sounds, positioned directly in front of the listener.
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Gameplay events
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Generic API used by gameplay scripts and custom UI sounds.
    // ---------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignClip(
            ref restartButtonSound,
            "Assets/SFX/UI/RestartButtonSFX.wav"
        );

        AutoAssignClip(
            ref spaceBombSpawnSound,
            "Assets/SFX/Traps/SpaceBombSpawnSFX.wav"
        );

        AutoAssignClip(
            ref bossAoeWarningSound,
            "Assets/SFX/Enemy/BOSSAOEWarningSFX.wav"
        );

        AutoAssignClip(
            ref bossSplitSound,
            "Assets/SFX/Enemy/BOSSSplitSFX.wav"
        );

        AutoAssignClip(
            ref laserWarningSound,
            "Assets/SFX/Environment/LaserWarningSFX.wav"
        );

        AutoAssignClip(
            ref comboStageSound,
            "Assets/SFX/UI/ComboStageSFX.wav"
        );

        AutoAssignClip(
            ref newSkinUnlockedSound,
            "Assets/SFX/GameState/NewSkinUnlockedSFX.wav"
        );

        spaceBombSpawnVolume = Mathf.Clamp01(spaceBombSpawnVolume);
        bossAoeWarningVolume = Mathf.Clamp01(bossAoeWarningVolume);
        bossSplitVolume = Mathf.Clamp01(bossSplitVolume);
        laserWarningVolume = Mathf.Clamp01(laserWarningVolume);
        comboStageVolume = Mathf.Clamp01(comboStageVolume);
        newSkinUnlockedVolume = Mathf.Clamp01(newSkinUnlockedVolume);

        coinPitchJitter = Mathf.Clamp(coinPitchJitter, 0f, 0.08f);
        dashPitchJitter = Mathf.Clamp(dashPitchJitter, 0f, 0.08f);
        pickupPitchJitter = Mathf.Clamp(pickupPitchJitter, 0f, 0.08f);
        clonePitchJitter = Mathf.Clamp(clonePitchJitter, 0f, 0.08f);
        frequentSfxVolumeJitter = Mathf.Clamp(frequentSfxVolumeJitter, 0f, 0.08f);

        spatialPoolSize = Mathf.Max(1, spatialPoolSize);
        spatialPoolMaxSize = Mathf.Max(spatialPoolSize, spatialPoolMaxSize);
        spatialMinDistance = Mathf.Max(0.01f, spatialMinDistance);
        spatialMaxDistance = Mathf.Max(spatialMinDistance + 0.01f, spatialMaxDistance);
        centeredVirtualDepth = Mathf.Max(0.1f, centeredVirtualDepth);
        uiHorizontalExtent = Mathf.Max(0.1f, uiHorizontalExtent);
        uiVerticalExtent = Mathf.Max(0.1f, uiVerticalExtent);
        uiVirtualDepth = Mathf.Max(0.1f, uiVirtualDepth);
    }

    private static void AutoAssignClip(
        ref AudioClip target,
        string assetPath)
    {
        if (target != null)
            return;

        target =
            UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                assetPath
            );
    }
#endif
}
