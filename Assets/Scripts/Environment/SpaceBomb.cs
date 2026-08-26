using System.Collections;
using UnityEngine;

public class SpaceBomb : MonoBehaviour
{
    [Header("Explosion")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;

    [Header("Spawn Safety")]
    public float spawnSafeTime = 0.35f;

    private bool triggered;
    private Collider2D bombCollider;

    private void Awake()
    {
        bombCollider = GetComponent<Collider2D>();

        SetColliderEnabled(false);
    }

    private IEnumerator Start()
    {
        float safeTime = Mathf.Max(0f, spawnSafeTime);

        if (safeTime > 0f)
            yield return new WaitForSeconds(safeTime);

        if (!triggered)
            SetColliderEnabled(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        triggered = true;
        SetColliderEnabled(false);

        PlayerArmor armor =
            other.GetComponentInParent<PlayerArmor>();

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        Explode();

        if (armor != null && armor.IsImmune)
            return;

        if (armor != null && armor.HasArmor)
        {
            armor.BreakArmor();
            return;
        }

        if (player != null)
        {
            player.GameOver("SPACE BOMB");
            return;
        }

        GameStateManager gameStateManager =
            FindAnyObjectByType<GameStateManager>();

        if (gameStateManager != null)
            gameStateManager.GameOver(0);
    }

    private void Explode()
    {
        CameraShake.Instance?.Shake(
            0.14f,
            0.10f
        );

        VibrationManager.Instance?.VibrateSpaceBomb();

        if (explosionEffectPrefab != null)
        {
            Instantiate(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (explosionSound != null)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayCriticalSoundAtWorld(
                    explosionSound,
                    transform.position
                );
            }
            else
            {
                PlayMixerRoutedExplosionFallback();
            }
        }

        Destroy(gameObject);
    }


    private void PlayMixerRoutedExplosionFallback()
    {
        if (explosionSound == null)
            return;

        GameObject audioObject =
            new GameObject("SpaceBomb_ExplosionAudio");

        audioObject.transform.position = transform.position;

        AudioSource source =
            audioObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.clip = explosionSound;
        source.volume = SoundManager.SFXVolume;
        source.pitch = 1f;

        // Keep the emergency path on the same mixer routing as the normal
        // SoundManager path so Slow/Boss/Pause snapshots still behave.
        SoundManager.ConfigureAsWorld3D(source);
        GameAudioMixerController.Route(
            source,
            GameAudioMixerController.AudioBus.CriticalSFX
        );

        source.Play();

        Destroy(
            audioObject,
            explosionSound.length + 0.15f
        );
    }

    private void SetColliderEnabled(bool enabledState)
    {
        if (bombCollider != null)
            bombCollider.enabled = enabledState;
    }

    private void OnValidate()
    {
        spawnSafeTime = Mathf.Max(0f, spawnSafeTime);
    }
}