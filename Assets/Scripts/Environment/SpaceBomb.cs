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
            AudioSource.PlayClipAtPoint(
                explosionSound,
                transform.position,
                SoundManager.SFXVolume
            );
        }

        Destroy(gameObject);
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