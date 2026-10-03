using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

// Same Unity component. Inspector data and lifecycle entry points remain in MenuMusicApply.cs.
public partial class MenuMusicApply
{
    private bool IsPlaybackInterrupted()
    {
        return isApplicationPaused ||
               !hasApplicationFocus ||
               AudioListener.pause;
    }

    private void CaptureInterruptionSnapshot()
    {
        if (interruptionSnapshotCaptured ||
            isStoppingMusic)
        {
            return;
        }

        interruptionSnapshotCaptured = true;

        CaptureSourceState(
            sourceA,
            sourceA == activeSource,
            sourceA == standbySource && standbyGain > 0f,
            out sourceASuspendedClip,
            out sourceASuspendedTimeSamples,
            out sourceAShouldResume
        );

        CaptureSourceState(
            sourceB,
            sourceB == activeSource,
            sourceB == standbySource && standbyGain > 0f,
            out sourceBSuspendedClip,
            out sourceBSuspendedTimeSamples,
            out sourceBShouldResume
        );
    }

    private static void CaptureSourceState(
        AudioSource source,
        bool isActive,
        bool isAudibleStandby,
        out AudioClip capturedClip,
        out int capturedTimeSamples,
        out bool shouldResume
    )
    {
        capturedClip = source != null
            ? source.clip
            : null;

        capturedTimeSamples =
            source != null && source.clip != null
                ? Mathf.Max(0, source.timeSamples)
                : 0;

        shouldResume =
            source != null &&
            capturedClip != null &&
            (source.isPlaying ||
             isActive ||
             isAudibleStandby);

        if (source != null && source.isPlaying)
            source.Pause();
    }

    private void RestoreInterruptionSnapshotIfReady()
    {
        if (!interruptionSnapshotCaptured ||
            IsPlaybackInterrupted() ||
            isStoppingMusic)
        {
            return;
        }

        RestoreSourceState(
            sourceA,
            sourceASuspendedClip,
            sourceASuspendedTimeSamples,
            sourceAShouldResume
        );

        RestoreSourceState(
            sourceB,
            sourceBSuspendedClip,
            sourceBSuspendedTimeSamples,
            sourceBShouldResume
        );

        ClearInterruptionSnapshot();
    }

    private static void RestoreSourceState(
        AudioSource source,
        AudioClip capturedClip,
        int capturedTimeSamples,
        bool shouldResume
    )
    {
        if (!shouldResume ||
            source == null ||
            capturedClip == null ||
            source.clip != capturedClip)
        {
            return;
        }

        int sampleCount =
            Mathf.Max(0, capturedClip.samples);

        if (sampleCount <= 1)
            return;

        int safeSample = Mathf.Clamp(
            capturedTimeSamples,
            0,
            sampleCount - 1
        );

        // A track that genuinely ended should still be allowed to advance.
        if (safeSample >= sampleCount - 2)
            return;

        source.timeSamples = safeSample;
        source.UnPause();

        if (!source.isPlaying)
        {
            source.Play();
            source.timeSamples = safeSample;
        }
    }

    private void ClearInterruptionSnapshot()
    {
        interruptionSnapshotCaptured = false;

        sourceASuspendedClip = null;
        sourceBSuspendedClip = null;
        sourceASuspendedTimeSamples = 0;
        sourceBSuspendedTimeSamples = 0;
        sourceAShouldResume = false;
        sourceBShouldResume = false;
    }

    private void OnApplicationPause(bool paused)
    {
        isApplicationPaused = paused;

        if (paused)
        {
            CaptureInterruptionSnapshot();
            return;
        }

        RestoreInterruptionSnapshotIfReady();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        hasApplicationFocus = hasFocus;

        if (!hasFocus)
        {
            CaptureInterruptionSnapshot();
            return;
        }

        RestoreInterruptionSnapshotIfReady();
    }

    private void StopActiveRoutines()
    {
        if (playlistRoutine != null)
        {
            StopCoroutine(playlistRoutine);
            playlistRoutine = null;
        }

        if (volumeRoutine != null)
        {
            StopCoroutine(volumeRoutine);
            volumeRoutine = null;
        }
    }
}
