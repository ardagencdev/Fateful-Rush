using System.Collections;
using UnityEngine;

public class LaserWall : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifeTime = 1.5f;

    [Header("Sound")]
    public AudioClip laserLoopSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("Laser yok olmadan hemen önce sesin smooth şekilde kapanma süresi.")]
    [Min(0f)]
    public float fadeOutDuration = 0.25f;

    private AudioSource audioSource;
    private bool soundWasPaused;
    private Coroutine lifetimeRoutine;

    private void Start()
    {
        SetupAudio();

        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        lifetimeRoutine = StartCoroutine(LifetimeRoutine());
    }

    private void SetupAudio()
    {
        if (laserLoopSound == null)
            return;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.clip = laserLoopSound;
        audioSource.volume =
            Mathf.Clamp01(volume) * SoundManager.SFXVolume;

        audioSource.loop = false;
        audioSource.spatialBlend = 1f;

        audioSource.Play();
    }

    private IEnumerator LifetimeRoutine()
    {
        float safeLifeTime = Mathf.Max(0f, lifeTime);
        float safeFadeDuration = Mathf.Clamp(
            fadeOutDuration,
            0f,
            safeLifeTime
        );

        float waitBeforeFade =
            Mathf.Max(0f, safeLifeTime - safeFadeDuration);

        if (waitBeforeFade > 0f)
            yield return new WaitForSeconds(waitBeforeFade);

        if (audioSource != null &&
            audioSource.isPlaying &&
            safeFadeDuration > 0f)
        {
            float startVolume = audioSource.volume;
            float elapsed = 0f;

            while (elapsed < safeFadeDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / safeFadeDuration
                );

                // SmoothStep ile son kısım daha doğal kapanır.
                float smoothT = t * t * (3f - 2f * t);

                audioSource.volume =
                    Mathf.Lerp(
                        startVolume,
                        0f,
                        smoothT
                    );

                yield return null;
            }

            audioSource.volume = 0f;
        }
        else if (safeFadeDuration > 0f)
        {
            // Ses yoksa bile laser'ın toplam lifetime'ını koru.
            yield return new WaitForSeconds(safeFadeDuration);
        }

        Destroy(gameObject);
        lifetimeRoutine = null;
    }

    public void FreezeLaser()
    {
        soundWasPaused = false;

        if (audioSource != null)
            audioSource.Stop();

        enabled = false;
    }

    public void PauseLaserSound()
    {
        if (audioSource == null || !audioSource.isPlaying)
            return;

        audioSource.Pause();
        soundWasPaused = true;
    }

    public void ResumeLaserSound()
    {
        if (audioSource == null || !soundWasPaused)
            return;

        audioSource.UnPause();
        soundWasPaused = false;
    }

    private void OnDisable()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        soundWasPaused = false;
    }

    private void OnValidate()
    {
        lifeTime = Mathf.Max(0f, lifeTime);
        volume = Mathf.Clamp01(volume);
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
    }
}