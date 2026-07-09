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

        if (bombCollider != null)
            bombCollider.enabled = false;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(spawnSafeTime);

        if (bombCollider != null)
            bombCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        Explode();

        PlayerMovement player = other.GetComponent<PlayerMovement>();

        if (player != null)
        {
            player.GameOver("SPACE BOMB");
            return;
        }

        GameStateManager gameStateManager = FindAnyObjectByType<GameStateManager>();

        if (gameStateManager != null)
            gameStateManager.GameOver(0);
    }

    private void Explode()
    {
        if (explosionEffectPrefab != null)
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

        if (explosionSound != null)
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, SoundManager.SFXVolume);

        Destroy(gameObject);
    }
}