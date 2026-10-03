using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(AudioSource))]
public partial class MenuMusicApply : MonoBehaviour
{
    [Header("Menu Music")]
    [SerializeField]
    private AudioClip[] menuMusics;

    [Tooltip(
        "Oyun ilk açıldığında çalacak parçanın listedeki index değeri."
    )]
    [SerializeField, Min(0)]
    private int firstTrackIndex;

    [SerializeField, Range(0f, 1f)]
    private float menuMusicBaseVolume = 0.2f;

    [Header("Transitions")]
    [SerializeField, Min(0f)]
    private float fadeInDuration = 1.5f;

    [Tooltip(
        "Bir sonraki menü müziği başladıktan sonra tam ses seviyesine ulaşma süresi."
    )]
    [FormerlySerializedAs("crossfadeDuration")]
    [SerializeField, Min(0.1f)]
    private float nextTrackFadeInDuration = 3f;

    [Tooltip(
        "Bir parça tamamen bittikten sonra sıradaki parça başlamadan önce beklenecek süre."
    )]
    [SerializeField, Range(1f, 2f)]
    private float interTrackDelay = 1.5f;

    [SerializeField, Min(0f)]
    private float fadeOutDuration = 0.8f;

    public float FadeOutDuration => fadeOutDuration;

    private AudioSource sourceA;
    private AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource standbySource;

    private Coroutine playlistRoutine;
    private Coroutine volumeRoutine;

    private readonly List<int> shuffledPlaylist = new List<int>();

    private int playlistPosition;
    private int lastPlayedIndex = -1;

    private float masterVolume;
    private float activeGain = 1f;
    private float standbyGain;

    private bool isStoppingMusic;

    // Full-screen ads and app backgrounding can temporarily make
    // AudioSource.isPlaying return false. Without this guard the playlist
    // interprets the interruption as "track ended" and starts over.
    private bool isApplicationPaused;
    private bool hasApplicationFocus = true;
    private bool interruptionSnapshotCaptured;

    private AudioClip sourceASuspendedClip;
    private AudioClip sourceBSuspendedClip;
    private int sourceASuspendedTimeSamples;
    private int sourceBSuspendedTimeSamples;
    private bool sourceAShouldResume;
    private bool sourceBShouldResume;

    private void Awake()
    {
        hasApplicationFocus = Application.isFocused;
        PrepareAudioSources();
    }

    private void Start()
    {
        StartMenuPlaylist();
    }

    private void OnDisable()
    {
        StopActiveRoutines();
        ClearInterruptionSnapshot();

        if (sourceA != null)
        {
            sourceA.Stop();
            sourceA.volume = 0f;
        }

        if (sourceB != null)
        {
            sourceB.Stop();
            sourceB.volume = 0f;
        }
    }

    private void OnValidate()
    {
        firstTrackIndex =
            Mathf.Max(0, firstTrackIndex);

        menuMusicBaseVolume =
            Mathf.Clamp01(
                menuMusicBaseVolume
            );

        fadeInDuration =
            Mathf.Max(
                0f,
                fadeInDuration
            );

        nextTrackFadeInDuration =
            Mathf.Max(
                0.1f,
                nextTrackFadeInDuration
            );

        interTrackDelay =
            Mathf.Clamp(
                interTrackDelay,
                1f,
                2f
            );

        fadeOutDuration =
            Mathf.Max(
                0f,
                fadeOutDuration
            );
    }
}
