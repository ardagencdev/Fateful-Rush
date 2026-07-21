using UnityEngine;

public class LaserWall : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifeTime = 1.5f;

    [Header("Sound")]
    public AudioClip laserLoopSound;

    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;
    private bool soundWasPaused;

    private void Start()
    {
        SetupAudio();

        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject, lifeTime);
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

        // Mevcut gameplay davranışını koruyoruz.
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;

        audioSource.Play();
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
    }
}